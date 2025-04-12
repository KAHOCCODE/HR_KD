namespace HR_KD.DTOs
{
    public class AssignRoleDto
    {
        public string Username { get; set; } = null!;
        public List<string> Roles { get; set; } = new();
    }
}
