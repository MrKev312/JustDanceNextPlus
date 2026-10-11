namespace JustDanceServer.Configuration;

public abstract class SpaceRouteConstraint(Guid expectedSpaceId) : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        return values.TryGetValue(routeKey, out object? routeValue)
            && Guid.TryParse(routeValue?.ToString(), out Guid spaceId)
            && spaceId == expectedSpaceId;
    }
}

public sealed class JdNextSpaceRouteConstraint()
    : SpaceRouteConstraint(JdNextConstants.GameSpaceId);

public sealed class JduSpaceRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
        => values.TryGetValue(routeKey, out object? routeValue)
            && Guid.TryParse(routeValue?.ToString(), out Guid spaceId)
            && spaceId != JdNextConstants.GameSpaceId
            && spaceId != JdNextConstants.SessionSpaceId;
}
