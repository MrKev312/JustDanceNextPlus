
using System.Diagnostics.CodeAnalysis;

namespace JustDanceServer.Configuration;

public sealed record UbisoftSessionIdentity(
    Guid SpaceId,
    string PlatformType,
    IReadOnlyList<string>? PlatformApplicationIds = null);
public sealed record UbisoftApplicationTarget(Guid SpaceId, bool UseDiscoveryHost);
public sealed record UbisoftApplicationProfile(
    Guid ApplicationId,
    UbisoftSessionIdentity Session,
    UbisoftApplicationTarget? Target = null);

public static class UbisoftApplicationProfiles
{
    private static IReadOnlyDictionary<Guid, UbisoftApplicationProfile> ByApplicationId { get; } =
        UbisoftClientProfiles.All
            .Select(CreateApplicationProfile)
            .ToDictionary(profile => profile.ApplicationId);

    private static IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> ApplicationIdsByTargetSpaceId { get; } =
        ByApplicationId.Values
            .Where(profile => profile.Target is not null)
            .GroupBy(profile => profile.Target!.SpaceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<Guid>)[.. group.Select(profile => profile.ApplicationId)]);

    public static bool TryGetSessionIdentity(
        Guid applicationId,
        [NotNullWhen(true)] out UbisoftSessionIdentity? identity)
    {
        if (ByApplicationId.TryGetValue(applicationId, out UbisoftApplicationProfile? profile))
        {
            identity = profile.Session;
            return true;
        }

        identity = null;
        return false;
    }

    public static bool TryGetApplicationTarget(
        Guid applicationId,
        [NotNullWhen(true)] out UbisoftApplicationTarget? target)
    {
        if (ByApplicationId.TryGetValue(
            applicationId,
            out UbisoftApplicationProfile? profile)
            && profile.Target is not null)
        {
            target = profile.Target;
            return true;
        }

        target = null;
        return false;
    }

    public static IReadOnlyList<Guid> GetApplicationIdsForTarget(Guid spaceId)
        => ApplicationIdsByTargetSpaceId.TryGetValue(spaceId, out IReadOnlyList<Guid>? applicationIds)
            ? applicationIds
            : [];

    private static UbisoftApplicationProfile CreateApplicationProfile(
        IUbisoftClientProfile client)
        => new(
            client.ApplicationId,
            new UbisoftSessionIdentity(
                client.SpaceId,
                client.PlatformType,
                client.PlatformApplicationIds),
            client.TargetSpaceId is Guid targetSpaceId
                ? new UbisoftApplicationTarget(targetSpaceId, client.UseDiscoveryHost)
                : null);
}
