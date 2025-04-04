namespace HR_KD.Data
{
    public class LoginHistory
    {
        public Guid LoginId { get; set; }
        public string UserId { get; set; }
        public string Username { get; set; }
        public string Roles { get; set; }
        public DateTime LoginTime { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
