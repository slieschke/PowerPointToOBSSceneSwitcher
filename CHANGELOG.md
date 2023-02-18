# Changelog

## Unreleased

### Fixed

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

[2.0.0]: https://github.com/slieschke/SceneSwitcher/compare/1.0.0...2.0.0
[1.0.0]: https://github.com/slieschke/SceneSwitcher/compare/8289a2d4...1.0.0

<!-- markdownlint-configure-file { "MD024": { "siblings_only": true } } -->
