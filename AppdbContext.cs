using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using QUIZAPP.Models;
using QUIZAPP.Areas.Admin.ViewModels;
using TSSC.Unified.Models;

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

    }
}
