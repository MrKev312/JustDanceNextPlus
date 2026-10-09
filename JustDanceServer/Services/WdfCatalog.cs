namespace JustDanceServer.Services;

public sealed class WdfCatalog(IMapService mapService)
{
	private static readonly IReadOnlyDictionary<int, IReadOnlySet<string>> ClassicBaseMaps =
		Enumerable.Range(2016, 3).ToDictionary(
			gameYear => gameYear,
			gameYear => (IReadOnlySet<string>)new HashSet<string>(
				ClassicJduBaseCatalog.GetWdfMaps(gameYear),
				StringComparer.OrdinalIgnoreCase));

	public IReadOnlyList<string> GetMapNames(WdfRoomId room)
		=> [.. mapService.Maps.Values
			.Where(map => IsEligible(room, map))
			.Select(map => map.Song.MapName)];

	internal WdfQueueItem ValidateQueueItem(
		WdfRoomId room,
		IEnumerable<string> mapNames,
		string parameterName)
	{
		string[] candidates = [.. mapNames
			.Select(mapName => mapService.GetMap(mapName)
				?? throw new ArgumentException($"Unknown map '{mapName}'.", parameterName))
			.Select(map => IsEligible(room, map)
				? map.Song.MapName
				: throw new ArgumentException(
					$"Map '{map.Song.MapName}' is not eligible for WDF room '{room.Name}'.",
					parameterName))
			.Distinct(StringComparer.OrdinalIgnoreCase)];
		if (candidates.Length == 0)
			throw new ArgumentException("A WDF map set cannot be empty.", parameterName);

		return new WdfQueueItem(candidates);
	}

	private static bool IsEligible(WdfRoomId room, ServerMap map)
	{
		if (room.IsHappyHour)
			return JduCatalogSelection.IsClassicCompatible(map.Song);

		if (ClassicBaseMaps.TryGetValue(room.GameYear, out IReadOnlySet<string>? baseMaps))
			return baseMaps.Contains(map.Song.MapName);

		return map.Song.Tags.Contains(
			$"songpack_game{room.GameYear}",
			StringComparer.OrdinalIgnoreCase);
	}
}
