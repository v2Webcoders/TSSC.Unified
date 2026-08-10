using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TSSC.Unified.Models
{
    public class LeaveAdjustment
    {
        [Key]
        public int LeaveAdjustmentId { get; set; }

        // Employee whose leave balance is being adjusted
        public int EmployeeId { get; set; }

        // Leave Type (Casual, Sick, etc.)
        public int LeaveTypeId { get; set; }

        // Credit or Deduct
        public string AdjustmentType { get; set; }

        // Number of days
        public decimal Days { get; set; }

        // Reason for adjustment
        public string Reason { get; set; }

        // HR user who made the adjustment
        public int AdjustedBy { get; set; }

        public DateTime AdjustedOn { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public Employee Employee { get; set; }

        public LeaveType LeaveType { get; set; }

        [ForeignKey(nameof(AdjustedBy))]
        public Employee AdjustedByEmployee { get; set; }
    }
}
