namespace App.Helper.Dto { }

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string UserId { get; set; }
    public int RemainingAttempts { get; set; }
}

