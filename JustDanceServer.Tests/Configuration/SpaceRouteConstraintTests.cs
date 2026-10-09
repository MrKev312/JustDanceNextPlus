using JustDanceServer.Configuration;

using Microsoft.AspNetCore.Routing;

namespace JustDanceServer.Tests.Configuration;

public class SpaceRouteConstraintTests
{
    [Fact]
    public void JdNextConstraint_ParsesGuidAndRejectsOtherSpaces()
    {
        JdNextSpaceRouteConstraint constraint = new();

        Assert.True(Matches(
            constraint,
            JdNextConstants.GameSpaceId.ToString("B").ToUpperInvariant()));
        Assert.False(Matches(constraint, Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca")));
    }

    [Fact]
    public void JduConstraint_UsesTheNonJdNextProductBranch()
    {
        JduSpaceRouteConstraint constraint = new();

        Assert.True(Matches(constraint, Guid.Parse("17599f67-8a3b-407d-ac5e-7dfdd66d5194")));
        Assert.True(Matches(constraint, Guid.Parse("ef680ba3-e1ae-4fb5-b434-ab448f1c3210")));
        Assert.True(Matches(constraint, Guid.Parse("041c03fa-1735-4ea7-b5fc-c16546d092ca")));
        Assert.False(Matches(constraint, JdNextConstants.GameSpaceId));
        Assert.False(Matches(constraint, JdNextConstants.SessionSpaceId));
    }

    private static bool Matches(IRouteConstraint constraint, object routeValue)
    {
        RouteValueDictionary values = new()
        {
            ["spaceId"] = routeValue
        };

        return constraint.Match(
            httpContext: null,
            route: null,
            routeKey: "spaceId",
            values,
            RouteDirection.IncomingRequest);
    }
}
