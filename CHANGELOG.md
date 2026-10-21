# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.0.1]

### Added
- Author metadata in `package.json`.
- Declared the Unity Commons dependency in `package.json`.

## [1.0.0]

### Added
- Initial release, extracted from Unity Commons: `CoroutineService` (with `ICoroutineService` and `CoroutineData`) for running coroutines detached from a specific `MonoBehaviour`. Compiles into the `Commons` assembly via an assembly definition reference, so it requires Unity Commons.
