using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class ProjectTeam
    {
        [Key]
        public int ProjectTeamId { get; set; }

        public int ProjectId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime CreatedDate { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee? Employee { get; set; }
    }
}
