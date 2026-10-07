using TSSC.Unified.Models;

namespace TSSC.Unified.ViewModel
{
    public class TeamMembersVM
    {
        public Employee? Manager { get; set; }
        public List<Employee> TeamMembers { get; set; } = new();
    }
}
