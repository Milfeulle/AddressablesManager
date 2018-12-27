using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.SceneManagement;

namespace AddressablesManagement
{
    public class AddressablesManager : MonoBehaviour
    {
        private static AddressablesManager _instance;
        private readonly object _opLock = new object();

        private const long MAX_TIMEOUT = 10000;
        private const int TASK_DELAY = 1;

        List<object> _objectsToLoadByLabel = new List<object>(100);
        GameObject _gameObjectToLoad;
        Scene _sceneToLoad;
        object _currentlyLoadingObject;
        bool _loadingDependencies;
        bool _loadingObjectsByLabel;
        bool _loadingScene;
        bool _unloadingScene;
        private bool _loadingGameObject;
        private int _currentTaskId = 0;

        private ConcurrentObjectPool<ICancellableTask> taskPool = new ConcurrentObjectPool<ICancellableTask>();

        #region PROPERTIES
        /// <summary>
        /// Reports whether there's an object currently loading or not.
        /// </summary>
        public bool LoadingGameObject
        {
            get { return _loadingGameObject; }
            set { _loadingGameObject = value; }
        }

        /// <summary>
        /// Instance object of this class
        /// </summary>
        public static AddressablesManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    Create();
                }

                return _instance;
            }
        }
        #endregion

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }

        //private void Update()
        //{
        //    Debug.Log("current task id = " + _currentTaskId);
        //    Debug.Log("current task count = " + _tasks.Count);
        //}

        #region UTILITY         
        private void Wait(int delay)
        {
            //if (_tasks[taskID].token.IsCancellationRequested)
            //{
            //    _tasks[taskID].token.ThrowIfCancellationRequested();
            //}

            Task.Delay(delay);
        }

        private async Task RunTask(Task task)
        {
            CancellableTask cancellableTask = new CancellableTask(task, task.Id);
            taskPool.Release(cancellableTask);

            try
            {
                await Task.Run(() => cancellableTask.task, cancellableTask.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogFormat("Task {0} cancelled.", cancellableTask.ID);
            }

            //taskPool.Get();
            cancellableTask.tokenSource.Dispose();
        }

        private async Task<T> RunTask<T>(Task<T> task)
        {
            T obj = default(T);

            CancellableTask<T> cancellableTask = new CancellableTask<T>(task, task.Id);
            taskPool.Release(cancellableTask);

            try
            {
                obj = await Task.Run(() => cancellableTask.task, cancellableTask.Token);
            }
            catch (OperationCanceledException)
            {
                Debug.LogFormat("Task {0} cancelled.", cancellableTask.ID);
            }

            //taskPool.Get();
            cancellableTask.tokenSource.Dispose();

            return obj;
        }
        #endregion

        #region TASKS
        /// <summary>
        /// Waits until the gameobject is ready.
        /// </summary>
        /// <returns>Returns a gameobject when it's been loaded.</returns>
        private GameObject WaitForGameObject(int taskID)
        {
            lock (_opLock)
            {
                //init
                _loadingGameObject = true;
                GameObject GO = null;

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                //execute
                while (_gameObjectToLoad == null && sw.ElapsedMilliseconds < MAX_TIMEOUT)
                {
                    Wait(TASK_DELAY);
                }

                GO = _gameObjectToLoad;
                _gameObjectToLoad = null;

                sw.Stop();
                //Debug.Log("Time elapsed loading gameobject = " + sw.ElapsedMilliseconds / 1000f);

                _loadingGameObject = false;
                return GO;
            }
        }

        /// <summary>
        /// Tries to load the given object into memory.
        /// </summary>
        /// <typeparam name="T">Type of the object to load.</typeparam>
        /// <returns>Returns an object of type T.</returns>
        private T TryGetObject<T>(int taskID) where T : class
        {
            lock (_opLock)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                T obj = null;
                while (_currentlyLoadingObject == null && sw.ElapsedMilliseconds < MAX_TIMEOUT)
                {
                    Wait(TASK_DELAY);
                }

                if (_currentlyLoadingObject is T)
                    obj = _currentlyLoadingObject as T;

                if (_currentlyLoadingObject == null)
                    Addressables.ReleaseAsset(_currentlyLoadingObject);
                _currentlyLoadingObject = null;

                sw.Stop();
                return obj;
            }
        }

        /// <summary>
        /// Waits until dependencies from object are loaded in or the operation times out.
        /// </summary>
        private void TryLoadDependencies(int taskID)
        {
            lock (_opLock)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                while (_loadingDependencies == true && sw.ElapsedMilliseconds < MAX_TIMEOUT)
                {
                    Wait(TASK_DELAY);
                }

                _loadingDependencies = false;

                sw.Stop();
                //Debug.Log("Time elapsed preloading dependencies = " + sw.ElapsedMilliseconds / 1000f);
            }
        }

        /// <summary>
        /// Loads all the assets inside a given label.
        /// </summary>
        /// <typeparam name="T">Type of the objects to load.</typeparam>
        /// <returns>Returns a list with all the assets of type T with the given label.</returns>
        private List<T> LoadByLabel<T>(int taskID) where T : class
        {
            lock (_opLock)
            {
                List<T> _objectsList = new List<T>(_objectsToLoadByLabel.Capacity);

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                while ((_objectsToLoadByLabel == null || _objectsToLoadByLabel.Count == 0) && sw.ElapsedMilliseconds < MAX_TIMEOUT)
                {
                    Wait(TASK_DELAY);
                }

                foreach (object item in _objectsToLoadByLabel)
                {
                    _objectsList.Add((T)item);
                }

                _objectsToLoadByLabel.Clear();
                _objectsToLoadByLabel = new List<object>(100);

                sw.Stop();
                //Debug.Log("Time elapsed loading object = " + sw.ElapsedMilliseconds / 1000f);

                return _objectsList;
            }
        }


        /// <summary>
        /// Waits until the given scene is unloaded.
        /// </summary>
        private void WaitForUnloadedScene(int taskID)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (_unloadingScene && sw.ElapsedMilliseconds < MAX_TIMEOUT)
            {
                Wait(TASK_DELAY);
            }

            sw.Stop();
        }

        /// <summary>
        /// Waits until the new scene is loaded in.
        /// </summary>
        /// <returns>Returns the newly loaded scene.</returns>
        private Scene GetLoadedScene(int taskID)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            Scene obtainedScene;
            while (_loadingScene && sw.ElapsedMilliseconds < MAX_TIMEOUT)
            {
                Wait(TASK_DELAY);
            }
            obtainedScene = _sceneToLoad;
            _sceneToLoad = default(Scene);
            Debug.Log("Scene  loaded in " + sw.ElapsedMilliseconds + " milliseconds");

            sw.Stop();
            return obtainedScene;
        }

        /// <summary>
        /// Waits until the new scene is loaded in.
        /// </summary>
        private void WaitForLoadedScene(int taskID)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            while (_loadingScene && sw.ElapsedMilliseconds < MAX_TIMEOUT)
            {
                Wait(TASK_DELAY);
            }

            sw.Stop();
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Loads a given scene either in additive or single mode to the current scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        public async Task<Scene> LoadScene(string sceneName, LoadSceneMode loadMode)
        {
            // Coroutine
            StartCoroutine(TryLoadScene(sceneName, loadMode));
            _loadingScene = true;

            // Thread
            Scene newScene;
            newScene = await RunTask(Task.Run(() => GetLoadedScene(_currentTaskId++)));
            return newScene;
        }

        /// <summary>
        /// Loads a given scene either in additive or single mode to the current scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        public async Task OnlyLoadScene(string sceneName, LoadSceneMode loadMode)
        {
            _loadingScene = true;

            StartCoroutine(TryLoadScene(sceneName, loadMode));
            await RunTask(Task.Run(() => WaitForLoadedScene(_currentTaskId)));
        }

        /// <summary>
        /// Unloads a given scene from memory asynchronously.
        /// </summary>
        /// <param name="scene">Scene object to unload.</param>
        public async Task UnloadScene(Scene scene)
        {
            _unloadingScene = true;

            StartCoroutine(TryUnloadScene(scene));
            await RunTask(Task.Run(() => WaitForUnloadedScene(_currentTaskId)));
        }

        /// <summary>
        /// Instantiates a gameobject in a given position and rotation in the world.
        /// </summary>
        /// <param name="path">Project path where the gameobject resides.</param>
        /// <param name="position">Position in the world to instantiate the gameobject.</param>
        /// <param name="rotation">Rotation to instantiate the gameobject.</param>
        /// <returns>Returns the gameobject set to instantiate.</returns>
        public async Task<GameObject> InstantiateGameObject(string path, Vector3 position, Quaternion rotation)
        {
            // Coroutine
            StartCoroutine(TryInstantiateObject(path, position, rotation));

            // Thread
            GameObject GO = null;
            GO = await RunTask(Task.Run(() => WaitForGameObject(_currentTaskId)));
            return GO;
        }

        /// <summary>
        /// Instantiates a gameobject in a default position and rotation in the world.
        /// </summary>
        /// <param name="path">Project path where the gameobject resides.</param>
        /// <returns>Returns the gameobject set to instantiate.</returns>
        public async Task<GameObject> InstantiateGameObject(string path)
        {
            // Coroutine
            StartCoroutine(TryInstantiateObject(path, Vector3.zero, Quaternion.identity));

            // Thread
            GameObject GO = null;
            GO = await RunTask(Task.Run(() => WaitForGameObject(_currentTaskId)));
            return GO;
        }

        /// <summary>
        /// Loads an object of type T into memory.
        /// </summary>
        /// <typeparam name="T">Type of object to load.</typeparam>
        /// <param name="path">Path of the object to load in the Addressables system.</param>
        /// <returns>Returns an object of type T.</returns>
        public async Task<T> Load<T>(string path) where T : class
        {
            // Coroutine
            StartCoroutine(TryLoadObject<T>(path));

            // Thread
            T anyObj = null;
            anyObj = await RunTask(Task.Run(() => TryGetObject<T>(_currentTaskId)));
            return anyObj;
        }

        /// <summary>
        /// Preloads all dependencies of an object, given its path.
        /// </summary>
        /// <param name="path">Path of the object to load dependencies from.</param>
        public async Task PreloadDependencies(string path)
        {
            _loadingDependencies = true;

            StartCoroutine(TryPreloadDependencies(path));
            await RunTask(Task.Run(() => TryLoadDependencies(_currentTaskId)));
        }

        /// <summary>
        /// Loads all assets of a given label into memory.
        /// </summary>
        /// <typeparam name="T">Type of the objects to load.</typeparam>
        /// <param name="label">Label in the Addressables of the objects to load.</param>
        /// <returns>Returns a list with elements of type T.</returns>
        public async Task<List<T>> LoadAssetsByLabel<T>(string label) where T : class
        {
            // Coroutine
            StartCoroutine(TryLoadObjectsByLabel<T>(label));

            // Thread
            List<T> objects = new List<T>(100);
            objects = await RunTask(Task.Run(() => LoadByLabel<T>(_currentTaskId)));
            return objects;
        }

        /// <summary>
        /// Releases prefab from memory and destroys its instance in the currently active scene.
        /// </summary>
        /// <param name="obj">Gameobject reference of the prefab to release.</param>
        public void ReleaseInstance(ref GameObject obj)
        {
            if (obj != null)
                Addressables.ReleaseInstance(obj);
        }
        /// <summary>
        /// Releases a given object from memory.
        /// </summary>
        /// <typeparam name="T">Type of the object to release.</typeparam>
        /// <param name="obj">Object reference to release.</param>
        public void ReleaseAsset<T>(ref T obj) where T : class
        {
            if (obj != null)
                Addressables.ReleaseAsset(obj);
        }
        #endregion

        #region COROUTINES
        /// <summary>
        /// Loads a given scene through the Addressables namespace.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load</param>
        /// <param name="loadMode">Scene load mode.</param>
        IEnumerator TryLoadScene(string sceneName, LoadSceneMode loadMode)
        {
            //float time = 0;
            var operation = Addressables.LoadScene(sceneName, loadMode);
            operation.Completed += (op) =>
            {
                _sceneToLoad = op.Result;
                _loadingScene = false;
                //Debug.LogFormat("Scene <b>{0}</b> loaded in {1:0.000} secs.", _sceneToLoad.name, time);
            };

            while (operation.Status != AsyncOperationStatus.Succeeded)
            {
                //time += Time.deltaTime;
                yield return operation;
            }
            operation.Release();
        }

        /// <summary>
        /// Unloads a given scene through the Addressables namespace
        /// </summary>
        /// <param name="scene">Scene object to unload</param>
        IEnumerator TryUnloadScene(Scene scene)
        {
            float time = 0;
            var operation = Addressables.UnloadScene(scene);
            operation.Completed += (op) =>
            {
                _unloadingScene = false;
                Debug.LogFormat("Scene <b>{0}</b> unloaded in {1:0.000} secs.", scene.name, time);
            };

            while (operation.Status != AsyncOperationStatus.Succeeded)
            {
                time += Time.deltaTime;
                yield return operation;
            }
            operation.Release();
        }

        /// <summary>
        /// Instantiates a gameobject through the Addressables namespace.
        /// </summary>
        /// <param name="path">Project path where the gameobject resides.</param>
        /// <param name="position">Position in the world to instantiate the gameobject.</param>
        /// <param name="rotation">Rotation to instantiate the gameobject in.</param>
        public IEnumerator TryInstantiateObject(string path, Vector3 position, Quaternion rotation)
        {
            GameObject _loadedGameObject = null;
            var operation = Addressables.Instantiate<GameObject>(path, position, rotation);
            operation.Completed += (op) =>
            {
                _loadedGameObject = op.Result;
                _gameObjectToLoad = _loadedGameObject;
            };
            yield return operation;
            operation.Release();
        }

        /// <summary>
        /// Loads an object through the Addressables namespace.
        /// </summary>
        /// <typeparam name="T">Type of the parameter to load.</typeparam>
        /// <param name="path">Project path of the object.</param>
        /// <returns></returns>
        IEnumerator TryLoadObject<T>(string path) where T : class
        {
            _currentlyLoadingObject = null;
            var operation = Addressables.LoadAsset<T>(path);
            operation.Completed += (op) =>
            {
                _currentlyLoadingObject = op.Result;
            };
            yield return operation;
            operation.Release();
        }

        /// <summary>
        /// This will load into memory all the Addressable objects that use the label.  The 
        /// load operation takes a list of Addressable addresses which, in this case, we're using the 
        /// built in method to get all addresses for objects with the label.
        /// </summary>
        /// <typeparam name="T">Type of the objects to load in.</typeparam>
        /// <param name="label">Addressables label to load.</param>
        IEnumerator TryLoadObjectsByLabel<T>(string label) where T : class
        {
            /*
             * The second parameter, currently null, is a callback Action that you can pass in to be executed on
             * completion.  We're also using the completed event to manipulate the result of the load call.
             */
            var operation = Addressables.LoadAssets<T>(label, null);
            operation.Completed += (op) =>
            {
                _objectsToLoadByLabel = new List<object>(op.Result.Count);
                _objectsToLoadByLabel.AddRange(op.Result);
            };
            yield return operation;
            operation.Release();
        }

        /// <summary>
        /// This will preload all of the dependencies for the Addressable object
        /// with the address of the given path. Any other objects(materials, meshes, textures, etc.)
        /// that need to be loaded prior to loading the object from "path" will be loaded into memory.
        /// </summary>
        /// <param name="path">Path of the object to load dependencies from.</param>    
        public IEnumerator TryPreloadDependencies(string path)
        {
            var operation = Addressables.DownloadDependencies(path);
            operation.Completed += (res) =>
            {
                _loadingDependencies = false;
            };
            yield return operation;
            operation.Release();
        }
        #endregion

        /// <summary>
        /// Creates a gameobject with the Addressables Manager as a component.
        /// </summary>
        private static void Create()
        {
            new GameObject("AddressablesManager").AddComponent<AddressablesManager>();
        }

        private void OnApplicationQuit()
        {
            //_tokenSource.Cancel();
            StopAllCoroutines();
        }

        [System.Serializable]
        public class CancellableTask<T> : ICancellableTask
        {
            public int ID { get; set; }
            public Task<T> task;
            public CancellationTokenSource tokenSource;
            public CancellationToken Token { get; set; }

            public CancellableTask(Task<T> task, int ID)
            {
                this.task = task;
                this.ID = ID;
                tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(MAX_TIMEOUT));
                Token = tokenSource.Token;
            }
        }

        public class CancellableTask : ICancellableTask
        {
            public int ID { get; set; }
            public Task task;
            public CancellationTokenSource tokenSource;
            public CancellationToken Token { get; set; }

            public CancellableTask(Task task, int ID)
            {
                this.task = task;
                this.ID = ID;
                tokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(MAX_TIMEOUT));
                Token = tokenSource.Token;
            }
        }

        public interface ICancellableTask
        {
            CancellationToken Token { get; }
        }
    }
}