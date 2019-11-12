using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace AddressablesManagement
{
    public class AddressablesManager : MonoBehaviour
    {
        private static AddressablesManager _instance;

        private bool _currentlyLoading;
        private Scene _currentlyLoadingScene;

        #region PROPERTIES
        /// <summary>
        /// Reports whether there's an object currently loading or not.
        /// </summary>
        public bool CurrentlyLoading
        {
            get { return _currentlyLoading; }
            private set { _currentlyLoading = value; }
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

        #region PUBLIC METHODS
        /// <summary>
        /// Loads a given scene either in additive or single mode to the current scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        public Task<Scene> LoadScene(string sceneName, LoadSceneMode loadMode)
        {
            _currentlyLoadingScene = default;
            _currentlyLoadingScene.name = "";            

            Addressables.LoadSceneAsync(sceneName, loadMode).Completed += AddressablesManager_OnSceneLoadCompleted;

            if (string.IsNullOrEmpty(_currentlyLoadingScene.name))
            {
                Task.Delay(1);
            }

            return Task.Run(() => _currentlyLoadingScene);
        }

        private void AddressablesManager_OnSceneLoadCompleted(AsyncOperationHandle<SceneInstance> obj)
        {
            if (obj.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                _currentlyLoadingScene = obj.Result.Scene;
            }
        }

        /// <summary>
        /// Loads a given scene either in additive or single mode to the current scene.
        /// </summary>
        /// <param name="sceneName">Name of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        public async Task OnlyLoadScene(string sceneName, LoadSceneMode loadMode)
        {
            await Task.Run(() => Addressables.LoadSceneAsync(sceneName, loadMode));
        }

        /// <summary>
        /// Unloads a given scene from memory asynchronously.
        /// </summary>
        /// <param name="scene">Scene object to unload.</param>
        public async Task UnloadScene(SceneInstance scene)
        {
            Addressables.UnloadSceneAsync(scene);
        }

        /// <summary>
        /// Instantiates a gameobject in a given position and rotation in the world.
        /// </summary>
        /// <param name="path">Addressables</param>
        /// <param name="position">Position in the world to instantiate the gameobject.</param>
        /// <param name="rotation">Rotation to instantiate the gameobject.</param>
        /// <returns>Returns the gameobject set to instantiate.</returns>
        public async Task<GameObject> InstantiateGameObject(string path, Vector3 position, Quaternion rotation)
        {
            GameObject GO = null;

            GO = await Addressables.InstantiateAsync(path, position, rotation) as GameObject;
            return GO;
        }

        /// <summary>
        /// Instantiates a gameobject in a default position and rotation in the world.
        /// </summary>
        /// <param name="path">Project path where the gameobject resides.</param>
        /// <returns>Returns the gameobject set to instantiate.</returns>
        public async Task<GameObject> InstantiateGameObject(string path)
        {
            GameObject GO = null;

            GO = await Addressables.InstantiateAsync(path, Vector3.zero, Quaternion.identity) as GameObject;
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
            T anyObj = null;
            anyObj = await Addressables.LoadAssetAsync<T>(path);
            return anyObj;
        }

        /// <summary>
        /// Preloads all dependencies of an object, given its path.
        /// </summary>
        /// <param name="path">Path of the object to load dependencies from.</param>
        public async Task DownloadDependencies(string path)
        {
            await Addressables.DownloadDependenciesAsync(path);
        }

        /// <summary>
        /// Loads all assets of a given label into memory.
        /// </summary>
        /// <typeparam name="T">Type of the objects to load.</typeparam>
        /// <param name="label">Label in the Addressables of the objects to load.</param>
        /// <returns>Returns a list with elements of type T.</returns>
        public async Task<List<T>> LoadAssetsByLabel<T>(string label) where T : class
        {
            IList<T> objects = new List<T>(100);
            objects = await Addressables.LoadAssetsAsync<T>(label, null);
            return (List<T>)objects;            
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
                Addressables.Release(obj);
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
            StopAllCoroutines();
        }      
    }
}