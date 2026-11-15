using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Rag.Service.Services.Interfaces
{
    public interface ITokenValidationService
    {
        Task<ClaimsPrincipal> ValidateTokenAsync(string token);
    }
}
