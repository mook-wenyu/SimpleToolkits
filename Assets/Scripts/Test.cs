using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    private Image img;

    private void Awake()
    {
        Init().Forget();
    }

    private async UniTaskVoid Init()
    {
        await ResMgr.Instance.Init();
        ConfigMgr.Init();
        await UIMgr.Instance.Init();
        
        var configs = ConfigMgr.GetAll<ExampleConfig>();
        StringBuilder sb = new (configs.Count);
        foreach (var c in configs)
        {
            sb.AppendJoin(",", c.id, c.name, c.hp, c.die, c.pos, c.target);
            sb.AppendLine();
            if (c.duiyou != null)
                sb.AppendJoin(",", c.duiyou);
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
        
        img = GameObject.Find("Image").GetComponent<Image>();
        img.sprite = await ResMgr.Instance.LoadAssetAsync<Sprite>("test");
    }
}
