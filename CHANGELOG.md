# Changelog

All notable changes to this package are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this package adheres to Semantic Versioning.

## [1.1.0]

### Added
- `SceneService<TScene>` / `ISceneService<TScene>` (namespace `Commons.Scenes`) — build-settings scene loading keyed by an enum whose values are build indices. Concurrent async loads of one scene share an operation; scenes passed as *swappable* replace each other (loading one unloads the others). Projects subclass it to bind their enum and swappable set. Logs through `CommonsLog` (requires Unity Commons 2.1.0+); override `Log` to redirect.

## [1.0.1]

### Added
- Author metadata in `package.json`.
- Declared the Unity Commons dependency in `package.json`.

## [1.0.0]

### Added
- Initial release, extracted from Unity Commons: `CoroutineService` (with `ICoroutineService` and `CoroutineData`) for running coroutines detached from a specific `MonoBehaviour`. Compiles into the `Commons` assembly via an assembly definition reference, so it requires Unity Commons.
