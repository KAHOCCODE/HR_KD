namespace HR_KD.Common
{
    public static class PayrollStatus
    {
        public const string Created = "BL1"; // Đã tạo
        public const string EmployeeConfirmed = "BL1A"; // Nhân viên xác nhận
        public const string EmployeeRejected = "BL1R"; // Nhân viên từ chối
        public const string SentToAccounting = "BL2"; // Gửi kế toán
        public const string AccountingRejected = "BL2R"; // Kế toán trả về
        public const string AccountingApproved = "BL3"; // Kế toán duyệt
        public const string DirectorRejected = "BL3R"; // Giám đốc trả về
        public const string DirectorApproved = "BL4"; // Giám đốc duyệt
        public const string SentToEmployee = "BL5"; // Đã gửi nhân viên
    }

    public static class AttendanceStatus
    {
        public const string Pending = "CC1"; // Chờ duyệt
        public const string FirstApproved = "CC2"; // Đã duyệt lần 1
        public const string Approved = "CC3"; // Đã duyệt
        public const string Rejected = "CC4"; // Từ chối
        public const string Paidleave = "CC5"; // Nghỉ phép có lương
        public const string Unpaidleave = "CC6"; // Nghỉ phép không lương
    }

    public static class OvertimeStatus
    {
        public const string Pending = "TC1"; // Chờ duyệt
        public const string FirstApproved = "TC2"; // Đã duyệt lần 1
        public const string Approved = "TC3"; // Đã duyệt
        public const string Rejected = "TC4"; // Từ chối
    }

    public static class LeaveStatus
    {
        public const string Pending = "NN1"; // Chờ duyệt
        public const string Approved = "NN2"; // Đã duyệt
        public const string Rejected = "NN3"; // Từ chối
        public const string Canceled = "NN4"; // Đã hủy
    }

    public static class AttendanceHistoryStatus
    {
        public const string Pending = "LS1"; // Chờ duyệt
        public const string FirstApproved = "LS2"; // Đã duyệt lần 1
        public const string Approved = "LS3"; // Đã duyệt
        public const string Rejected = "LS4"; // Từ chối
    }
    // ngày lễ  "NL"
    public static class HolidayStatus
    {
        public const string Pending = "NL1"; // Chờ duyệt
        public const string Approved = "NL2"; // Đã duyệt
        public const string ApprovedWeekend = "NL3"; // Đã duyệt cuối tuần
        public const string Rejected = "NL4"; // Từ chối
    }
}