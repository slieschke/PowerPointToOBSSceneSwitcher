# SceneSwitcher

A .NET core based scene switcher that connects to OBS Studio and changes scenes based on note metadata in PowerPoint.

This fork of <https://github.com/shanselman/PowerPointToOBSSceneSwitcher> has been customised and enhanced according to [Faith Lutheran Church Ottawa]'s needs for livestreaming worship services.

## Version Requirements

- [OBS Studio] 30.0.2

## Commands

All command names are case insensitive.

### AUDIO

Set the configured variable OBS audio sources to unmute. All other configured variable audio sources will be muted. Setting this to no value will mute all the variable audio sources.

```text
AUDIO: <comma separated list of OBS audio source names>
```

### VIDEO

Set the video to display for a slide with:

```text
VIDEO: <scene name>
```

If the scene is a PTZ scene from the configuration, the PTZ scene will be activated, and OBS will be switched to the OBS scene that the PTZ scene is configured with.

### VIDEO-LONG-DELAY, VIDEO-SHORT-DELAY

Set the OBS scene to display for a slide after a delay period. This allows a title from the slideshow to be broadcast to the livestream, then switched to a live camera from a single slide.

```text
VIDEO-LONG-DELAY: <scene name>
VIDEO-SHORT-DELAY: <scene name>
```

## Automatic PTZ Scene Priming

Whenever the video scene displayed is changed, the next immediate video scene is identified. If that video scene is a PTZ camera, and the current video scene isn't using the same PTZ camera, the PTZ scene will be primed at this point to otherwise avoid the PTZ scene change otherwise being broadcast to the livestream.

## Local Testing

Running `SceneSwitcher.exe skipAllRequests` will suppress the PTZ HTTP-CGI command and tally light URLs from being requested, allowing testing without emitting HTTP request errors to the console when no PTZ cameras and tally lights are present on the local network.

## Configuration

SceneSwitcher is configured through a `config.json` file contained in the same directory as the executable. The following properties can be configured:

| Property               | Description                                                                                                                                                                                                                                                                                          |
| ---------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `longDelay`            | The delay period in milliseconds to wait when switching to an OBS scene with the VIDEO-LONG-DELAY command. Optional; defaults to `5000`.                                                                                                                                                             |
| `shortDelay`           | The delay period in milliseconds to wait when switching to an OBS scene with the VIDEO-SHORT-DELAY command. Optional; defaults to `2000`.                                                                                                                                                            |
| `ptzScenes`            | A mapping of OBS scene names that correspond to PTZ cameras to configuration that describes that camera's scenes. Each configuration is a mapping of PTZ scene names (that can be used with the `VIDEO*` commands) to the PTZ HTTP-CGI command URL that will switch to that scene on the PTZ camera. |
| `tallyLights`          | A list of [tally light configuration]s that can drive tally lights to indicate when a camera is live.                                                                                                                                                                                                |
| `variableAudioSources` | A list of OBS audio sources that can be controlled using the AUDIO command.                                                                                                                                                                                                                          |
| `webSocketConfig`      | The [obs-websocket configuration]. Optional, will use the defaults documented in the web socket configuration.                                                                                                                                                                                       |

### Tally light Configuration

| Property     | Description                                                                 |
| ------------ | --------------------------------------------------------------------------- |
| `baseUrl`    | The base URL used to turn on/off the the tally light.                       |
| `brightness` | The brightness of the tally light when turned on, a number between 0 and 1. |
| `liveColor`  | The colour the tally light will display when turned on.                     |
| `obsScene`   | The OBS scene that the tally light will be turned on for when active.       |

A tally light will be turned on using the above configuration by sending a request to `<baseUrl>?state=live&brightness=<brightness>&color=<liveColor>`, and turned off by sending a request to `<baseUrl>?state=off`.

### obs-websocket configuration

| Property   | Description                                                                              |
| ---------- | ---------------------------------------------------------------------------------------- |
| `host`     | The host running OBS Studio. Optional, defaults to `127.0.0.1`.                          |
| `port`     | The server port configured for obs-websocket. Optional, a number that defaults to `4444` |
| `password` | The password configured for obs-websocket. Optional, defaults to an empty string.        |

### Example

```json
{
  "longDelay": 7500,
  "shortDelay": 1000,
  "ptzScenes": {
    "PTZ": {
      "Altar Full": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&1",
      "Altar Mid": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&3",
      "Altar Zoom": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&2",
      "Lectern": "http://192.168.1.8/cgi-bin/ptzctrl.cgi?ptzcmd&poscall&4"
    }
  },
  "tallyLights": [
    {
      "baseUrl": "http://192.168.1.10:7413/set",
      "brightness": 1,
      "liveColor": "FF0000",
      "obsSource": "PTZ"
    },
    {
      "baseUrl": "http://192.168.1.11:7413/set",
      "brightness": 0.05,
      "liveColor": "FF0000",
      "obsScene": "DeskCam"
    }
  ],
  "webSocketConfig": {
    "port": 4444,
    "password": "53cr375qu1rr3l"
  }
}
```

[faith lutheran church ottawa]: https://faithottawa.ca
[obs studio]: https://obsproject.com/
[obs-websocket configuration]: #obs-websocket-configuration
[tally light configuration]: #tally-light-configuration
