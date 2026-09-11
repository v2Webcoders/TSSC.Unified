using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.Areas.Admin.ViewModels;
using TSSC.Unified.Models;
using Microsoft.Identity.Client;

namespace QUIZAPP
{
    public class AppdbContext : IdentityDbContext<AppUser>
    {
        private string ConnectionString { get; }
        public AppdbContext(DbContextOptions options) : base(options)
        {

        }
        public DbSet<AppUser> AppUser { get; set; }
        internal DbSet<LookupMaster> LookupMaster { get; set; }
        internal DbSet<State> State { get; set; }

        internal DbSet<District> District { get; set; }

        internal DbSet<City> City { get; set; }
        public DbSet<Dealer> Dealer { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistory { get; set; }
        public DbSet<Employee> Employee { get; set; }
        public DbSet<Department> Department { get; set; }

        public DbSet<Designation> Designation { get; set; }
        public DbSet<HRPolicy> HRPolicies { get; set; }

        public DbSet<Branch> Branch { get; set; }

        public DbSet<SubBranch> SubBranch { get; set; }
        public DbSet<LeaveType> LeaveType { get; set; }
        public DbSet<LeaveRequest> LeaveRequest { get; set; }

        public DbSet<Tasks> Tasks { get; set; }
        public DbSet<LetterRegister> LetterRegister { get; set; }
        public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalance { get; set; }

        public DbSet<LeaveAdjustment> LeaveAdjustment { get; set; }
        public DbSet<LeavePolicy> LeavePolicy { get; set; }

        public DbSet<EmployeeAttendance> EmployeeAttendance { get; set; }
        public DbSet<AttendanceRegularization> AttendanceRegularization { get; set; }
        public DbSet<ODRequest> ODRequest { get; set; }
        public DbSet<Holiday> Holiday { get; set; }

        public DbSet<Reimbursement> Reimbursement { get; set; }
        public DbSet<Reimbursements> Reimbursements { get; set; }
        public DbSet<EmployeeHierarchy> EmployeeHierarchy { get; set; }


        //for certificate generation--------------------------------------------------------------
        public DbSet<Certificate> Certificate { get; set; }
        public DbSet<CertificateGeneration> CertificateGeneration { get; set; }
        public DbSet<CertificateGenerationDetail> CertificateGenerationDetail{ get; set; }
        //for add JobRole module--------------------------------------------------------------
        public DbSet<JobRole> JobRoles { get; set; }
        public DbSet<JobRoleDocument> JobRoleDocuments { get; set; }
        public DbSet<SubSector> SubSectors { get; set; }

        //for add trainer module--------------------------------------------------------------
        public DbSet<TrainerRegistration> TrainerRegistration { get; set; }


        //public DbSet<Certificate> Certificates { get; set; }
        //----------------------------------------------------------------------------------------
        public DbSet<TPRegistration> TPRegistrations { get; set; }
        public DbSet<TPRegistrationDocument> TPRegistrationDocuments { get; set; }

        public DbSet<TrainerRegistrationQualification>TrainerRegistrationQualification{ get; set; }

        public DbSet<TrainerRegistrationExperience>TrainerRegistrationExperience{ get; set; }

        public DbSet<TrainerRegistrationDocument>TrainerRegistrationDocument{ get; set; }

    }
}
