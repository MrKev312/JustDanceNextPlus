using Microsoft.EntityFrameworkCore;

using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;
[PrimaryKey("MapId", "ProfileId")]
public class PlayerMapData
{
	[JsonIgnore]
	public Guid MapId { get; set; }
	[JsonIgnore]
	public Guid ProfileId { get; set; }
}