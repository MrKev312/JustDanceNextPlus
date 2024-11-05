# JustDanceNextPlus

Just Dance Next Plus is a custom Just Dance server that allows you to play both official and custom Just Dance maps.

Currently, the server is still undergoing development and is **not yet ready for public use**.
However, I have made the source code available for anyone who is interested in contributing to the project.

## Features
- Play official Just Dance maps
- Play custom Just Dance maps
- Save your scores and view leaderboards for maps that have 3 or more scores submitted (quirk of the game)

## Installation
1. Clone the repository
2. Place the json files under wwwroot/jsons/ (you will need to dump these yourself for legal reasons, I will not provide them nor will I tell you how to get them)
3. Add maps under the wwwroot/maps/ folder
4. Run the server
5. Run a proxy or custom dns server to redirect your game to the server (you will need to disable ssl certification on the switch to do this, can be found [here](https://github.com/misson20000/exefs_patches), 18.0.0 and above [here](https://github.com/borntohonk/exefs_patches)
6. Enjoy!

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
	  <coach_file.bundle
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
where the SongInfo.json is in the following format:
```
{
  "songID": "guid here",
  "artist": "Artist here",
  "coachCount": 1,
  "coachNamesLocIds": [],
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
  "tagIds": [],
  "tags": [
	(Pick which are applicable)
    "Main",
    "jdplus",
	"songpack_year1",
	"songpack_year2",
	"songpack_year3",
    "Custom"
  ],
  "title": "song name here"
}
```

## Contributing
If you would like to contribute to the project, feel free to fork the repository and submit a pull request with your changes.

## License
This project is licensed under the GNU GPLv3 License - see the [LICENSE](LICENSE) file for details.
