using OpenAI.Chat;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using System.Globalization;
using System.Text.Json;

namespace Rag.Service.Agent
{
    /// <summary>
    /// OpenAI function tools for the restaurant agent: get_menu, check_availability, create_booking, cancel_booking.
    /// Every tool returns a JSON string; validation problems come back as {"error": "..."} so the model can recover.
    /// </summary>
    public class RestaurantToolbox
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        private static readonly string[] DateFormats = { "yyyy-MM-dd" };
        private static readonly string[] TimeFormats = { "HH:mm", "H:mm", "HH:mm:ss" };

        private readonly IRestaurantService _restaurantService;

        public RestaurantToolbox(IRestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        public IReadOnlyList<ChatTool> Tools { get; } = new List<ChatTool>
        {
            ChatTool.CreateFunctionTool(
                functionName: "get_menu",
                functionDescription: "Returns the restaurant menu with prices in AMD and availability. Optionally filter by category (Salads, Soups, Grill, Main courses, Desserts, Drinks).",
                functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "category": { "type": "string", "description": "Optional category in English, e.g. Grill or Desserts." }
                  }
                }
                """)),

            ChatTool.CreateFunctionTool(
                functionName: "check_availability",
                functionDescription: "Finds free tables for a party at a given date and time. A booking lasts 2 hours.",
                functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "date":   { "type": "string", "description": "Date as YYYY-MM-DD." },
                    "time":   { "type": "string", "description": "Start time as HH:mm (24h)." },
                    "guests": { "type": "integer", "minimum": 1, "description": "Number of guests." }
                  },
                  "required": ["date", "time", "guests"]
                }
                """)),

            ChatTool.CreateFunctionTool(
                functionName: "create_booking",
                functionDescription: "Creates a table booking. Call ONLY after the guest confirmed name, phone, guests, date and time. Picks the smallest free table unless table_number is given.",
                functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "customer_name": { "type": "string" },
                    "phone":         { "type": "string", "description": "Phone number as the guest said it." },
                    "guests":        { "type": "integer", "minimum": 1 },
                    "date":          { "type": "string", "description": "Date as YYYY-MM-DD." },
                    "time":          { "type": "string", "description": "Start time as HH:mm (24h)." },
                    "notes":         { "type": "string", "description": "Optional wishes, e.g. birthday or terrace." },
                    "table_number":  { "type": "integer", "description": "Optional specific table number from check_availability." }
                  },
                  "required": ["customer_name", "phone", "guests", "date", "time"]
                }
                """)),

            ChatTool.CreateFunctionTool(
                functionName: "cancel_booking",
                functionDescription: "Cancels a booking by booking id and/or the phone number used for it. If several bookings match the phone, returns them so you can ask which one.",
                functionParameters: BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": {
                    "booking_id": { "type": "integer" },
                    "phone":      { "type": "string" }
                  }
                }
                """))
        };

        public async Task<string> ExecuteAsync(string name, string argumentsJson, CancellationToken ct)
        {
            JsonElement args;
            try
            {
                using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
                args = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return Error("Arguments were not valid JSON.");
            }

            return name switch
            {
                "get_menu" => await GetMenuAsync(args),
                "check_availability" => await CheckAvailabilityAsync(args),
                "create_booking" => await CreateBookingAsync(args),
                "cancel_booking" => await CancelBookingAsync(args),
                _ => Error($"Unknown tool '{name}'.")
            };
        }

        private async Task<string> GetMenuAsync(JsonElement args)
        {
            var category = GetString(args, "category");
            var items = await _restaurantService.GetMenuAsync(category);
            string? note = null;
            if (items.Count == 0 && !string.IsNullOrWhiteSpace(category))
            {
                items = await _restaurantService.GetMenuAsync();
                note = $"No category matched '{category}'. Full menu returned.";
            }

            return Serialize(new
            {
                note,
                items = items.Select(i => new { i.Name, i.Category, i.Description, PriceAmd = i.Price, Available = i.IsAvailable })
            });
        }

        private async Task<string> CheckAvailabilityAsync(JsonElement args)
        {
            if (!TryGetStart(args, out var start, out var error)) return Error(error);
            if (!TryGetInt(args, "guests", out var guests)) return Error("guests is required.");

            var result = await _restaurantService.CheckAvailabilityAsync(start, guests);
            if (!result.IsValidRequest) return Error(result.Message ?? "Invalid request.");

            return Serialize(new
            {
                start = Format(result.StartTime),
                end = Format(result.EndTime),
                result.Guests,
                available = result.FreeTables.Count > 0,
                message = result.Message,
                freeTables = result.FreeTables.Select(t => new { TableNumber = t.Number, t.Seats, t.Location })
            });
        }

        private async Task<string> CreateBookingAsync(JsonElement args)
        {
            if (!TryGetStart(args, out var start, out var error)) return Error(error);
            if (!TryGetInt(args, "guests", out var guests)) return Error("guests is required.");

            var dto = new CreateBookingDto
            {
                CustomerName = GetString(args, "customer_name") ?? string.Empty,
                Phone = GetString(args, "phone") ?? string.Empty,
                Guests = guests,
                StartTime = start,
                Notes = GetString(args, "notes"),
                TableNumber = TryGetInt(args, "table_number", out var tableNumber) ? tableNumber : null
            };

            var result = await _restaurantService.CreateBookingAsync(dto);
            return BookingResult(result);
        }

        private async Task<string> CancelBookingAsync(JsonElement args)
        {
            long? bookingId = TryGetInt(args, "booking_id", out var id) ? id : null;
            var result = await _restaurantService.CancelBookingAsync(bookingId, GetString(args, "phone"));
            return BookingResult(result);
        }

        #region helpers

        private static string BookingResult(BookingResultDto result) => Serialize(new
        {
            result.Success,
            result.Message,
            booking = result.Booking is null ? null : Describe(result.Booking),
            candidates = result.Candidates?.Select(Describe)
        });

        private static object Describe(BookingDto b) => new
        {
            BookingId = b.Id,
            b.CustomerName,
            b.Phone,
            b.Guests,
            start = Format(b.StartTime),
            end = Format(b.EndTime),
            b.TableNumber,
            b.TableLocation,
            b.Notes,
            b.Status
        };

        private static bool TryGetStart(JsonElement args, out DateTime start, out string error)
        {
            start = default;
            error = string.Empty;
            var date = GetString(args, "date");
            var time = GetString(args, "time");
            if (!DateTime.TryParseExact(date, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                error = "date must be YYYY-MM-DD.";
                return false;
            }
            if (!DateTime.TryParseExact(time, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            {
                error = "time must be HH:mm (24h).";
                return false;
            }
            start = d.Date + t.TimeOfDay;
            return true;
        }

        private static string? GetString(JsonElement args, string name) =>
            args.ValueKind == JsonValueKind.Object && args.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? (string.IsNullOrWhiteSpace(v.GetString()) ? null : v.GetString())
                : null;

        private static bool TryGetInt(JsonElement args, string name, out int value)
        {
            value = 0;
            if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var v)) return false;
            if (v.ValueKind == JsonValueKind.Number) return v.TryGetInt32(out value);
            return v.ValueKind == JsonValueKind.String && int.TryParse(v.GetString(), out value);
        }

        private static string Format(DateTime dt) => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        private static string Serialize(object value) => JsonSerializer.Serialize(value, Json);

        private static string Error(string message) => Serialize(new { error = message });

        #endregion
    }
}
