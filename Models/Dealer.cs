using System.ComponentModel.DataAnnotations;

namespace QUIZAPP.Models
{
    public class Dealer
    {
        [Key]
        public int Id { get; set; }
        public string DealerCode { get; set; }  // New
        public string Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? DState { get; set; }
        public string? Pincode { get; set; }
        public string? DType { get; set; }
        public string? DealerStatus { get; set; }
    }

}
