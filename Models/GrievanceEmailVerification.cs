namespace TSSC.Unified.Models
{
    public class GrievanceEmailVerification
    {
        public int Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string OtpHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool IsVerified { get; set; }

        public bool IsUsed { get; set; }

        public int AttemptCount { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? VerifiedDate { get; set; }

        public DateTime? UsedDate { get; set; }
    }
}
