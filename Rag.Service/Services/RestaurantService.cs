using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Rag.Data.Entity;
using Rag.Data.Repositories.Interfaces;
using Rag.Service.Dtos;
using Rag.Service.Services.Interfaces;
using Serilog;

namespace Rag.Service.Services
{
    /// <summary>
    /// Menu, tables and bookings. Booking times are stored exactly as received (no time zone logic).
    /// A booking lasts <see cref="DefaultBookingMinutes"/>; a table is free when no confirmed booking overlaps.
    /// </summary>
    public class RestaurantService : IRestaurantService
    {
        public const int DefaultBookingMinutes = 120;

        private readonly IMenuItemRepository _menuItemRepository;
        private readonly IRestaurantTableRepository _tableRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IMapper _mapper;

        public RestaurantService(
            IMenuItemRepository menuItemRepository,
            IRestaurantTableRepository tableRepository,
            IBookingRepository bookingRepository,
            IMapper mapper)
        {
            _menuItemRepository = menuItemRepository;
            _tableRepository = tableRepository;
            _bookingRepository = bookingRepository;
            _mapper = mapper;
        }

        public async Task<List<MenuItemDto>> GetMenuAsync(string? category = null, bool onlyAvailable = false)
        {
            var query = _menuItemRepository.SelectAll(x => !onlyAvailable || x.IsAvailable);
            if (!string.IsNullOrWhiteSpace(category))
            {
                var pattern = $"%{category.Trim()}%";
                query = query.Where(x => EF.Functions.ILike(x.Category, pattern));
            }
            var items = await query.OrderBy(x => x.Category).ThenBy(x => x.Name).AsNoTracking().ToListAsync();
            return _mapper.Map<List<MenuItemDto>>(items);
        }

        public async Task<List<RestaurantTableDto>> GetTablesAsync()
        {
            var tables = await _tableRepository.SelectAll(x => true).OrderBy(x => x.Number).AsNoTracking().ToListAsync();
            return _mapper.Map<List<RestaurantTableDto>>(tables);
        }

        public async Task<List<BookingDto>> GetBookingsAsync(DateTime? from = null, DateTime? to = null, bool includeCancelled = false)
        {
            var query = _bookingRepository.SelectAll(x => includeCancelled || x.Status == BookingStatus.Confirmed);
            if (from.HasValue) query = query.Where(x => x.EndTime > from.Value);
            if (to.HasValue) query = query.Where(x => x.StartTime < to.Value);
            var bookings = await query.Include(x => x.Table).OrderBy(x => x.StartTime).AsNoTracking().ToListAsync();
            return _mapper.Map<List<BookingDto>>(bookings);
        }

        public async Task<AvailabilityResultDto> CheckAvailabilityAsync(DateTime start, int guests)
        {
            start = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
            var end = start.AddMinutes(DefaultBookingMinutes);
            var result = new AvailabilityResultDto { StartTime = start, EndTime = end, Guests = guests };

            var error = await ValidateGuestsAsync(guests);
            if (error != null)
            {
                result.IsValidRequest = false;
                result.Message = error;
                return result;
            }

            result.IsValidRequest = true;
            result.FreeTables = _mapper.Map<List<RestaurantTableDto>>(await FindFreeTablesAsync(start, end, guests));
            result.Message = result.FreeTables.Count == 0 ? "No free tables for this time and party size." : null;
            Log.Information("Availability {Start} for {Guests} guests: {Count} free tables", start, guests, result.FreeTables.Count);
            return result;
        }

        public async Task<BookingResultDto> CreateBookingAsync(CreateBookingDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CustomerName))
                return Fail("Customer name is required.");
            if (NormalizePhone(dto.Phone).Length < 6)
                return Fail("A valid phone number is required.");

            var error = await ValidateGuestsAsync(dto.Guests);
            if (error != null) return Fail(error);

            var start = DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Unspecified);
            var end = start.AddMinutes(DefaultBookingMinutes);

            var freeTables = await FindFreeTablesAsync(start, end, dto.Guests);
            RestaurantTable? table;
            if (dto.TableNumber.HasValue)
            {
                table = freeTables.FirstOrDefault(t => t.Number == dto.TableNumber.Value);
                if (table == null)
                    return Fail($"Table {dto.TableNumber} is not free for {dto.Guests} guests at that time.");
            }
            else
            {
                table = freeTables.FirstOrDefault(); // smallest suitable table first
                if (table == null)
                    return Fail("No free tables for this time and party size.");
            }

            var booking = new Booking
            {
                TableId = table.Id,
                CustomerName = dto.CustomerName.Trim(),
                Phone = dto.Phone.Trim(),
                Guests = dto.Guests,
                StartTime = start,
                EndTime = end,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Status = BookingStatus.Confirmed,
                CreatedAt = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified)
            };
            await _bookingRepository.InsertAsync(booking);
            await _bookingRepository.SaveChangesAsync();
            booking.Table = table;

            Log.Information("Booking {Id} created: table {Table}, {Start}, {Guests} guests", booking.Id, table.Number, start, dto.Guests);
            return new BookingResultDto { Success = true, Message = "Booking confirmed.", Booking = _mapper.Map<BookingDto>(booking) };
        }

        public async Task<BookingResultDto> CancelBookingAsync(long? bookingId, string? phone)
        {
            var normalizedPhone = NormalizePhone(phone);
            if (bookingId is null && normalizedPhone.Length == 0)
                return Fail("Provide a booking id or the phone number used for the booking.");

            Booking? booking;
            if (bookingId is not null)
            {
                booking = await _bookingRepository.SelectAll(x => x.Id == bookingId.Value).Include(x => x.Table).FirstOrDefaultAsync();
                if (booking == null)
                    return Fail($"Booking {bookingId} was not found.");
                if (normalizedPhone.Length > 0 && !PhonesMatch(booking.Phone, normalizedPhone))
                    return Fail("The phone number does not match this booking.");
            }
            else
            {
                // Phone formats differ ("+374 91..." vs "091..."), so compare digits in memory.
                var confirmed = await _bookingRepository
                    .SelectAll(x => x.Status == BookingStatus.Confirmed)
                    .Include(x => x.Table)
                    .OrderBy(x => x.StartTime)
                    .ToListAsync();
                var matches = confirmed.Where(x => PhonesMatch(x.Phone, normalizedPhone)).ToList();

                if (matches.Count == 0)
                    return Fail("No active bookings found for this phone number.");
                if (matches.Count > 1)
                    return new BookingResultDto
                    {
                        Success = false,
                        Message = "Several bookings match this phone number. Ask which one to cancel and pass its booking id.",
                        Candidates = _mapper.Map<List<BookingDto>>(matches)
                    };
                booking = matches[0];
            }

            if (booking.Status == BookingStatus.Cancelled)
                return Fail($"Booking {booking.Id} is already cancelled.");

            booking.Status = BookingStatus.Cancelled;
            _bookingRepository.Update(booking);
            await _bookingRepository.SaveChangesAsync();

            Log.Information("Booking {Id} cancelled", booking.Id);
            return new BookingResultDto { Success = true, Message = "Booking cancelled.", Booking = _mapper.Map<BookingDto>(booking) };
        }

        #region helpers

        private async Task<string?> ValidateGuestsAsync(int guests)
        {
            if (guests < 1)
                return "Number of guests must be at least 1.";
            var maxSeats = await _tableRepository.SelectAll(x => true).Select(x => (int?)x.Seats).MaxAsync() ?? 0;
            if (guests > maxSeats)
                return $"The largest table seats {maxSeats} guests.";
            return null;
        }

        /// <summary>Tables with enough seats and no overlapping confirmed booking, smallest first.</summary>
        private async Task<List<RestaurantTable>> FindFreeTablesAsync(DateTime start, DateTime end, int guests)
        {
            var busyTableIds = _bookingRepository
                .SelectAll(b => b.Status == BookingStatus.Confirmed && b.StartTime < end && start < b.EndTime)
                .Select(b => b.TableId);

            return await _tableRepository
                .SelectAll(t => t.Seats >= guests && !busyTableIds.Contains(t.Id))
                .OrderBy(t => t.Seats).ThenBy(t => t.Number)
                .ToListAsync();
        }

        private static BookingResultDto Fail(string message) => new() { Success = false, Message = message };

        private static string NormalizePhone(string? phone) =>
            new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());

        /// <summary>Matches "+374 91 123456" with "091123456" by comparing the last 8 digits.</summary>
        private static bool PhonesMatch(string stored, string normalizedInput)
        {
            var a = NormalizePhone(stored);
            if (a.Length == 0 || normalizedInput.Length == 0) return false;
            var n = Math.Min(8, Math.Min(a.Length, normalizedInput.Length));
            return a[^n..] == normalizedInput[^n..];
        }

        #endregion
    }
}
