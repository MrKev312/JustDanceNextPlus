using JustDanceNextPlus.JustDanceClasses.Endpoints;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class JustDanceSongDB
{
	public Dictionary<Guid, JustDanceSongDBEntry> Songs { get; set; } = [];
	public Dictionary<Guid, ContentAuthorization> ContentAuthorization { get; set; } = [];
	public SongDBTypeSet SongDBTypeSet { get; set; } = new();
}
