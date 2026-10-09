namespace JustDanceServer.JustDanceClasses.Database.Profile;

/// <summary>
/// The classic JDU representation of a profile. Cosmetics use integer identifiers in
/// these games, so they cannot be folded into the GUID-based JD+ dancer card.
/// Scores deliberately do not live here; both products use <see cref="MapStats"/>.
/// </summary>
public sealed class ClassicProfile
{
	public Guid ProfileId { get; set; }
	public string Name { get; set; } = "Player";
	public string Nickname { get; set; } = "Player 1";
	public uint AvatarId { get; set; }
	public int CountryId { get; set; }
	public uint SkinId { get; set; }
	public uint AliasId { get; set; }
	public int AliasGender { get; set; }
	public uint PortraitBorderId { get; set; }
	public int JdPoints { get; set; }
	public string PlatformId { get; set; } = string.Empty;
	public string Platform { get; set; } = string.Empty;
	public string PayloadJson { get; set; } = "{}";
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
