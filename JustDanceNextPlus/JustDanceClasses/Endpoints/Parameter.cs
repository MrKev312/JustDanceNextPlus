namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public record ParameterList
{
	public Dictionary<string, Parameter> Parameters { get; init; } = [];
}

public record Parameter
{
	public Dictionary<string, object?> Fields { get; init; } = [];
    public RelatedPopulation? RelatedPopulation { get; init; }
}

public record RelatedPopulation
{
	public string? Name { get; init; }
	public string? Subject { get; init; }
}
