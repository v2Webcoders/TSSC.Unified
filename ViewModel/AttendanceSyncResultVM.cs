namespace TSSC.Unified.ViewModel
{
    public class AttendanceSyncResultVM
    {
        public int TotalBiometricRecords { get; set; }

        public int EmployeesProcessed { get; set; }

        public int AttendanceCreated { get; set; }

        public int AttendanceUpdated { get; set; }

        public int MissingPunch { get; set; }

        public int DuplicatePunches { get; set; }

        public int UnmappedEmployees { get; set; }
    }
}
