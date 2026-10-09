namespace JustDanceServer.Services;

public sealed class ApiCachePolicyMiddleware(RequestDelegate next)
{
	public Task InvokeAsync(HttpContext context)
	{
		// Catalogs also vary by SKU/platform headers. Static downloads run before this middleware.
		context.Response.Headers.CacheControl = "private, no-store";
		return next(context);
	}
}
