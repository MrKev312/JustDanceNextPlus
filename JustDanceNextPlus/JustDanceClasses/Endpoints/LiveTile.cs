using JustDanceNextPlus.Utilities;

namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record LiveTile
{
	public required OasisTag ButtonId { get; init; }
	public required OasisTag SubtitleId { get; init; }
	public string DeepLink { get; init; } = string.Empty;
	public int Priority { get; init; }
	public LiveTileAssets Assets { get; init; } = new();
}

public record LiveTileAssets
{
	public string BackgroundImage { get; init; } = string.Empty;
}