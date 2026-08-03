using QUIZAPP.Models;

namespace QUIZAPP.Services
{
    public class UtilityService
    {
        private readonly AppdbContext _dbContext;

        public UtilityService(AppdbContext context)
        {
            _dbContext = context;
        }
        /// <summary>
        /// Logs any user action (Login, Logout, Failed Login, etc.)
        /// </summary>
        public async Task<string> LogUserActionAsync(string userId, string userType, string actionType,
                                                     string ipAddress, string browserInfo)
        {
            //try
            {
                var log = new UserLoginHistory
                {
                    UserId = userId,
                    UserType = userType,
                    Action = actionType,
                    ActionTime = DateTime.Now,
                    IPAddress = ipAddress,
                    BrowserInfo = browserInfo
                };

                await _dbContext.UserLoginHistory.AddAsync(log);
                await _dbContext.SaveChangesAsync();

                return "True";
            }
            //catch (Exception ex)
            //{
            //    return ex.Message;
            //}
        }
    }

}
