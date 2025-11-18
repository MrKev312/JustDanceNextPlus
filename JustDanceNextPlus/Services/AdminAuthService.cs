namespace JustDanceNextPlus.Services;

public class AdminAuthService
{
    private readonly ISecurityService _securityService;
    
    public bool IsAuthenticated { get; private set; }
    
    public event Action? OnAuthStateChanged;

    public AdminAuthService(ISecurityService securityService)
    {
        _securityService = securityService;
    }

    public bool Login(string username, string password)
    {
        if (_securityService.ValidateCredentials(username, password))
        {
            IsAuthenticated = true;
            NotifyStateChanged();
            return true;
        }
        return false;
    }

    public void Logout()
    {
        IsAuthenticated = false;
        NotifyStateChanged();
    }
    
    private void NotifyStateChanged() => OnAuthStateChanged?.Invoke();
}
