# JustDanceNextPlus

Just Dance Next Plus is a custom Just Dance server that allows you to play both official and custom Just Dance maps.

Currently, the server is still undergoing development and is **not yet ready for public use**.
However, I have made the source code available for anyone who is interested in contributing to the project.

## ⚠️ **WARNING: USE AT YOUR OWN RISK!** ⚠️

**By choosing to use this tool, you accept that there are NO guarantees your save data will remain intact.**  
With every update, you risk **permanently losing ALL stored data**.  
**Proceed only if you're prepared for this possibility!**

## Features
- Play official Just Dance maps
- Play custom Just Dance maps
- Save your scores and view leaderboards for maps that have scores submitted from at least 1 other player (quirk of the game)

## Installation
1. Clone the repository
2. Place the json files under database/config/ (you will need to dump these yourself for legal reasons, I will not provide them nor will I tell you how to get them)
3. Add maps under the wwwroot/maps/ folder
4. Run the server
5. Run a proxy or custom dns server to redirect your game to the server (you will need to disable ssl certification on the switch to do this, can be found [here](https://github.com/misson20000/exefs_patches), if your system version is not supported you can use the script found [here](https://github.com/misson20000/exefs_patches/pull/41#issuecomment-2423585536)
6. Enjoy!

## Needed JSONs
- activity-page.json

The following are not needed but are recommended:
- tagdb.json
- localizedstrings.json (from the `All.US-en.*.json.gz` file)

## Bundles
Feel free to modify the bundles to your liking, but make sure to keep the same structure as the original bundles.

## Map Format
Maps are stored in the following format:
```
maps/
  <map_codename>/
	SongInfo.json
	Audio_opus/
	  <audio_file>.opus
	AudioPreview_opus/
	  <audio_preview_file>.opus
	CoachesLarge/
	  <coach_file>.bundle
	CoachesSmall/
	  <coach_file>.bundle
	Cover/
	  <cover_file>.bundle
	MapPackage/
	  <map_file>.bundle
	video/
	  <video_file>.webm (max of 4 versions)
	videoPreview/
	  <video_preview_file>.webm (max of 4 versions)
```
where all files except for the `SongInfo.json` are named after their md5 hash.
The following powershell script can automate this process, run it within your maps folder:
```powershell
Get-ChildItem -Path . -Recurse -Include *.webm,*.bundle,*.opus | ForEach-Object -Parallel {
    $hash = Get-FileHash $_.FullName -Algorithm MD5
    $dir = $_.DirectoryName
    $ext = $_.Extension
    $newName = "$($hash.Hash)$ext"
    Rename-Item -Path $_.FullName -NewName (Join-Path $dir $newName) -Force
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
  "hasCameraScoring": false,
  "lyricsColor": "#FFFFFFFF",
  "mapLength": 123.456,
  "mapName": "codename here",
  "originalJDVersion": 2020,
  "parentMapName": "codename here",
  "sweatDifficulty": 1,
  "tagIds": [], (either as a tag guid or as a string)
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
Playlists are stored in the following format:
```json
{
	"guid": "guid here",
	"playlistName": "codename here",
	"localizedTitle": "Title here",
	"localizedDescription": "Description here",
	"coverUrl": "URL to the cover bundle here",
	"coverDetailsUrl": "URL to the cover details bundle here",
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
If you have both a query and an itemList, the query will be appended to the itemList.

## Contributing
If you would like to contribute to the project, feel free to fork the repository and submit a pull request with your changes.

## License
This project is licensed under the GNU GPLv3 License - see the [LICENSE](LICENSE) file for details.

Ubisoft owns the rights to Just Dance and all related assets. This project is not affiliated with Ubisoft in any way.
