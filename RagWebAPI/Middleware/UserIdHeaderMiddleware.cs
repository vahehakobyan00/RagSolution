namespace RagWebAPI.Middleware
{
    using RagWebAPI.Attributes;
    using System.Security.Claims;

    /////// How to Read it in actions
    //////var uid = HttpContext.GetUserId(); // from Items, or:
    //////var nameId = User.FindFirstValue(ClaimTypes.NameIdentifier); // from claims

    public sealed class UserIdHeaderOptions
    {
        /// Header to read the user id from.
        public string HeaderName { get; set; } = "X-User-Id";

        /// If true, reject the request (400) when the header is missing/invalid.
        public bool Required { get; set; } = true;

        /// If true, only accept GUID values (useful when you store GUID ids).
        public bool AcceptGuidOnly { get; set; } = false;

        /// Optional: also stamp a ClaimTypes.NameIdentifier claim on HttpContext.User.
        public bool StampClaimsPrincipal { get; set; } = true;
    }

    public sealed class UserIdHeaderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly UserIdHeaderOptions _options;

        public const string HttpItemsKey = "uid";

        public UserIdHeaderMiddleware(RequestDelegate next, UserIdHeaderOptions options)
        {
            _next = next;
            _options = options ?? new UserIdHeaderOptions();
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<SkipUserIdHeaderAttribute>() != null)
            {
                await _next(context);
                return;
            }
            string? userId = null;

            if (context.Request.Headers.TryGetValue(_options.HeaderName, out var values))
            {
                userId = values.FirstOrDefault()?.Trim();
                if (string.IsNullOrWhiteSpace(userId))
                    userId = null;
            }

            // Optional GUID-only validation
            if (_options.AcceptGuidOnly && userId is not null && !Guid.TryParse(userId, out _))
            {
                if (_options.Required)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync($"Invalid {_options.HeaderName}: must be a GUID");
                    return;
                }
                userId = null;
            }

            // Enforce required
            if (_options.Required && userId is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync($"Missing required header: {_options.HeaderName}");
                return;
            }

            if (userId is not null)
            {
                // Make available to downstream code
                context.Items[HttpItemsKey] = userId;

                if (_options.StampClaimsPrincipal)
                {
                    // If there’s already a user, append; else create a simple identity
                    var identity = new ClaimsIdentity("UserIdHeader");
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
                    identity.AddClaim(new Claim("uid", userId));

                    // Merge with existing principal
                    context.User = new ClaimsPrincipal(
                        context.User.Identities.Append(identity));
                }
            }

            await _next(context);
        }
    }

    // Convenience extensions
    public static class UserIdHeaderExtensions
    {
        public static IApplicationBuilder UseUserIdHeader(
            this IApplicationBuilder app,
            Action<UserIdHeaderOptions>? configure = null)
        {
            var opts = new UserIdHeaderOptions();
            configure?.Invoke(opts);
            return app.UseMiddleware<UserIdHeaderMiddleware>(opts);
        }

        public static string? GetUserId(this HttpContext ctx)
            => ctx.Items.TryGetValue(UserIdHeaderMiddleware.HttpItemsKey, out var v) ? v as string : null;
    }

}
