namespace SandWebApi.Models.Requests
{
    public class SignupRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? Name { get; set; }
    }
}
