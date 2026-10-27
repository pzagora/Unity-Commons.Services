using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Commons.Scenes
{
    /// <summary>
    /// Build-settings scene loading keyed by an enum whose values are the scenes' build indices. Concurrent async loads
    /// of the same scene share one operation, and the <see cref="SwappableScenes"/> replace each other: loading one
    /// unloads the others. Projects subclass it to bind their enum and swappable set. Messages go through
    /// <see cref="CommonsLog"/> (Reporter when present, otherwise the Unity console); override <see cref="Log"/> to redirect them.
    /// </summary>
    public class SceneService<TScene> : ISceneService<TScene>
        where TScene : struct, Enum
    {
        private const string LOADED_SCENE_FORMAT = "Loaded scene: {0}";
        private const string SET_ACTIVE_SCENE_FORMAT = "Set active scene: {0}";
        private const string UNLOADED_SCENE_FORMAT = "Unloaded scene: {0}";
        private const string SCENE_LOAD_LOG_FORMAT = "Requested scene: {0}\nParams: Mode: {1} | Active: {2}";
        private const string SCENE_LOAD_ASYNC_LOG_FORMAT = "Requested scene (async): {0}\nParams: Mode: {1} | Active: {2}";
        private const string NULL_LOAD_OPERATION_FORMAT = "LoadSceneAsync returned null for scene {0} (buildIndex {1}). Is it in Build Settings?";

        private const float SCENE_READY_THRESHOLD = 0.9f;
        private const float SCENE_COMPLETE_PROGRESS = 1f;

        private readonly Dictionary<TScene, UniTaskCompletionSource<Scene>> _inFlightLoads = new();
        private readonly object _gate = new();
        private readonly HashSet<TScene> _swappableScenes;

        public SceneService(IEnumerable<TScene> swappableScenes = null)
        {
            _swappableScenes = swappableScenes != null
                ? new HashSet<TScene>(swappableScenes)
                : new HashSet<TScene>();
        }

        public IReadOnlyCollection<TScene> SwappableScenes => _swappableScenes;

        #region Public Methods

        public Scene LoadScene(
            TScene scene,
            LoadSceneMode mode = LoadSceneMode.Additive,
            bool setActive = true,
            Action beforeActivate = null,
            Action afterActivate = null)
        {
            var sceneId = ToBuildIndex(scene);

            Log(string.Format(SCENE_LOAD_LOG_FORMAT, scene, mode, setActive));

            beforeActivate?.Invoke();

            SceneManager.LoadScene(sceneId, mode);
            var loadedScene = SceneManager.GetSceneByBuildIndex(sceneId);

            if (setActive)
                SetSceneActive(loadedScene);

            afterActivate?.Invoke();

            Log(string.Format(LOADED_SCENE_FORMAT, scene));
            return loadedScene;
        }

        public UniTask<Scene> LoadSceneAsync(
            TScene scene,
            IProgress<float> progress,
            CancellationToken ct,
            LoadSceneMode mode = LoadSceneMode.Additive,
            bool setActive = true,
            Action beforeActivate = null,
            Action afterActivate = null)
        {
            UniTaskCompletionSource<Scene> tcs;

            lock (_gate)
            {
                if (_inFlightLoads.TryGetValue(scene, out tcs))
                    return tcs.Task;

                tcs = new UniTaskCompletionSource<Scene>();
                _inFlightLoads[scene] = tcs;
            }

            LoadSceneAsyncInternal(scene, progress, ct, mode, setActive, beforeActivate, afterActivate, tcs)
                .Forget(ex => tcs.TrySetException(ex));

            return tcs.Task;
        }

        public async UniTask UnloadSceneAsync(TScene scene, IProgress<float> progress, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            UniTaskCompletionSource<Scene> inFlightTcs;
            lock (_gate)
            {
                _inFlightLoads.TryGetValue(scene, out inFlightTcs);
            }

            if (inFlightTcs != null)
                await inFlightTcs.Task.AttachExternalCancellation(ct);

            var unityScene = SceneManager.GetSceneByBuildIndex(ToBuildIndex(scene));
            if (!unityScene.isLoaded)
                return;

            var op = SceneManager.UnloadSceneAsync(unityScene);
            if (op == null)
                return;

            await op.ToUniTask(cancellationToken: ct);

            Log(string.Format(UNLOADED_SCENE_FORMAT, scene));
        }

        public bool TryInstantiateOnScene(TScene scene, GameObject template, out GameObject spawned)
            => TryInstantiateOnScene(scene, template, null, out spawned);

        public bool TryInstantiateOnScene(TScene scene, GameObject template, string name, out GameObject spawned)
        {
            spawned = null;

            var selectedScene = SceneManager.GetSceneByBuildIndex(ToBuildIndex(scene));
            if (!selectedScene.isLoaded)
                return false;

            spawned = Object.Instantiate(template);
            spawned.name = string.IsNullOrEmpty(name) ? template.name : name;
            SceneManager.MoveGameObjectToScene(spawned, selectedScene);

            return true;
        }

        #endregion

        #region Protected Methods

        protected virtual void Log(string message)
            => CommonsLog.Event(GetType().Name, message);

        #endregion

        #region Async Load Internals

        private async UniTask LoadSceneAsyncInternal(
            TScene scene,
            IProgress<float> progress,
            CancellationToken ct,
            LoadSceneMode mode,
            bool setActive,
            Action beforeActivate,
            Action afterActivate,
            UniTaskCompletionSource<Scene> tcs)
        {
            try
            {
                Log(string.Format(SCENE_LOAD_ASYNC_LOG_FORMAT, scene, mode, setActive));

                var sceneId = ToBuildIndex(scene);
                var selectedScene = SceneManager.GetSceneByBuildIndex(sceneId);

                if (selectedScene.isLoaded)
                {
                    var alreadyLoaded = await HandleAlreadyLoadedAsync(scene, selectedScene, progress, ct, setActive);
                    tcs.TrySetResult(alreadyLoaded);
                    return;
                }

                await UnloadSwappableScenesIfNeededAsync(scene, progress, ct);

                var op = BeginLoad(sceneId, mode);
                if (op == null)
                    throw new InvalidOperationException(string.Format(NULL_LOAD_OPERATION_FORMAT, scene, sceneId));

                try
                {
                    await WaitUntilReadyAsync(op, progress, ct);

                    beforeActivate?.Invoke();

                    await ActivateAndWaitDoneAsync(op, progress, ct);
                }
                catch (OperationCanceledException)
                {
                    op.allowSceneActivation = true;
                    throw;
                }

                selectedScene = SceneManager.GetSceneByBuildIndex(sceneId);
                FinalizeLoadedScene(scene, selectedScene, setActive, afterActivate);

                tcs.TrySetResult(selectedScene);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                lock (_gate)
                    _inFlightLoads.Remove(scene);
            }
        }

        #endregion

        #region Private Methods

        private static int ToBuildIndex(TScene scene)
            => Convert.ToInt32(scene);

        private static TScene FromBuildIndex(int buildIndex)
            => (TScene)Enum.ToObject(typeof(TScene), buildIndex);

        private void SetSceneActive(Scene scene)
        {
            SceneManager.SetActiveScene(scene);
            Log(string.Format(SET_ACTIVE_SCENE_FORMAT, FromBuildIndex(scene.buildIndex)));
        }

        private async UniTask<Scene> HandleAlreadyLoadedAsync(
            TScene requestedScene,
            Scene loadedScene,
            IProgress<float> progress,
            CancellationToken ct,
            bool setActive)
        {
            ct.ThrowIfCancellationRequested();

            if (setActive)
                SetSceneActive(loadedScene);

            await UnloadSwappableScenesIfNeededAsync(requestedScene, progress, ct);

            progress?.Report(SCENE_COMPLETE_PROGRESS);
            return loadedScene;
        }

        private async UniTask UnloadSwappableScenesIfNeededAsync(
            TScene requestedScene,
            IProgress<float> progress,
            CancellationToken ct)
        {
            if (!_swappableScenes.Contains(requestedScene))
                return;

            foreach (var scene in _swappableScenes)
            {
                if (EqualityComparer<TScene>.Default.Equals(scene, requestedScene))
                    continue;

                await UnloadSceneAsync(scene, progress, ct);
            }
        }

        private static AsyncOperation BeginLoad(int sceneId, LoadSceneMode mode)
        {
            var op = SceneManager.LoadSceneAsync(sceneId, mode);
            if (op != null)
                op.allowSceneActivation = false;
            return op;
        }

        private static async UniTask WaitUntilReadyAsync(AsyncOperation op, IProgress<float> progress, CancellationToken ct)
        {
            while (op.progress < SCENE_READY_THRESHOLD)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(op.progress);
                await UniTask.Yield();
            }
        }

        private static async UniTask ActivateAndWaitDoneAsync(AsyncOperation op, IProgress<float> progress, CancellationToken ct)
        {
            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(SCENE_COMPLETE_PROGRESS);
                await UniTask.Yield();
            }
        }

        private void FinalizeLoadedScene(TScene scene, Scene selectedScene, bool setActive, Action afterActivate)
        {
            if (setActive)
                SetSceneActive(selectedScene);

            afterActivate?.Invoke();

            Log(string.Format(LOADED_SCENE_FORMAT, scene));
        }

        #endregion
    }
}
