using JustDanceNextPlus.JustDanceClasses.Endpoints;
using JustDanceNextPlus.Services;

namespace JustDanceNextPlus.Tests.Services;

public class PartyManagerTests
{
    private readonly PartyManager _partyManager;

    public PartyManagerTests()
    {
        _partyManager = new PartyManager();
    }

    [Fact]
    public void GenerateParty_CreatesPartyAndReturnsId()
    {
		// Arrange
		Guid profileId = Guid.NewGuid();

		// Act
		Guid partyId = _partyManager.GenerateParty(profileId);
		Party? party = _partyManager.GetParty(partyId);

        // Assert
        Assert.NotEqual(Guid.Empty, partyId);
        Assert.NotNull(party);
        Assert.Single(party.PartyMembers);
        Assert.Contains(profileId, party.PartyMembers);
    }

    [Fact]
    public void GetPartyByProfileId_FindsCorrectParty()
    {
		// Arrange
		Guid profileId1 = Guid.NewGuid();
		Guid profileId2 = Guid.NewGuid();
		Guid partyId = _partyManager.GenerateParty(profileId1);
        _partyManager.AddPartyMember(partyId, profileId2);

		// Act
		Party? foundParty = _partyManager.GetPartyByProfileId(profileId2);

        // Assert
        Assert.NotNull(foundParty);
        Assert.Equal(partyId, foundParty.PartyId);
    }

    [Fact]
    public void AddPartyMember_AddsMemberSuccessfully()
    {
		// Arrange
		Guid profileId1 = Guid.NewGuid();
		Guid profileId2 = Guid.NewGuid();
		Guid partyId = _partyManager.GenerateParty(profileId1);

		// Act
		bool result = _partyManager.AddPartyMember(partyId, profileId2);
		Party? party = _partyManager.GetParty(partyId);

        // Assert
        Assert.True(result);
        Assert.NotNull(party);
        Assert.Equal(2, party.PartyMembers.Count);
        Assert.Contains(profileId2, party.PartyMembers);
    }

    [Fact]
    public void RemovePartyMember_RemovesMemberButKeepsParty()
    {
		// Arrange
		Guid profileId1 = Guid.NewGuid();
		Guid profileId2 = Guid.NewGuid();
		Guid partyId = _partyManager.GenerateParty(profileId1);
        _partyManager.AddPartyMember(partyId, profileId2);

        // Act
        _partyManager.RemovePartyMember(partyId, profileId2);
		Party? party = _partyManager.GetParty(partyId);

        // Assert
        Assert.NotNull(party);
        Assert.Single(party.PartyMembers);
        Assert.DoesNotContain(profileId2, party.PartyMembers);
    }

    [Fact]
    public void RemovePartyMember_RemovesLastMemberAndDeletesParty()
    {
		// Arrange
		Guid profileId = Guid.NewGuid();
		Guid partyId = _partyManager.GenerateParty(profileId);

        // Act
        _partyManager.RemovePartyMember(partyId, profileId);
		Party? party = _partyManager.GetParty(partyId);

        // Assert
        Assert.Null(party);
    }

    [Fact]
    public void AddPartyMember_FailsForNonExistentParty()
    {
		// Arrange
		Guid nonExistentPartyId = Guid.NewGuid();
		Guid profileId = Guid.NewGuid();

		// Act
		bool result = _partyManager.AddPartyMember(nonExistentPartyId, profileId);

        // Assert
        Assert.False(result);
    }
}