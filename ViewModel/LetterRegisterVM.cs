using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
namespace TSSC.Unified.ViewModel
{
    public class LetterRegisterVM
    {
        public int LetterId { get; set; }

        public string? LetterType { get; set; }

        [Required]
        public string ItemName { get; set; }

        public string? SenderName { get; set; }

        public string? ReceiverName { get; set; }

        public string? Mode { get; set; }

        public string? DocketNo { get; set; }

        public int? ReceivedBy { get; set; }

        public int? SentBy { get; set; }

        public int? DelegatedBy { get; set; }

        public int? DelegatedTo { get; set; }

        public string? Handover { get; set; }

        public DateTime? HandoverDate { get; set; }
        public DateTime? CreatedOn { get; set; }

        public string? Remarks { get; set; }

        public List<SelectListItem>? EmployeeList { get; set; }
        public IFormFile? LetterAttachmentFile { get; set; }

        public string? LetterAttachment { get; set; }
    }
}
