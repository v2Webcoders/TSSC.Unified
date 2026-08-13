namespace TSSC.Unified.Models
{
    public class DeviceLog
    {
        public int DeviceLogId { get; set; }

        public DateTime? DownloadDate { get; set; }

        public int DeviceId { get; set; }

        public int UserId { get; set; }

        public DateTime LogDate { get; set; }

        public string Direction { get; set; }

        public string AttDirection { get; set; }

        public int? WorkCode { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? LastModifiedDate { get; set; }
    }
}
