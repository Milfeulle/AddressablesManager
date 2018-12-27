using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace AddressablesManagement
{
    public class AddressablesExample : MonoBehaviour
    {

        public AssetReference monk;
        public string label;
        GameObject testObject;
        //List<GameObject> testObject2 = new List<GameObject>(100);
        //Material mat = null;

        async void Start()
        {
            if (AddressablesManager.Instance)
            {
                //await AddressablesManager.Instance.PreloadDependencies("Prefabs/World");
                //await AddressablesManager.Instance.PreloadDependencies(label);            

                //mat = await AddressablesManager.Instance.Load<Material>("Materials/Monk");
                //testObject = await AddressablesManager.Instance.Load<GameObject>("Prefabs/World");

                //monk.Instantiate<GameObject>();           

                //await AddressablesManager.Instance.PreloadDependencies("Main2");

                await AddressablesManager.Instance.OnlyLoadScene("Main2", LoadSceneMode.Additive);

                //monk.Instantiate<GameObject>(new Vector3(145.54f, -7.58f, 0), Quaternion.identity);

                //await Task.Delay(3000);

                //await AddressablesManager.Instance.UnloadScene(scene);

                //Instantiate(testObject2);

                //testObject2 = await AddressablesManager.Instance.LoadAssetsByLabel<GameObject>(label);
                //Debug.Log(testObject2[0].name);
                //Debug.Log(testObject2[1].name);


                //Debug.Log(testObject.name);
            }
            else
                Debug.Log("No AddressablesManager instance found");
        }
    }
}