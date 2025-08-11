using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI遮罩面板
/// 继承UIPanelBase，使用统一的面板管理机制和对象池系统
/// </summary>
public class UIMask : UIPanelBase
{
    /// <summary>
    /// 遮罩初始化
    /// </summary>
    public override void Init(UIMgr uiMgr)
    {
        base.Init(uiMgr);
        // 遮罩不需要特殊的初始化逻辑
    }

    /// <summary>
    /// 销毁遮罩
    /// </summary>
    public override void DestroyPanel()
    {
        // 清理按钮事件
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
        }

        base.DestroyPanel();
    }
}
