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

        [StringLength(100)]
        public string? CityName { get; set; }

        // Foreign key property
        public int StateId { get; set; }

        // Navigation property to the related State
        [ForeignKey("StateId")]
        public State State { get; set; }
    }

}
