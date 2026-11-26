using Microsoft.AspNetCore.Identity;
using QUIZAPP.Models;
using System.Security.Claims;

namespace TSSC.Unified.Areas.HRMS.Controllers
{
    internal class UserManager
    {
        internal static async Task GetUserAsync(ClaimsPrincipal user)
        {
            throw new NotImplementedException();
        }

        public static implicit operator UserManager(UserManager<AppUser> v)
        {
            throw new NotImplementedException();
        }
    }
}