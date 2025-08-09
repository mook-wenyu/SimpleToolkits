using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    private Image img;
    
    // Start is called before the first frame update
    void Start()
    {
        ConfigMgr.Init();
        ResMgr.Instance.OnSingletonInit();
        
        //var config = ConfigMgr.Get<ExampleConfig>("1001");
        //Debug.Log(config.name);
        
        img = GameObject.Find("Image").GetComponent<Image>();
        Init().Forget();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async UniTaskVoid Init()
    {
        await UniTask.Delay(500);
        
        img.sprite = await ResMgr.Instance.LoadAssetAsync<Sprite>("test");
    }
}
