namespace RentalX.Admin.Services;

public sealed class AdminSession : IAdminSession
{
    public string? Token { get; private set; }
    public string? Username { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);
    public event Action? SignedOut;

    public void SetSession(string token, string displayName, string? role = null, string? username = null)
    {
        Token = token;
        DisplayName = displayName;
        Role = role;
        Username = username;
    }

    public void UpdateUsername(string username)
    {
        Username = username;
    }

    public void Clear(bool raiseSignedOut = true)
    {
        Token = null;
        Username = null;
        DisplayName = null;
        Role = null;
        if (raiseSignedOut)
        {
            SignedOut?.Invoke();
        }
    }
}
