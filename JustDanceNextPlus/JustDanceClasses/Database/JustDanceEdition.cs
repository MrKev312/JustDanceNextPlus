using JustDanceNextPlus.Utilities;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database;

public enum VersionType
{
	Yearly,
	Exclusive
}

public class JustDanceEdition
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public VersionType DlcType { get; set; }
	public string Name { get; set; } = "";

	// For dlcProducts
	public List<string>? ClaimIds { get; set; }
	public OasisTag? ProductLocId { get; set; }
	public OasisTag? ProductDescriptionId { get; set; }

	// For productGroups
	[Required]
	public required OasisTag GroupLocId { get; set; }
	public OasisTag? SongsCountLocId { get; set; }
	public OasisTag? GroupDescriptionLocId { get; set; }
	public List<MapTag> TracklistExtended { get; set; } = [];
	public List<MapTag> TracklistLimited { get; set; } = [];
	public OasisTag? TracklistExtendedLocId { get; set; }
	public OasisTag? TracklistLimitedLocId { get; set; }
	public string ProductGroupBundle { get; set; } = "";
}
