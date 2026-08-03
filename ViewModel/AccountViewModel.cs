using System.ComponentModel.DataAnnotations;

namespace QUIZAPP.ViewModel
{
        public class LoginViewModel
        {
            [Required(ErrorMessage = "Required")]
            [Display(Name = "User Name *")]
            public string userid { get; set; }

            [Required(ErrorMessage = "Required")]
            [Display(Name = "Password *")]
        [RegularExpression(@"^([\S\s]{3,20})", ErrorMessage = "Please enter Minimum 3 characters Required!")]
        public string Password { get; set; }
            [Display(Name = "Remember Me")]
            public string? RememberMe { get; set; }
        }
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "User Name is Required!")]
        [Display(Name = "User Name *")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "New Password is Required!")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password *")]
        [RegularExpression(@"^([\S\s]{3,40})", ErrorMessage = "Please enter Minimum 3 characters Required!")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Confirm Password is Required!")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password *")]
        [Compare("Password", ErrorMessage = "New Password and Confirm Password doesn't match")]
        public string ConfirmPassword { get; set; }
        [Required]
        public string Token { get; set; }
    }
    //public class ResetPasswordViewModel
    //{
    //    [Required(ErrorMessage = "Old Password is Required!")]
    //    [DataType(DataType.Password)]
    //    [Display(Name = "Old Password *")]
    //    public string OldPassword { get; set; }
    //    [Required(ErrorMessage = "New Password is Required!")]
    //    [DataType(DataType.Password)]
    //    [Display(Name = "New Password *")]
    //    [RegularExpression(@"^([\S\s]{3,40})", ErrorMessage = "Please enter Minimum 3 characters Required!")]
    //    public string Password { get; set; }
    //    [Required(ErrorMessage = "Confirm Password is Required!")]
    //    [DataType(DataType.Password)]
    //    [Display(Name = "Confirm Password *")]
    //    [Compare("Password", ErrorMessage = "New Password and Confirm Password doesn't match")]
    //    public string ConfirmPassword { get; set; }
    //}
}

