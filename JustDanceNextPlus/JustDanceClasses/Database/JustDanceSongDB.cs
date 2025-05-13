using JustDanceNextPlus.JustDanceClasses.Endpoints;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public class JustDanceSongDB(string url)
{
	public OrderedDictionary<Guid, JustDanceSongDBEntry> Songs { get; set; } = [];
	public OrderedDictionary<Guid, ContentAuthorization> ContentAuthorization { get; set; } = [];
	public SongDBTypeSet SongDBTypeSet { get; set; } = new(url);
	public OrderedDictionary<Guid, Dictionary<string, AssetMetadata>> AssetMetadataPerSong { get; set; } = [];
}
