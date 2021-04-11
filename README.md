# SceneSwitcher
A .NET core based scene switcher that connects to OBS and changes scenes based on note metadata in PowerPoint. This requires [the obs-websocket server](https://github.com/Palakis/obs-websocket) to be installed and running in OBS.

This fork of <https://github.com/shanselman/PowerPointToOBSSceneSwitcher> has been customised and enhanced according to the needs of [Faith Lutheran Church Ottawa](https://faithottawa.ca)'s for livestreaming worship services.

## Usage
* Set a scene for a slide with 
```<language>
OBS:{Scene name as it appears in OBS}
```

Example:
```<language>
OBS:Scenename
```

* Set a default scene (used when a scene is not defined) with
```<language>
OBSDEF:{Scene name as it appears in OBS}
```

Example:
```<language>
OBSDEF:DefaultScene
```
