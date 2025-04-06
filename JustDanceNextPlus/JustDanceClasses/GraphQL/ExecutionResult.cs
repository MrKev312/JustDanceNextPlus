namespace JustDanceNextPlus.JustDanceClasses.GraphQL;

public class ExecutionResult<T>
{
	public bool Success { get; set; }
	public T? Response { get; set; }
}
