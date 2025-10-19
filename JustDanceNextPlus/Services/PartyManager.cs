using JustDanceNextPlus.JustDanceClasses.Endpoints;

using System.Collections.Concurrent;

namespace JustDanceNextPlus.Services;

public interface IPartyManager
{
	bool AddPartyMember(Guid partyId, Guid profileId);
	Guid GenerateParty(Guid profileId);
	Party? GetParty(Guid partyId);
	Party? GetPartyByProfileId(Guid profileId);
	void RemovePartyMember(Guid partyId, Guid profileId);
}

public class PartyManager : IPartyManager
{
	private readonly ConcurrentDictionary<Guid, Party> parties = new();

	public Party? GetParty(Guid partyId)
	{
		parties.TryGetValue(partyId, out Party? party);

		return party;
	}

	public Party? GetPartyByProfileId(Guid profileId)
	{
		return parties.Values.FirstOrDefault(party => party.PartyMembers.Contains(profileId));
	}

	public Guid GenerateParty(Guid profileId)
	{
		// Generate new guid for party id
		Guid partyId;
		do
		{
			partyId = Guid.NewGuid();
		}
		while (parties.ContainsKey(partyId));

		Party party = new()
		{
			PartyId = partyId,
			PartyMembers = { profileId }
		};

		parties[partyId] = party;

		return partyId;
	}

	public bool AddPartyMember(Guid partyId, Guid profileId)
	{
		Party? party = GetParty(partyId);

		if (party == null)
			return false;

		party.PartyMembers.Add(profileId);
		return true;
	}

	public void RemovePartyMember(Guid partyId, Guid profileId)
	{
		if (!parties.TryGetValue(partyId, out Party? party))
			return;

		party.PartyMembers.Remove(profileId);

		if (party.PartyMembers.Count != 0)
			return;

		parties.TryRemove(partyId, out _);
	}
}
