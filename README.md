# AddressablesManager
Tested with Addressables version 0.6.7.

An unofficial intermediary for Unity's Addressables system. With this, you can load an asset without having to make use of a coroutine each time you need to use the Addressables system.

For a quick guide on how to setup Addressables, check the [Getting Started guide](https://docs.google.com/document/d/1Qdrhi3NdTR_ub5e1NVjzvijCcVlR56e_zCro41KHfyM/edit#heading=h.zfo4zcp1lrzf) and the [forum post](https://forum.unity.com/threads/addressables-are-here.536304/)

The [IAsyncOperationExtensions](https://github.com/Milfeulle/AddressablesManager/blob/master/Assets/AddressablesManager/Scripts/Extensions/IAsyncOperationExtensions.cs) file was created by Unity forum user [rigidbuddy](https://forum.unity.com/threads/async-await-support-for-loading-assets.538898/#post-3553571).

# Async/Await

This system needs to be prefaced with the fact that it uses async/await methods exclusively, what this basically means is that methods with the async modifier in front of them can "await" other asynchronous methods. You can find some examples [here](https://www.dotnetperls.com/async).

# Usage

To use the manager, you must declare your current method with the async modifier so that it can await the Addressables Manager's methods.

```c#
async void GetAnInteger()
{
    int x = await GetInt();
}
```

## Instantiating a gameobject

```c#
GameObject loneGameObject;

async void InstantiateSingleObject(string path)
{
    loneGameObject = await AddressablesManager.Instance.InstantiateGameObject(path);
}
```

## Loading objects

You can load any kind of object by using the Load<T> method, you can then assign these objects somewhere else if you wish.

```c#
Sprite _sprite;

async void LoadSprite()
{
    _sprite = await AddressablesManager.Instance.Load<Sprite>("sprite");
}
```

## Loading assets by label

You can load an array of assets using the labels they use in the Addressables system.

```c#
List<Material> materials;

async void TestLoadByLabel()
{
    materials = await AddressablesManager.Instance.LoadAssetsByLabel<Material>("materials");
}
```

## Loading a scene

With loading scenes, you can choose whether to load them in either of the LoadSceneModes (Additive or Single) or whether to only load them or load them and assign them to a variable.

```c#
Scene sceneToLoad;

async void LoadScene()
{
    sceneToLoad = await AddressablesManager.Instance.LoadScene("sceneName", LoadSceneMode.Additive);
}

async void OnlyLoadScene()
{
    await AddressablesManager.Instance.LoadScene("sceneName", LoadSceneMode.Additive);
}
```

## Unloading scenes

You can unload scenes you hold a reference to.

```c#
async void UnloadScene()
{
    await AddressablesManager.Instance.UnloadScene(sceneToLoad);
}
```
