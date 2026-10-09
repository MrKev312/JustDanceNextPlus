namespace JustDanceServer.JustDanceClasses.Database.Profile;

/// <summary>
/// A stable first-party platform login linked to one local Just Dance account.
/// The provider/subject pair is the authentication identity; IdOnPlatform is
/// retained separately because it is the value UbiServices exposes to clients.
/// A future master identity (for example Discord) belongs to the account, not
/// in this table, so platform links can be moved or removed independently.
/// </summary>
public sealed class PlatformIdentity
{
	public string Provider { get; set; } = string.Empty;
	public string Subject { get; set; } = string.Empty;
	public Guid ProfileId { get; set; }
	public string PlatformType { get; set; } = string.Empty;
	public string IdOnPlatform { get; set; } = string.Empty;
	public string NameOnPlatform { get; set; } = string.Empty;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
