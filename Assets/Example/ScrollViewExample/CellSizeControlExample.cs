using UnityEngine;
using SimpleToolkits;

namespace SimpleToolkits.ScrollViewExample
{
    /// <summary>
    /// 演示Cell控制子对象大小的问题及解决方案
    /// </summary>
    public class CellSizeControlExample : MonoBehaviour
    {
        [Header("ScrollView设置")]
        [SerializeField] private ScrollView _scrollView;
        [SerializeField] private RectTransform _cellTemplate;
        [SerializeField] private ScrollVerticalLayout _verticalLayout;

        [Header("测试数据")]
        [SerializeField] private int _itemCount = 20;
        [SerializeField] private Vector2 _cellSize = new Vector2(200, 50);

        private System.Collections.Generic.List<string> _data = new();
        private StandardVariableSizeAdapter _adapter;

        void Start()
        {
            InitializeScrollView();
            AddTestData();
        }

        private void InitializeScrollView()
        {
            // 创建测试数据
            for (int i = 0; i < _itemCount; i++)
            {
                _data.Add($"项目 {i + 1}: 这是一些测试文本内容");
            }

            // 创建简单的绑定器
            var binder = new SimpleDataBinder(_data);

            // 创建适配器
            _adapter = StandardVariableSizeAdapter.CreateForVertical(
                prefab: _cellTemplate,
                countGetter: () => _data.Count,
                dataGetter: index => index >= 0 && index < _data.Count ? _data[index] : null,
                binder: binder,
                fixedWidth: _cellSize.x,
                minHeight: _cellSize.y,
                maxHeight: _cellSize.y,
                enableCache: true
            );

            // 初始化ScrollView
            if (_scrollView != null)
            {
                _scrollView.Initialize(_adapter);
            }
        }

        private void AddTestData()
        {
            // 数据已添加，刷新ScrollView
            if (_scrollView != null && _scrollView.Initialized)
            {
                _scrollView.Refresh();
            }
        }

        [ContextMenu("测试正常状态（不控制子对象大小）")]
        public void TestNormalState()
        {
            if (_verticalLayout != null)
            {
                _verticalLayout.controlChildWidth = false;
                _verticalLayout.controlChildHeight = false;
                Debug.Log("设置为正常状态：不控制子对象大小");
                RefreshScrollView();
            }
        }

        [ContextMenu("测试问题状态（控制子对象宽度）")]
        public void TestProblemState()
        {
            if (_verticalLayout != null)
            {
                _verticalLayout.controlChildWidth = true;
                _verticalLayout.controlChildHeight = false;
                Debug.Log("设置为问题状态：控制子对象宽度");
                RefreshScrollView();
            }
        }

        [ContextMenu("测试修复状态（安全的尺寸控制）")]
        public void TestFixedState()
        {
            if (_verticalLayout != null)
            {
                _verticalLayout.controlChildWidth = true;
                _verticalLayout.controlChildHeight = false;
                Debug.Log("设置为修复状态：安全的尺寸控制");
                RefreshScrollView();
            }
        }

        private void RefreshScrollView()
        {
            if (_scrollView != null && _scrollView.Initialized)
            {
                _scrollView.Refresh();
            }
        }

        /// <summary>
        /// 简单的数据绑定器
        /// </summary>
        private class SimpleDataBinder : ICellBinder
        {
            private readonly System.Collections.Generic.List<string> _data;

            public SimpleDataBinder(System.Collections.Generic.List<string> data)
            {
                _data = data;
            }

            public void BindCell(int index, RectTransform cell)
            {
                var text = cell.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (text != null && index >= 0 && index < _data.Count)
                {
                    text.text = _data[index];
                }
            }
        }
    }
}