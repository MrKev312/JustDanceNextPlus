using JustDanceServer.Services;
using Microsoft.AspNetCore.Http;

namespace JustDanceServer.Tests.Services;

public sealed class ApiCachePolicyTests
{
    [Fact]
    public async Task PersonalizedResponsesAreNotSharedAndTheirPayloadIsPreserved()
    {
        const string payload = "{\"friends\":[{\"pid\":\"player\",\"state\":\"Friends\"}]}";
        using MemoryStream output = new();
        DefaultHttpContext http = new();
        http.Response.Body = output;
        http.Response.Headers.CacheControl = "public, max-age=300";
        ApiCachePolicyMiddleware middleware = new(context => context.Response.WriteAsync(payload));
        await middleware.InvokeAsync(http);
        Assert.Equal("private, no-store", http.Response.Headers.CacheControl);
        Assert.Equal(payload, System.Text.Encoding.UTF8.GetString(output.ToArray()));
        Assert.Equal(200, http.Response.StatusCode);
    }
}
