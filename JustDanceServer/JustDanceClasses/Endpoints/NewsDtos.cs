using JustDanceServer.Services;

namespace JustDanceServer.JustDanceClasses.Endpoints;

public sealed record UbisoftNewsEntry
{
	public required Guid SpaceId { get; init; }
	public required string NewsId { get; init; }
	public required string Type { get; init; }
	public required string Placement { get; init; }
	public required int Priority { get; init; }
	public int DisplayTime { get; init; }
	public required DateTimeOffset PublicationDate { get; init; }
	public DateTimeOffset? ExpirationDate { get; init; }
	public required string Locale { get; init; }
	public required string Title { get; init; }
	public string ContentType { get; init; } = "plaintext";
	public required string Body { get; init; }
	public required string MediaURL { get; init; }
	public string MediaType { get; init; } = "None";
	public Guid? ProfileId { get; init; }
	public object Obj { get; init; } = new { promotionalContent = false };
	public required string Summary { get; init; }
	public object[] Links { get; init; } = [];
	public string[] Tags { get; init; } = [];
}

public static class NewsWireProjection
{
	public static UbisoftNewsEntry ToResponse(EditorialEntry entry, Guid spaceId)
		=> new()
		{
			SpaceId = spaceId,
			NewsId = entry.Id.ToString(),
			Type = entry.Type,
			Placement = entry.Placement,
			Priority = entry.Priority,
			PublicationDate = entry.PublicationDate,
			ExpirationDate = entry.ExpirationDate,
			Locale = entry.Locale,
			Title = entry.Title,
			Body = entry.Body,
			MediaURL = entry.ImageUrl,
			Summary = entry.Summary
		};
}
