using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 提示类型
/// </summary>
public enum TipType
{
    /// <summary>
    /// 普通提示
    /// </summary>
    Normal,

    /// <summary>
    /// 成功提示
    /// </summary>
    Success,

    /// <summary>
    /// 警告提示
    /// </summary>
    Warning,

    /// <summary>
    /// 错误提示
    /// </summary>
    Error
}

/// <summary>
/// 提示信息数据
/// </summary>
public class TipInfo
{
    /// <summary>
    /// 提示消息
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// 显示时间（秒），为0则手动关闭
    /// </summary>
    public float Duration { get; set; } = 2.0f;

    /// <summary>
    /// 提示类型
    /// </summary>
    public TipType Type { get; set; } = TipType.Normal;

    /// <summary>
    /// 关闭回调
    /// </summary>
    public Action OnClose { get; set; }
}

/// <summary>
/// 提示面板
/// </summary>
public class UITipPanel : UIPanelBase
{
    // UI组件
    private Image _imgBg;
    private Image _imgIcon;
    private Button _btnClose;

    private TextMeshProUGUI _txtMessage;

    // 提示图标
    private Sprite _iconNormal;
    private Sprite _iconSuccess;
    private Sprite _iconWarning;
    private Sprite _iconError;

    // 回调
    private Action _onClose;

    // 自动关闭计时器
    private float _duration;
    private CancellationTokenSource _cts;

    protected override void OnInit()
    {
        // 获取组件引用
        _txtMessage = transform.Find("TxtMessage").GetComponent<TextMeshProUGUI>();
        _imgBg = transform.Find("ImgBg").GetComponent<Image>();
        _imgIcon = transform.Find("ImgIcon").GetComponent<Image>();
        _btnClose = transform.Find("BtnClose").GetComponent<Button>();

        // 加载图标资源
        // LoadIcons();

        // 添加事件监听
        _btnClose.onClick.AddListener(OnCloseClick);
    }

    private void LoadIcons()
    {
        ResMgr.LoadAssetAsync<Sprite>("tip_icon_normal", sprite => _iconNormal = sprite).Forget();
        ResMgr.LoadAssetAsync<Sprite>("tip_icon_success", sprite => _iconSuccess = sprite).Forget();
        ResMgr.LoadAssetAsync<Sprite>("tip_icon_warning", sprite => _iconWarning = sprite).Forget();
        ResMgr.LoadAssetAsync<Sprite>("tip_icon_error", sprite => _iconError = sprite).Forget();
    }

    protected override void OnShow(object args)
    {
        // 取消之前的自动关闭任务（如果存在）
        CancelAutoCloseTask();

        if (args is TipInfo info)
        {
            // 设置提示内容
            _txtMessage.text = info.Message;

            // 设置图标
            /*_imgIcon.sprite = info.Type switch
            {
                TipType.Normal => _iconNormal,
                TipType.Success => _iconSuccess,
                TipType.Warning => _iconWarning,
                TipType.Error => _iconError,
                _ => _iconNormal
            };*/

            // 设置自动关闭
            _duration = info.Duration;

            // 设置回调
            _onClose = info.OnClose;

            // 是否显示关闭按钮
            _btnClose.gameObject.SetActive(_duration <= 0);
        }
        else
        {
            // 默认提示
            _txtMessage.text = "操作成功";
            // _imgIcon.sprite = _iconNormal;
            _duration = 2.0f;
            _onClose = null;
            _btnClose.gameObject.SetActive(false);
        }

        // 启动自动关闭任务
        if (_duration > 0)
        {
            StartAutoCloseTask().Forget();
        }
    }

    /// <summary>
    /// 启动自动关闭任务
    /// </summary>
    private async UniTaskVoid StartAutoCloseTask()
    {
        // 创建新的取消令牌
        _cts = new CancellationTokenSource();

        try
        {
            // 等待指定时间后关闭
            await UniTask.Delay((int)(_duration * 1000), cancellationToken: _cts.Token);
            Hide();
        }
        catch (OperationCanceledException)
        {
            // 任务被取消，不做任何处理
        }
    }

    /// <summary>
    /// 取消自动关闭任务
    /// </summary>
    private void CancelAutoCloseTask()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnCloseClick()
    {
        Hide();
    }

    protected override void OnHide()
    {
        // 取消自动关闭任务
        CancelAutoCloseTask();

        // 执行关闭回调
        _onClose?.Invoke();
        _onClose = null;

        // 取消自动关闭任务
        CancelAutoCloseTask();

        // 释放资源
        _btnClose.onClick.RemoveAllListeners();
    }
}
