using System.ComponentModel.DataAnnotations;

namespace TSSC.Unified.ViewModel
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class EmployeeVM
    {
        // ================= Basic Information =================

        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Employee Code")]
        public string EmployeeCode { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        public string? Gender { get; set; }

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }

        [Display(Name = "Blood Group")]
        public string? BloodGroup { get; set; }

        [Display(Name = "Marital Status")]
        public string? MaritalStatus { get; set; }


        // ================= Official Information =================


        [Required(ErrorMessage = "Please select Department")]
        [Display(Name = "Department")]
        public int? DepartmentId { get; set; }

        [Required(ErrorMessage = "Please select Designation")]
        [Display(Name = "Designation")]
        public int? DesignationId { get; set; }

        [Display(Name = "Reporting Manager")]
        public int? ReportingManagerId { get; set; }

        [Required]
        [Display(Name = "Joining Date")]
        [DataType(DataType.Date)]
        public DateTime? JoiningDate { get; set; }

        [Required]
        [Display(Name = "Employee Type")]
        public string EmployeeType { get; set; }


        [Required]
        [Display(Name = "Employment Status")]
        public string EmploymentStatus { get; set; }


        // ================= Contact Information =================

        [EmailAddress]
        [Display(Name = "Official Email")]
        public string OfficialEmail { get; set; }

        [EmailAddress]
        [Display(Name = "Personal Email")]
        public string? PersonalEmail { get; set; }

        
        [Phone]
        [Display(Name = "Mobile Number")]
        public string? MobileNo { get; set; }

        [Phone]
        [Display(Name = "Alternate Mobile")]
        public string? AlternateMobile { get; set; }


        // ================= Address =================

        public string? CurrentAddress { get; set; }

        public string? PermanentAddress { get; set; }


        // ================= Identity =================

        [Display(Name = "Aadhaar Number")]
        [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhaar Number must be exactly 12 digits.")]
        public string? AadhaarNo { get; set; }

        [Display(Name = "PAN Number")]
        public string? PANNo { get; set; }

        [Display(Name = "Passport Number")]
        public string? PassportNo { get; set; }

        [Display(Name = "UAN Number")]
        public string? UANNo { get; set; }

        [Display(Name = "PF Number")]
        public string? PFNo { get; set; }

        [Display(Name = "ESIC Number")]
        public string? ESICNo { get; set; }


        // ================= Bank Details =================

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }

        [Display(Name = "Account Number")]
        public string? AccountNo { get; set; }

        [Display(Name = "IFSC Code")]
        public string? IFSCCode { get; set; }


        // ================= Salary =================

        public decimal? CTC { get; set; }

        [Display(Name = "Basic Salary")]
        public decimal? BasicSalary { get; set; }

        public decimal? HRA { get; set; }

        [Display(Name = "Special Allowance")]
        public decimal? SpecialAllowance { get; set; }


        // ================= Login Information =================

        [Required]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        
        [Display(Name = "Role")]
        public string RoleId { get; set; }

        public string? DepartmentName { get; set; }

        public string? DesignationName { get; set; }

        public string? ReportingManagerName { get; set; }
        public string? RoleName { get; set; }
        
        // ASP.NET Identity User Id
        public string? ApplicationUserId { get; set; }

        public IFormFile? Photo { get; set; }

        public string? PhotoPath { get; set; }
        // ================= Status =================

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Prefix")]
        public string? Prefix { get; set; }

        [Display(Name = "Branch")]
        public int? BranchId { get; set; }

        [Display(Name = "Sub Branch")]
        public int? SubBranchId { get; set; }

        [Required]
        [Display(Name = "Notice Period (Days)")]
        public int? NoticePeriod { get; set; }
        public string? BranchName { get; set; }

        public string? SubBranchName { get; set; }
        
        [Required]
        public string ProfileStatus { get; set; } = "Pending";
        [Display(Name = "Resignation Date")]
        public DateTime? ResignationDate { get; set; }

        [Display(Name = "Relieving Date")]
        public DateTime? RelievingDate { get; set; }
    }
}
