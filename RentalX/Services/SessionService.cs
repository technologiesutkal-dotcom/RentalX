using RentalX.Models;

namespace RentalX.Services;

public class SessionService : ISessionService
{
    public bool IsAuthenticated => CurrentAccount is not null;
    public LoginAccount? CurrentAccount { get; private set; }
    public event Action? SignedOut;

    public void SignIn(LoginAccount account)
    {
        CurrentAccount = account;
    }

    public void SignOut(bool raiseSignedOut = true)
    {
        CurrentAccount = null;
        if (raiseSignedOut)
        {
            SignedOut?.Invoke();
        }
    }
}
