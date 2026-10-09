using System.ComponentModel.DataAnnotations;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record JduMapEndedEntry
{
	[Required, StringLength(128)]
	public string MapName { get; init; } = string.Empty;
	[Range(0, 13333)]
	public int Score { get; init; }
	public long Timestamp { get; init; }
	[Required(AllowEmptyStrings = true), StringLength(32)]
	public string GameMode { get; init; } = string.Empty;
	[Range(0, 6)]
	public int NbPlayers { get; init; }
	[Range(0, 6)]
	public int Position { get; init; }
	public bool Wdf { get; init; }
	public bool Coop { get; init; }
	public bool Kids { get; init; }
	public bool Playlist { get; init; }
	public bool SweatEnabled { get; init; }
}
