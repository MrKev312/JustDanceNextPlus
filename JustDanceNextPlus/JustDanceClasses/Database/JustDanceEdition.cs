using JustDanceNextPlus.Utilities;

using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public enum VersionType
{
	Yearly,
	Exclusive
}

public record JustDanceEdition
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public VersionType DlcType { get; init; }
	public string Name { get; init; } = "";

	// For dlcProducts
	public ImmutableArray<string>? ClaimIds { get; init; }
	public OasisTag? ProductLocId { get; init; }
	public OasisTag? ProductDescriptionId { get; init; }

	// For productGroups
	[Required]
	public required OasisTag GroupLocId { get; init; }
	public OasisTag? SongsCountLocId { get; init; }
	public OasisTag? GroupDescriptionLocId { get; init; }
	public ImmutableArray<MapTag> TracklistExtended { get; init; } = [];
	public ImmutableArray<MapTag> TracklistLimited { get; init; } = [];
	public OasisTag? TracklistExtendedLocId { get; init; }
	public OasisTag? TracklistLimitedLocId { get; init; }
	public string ProductGroupBundle { get; init; } = "";
}
