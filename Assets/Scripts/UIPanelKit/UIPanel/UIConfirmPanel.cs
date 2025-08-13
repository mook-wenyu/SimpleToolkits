using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 确认对话框数据
/// </summary>
public class ConfirmInfo
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; } = "提示";

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 确认按钮文本
    /// </summary>
    public string OkText { get; set; } = "确定";

    /// <summary>
    /// 取消按钮文本
    /// </summary>
    public string CancelText { get; set; } = "取消";

    /// <summary>
    /// 确认回调
    /// </summary>
    public Action OkCallback { get; set; }

    /// <summary>
    /// 取消回调
    /// </summary>
    public Action CancelCallback { get; set; }

    /// <summary>
    /// 是否显示取消按钮
    /// </summary>
    public bool ShowCancel { get; set; } = true;
}

/// <summary>
/// 确认对话框面板
/// </summary>
public class UIConfirmPanel : UIPanelBase
{
    // UI组件
    private TextMeshProUGUI _txtTitle;
    private TextMeshProUGUI _txtContent;
    
    private Button _btnOk;
    private Button _btnCancel;
    private TextMeshProUGUI _txtOk;
    private TextMeshProUGUI _txtCancel;

    // 回调
    private Action _okCallback;
    private Action _cancelCallback;

    protected override void OnInit()
    {
        // 获取组件引用
        _txtTitle = transform.Find("TxtTitle").GetComponent<TextMeshProUGUI>();
        _txtContent = transform.Find("TxtContent").GetComponent<TextMeshProUGUI>();
        
        _btnOk = transform.Find("Options/BtnOk").GetComponent<Button>();
        _btnCancel = transform.Find("Options/BtnCancel").GetComponent<Button>();
        _txtOk = _btnOk.GetComponentInChildren<TextMeshProUGUI>();
        _txtCancel = _btnCancel.GetComponentInChildren<TextMeshProUGUI>();

        // 添加事件监听
        _btnOk.onClick.AddListener(OnOkClick);
        _btnCancel.onClick.AddListener(OnCancelClick);
    }

    protected override void OnShow(object args)
    {
        if (args is ConfirmInfo info)
        {
            _txtTitle.text = info.Title;
            _txtContent.text = info.Content;
            _txtOk.text = info.OkText;
            _txtCancel.text = info.CancelText;

            _btnCancel.gameObject.SetActive(info.ShowCancel);

            _okCallback = info.OkCallback;
            _cancelCallback = info.CancelCallback;
        }
        else
        {
            _txtTitle.text = "提示";
            _txtContent.text = "确认执行此操作吗？";
            _txtOk.text = "确定";
            _txtCancel.text = "取消";

            _btnCancel.gameObject.SetActive(true);

            _okCallback = null;
            _cancelCallback = null;
        }
    }

    private void OnOkClick()
    {
        // 执行确认回调
        _okCallback?.Invoke();

        // 关闭对话框
        Hide();
    }

    private void OnCancelClick()
    {
        // 执行取消回调
        _cancelCallback?.Invoke();

        // 关闭对话框
        Hide();
    }

    protected override void OnHide()
    {
        // 释放资源
        _btnOk.onClick.RemoveAllListeners();
        _btnCancel.onClick.RemoveAllListeners();

        _okCallback = null;
        _cancelCallback = null;
    }
}
