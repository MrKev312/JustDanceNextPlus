using JustDanceNextPlus.JustDanceClasses.Endpoints;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class JustDanceSongDB
{
	public OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; set; } = [];
	public OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; set; } = [];
	public SongDBTypeSet SongDBTypeSet { get; set; } = new();
}
