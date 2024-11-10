namespace JustDanceNextPlus.JustDanceClasses.Endpoints;

public class Parameter
{
	public Dictionary<string, object> Fields { get; set; } = [];
	public RelatedPopulation? RelatedPopulation { get; set; }
}

public class RelatedPopulation
{
	public string? Name { get; set; }
	public string? Subject { get; set; }
}
