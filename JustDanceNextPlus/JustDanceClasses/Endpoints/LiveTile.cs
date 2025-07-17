using JustDanceNextPlus.Utilities;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class LiveTile
{
	public required OasisTag ButtonId { get; set; }
	public required OasisTag SubtitleId { get; set; }
	public string DeepLink { get; set; } = string.Empty;
	public int Priority { get; set; }
	public LiveTileAssets Assets { get; set; } = new();
}

public class LiveTileAssets
{
	public string BackgroundImage { get; set; } = string.Empty;
}