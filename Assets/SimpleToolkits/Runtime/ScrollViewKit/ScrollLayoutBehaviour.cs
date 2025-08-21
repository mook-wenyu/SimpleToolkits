namespace SimpleToolkits
{
    using UnityEngine;
    using UnityEngine.EventSystems;
    using System.Collections;

    /// <summary>
    /// 可视化滚动布局组件基类 - 简化版，只有两种模式
    /// </summary>
    public abstract class ScrollLayoutBehaviour : UIBehaviour, IScrollLayout
    {
        [Header("布局设置")]
        [SerializeField] protected float _spacing = 0f;
        [SerializeField] protected RectOffset _padding = new RectOffset();
        
        [Header("自动布局")]
        [SerializeField] protected bool _autoApplyLayout = true;
        [SerializeField] protected bool _updateChildrenLayout = true;
        [SerializeField] protected bool _forceIndependentMode = false;

        public abstract LayoutType Type { get; }
        public abstract bool IsVertical { get; }

        public virtual float Spacing 
        { 
            get => _spacing; 
            set 
            { 
                if (_spacing != value)
                {
                    _spacing = value;
                    SetDirtyAndUpdate();
                }
            } 
        }

        public virtual RectOffset Padding 
        { 
            get => _padding; 
            set 
            { 
                if (_padding != value)
                {
                    _padding = value ?? new RectOffset();
                    SetDirtyAndUpdate();
                }
            } 
        }

        public virtual bool AutoApplyLayout
        {
            get => _autoApplyLayout;
            set
            {
                if (_autoApplyLayout != value)
                {
                    _autoApplyLayout = value;
                    if (_autoApplyLayout && Application.isPlaying)
                    {
                        ApplyLayoutToSelf();
                    }
                }
            }
        }
        
        public virtual bool UpdateChildrenLayout
        {
            get => _updateChildrenLayout;
            set
            {
                if (_updateChildrenLayout != value)
                {
                    _updateChildrenLayout = value;
                    if (value && Application.isPlaying)
                    {
                        ApplyLayoutToSelf();
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
                        if (Application.isPlaying) ApplyLayoutToSelf();
                    }
                }
            }
        }
        
        /// <summary>缓存的RectTransform和子对象</summary>
        private RectTransform _rectTransform;
        private RectTransform[] _childRects;
        
        /// <summary>被管理状态</summary>
        private bool _managedByScrollView = false;
        private ScrollView _managingScrollView = null;
        
        /// <summary>布局是否需要更新</summary>
        protected bool _isDirty = true;
        
        /// <summary>组件是否被ScrollView管理</summary>
        public bool IsManagedByScrollView => _managedByScrollView && !_forceIndependentMode;
        
        /// <summary>管理该组件的ScrollView</summary>
        public ScrollView ManagedBy => _managingScrollView;
        
        /// <summary>是否应该自动应用布局（简化：只有独立/被管理两种模式）</summary>
        public bool ShouldAutoApplyLayout => _autoApplyLayout && !IsManagedByScrollView;
        
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
        
        /// <summary>获取所有子RectTransform</summary>
        protected RectTransform[] GetChildRects()
        {
            if (_childRects == null || _childRects.Length != transform.childCount)
            {
                _childRects = new RectTransform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    _childRects[i] = transform.GetChild(i) as RectTransform;
                }
            }
            return _childRects;
        }

        public abstract Vector2 CalculateContentSize(int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize);
        public abstract (int first, int last) CalculateVisibleRange(Vector2 contentPosition, Vector2 viewportSize, int itemCount, IScrollSizeProvider sizeProvider);
        public abstract Vector2 CalculateItemPosition(int index, int itemCount, IScrollSizeProvider sizeProvider, Vector2 viewportSize);
        public abstract void SetupContent(RectTransform content);
        
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
            
            if (_autoApplyLayout && Application.isPlaying)
            {
                ApplyLayoutToSelf();
            }
        }
        
        /// <summary>应用布局到自身和子对象</summary>
        public virtual void ApplyLayoutToSelf()
        {
            if (!ShouldAutoApplyLayout || !Application.isPlaying) return;
            
            var rect = RectTransform;
            if (rect == null) return;
            
            SetupContent(rect);
            
            if (_updateChildrenLayout)
            {
                ApplyLayoutToChildren();
            }
            
            ScrollComponentNotifier.NotifyLayoutChanged(this);
        }
        
        /// <summary>应用布局到子对象</summary>
        protected virtual void ApplyLayoutToChildren()
        {
            var childRects = GetChildRects();
            if (childRects == null || childRects.Length == 0) return;
            
            var viewportSize = RectTransform.rect.size;
            var defaultSizeProvider = new DefaultSizeProvider(new Vector2(100, 100));
            
            for (int i = 0; i < childRects.Length; i++)
            {
                if (childRects[i] == null) continue;
                
                var position = CalculateItemPosition(i, childRects.Length, defaultSizeProvider, viewportSize);
                childRects[i].anchoredPosition = position;
                
                var size = defaultSizeProvider.GetItemSize(i, viewportSize);
                childRects[i].sizeDelta = size;
            }
            
            var contentSize = CalculateContentSize(childRects.Length, defaultSizeProvider, viewportSize);
            RectTransform.sizeDelta = contentSize;
        }

        /// <summary>标记布局为脏状态并通知变化</summary>
        protected virtual void SetDirtyAndUpdate()
        {
            _isDirty = true;
            if (Application.isPlaying && ShouldAutoApplyLayout)
            {
                ApplyLayoutToSelf();
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
            if (ShouldAutoApplyLayout)
            {
                ApplyLayoutToSelf();
            }
        }
        
        protected override void OnEnable()
        {
            base.OnEnable();
            if (ShouldAutoApplyLayout && Application.isPlaying)
            {
                ApplyLayoutToSelf();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            
            _spacing = Mathf.Max(0, _spacing);
            if (_padding == null) _padding = new RectOffset();
            
            SetDirtyAndUpdate();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _childRects = null;
            SetDirtyAndUpdate();
        }
        
        /// <summary>手动触发立即更新</summary>
        [ContextMenu("立即应用布局")]
        public void ForceUpdate()
        {
            _isDirty = false;
            if (ShouldAutoApplyLayout)
            {
                ApplyLayoutToSelf();
            }
        }
        
        /// <summary>脱离管理，恢复独立工作</summary>
        [ContextMenu("脱离管理")]
        public void BreakFromManagement()
        {
            ForceIndependentMode = true;
        }
    }
    
    /// <summary>
    /// 默认尺寸提供器，用于独立布局模式
    /// </summary>
    internal class DefaultSizeProvider : IScrollSizeProvider
    {
        private readonly Vector2 _defaultSize;
        
        public DefaultSizeProvider(Vector2 defaultSize)
        {
            _defaultSize = defaultSize;
        }
        
        public bool SupportsVariableSize => false;
        
        public Vector2 GetItemSize(int index, Vector2 viewportSize)
        {
            return _defaultSize;
        }
        
        public Vector2 GetAverageSize(Vector2 viewportSize)
        {
            return _defaultSize;
        }
    }
}