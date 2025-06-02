namespace BUILD.ING.Models
{
    /// <summary>
    /// Represents the data sent when a user logs in.
    /// </summary>
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
