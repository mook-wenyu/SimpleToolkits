using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleToolkits.Example
{
    /// <summary>
    /// 示例管理器 - 负责管理和切换不同的示例
    /// </summary>
    public class ExampleManager : MonoBehaviour
    {
        [System.Serializable]
        public class ExampleItem
        {
            public string name;
            public GameObject container;
            public Button button;
            public bool isActive = false;
        }

        [Header("示例配置")]
        [SerializeField] private ExampleItem[] examples;

        [Header("UI组件")]
        [SerializeField] private TextMeshProUGUI titleText;

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {
            InitializeExamples();
        }

        /// <summary>
        /// 初始化示例
        /// </summary>
        private void InitializeExamples()
        {
            if (examples == null || examples.Length == 0)
            {
                Debug.LogWarning("[ExampleManager] 没有配置示例");
                return;
            }

            for (int i = 0; i < examples.Length; i++)
            {
                var example = examples[i];
                if (example.container != null)
                {
                    // 初始状态：只有第一个示例激活
                    example.container.SetActive(i == 0);
                    example.isActive = i == 0;
                }

                if (example.button != null)
                {
                    int index = i;
                    example.button.onClick.AddListener(() => ShowExample(index));
                    
                    // 更新按钮状态
                    UpdateButtonVisual(example.button, i == 0);
                }
            }

            // 更新标题
            UpdateTitle();
        }

        /// <summary>
        /// 显示指定示例
        /// </summary>
        /// <param name="index">示例索引</param>
        public void ShowExample(int index)
        {
            if (index < 0 || index >= examples.Length)
            {
                Debug.LogWarning($"[ExampleManager] 无效的示例索引: {index}");
                return;
            }

            // 隐藏所有示例
            foreach (var example in examples)
            {
                if (example.container != null)
                {
                    example.container.SetActive(false);
                    example.isActive = false;
                }
                if (example.button != null)
                {
                    UpdateButtonVisual(example.button, false);
                }
            }

            // 显示选中的示例
            var selectedExample = examples[index];
            if (selectedExample.container != null)
            {
                selectedExample.container.SetActive(true);
                selectedExample.isActive = true;
            }
            if (selectedExample.button != null)
            {
                UpdateButtonVisual(selectedExample.button, true);
            }

            // 更新标题
            UpdateTitle();
        }

        /// <summary>
        /// 更新按钮视觉效果
        /// </summary>
        /// <param name="button">目标按钮</param>
        /// <param name="isActive">是否激活</param>
        private void UpdateButtonVisual(Button button, bool isActive)
        {
            if (button == null) return;

            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = isActive ? Color.white : Color.black;
            }
        }

        /// <summary>
        /// 更新标题
        /// </summary>
        private void UpdateTitle()
        {
            if (titleText != null)
            {
                foreach (var example in examples)
                {
                    if (example.isActive)
                    {
                        titleText.text = example.name;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (examples != null)
            {
                foreach (var example in examples)
                {
                    if (example.button != null)
                    {
                        example.button.onClick.RemoveAllListeners();
                    }
                }
            }
        }
    }
}