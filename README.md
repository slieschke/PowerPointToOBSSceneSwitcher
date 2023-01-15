# SceneSwitcher

A .NET core based scene switcher that connects to OBS and changes scenes based on note metadata in PowerPoint. This requires [the obs-websocket server](https://github.com/Palakis/obs-websocket) to be installed and running in OBS.

This fork of <https://github.com/shanselman/PowerPointToOBSSceneSwitcher> has been customised and enhanced according to the needs of [Faith Lutheran Church Ottawa](https://faithottawa.ca)'s for livestreaming worship services.

## Commands

### AUDIO

Set the configured variable OBS audio sources to unmute. All other configured variable audio sources will be muted. Setting this to no value will mute all the variable audio sources.

```text
AUDIO:<comma separated list of OBS audio source names>
```

### OBS

Set the OBS scene to display for a slide with:

```text
OBS:<Scene name as it appears in OBS>
```

Set the OBS scene to display for a slide after a delay period. If a PTZ camera scene (per below) exists with the same name as the OBS scene, that PTZ camera scene will be switched immediately on slide transition. This primes the camera to its new position to avoid the camera movement appearing on the stream.

```text
OBS-LONG-DELAY:<Scene name as it appears in OBS>
OBS-SHORT-DELAY:<Scene name as it appears in OBS>
```

### PTZ

Change the PTZ camera scene with:

```text
PTZ:<PTZ scene name from config.json>
```

This command changes the scene the PTZ camera is set to by requesting a PTZ HTTP-CGI command URL. It can be used from a slide while OBS is not displaying a scene that uses the PTZ camera to prime the camera to a new scene. This avoids the PTZ camera scene change otherwise being broadcast to the livestream when switching to a OBS scene that uses the PTZ camera set to a different scene.

Running `SceneSwitcher.exe test` will suppress the PTZ HTTP-CGI URLs from being requested, for testing where the PTZ camera is not present on the local network.

### Configuration

SceneSwitcher is configured through a `config.json` file contained in the same directory as the executable. The following properties can be configured:

| Property               | Description                                                                                                                               |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| `longDelay`            | The delay period in milliseconds to wait when switching to an OBS scene with the OBS-LONG-DELAY command. Optional; defaults to `5000`.    |
| `shortDelay`           | The delay period in milliseconds to wait when switching to an OBS scene with the OBS-SHORT-DELAY command. Optional; defaults to `2000`.   |
| `ptzPresets`           | A mapping of PTZ scene names to PTZ HTTP-CGI command URLs that, when requested, will change the PTZ camera scene.                         |
| `tallyLights`          | A list of tally light configurations that can drive tally lights to indicate when a camera is live.                                       |
| `variableAudioSources` | A list of OBS audio sources that can be controlled using the AUDIO command.                                                               |

#### Tally light configuration

| Property     | Description                                                                 |
| ------------ | --------------------------------------------------------------------------- |
| `baseUrl`    | The base URL used to turn on/off the the tally light.                       |
| `brightness` | The brightness of the tally light when turned on, a number between 0 and 1. |
| `liveColor`  | The colour the tally light will display when turned on.                     |
| `obsSource`  | The OBS source that the tally light will be turned on for when active.      |

A tally light will be turned on using the above configuration by sending a request to `<baseUrl>?state=live&brightness=<brightness>&color=<liveColor>`, and turned off by sending a request to `<baseUrl>?state=off`.

#### Example

```json
{
  "longDelay": 7500,
  "shortDelay": 1000,
  "ptzPresets": {
    "Altar Full": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&1",
    "Altar Mid": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&3",
    "Altar Zoom": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&2",
    "Lectern": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&4"
  },
  "tallyLights": [
    {
      "baseUrl": "http://192.168.1.10:7413/set",
      "brightness": 1,
      "liveColor": "FF0000",
      "obsSource": "VBase PTZ"
    },
    {
      "baseUrl": "http://192.168.1.11:7413/set",
      "brightness": 0.05,
      "liveColor": "FF0000",
      "obsSource": "VBase DeskCam"
    }
  ]
}
```
