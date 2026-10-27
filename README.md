# Unity Commons — Services

Runtime services for Unity Commons:

- `CoroutineService` (with `ICoroutineService` and `CoroutineData`) — runs coroutines detached from a specific `MonoBehaviour`.
- `SceneService<TScene>` (with `ISceneService<TScene>`, namespace `Commons.Scenes`) — build-settings scene loading keyed by an enum whose values are build indices. Concurrent async loads of the same scene share one operation, and *swappable* scenes replace each other: loading one unloads the others.

## Dependency

This package is **not standalone**. Its `Runtime/` carries a `.asmref` pointing at the `Commons` assembly, so its code compiles **into** Commons rather than into an assembly of its own. It therefore requires [Unity Commons](https://github.com/pzagora/Unity-Commons) (it implements `IService` from Commons), and consumers reach these types through the `Commons` assembly (namespace `Commons`) — no separate assembly reference is needed.

## Installation

Add both packages to `Packages/manifest.json`:

```json
"com.zagoradev.unity.commons": "https://github.com/pzagora/Unity-Commons.git",
"com.zagoradev.unity.commons.services": "https://github.com/pzagora/Unity-Commons.Services.git"
```

Requires Unity `6000.0+`.

## Usage

```csharp
using Commons;

ICoroutineService coroutines = /* resolved CoroutineService */;
coroutines.Run(MyRoutine());
```

### Scenes

Subclass once per project to bind the scene enum and the swappable set:

```csharp
using Commons.Scenes;

public interface ISceneService : ISceneService<Scene> { }

public sealed class SceneService : SceneService<Scene>, ISceneService
{
    public override IReadOnlyCollection<Scene> SwappableScenes { get; } = new[] { Scene.Landing, Scene.Meta, Scene.Match };
}

await sceneService.LoadSceneAsync(Scene.Meta, progress, ct);
```

Messages go through `CommonsLog` (Unity Reporter when present); override `Log` to redirect them.
