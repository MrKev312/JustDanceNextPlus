using System.Text.Json.Serialization;

namespace JustDanceServer.JustDanceClasses.Database.Profile;

public class PlaylistStats
{
    [JsonIgnore]
    public Guid PlaylistId { get; set; }
    [JsonIgnore]
    public Guid ProfileId { get; set; }

    public int HighScore { get; set; }
    public int PlayCount { get; set; }
    public string Platform { get; set; } = "Unknown";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, int> HighScorePerMap { get; set; } = [with(StringComparer.OrdinalIgnoreCase)];
}
