using UnityEngine.UI;

/// <summary>
/// UI遮罩面板
/// 继承UIPanelBase，使用统一的面板管理机制和对象池系统
/// </summary>
public class UIMaskPanel : UIPanelBase
{
    /// <summary>
    /// 隐藏遮罩
    /// </summary>
    /// <param name="destroy">是否销毁</param>
    public override void Hide(bool destroy = false)
    {
        // 清理按钮事件
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
        }
        
        base.Hide(destroy);
    }

}
