namespace BlazorIdleGame.Client.Services.Auth
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RegisterResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}