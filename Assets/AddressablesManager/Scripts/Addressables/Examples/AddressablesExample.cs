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
        public GameObject loneGameObject;
        public GameObject[] testObjects = new GameObject[20];
        public List<Material> materials;

        public Vector3 startingPos;

        void Start()
        {
            materials = new List<Material>(5);

            InstantiateSingleObject("TestObject");
            //TestInstantiateObjects();
            //TestLoadByLabel();
        }

        async void TestLoadByLabel()
        {
            materials = await AddressablesManager.Instance.LoadAssetsByLabel<Material>("materials");
        }

        async void TestInstantiateObjects()
        {
            Vector3 pos = startingPos;

            for (int i = 0; i < testObjects.Length; i++)
            {
                testObjects[i] = await AddressablesManager.Instance.InstantiateGameObject("TestObject");
                testObjects[i].transform.position = pos;
                pos += new Vector3(2f, 0, 0);
            }
        }

        async void InstantiateSingleObject(string path)
        {
            loneGameObject = await AddressablesManager.Instance.InstantiateGameObject(path);
        }
    }
}