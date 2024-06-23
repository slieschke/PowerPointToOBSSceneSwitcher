# Changelog

## [3.0.1] (2024-04-27)

### Fixed

- PTZ camera priming has been fixed to work correctly with OBS scenes containing an embedded PTZ camera scene.

## [3.0.0] (2024-03-02)

### Changed

- **BREAKING**: SceneSwitcher now requires OBS 30.0.2 or later to run.

### Fixed

- When an embedded slideshow is displayed the slideshow commands of the parent slideshow are not incorrectly triggered when advancing through the embedded slideshow.

## [2.0.2] (2023-02-18)

### Fixed

- Video and audio is now set correctly when jumping forward slides in certain situations.

## [2.0.1] (2023-02-18)

### Fixed

- The expected video and audio scenes are now set when going back or jumping slides.
- "Flurl.Http.FlurlResponse" is no longer logged when changing PTZ scenes.

## [2.0.0] (2023-02-08)

### Added

- Automatic PTZ scene switching: priming PTZ cameras to avoid scene changes getting broadcast to livestreams is now handled automatically.
- The obs-websocket host, port and password can now be configured.

### Changed

- **BREAKING**: the `config.json` file format has changed:
  - PTZ HTTP-CGI command URLs are no longer duplicated across SceneSwitcher and OBS configurations, allowing only one OBS scene to be configured per PTZ camera.
  - Tally light configurations now reference an OBS scene instead of an OBS source.
- Whitespace is trimmed from command arguments.
- Any case can now be used for commands.

### Fixed

- Provided a useful error message on start failure when OBS Studio is not open.
- Navigating back through slides is now aware of `VIDEO` and `AUDIO` commands.

### Removed

- **BREAKING**: the `PTZ` command has been removed.

## [1.0.0] (2023-01-11)

Initial release.

[3.0.1]: https://github.com/slieschke/SceneSwitcher/compare/v3.0.0...v3.0.1
[3.0.0]: https://github.com/slieschke/SceneSwitcher/compare/v2.0.2...v3.0.0
[2.0.2]: https://github.com/slieschke/SceneSwitcher/compare/v2.0.1...v2.0.2
[2.0.1]: https://github.com/slieschke/SceneSwitcher/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/slieschke/SceneSwitcher/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/slieschke/SceneSwitcher/compare/8289a2d4...v1.0.0

<!-- markdownlint-configure-file { "MD024": { "siblings_only": true } } -->
