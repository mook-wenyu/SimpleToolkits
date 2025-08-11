using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI面板基类
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    // UI管理器引用
    protected UIMgr uiMgr;

    // 当前面板状态
    protected UIPanelStateType mStateType = UIPanelStateType.None;

    // 面板唯一标识符
    private string _uniqueId;

    // 面板名称
    public string PanelName => GetType().Name;

    // 面板唯一标识符
    public string UniqueId
    {
        get
        {
            if (string.IsNullOrEmpty(_uniqueId))
            {
                _uniqueId = System.Guid.NewGuid().ToString();
            }
            return _uniqueId;
        }
    }

    /// <summary>
    /// 初始化面板
    /// </summary>
    public virtual void Init(UIMgr uiMgrr)
    {
        this.uiMgr = uiMgrr;
        mStateType = UIPanelStateType.Loaded;
        OnInit();
    }

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void Show(object args = null)
    {
        gameObject.SetActive(true);
        mStateType = UIPanelStateType.Showing;
        OnShow(args);
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    /// <param name="destroy">是否强制销毁面板</param>
    /// <param name="usePool">是否使用对象池回收</param>
    /// <param name="animType">关闭动画类型</param>
    public virtual void Hide(bool destroy = false, bool usePool = false, UIPanelAnimType animType = UIPanelAnimType.None)
    {
        // 通过UI管理器处理面板关闭
        uiMgr.ClosePanel(this, destroy, usePool, animType).Forget();
    }

    /// <summary>
    /// 内部隐藏方法，仅供UI管理器调用，避免循环调用
    /// </summary>
    internal virtual void HideInternal()
    {
        gameObject.SetActive(false);
        mStateType = UIPanelStateType.Hidden;

        // 从正在显示的面板字典中移除
        if (uiMgr != null)
        {
            uiMgr.RemoveFromOpenedPanels(this);
        }

        OnHide();
    }

    /// <summary>
    /// 销毁面板
    /// </summary>
    public virtual void DestroyPanel()
    {
        mStateType = UIPanelStateType.Destroyed;
        OnDestroyPanel();
    }

    /// <summary>
    /// 刷新面板
    /// </summary>
    public virtual void Refresh(object args = null)
    {
        OnRefresh(args);
    }

    /// <summary>
    /// 初始化回调
    /// </summary>
    protected virtual void OnInit() { }

    /// <summary>
    /// 显示回调
    /// </summary>
    protected virtual void OnShow(object args) { }

    /// <summary>
    /// 隐藏回调
    /// </summary>
    protected virtual void OnHide() { }

    /// <summary>
    /// 销毁面板回调
    /// </summary>
    protected virtual void OnDestroyPanel() { }

    /// <summary>
    /// 刷新回调
    /// </summary>
    protected virtual void OnRefresh(object args) { }

    /// <summary>
    /// 获取面板状态
    /// </summary>
    public UIPanelStateType GetState()
    {
        return mStateType;
    }


}