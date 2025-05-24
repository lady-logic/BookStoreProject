using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BookStoreApi
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Debug: Schaue nach Authorization Header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            Logger.LogInformation($"[TestAuthHandler] Authorization Header: '{authHeader ?? "null"}'");

            // Prüfe verschiedene Varianten des Authorization Headers
            bool hasAuthHeader = !string.IsNullOrEmpty(authHeader) &&
                                (authHeader.StartsWith("Bearer ") || authHeader.StartsWith("Test "));

            Logger.LogInformation($"[TestAuthHandler] Has valid auth header: {hasAuthHeader}");

            if (!hasAuthHeader)
            {
                Logger.LogInformation("[TestAuthHandler] No valid authorization header found, returning NoResult");
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            // Wenn Authorization Header vorhanden ist, authentifizieren
            Logger.LogInformation("[TestAuthHandler] Creating authenticated user");

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            Logger.LogInformation("[TestAuthHandler] Authentication successful");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}