using RentalX.Models;

namespace RentalX.Services;

public interface ISessionService
{
    bool IsAuthenticated { get; }
    LoginAccount? CurrentAccount { get; }
    void SignIn(LoginAccount account);
    void SignOut(bool raiseSignedOut = true);
    event Action? SignedOut;
}
