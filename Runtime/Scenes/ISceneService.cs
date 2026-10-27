using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Commons.Scenes
{
    /// <summary>
    /// Loads and unloads build-settings scenes identified by an enum whose values are the scenes' build indices.
    /// </summary>
    public interface ISceneService<TScene> : IService
        where TScene : struct, Enum
    {
        /// <summary>Scenes that replace each other: loading one unloads the rest.</summary>
        IReadOnlyCollection<TScene> SwappableScenes { get; }

        Scene LoadScene(
            TScene scene,
            LoadSceneMode mode = LoadSceneMode.Additive,
            bool setActive = true,
            Action beforeActivate = null,
            Action afterActivate = null
        );

        UniTask<Scene> LoadSceneAsync(
            TScene scene,
            IProgress<float> progress,
            CancellationToken ct,
            LoadSceneMode mode = LoadSceneMode.Additive,
            bool setActive = true,
            Action beforeActivate = null,
            Action afterActivate = null
        );

        UniTask UnloadSceneAsync(
            TScene scene,
            IProgress<float> progress,
            CancellationToken ct
        );

        bool TryInstantiateOnScene(
            TScene scene,
            GameObject template,
            out GameObject spawned
        );

        bool TryInstantiateOnScene(
            TScene scene,
            GameObject template,
            string name,
            out GameObject spawned
        );
    }
}
