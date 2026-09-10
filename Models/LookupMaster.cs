using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QUIZAPP.Models
{
    public class LookupMaster
    {
        // Primary key
        [Key]
        public int Id { get; set; }

        // Name field
        public string Name { get; set; }

        // MasterName field
        public string MasterName { get; set; }

        // Status indicator
        public bool IsActive { get; set; }

        // Date the record was created
        public DateTime CreatedOn { get; set; }

        // Optional parent reference
        public int? ParentId { get; set; }  // Nullable if ParentId is optional
    }

    public class State
    {
        [Key]
        public int Id { get; set; }

        [StringLength(100)]
        public string? StateName { get; set; }
        public ICollection<City> City { get; set; }

    }
    
    public class City
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DistrictId { get; set; }

        [ForeignKey(nameof(DistrictId))]
        public District District { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string CityName { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? Pincode { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; }
    }
    public class District
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StateId { get; set; }

        [ForeignKey("StateId")]
        public State State { get; set; } = null!;

        [Required]
        [MaxLength(150)]
        public string DistrictName { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public bool IsActive { get; set; }

        public ICollection<City> Cities { get; set; } = new List<City>();
    }

}
