# Unity Commons — Services

Runtime services for Unity Commons. Currently `CoroutineService` (with `ICoroutineService` and `CoroutineData`) for running coroutines detached from a specific `MonoBehaviour`.

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
