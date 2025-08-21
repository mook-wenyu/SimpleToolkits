namespace SimpleToolkits
{
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 可视化尺寸提供器基类 - 简化版，只有两种模式
    /// </summary>
    public abstract class ScrollSizeProviderBehaviour : UIBehaviour, IScrollSizeProvider
    {
        [Header("自动配置")]
        [SerializeField] protected bool _autoResizeSelf = false;
        [SerializeField] protected bool _forceIndependentMode = false;

        public abstract bool SupportsVariableSize { get; }
        public abstract Vector2 GetItemSize(int index, Vector2 viewportSize);
        public abstract Vector2 GetAverageSize(Vector2 viewportSize);

        public virtual bool AutoResizeSelf
        {
            get => _autoResizeSelf;
            set
            {
                if (_autoResizeSelf != value)
                {
                    _autoResizeSelf = value;
                    if (_autoResizeSelf && Application.isPlaying)
                    {
                        ApplySizeToSelf();
                    }
                }
            }
        }

        public virtual bool ForceIndependentMode
        {
            get => _forceIndependentMode;
            set
            {
                if (_forceIndependentMode != value)
                {
                    _forceIndependentMode = value;
                    if (_forceIndependentMode)
                    {
                        SetUnmanaged();
                        if (Application.isPlaying) ApplySizeToSelf();
                    }
                }
            }
        }
        
        /// <summary>缓存的RectTransform</summary>
        private RectTransform _rectTransform;
        
        /// <summary>被管理状态</summary>
        private bool _managedByScrollView = false;
        private ScrollView _managingScrollView = null;
        
        /// <summary>组件是否被ScrollView管理</summary>
        public bool IsManagedByScrollView => _managedByScrollView && !_forceIndependentMode;
        
        /// <summary>管理该组件的ScrollView</summary>
        public ScrollView ManagedBy => _managingScrollView;
        
        /// <summary>是否应该自动调整尺寸（简化：只有独立/被管理两种模式）</summary>
        public bool ShouldAutoResizeSelf => _autoResizeSelf && !IsManagedByScrollView;
        
        /// <summary>获取缓存的RectTransform</summary>
        protected RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = GetComponent<RectTransform>();
                return _rectTransform;
            }
        }
        
        /// <summary>ScrollView设置管理状态</summary>
        public void SetManagedBy(ScrollView scrollView)
        {
            if (_forceIndependentMode) return;
            
            _managedByScrollView = true;
            _managingScrollView = scrollView;
        }
        
        /// <summary>取消ScrollView管理</summary>
        public void SetUnmanaged()
        {
            _managedByScrollView = false;
            _managingScrollView = null;
            
            if (_autoResizeSelf && Application.isPlaying)
            {
                ApplySizeToSelf();
            }
        }
        
        /// <summary>应用尺寸到自身</summary>
        public virtual void ApplySizeToSelf()
        {
            if (!ShouldAutoResizeSelf || !Application.isPlaying) return;
            
            var rect = RectTransform;
            if (rect == null) return;
            
            var parentRect = rect.parent as RectTransform;
            var viewportSize = parentRect != null ? parentRect.rect.size : new Vector2(300, 200);
            
            var newSize = GetItemSize(0, viewportSize);
            rect.sizeDelta = newSize;
            
            ScrollComponentNotifier.NotifySizeProviderChanged(this);
        }

        /// <summary>标记需要重新计算并通知变化</summary>
        protected virtual void SetDirtyAndUpdate()
        {
            if (Application.isPlaying && ShouldAutoResizeSelf)
            {
                ApplySizeToSelf();
            }
        }
        
        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }
        
        protected override void Start()
        {
            base.Start();
            if (ShouldAutoResizeSelf)
            {
                ApplySizeToSelf();
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (ShouldAutoResizeSelf && Application.isPlaying)
            {
                ApplySizeToSelf();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SetDirtyAndUpdate();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirtyAndUpdate();
        }

        /// <summary>手动触发立即更新</summary>
        [ContextMenu("立即应用尺寸")]
        public void ForceUpdate()
        {
            if (ShouldAutoResizeSelf)
            {
                ApplySizeToSelf();
            }
        }
        
        /// <summary>脱离管理，恢复独立工作</summary>
        [ContextMenu("脱离管理")]
        public void BreakFromManagement()
        {
            ForceIndependentMode = true;
        }
    }
}