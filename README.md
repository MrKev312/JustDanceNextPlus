# JustDanceServer

JustDanceServer is a custom Just Dance server that allows you to play both official and custom Just Dance maps.

Currently, the server is still undergoing development and is **not yet ready for public use**.
However, I have made the source code available for anyone who is interested in contributing to the project.

## ⚠️ **WARNING: USE AT YOUR OWN RISK!** ⚠️

**By choosing to use this tool, you accept that there are NO guarantees your save data will remain intact.**  
With every update, you risk **permanently losing ALL stored data**.  
**Proceed only if you're prepared for this possibility!**

## Features

- Play official Just Dance maps
- Play custom Just Dance maps
- Save your scores and view leaderboards (some game clients may only display them once another player has submitted a score)

## Installation

1. Clone the repository
2. Place the missing JSON files listed below under `JustDanceServer/database/config/` (you will need to dump these yourself for legal reasons, I will not provide them nor will I tell you how to get them)
3. Add maps under the `JustDanceServer/wwwroot/maps/` folder using the format below
4. Run the server
5. Run a proxy or custom dns server to redirect your game to the server (you will need to disable ssl certification on the switch to do this, can be found [here](https://github.com/misson20000/exefs_patches), if your system version is not supported you can use the script found [here](https://github.com/misson20000/exefs_patches/pull/41#issuecomment-2423585536))
6. Enjoy!

## Needed JSONs

Both files must be present under `JustDanceServer/database/config/` for the server to start:

- `activity-page.json` (you must provide this)
- `JustDanceEditions.json` (included in the repository)

The following are not needed but are recommended:

- `tagdb.json`
- `localizedstrings.json` (from the `All.US-en.*.json.gz` file)

## Bundles

JDNext bundles are stored under `JustDanceServer/wwwroot/JDNext/<platform>/`, where `<platform>` is `NX`, `PC`, `PS5`, or `XboxScarlett`. Keep the folder and filename casing as shown, especially when hosting on Linux.

Playlist covers use `<assetBundleName>.bundle` and `<assetBundleName>_details.bundle` in each platform folder. If no `assetBundleName` is set, the playlist name is used instead.

Feel free to modify the bundles to your liking, but make sure to keep the same structure as the original bundles.

## Map Format

Maps are stored in the following format:

```
maps/
  <map_codename>/
    SongInfo.json
    Video/
      vp8/
        <video_file>.webm
      vp9/
        <video_file>.webm
    JDPlus/
      Audio/
        <audio_file>.opus
      Preview/
        <audio_preview_file>.opus
        vp8/
          <video_preview_file>.webm
        vp9/
          <video_preview_file>.webm
      Assets/
        <platform>/
          CoachesLarge/
            <coach_file>.bundle
          CoachesSmall/
            <coach_file>.bundle
          Cover/
            <cover_file>.bundle
          MapPackage/
            <map_file>.bundle
    JDUnlimited/
      Audio/
        <audio_file>.ogg
      Preview/
        <audio_preview_file>.ogg
        vp8/
          <video_preview_file>.webm
        vp9/
          <video_preview_file>.webm
      Assets/
        <platform>/
          AlbumBackground/
            <image_file>.ckd
          AlbumCoach/
            <image_file>.ckd
          BannerBackground/
            <image_file>.ckd
          MapBackground/
            <image_file>.ckd
          Cover/
            <image_file>.ckd
          CoverSmall/
            <image_file>.ckd
          Coach1/
            <coach_file>.ckd
          Coach2/ ... Coach<coachCount>/ (for maps with more than one coach)
          MainScene/
            <map_file>.zip
```

`SongInfo.json` and the full videos under `Video/` are shared between JDNext and JDU. Audio-preview timing and camera-scoring metadata are read once from an available `JDPlus/Assets/<platform>/MapPackage` bundle and shared as well.

Each map must contain both the `JDPlus` and `JDUnlimited` media layouts, including VP8 and VP9 previews and full videos. Each video folder must contain between one and four WEBM files, ordered by bitrate by the server. If fewer than four are supplied, the highest-bitrate file is reused for the remaining quality slots.

JDPlus platform folder names are `NX`, `PC`, `PS5`, and `XboxScarlett`. JDU platform folder names are `NX`, `PC`, `WIIU`, `ORBIS`, `DURANGO`, `PROSPERO`, `SCARLETT`, and `GGP`. Supply the assets for the clients you intend to serve; assets are not converted between platforms. Every asset-role folder must contain exactly one file, and JDU requires a `Coach1` through `Coach<coachCount>` image for each map. Its map package is an MD5-named ZIP under `MainScene/`.

Incomplete maps intentionally stop startup, and missing assets for a requested platform cause that catalog request to fail rather than silently omit the map. Keep both media layouts complete and preserve the folder casing shown above.

The media and asset files shown above are named after their MD5 hash; `SongInfo.json` keeps its name. The following PowerShell 7 script hashes the relevant file types. Run it only within your maps folder, and back up your files first. It renames files on disk, not the contents of bundles or ZIP archives.

```powershell
Get-ChildItem -LiteralPath . -File -Recurse -Include *.webm,*.bundle,*.opus,*.ogg,*.ckd,*.zip | ForEach-Object -Parallel {
    $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm MD5 -ErrorAction Stop
    $ext = $_.Extension
    $newName = "$($hash.Hash.ToLowerInvariant())$ext"
    if ($_.Name -ne $newName) {
        Rename-Item -LiteralPath $_.FullName -NewName $newName -ErrorAction Stop
    }
}
```

And where the SongInfo.json is in the following format:

```json
{
  "songID": "guid here",
  "artist": "Artist here",
  "coachCount": 1,
  "coachNamesLocIds": [], (names of the coaches from left to right, either as oasisId or as a string)
  "credits": "Credits here",
  "danceVersionLocId": 0,
  "difficulty": 1,
  "doubleScoringType": null,
  "lyricsColor": "#FFFFFFFF",
  "mapLength": 123.456,
  "mapName": "codename here",
  "originalJDVersion": 2020,
  "parentMapName": "codename here",
  "sweatDifficulty": 1,
  "tagIds": [], (either a string GUID or a string in the form of "{category}:{tagname}")
  "tags": [
	(Pick which are applicable)
	"Main",
	"jdplus",
	"songpack_game1",
	"songpack_game..",
	"songpack_game2022",
	"songpack_year1",
	"songpack_year2",
	"songpack_year3",
	"Custom"
  ],
  "title": "song name here"
}
```

## Playlist Format

Playlists are stored under `JustDanceServer/database/playlists/` in the following format:

```json
{
	"guid": "guid here",
	"playlistName": "codename here", (defaults to the filename without .json if omitted or empty)
	"assetBundleName": "recoforyou", (optional; defaults to playlistName)
	"localizedTitle": "Title here",
	"localizedDescription": "Description here",
	(Optional field)
	"tags": [ 
		"tag here"
	],
	(Optional field)
	"itemList": [
		"map codenames or map guids here"
	]
	(Optional query fields with example, you can query on anything from SongInfo)
	"query": "Artist.Contains(\"Lady Gaga\") && !Tags.Contains(\"Alternate\")"
	"orderBy": "OriginalJDVersion, MapName"
}
```

JDNext cover URLs are generated from the configured CDN, the client's platform, and `assetBundleName`; `coverUrl` and `coverDetailsUrl` do not override those generated URLs. The provided JD2019–JD2022 playlists use `recoforyou`, which selects `recoforyou.bundle` and `recoforyou_details.bundle`.

If you have both a query and an itemList, the query results are appended to the itemList. `orderBy`, when supplied, sorts the combined list. Duplicate map IDs are removed, and playlists with fewer than two entries after deduplication are skipped.

## Contributing

If you would like to contribute to the project, feel free to fork the repository and submit a pull request with your changes.

## License

This project is licensed under the GNU GPLv3 License - see the [LICENSE](LICENSE.txt) file for details.

Ubisoft owns the rights to Just Dance and all related assets. This project is not affiliated with Ubisoft in any way.
