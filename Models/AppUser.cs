using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QUIZAPP.Models
{
    public class AppUser:IdentityUser
    {
        /// <summary>
        /// Not to commit
        /// </summary>
        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? UserRole { get; set; }
       
        [StringLength(20)]
        public string? MobileNo { get; set; }

        [StringLength(50)]
        public string? Password { get; set; }
        
        [DataType(DataType.DateTime)]
        public DateTime? CreatedOn { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedOn { get; set; }
       
    }
    public class UserLoginHistory
    {
        public int Id { get; set; }

        public string UserId { get; set; }          // AdminId or DealerCode

        public string UserType { get; set; }        // Admin | Dealer

        public DateTime ActionTime { get; set; }

        public string? IPAddress { get; set; }

        public string? BrowserInfo { get; set; }

        public string? Action { get; set; }          // Login | Logout | SessionExpired etc.
    }


}
