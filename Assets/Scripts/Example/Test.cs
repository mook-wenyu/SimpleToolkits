using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    public class Test : MonoBehaviour
    {
        private Image _img;

        private void Awake()
        {
            Init().Forget();
        }

        private async UniTaskVoid Init()
        {
            await GSMgr.Instance.Init();

            var configs = GSMgr.Instance.GetObject<ConfigData>().GetAll<ExampleConfig>();
            StringBuilder sb = new(configs.Count);
            foreach (var c in configs)
            {
                sb.AppendJoin(",", c.id, c.name, c.hp, c.die, c.pos, c.target);
                sb.AppendLine();
                if (c.duiyou != null)
                    sb.AppendJoin(",", c.duiyou);
                sb.AppendLine();
            }
            Debug.Log(sb.ToString());

            _img = GameObject.Find("Image").GetComponent<Image>();
            _img.sprite = await GSMgr.Instance.GetObject<YooAssetLoader>().LoadAssetAsync<Sprite>("test");

            await GSMgr.Instance.GetObject<UIComponent>().RegisterPanel<UIConfirmPanel>(UILayerType.Popup, true, true);

            //await UIComponent.Instance.OpenPanel<UIConfirmPanel>();
            //await UIComponent.Instance.OpenPanel<UIConfirmPanel>();
            //await UIComponent.Instance.OpenPanel<UIConfirmPanel>();

            var scene = await GSMgr.Instance.GetObject<SceneComponent>().LoadSceneAsync("TestScene");
            await UniTask.Delay(1000);
            Debug.Log(scene.UnSuspend());
            await UniTask.Delay(1000);
            Debug.Log(scene.Status);
        }
    }
}
