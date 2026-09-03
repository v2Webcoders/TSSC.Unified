using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using QUIZAPP.Models;

namespace TSSC.Unified.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        // ASP.NET Identity
        [Required]
        public string ApplicationUserId { get; set; }

        // Basic Information
        [Required]
        [StringLength(20)]
        public string EmployeeCode { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? Gender { get; set; }

        public DateTime? DOB { get; set; }

        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [StringLength(20)]
        public string? MaritalStatus { get; set; }

        // Official Information
        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int DesignationId { get; set; }

        public int? ReportingManagerId { get; set; }

        public DateTime JoiningDate { get; set; }

        [StringLength(30)]
        public string EmployeeType { get; set; }

        [StringLength(30)]
        public string EmploymentStatus { get; set; }

        // Contact Information
        [StringLength(150)]
        public string? OfficialEmail { get; set; }

        [StringLength(150)]
        public string? PersonalEmail { get; set; }

       
        [StringLength(15)]
        public string? MobileNo { get; set; }

        [StringLength(15)]
        public string? AlternateMobile { get; set; }

        // Address
        [StringLength(500)]
        public string? CurrentAddress { get; set; }

        [StringLength(500)]
        public string? PermanentAddress { get; set; }

        // Identity Details
        [StringLength(20)]
        public string? AadhaarNo { get; set; }

        [StringLength(20)]
        public string? PANNo { get; set; }

        [StringLength(30)]
        public string? PassportNo { get; set; }

        [StringLength(30)]
        public string? UANNo { get; set; }

        [StringLength(30)]
        public string? PFNo { get; set; }

        [StringLength(30)]
        public string? ESICNo { get; set; }

        // Bank Details
        [StringLength(150)]
        public string? BankName { get; set; }

        [StringLength(50)]
        public string? AccountNo { get; set; }

        [StringLength(20)]
        public string? IFSCCode { get; set; }

        // Salary
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CTC { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? BasicSalary { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? HRA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SpecialAllowance { get; set; }

        // Status
        public bool IsActive { get; set; } = true;

        // Audit
        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties

        [ForeignKey(nameof(ApplicationUserId))]
        public virtual AppUser ApplicationUser { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public virtual Department Department { get; set; }

        [ForeignKey(nameof(DesignationId))]
        public virtual Designation Designation { get; set; }

        [ForeignKey(nameof(ReportingManagerId))]
        public virtual Employee? ReportingManager { get; set; }
        public string? PhotoPath { get; set; }
        public string? Prefix { get; set; }

        public int? BranchId { get; set; }

        public int? SubBranchId { get; set; }

        public int? NoticePeriod { get; set; }
        
        [NotMapped]
        public string EmployeeDisplayName
        {
            get
            {
                return string.Join(" ", new[] { Prefix, FirstName, LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                    + $" - EMP{EmployeeCode}";
            }
        }

        public string ProfileStatus { get; set; }

        [Display(Name = "Resignation Date")]
        public DateTime? ResignationDate { get; set; }

        [Display(Name = "Relieving Date")]
        public DateTime? RelievingDate { get; set; }
    }
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }
        
        [NotMapped]

        // Navigation Property
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
    public class Designation
    {
        [Key]
        public int DesignationId { get; set; }

        [Required]
        [StringLength(100)]
        public string DesignationName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation Property
        public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
