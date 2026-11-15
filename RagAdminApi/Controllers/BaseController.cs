using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RagAdminApi.Controllers
{
    public abstract class BaseController<T> : ControllerBase where T : BaseController<T>
    {
        protected string UserId => GetUserId();
        protected string UserEmail => GetUserEmail();
        private string GetUserId()
        {
            string userId = this.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                return userId;
            else
                throw new ArgumentNullException(nameof(userId));
        }

        private string GetUserEmail()
        {
            string userEmail = this.User?.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(userEmail))
                return userEmail;
            else
                throw new ArgumentNullException(nameof(userEmail));
        }
    }
}
