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
            await Mgr.Instance.Init();

            var configs = Mgr.Instance.Data.GetAll<ExampleConfig>();
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
            _img.sprite = await Mgr.Instance.Loader.LoadAssetAsync<Sprite>("test");

            await Mgr.Instance.UI.RegisterPanel<UIConfirmPanel>(UILayerType.Popup, true, true);

            //await UIBehaviour.Instance.OpenPanel<UIConfirmPanel>();
            //await UIBehaviour.Instance.OpenPanel<UIConfirmPanel>();
            //await UIBehaviour.Instance.OpenPanel<UIConfirmPanel>();

            var scene = await Mgr.Instance.Scene.LoadSceneAsync("TestScene");
            Debug.Log(scene.Status);
            await UniTask.Delay(3000);
            Debug.Log(scene.UnSuspend());
        }
    }
}
