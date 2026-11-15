using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Rag.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services
{
    public class TokenValidationService : ITokenValidationService
    {
        private readonly IConfiguration _configuration;
        public TokenValidationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
        {
            var JwtSection = _configuration.GetSection("Jwt");
            IEnumerable<string> issuers = new[] { JwtSection["Issuer"] };
            var JwtAudience = JwtSection["Audience"];

            var JwtKey = JwtSection["Key"];

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                ValidateIssuer = false,
                ValidIssuers = issuers,
                ValidateAudience = false,
                ValidAudience = JwtAudience,
                ValidateLifetime = false,
                ClockSkew = TimeSpan.Zero // Optional: set to zero to eliminate the default 5 min clock skew
            };

            try
            {
                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                throw new SecurityTokenExpiredException("Token has expired.");
            }
            catch (SecurityTokenInvalidSignatureException)
            {
                throw new SecurityTokenInvalidSignatureException("Token has an invalid signature.");
            }
            catch (Exception ex)
            {
                throw new SecurityTokenException("Token validation failed: " + ex.Message);
            }
        }
    }
}
