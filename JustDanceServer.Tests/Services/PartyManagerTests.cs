using JustDanceServer.Services;

namespace JustDanceServer.Tests.Services;

public sealed class PartyServiceTests
{
    private readonly PartyService parties = new(new UbiServicesNotificationHub());

    [Fact]
    public void CreateBuildsAnOwnedParty()
    {
        Guid owner = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);

        Assert.NotEqual(Guid.Empty, party.PartyId);
        Assert.Equal(owner, party.OwnerProfileId);
        Assert.Equal(owner, Assert.Single(party.Members).ProfileId);
        Assert.Equal(party.PartyId, parties.GetByProfileId(owner)!.PartyId);
    }

    [Fact]
    public void AnIssuedTokenCanBeUsedOnlyOnce()
    {
        Guid owner = Guid.NewGuid();
        Guid member = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);
        PartyAccessToken token = Assert.IsType<PartyAccessToken>(
            parties.IssueAccessToken(party.PartyId, owner, member));

        Assert.True(parties.Join(party.PartyId, member, token.Value));
        Assert.False(parties.Join(party.PartyId, Guid.NewGuid(), token.Value));
        Assert.Equal(2, parties.Get(party.PartyId)!.Members.Count);
    }

    [Fact]
    public void OwnerCanRemoveAMemberWithoutDeletingTheParty()
    {
        Guid owner = Guid.NewGuid();
        Guid member = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);
        PartyAccessToken token = parties.IssueAccessToken(party.PartyId, owner, member)!;
        Assert.True(parties.Join(party.PartyId, member, token.Value));

        Assert.True(parties.Leave(party.PartyId, member, owner));
        Assert.Equal(owner, Assert.Single(parties.Get(party.PartyId)!.Members).ProfileId);
    }

    [Fact]
    public void LastMemberLeavingDeletesTheParty()
    {
        Guid owner = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);

        Assert.True(parties.Leave(party.PartyId, owner, owner));
        Assert.Null(parties.Get(party.PartyId));
    }

    [Fact]
    public void ATokenForAnotherProfileIsRejected()
    {
        Guid owner = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);
        PartyAccessToken token = parties.IssueAccessToken(
            party.PartyId,
            owner,
            Guid.NewGuid())!;

        Assert.False(parties.Join(party.PartyId, Guid.NewGuid(), token.Value));
    }

    [Fact]
    public void DisconnectOnlyRemovesMembersWhoEnabledAutoRemove()
    {
        Guid owner = Guid.NewGuid();
        Guid member = Guid.NewGuid();
        PartySnapshot party = parties.Create(owner);
        PartyAccessToken token = parties.IssueAccessToken(party.PartyId, owner, member)!;
        Assert.True(parties.Join(party.PartyId, member, token.Value));

        parties.HandleDisconnect(member);
        Assert.Equal(2, parties.Get(party.PartyId)!.Members.Count);

        Assert.True(parties.SetAutoRemove(party.PartyId, member, true));
        parties.HandleDisconnect(member);
        Assert.Equal(owner, Assert.Single(parties.Get(party.PartyId)!.Members).ProfileId);
    }
}
