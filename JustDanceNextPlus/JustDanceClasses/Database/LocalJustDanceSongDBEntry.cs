namespace JustDanceNextPlus.JustDanceClasses.Database;

public record LocalJustDanceSongDBEntry : JustDanceSongDBEntry
{
	public Guid SongID { get; init; }
}