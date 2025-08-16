using UnityEngine;
using UnityEngine.UI;

namespace SimpleToolkits
{
    /// <summary>
    /// UI遮罩面板
    /// 简化的遮罩实现，不继承UIPanelBase，避免重复释放问题
    /// </summary>
    public class UIMaskPanel : MonoBehaviour
    {
        /// <summary>
        /// 显示遮罩
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 隐藏遮罩并清理资源
        /// </summary>
        public void Hide()
        {
            // 清理按钮事件
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
            }

            // 隐藏对象
            gameObject.SetActive(false);
        }
    }
}
