namespace JustDanceServer.Services;

public static class SessionIdentityKey
{
    public static string ForApplication(string playerIdentityKey, Guid applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerIdentityKey);
        if (applicationId == Guid.Empty)
            throw new ArgumentException("The application ID cannot be empty.", nameof(applicationId));

        return $"{playerIdentityKey.Trim()}:{applicationId:D}";
    }
}
