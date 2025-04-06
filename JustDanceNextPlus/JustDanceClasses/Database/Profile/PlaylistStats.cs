using HotChocolate;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustDanceNextPlus.JustDanceClasses.Database.Profile;

[PrimaryKey("PlaylistId", "ProfileId")]
public class PlaylistStats
{
	[JsonIgnore]
	[GraphQLIgnore]
	public Guid PlaylistId { get; set; }
	[JsonIgnore]
	[GraphQLIgnore]
	public Guid ProfileId { get; set; }

	public int HighScore { get; set; }
	public int PlayCount { get; set; }
	public string Platform { get; set; } = "Unknown";
	[GraphQLIgnore]
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	public Dictionary<Guid, int> HighScorePerMap { get; set; } = [];
}
