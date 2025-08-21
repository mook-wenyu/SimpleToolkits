using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SimpleToolkits;
using System.Collections.Generic;

namespace SimpleToolkits.ScrollViewExample.Binds
{
    /// <summary>
    /// 聊天消息绑定器 - 负责将聊天消息数据绑定到UI上
    /// </summary>
    public class ChatMessageBinder : ICellBinder
    {
        private readonly List<Models.ChatMessage> _messages;

        // 组件缓存类
        private class ComponentCache
        {
            public TextMeshProUGUI SenderText { get; set; }
            public TextMeshProUGUI ContentText { get; set; }
            public TextMeshProUGUI TimeText { get; set; }
        }

        // 组件缓存字典 - 使用LinkedList实现LRU缓存
        private readonly Dictionary<int, ComponentCache> _componentCache = new();
        private readonly LinkedList<int> _cacheAccessOrder = new();
        private readonly int _maxCacheSize = 50; // 限制缓存大小，避免内存泄漏

        public ChatMessageBinder(List<Models.ChatMessage> messages)
        {
            _messages = messages ?? throw new System.ArgumentNullException(nameof(messages));
        }

        /// <summary>
        /// 从LRU缓存中获取组件缓存
        /// </summary>
        private ComponentCache GetComponentCache(int instanceId)
        {
            if (_componentCache.TryGetValue(instanceId, out var cache))
            {
                // 更新访问顺序
                _cacheAccessOrder.Remove(instanceId);
                _cacheAccessOrder.AddFirst(instanceId);
                return cache;
            }
            return null;
        }

        /// <summary>
        /// 添加组件缓存到LRU缓存
        /// </summary>
        private void AddComponentCache(int instanceId, ComponentCache cache)
        {
            // 如果缓存已满，移除最久未使用的项
            if (_componentCache.Count >= _maxCacheSize)
            {
                var lruKey = _cacheAccessOrder.Last.Value;
                _componentCache.Remove(lruKey);
                _cacheAccessOrder.RemoveLast();
            }

            _componentCache[instanceId] = cache;
            _cacheAccessOrder.AddFirst(instanceId);
        }

        /// <summary>
        /// 清理组件缓存
        /// </summary>
        private void ClearComponentCache()
        {
            _componentCache.Clear();
            _cacheAccessOrder.Clear();
        }

        public void OnBind(int index, RectTransform item)
        {
            if (index < 0 || index >= _messages.Count) return;

            var message = _messages[index];
            if (message == null) return;

            var instanceId = item.GetInstanceID();

            // 从LRU缓存中获取组件引用
            var cache = GetComponentCache(instanceId);
            if (cache == null)
            {
                // 如果缓存中没有，重新获取并缓存
                cache = new ComponentCache
                {
                    SenderText = item.Find("SenderText").GetComponentInChildren<TextMeshProUGUI>(),
                    ContentText = item.Find("ContentText").GetComponentInChildren<TextMeshProUGUI>(),
                    TimeText = item.Find("TimeText").GetComponentInChildren<TextMeshProUGUI>(),
                };
                AddComponentCache(instanceId, cache);
            }

            // 绑定数据（使用缓存的组件引用）
            if (cache.SenderText != null)
            {
                cache.SenderText.text = message.Sender;
                cache.SenderText.color = GetMessageTypeColor(message.Type);
            }

            if (cache.ContentText != null)
            {
                cache.ContentText.text = message.Content;
                cache.ContentText.color = GetMessageTypeColor(message.Type);
            }

            if (cache.TimeText != null)
            {
                cache.TimeText.text = message.Time;
                cache.TimeText.color = GetMessageTypeColor(message.Type);
            }
        }

        /// <summary>
        /// Cell 首次实例化时调用（每个实例仅一次）
        /// </summary>
        public void OnCreated(RectTransform cell)
        {
            var instanceId = cell.GetInstanceID();

            // 如果已经存在缓存，先清理
            _componentCache.Remove(instanceId);

            // 缓存组件引用，避免每次都GetComponent
            var cache = new ComponentCache
            {
                SenderText = cell.Find("SenderText").GetComponentInChildren<TextMeshProUGUI>(),
                ContentText = cell.Find("ContentText").GetComponentInChildren<TextMeshProUGUI>(),
                TimeText = cell.Find("TimeText").GetComponentInChildren<TextMeshProUGUI>(),
            };

            // 使用LRU缓存
            AddComponentCache(instanceId, cache);
        }

        /// <summary>
        /// Cell 回收时调用，用于解绑与资源回收
        /// </summary>
        public void OnRecycled(int index, RectTransform item)
        {
            var instanceId = item.GetInstanceID();

            // 从LRU缓存中获取组件引用
            var cache = GetComponentCache(instanceId);
            if (cache != null)
            {
                // 清理UI组件（使用缓存的组件引用）
                if (cache.SenderText != null) cache.SenderText.text = string.Empty;
                if (cache.ContentText != null) cache.ContentText.text = string.Empty;
                if (cache.TimeText != null) cache.TimeText.text = string.Empty;
            }
        }

        /// <summary>
        /// 获取消息类型的文本颜色
        /// </summary>
        private Color GetMessageTypeColor(Models.MessageType messageType)
        {
            return messageType switch
            {
                Models.MessageType.System => Color.gray,
                Models.MessageType.Error => Color.red,
                Models.MessageType.Warning => Color.yellow,
                Models.MessageType.Success => Color.green,
                Models.MessageType.User => Color.white,
                Models.MessageType.Normal => Color.white,
                _ => Color.white
            };
        }

        /// <summary>
        /// 获取消息类型的背景颜色
        /// </summary>
        private Color GetMessageTypeBackgroundColor(Models.MessageType messageType)
        {
            return messageType switch
            {
                Models.MessageType.System => new Color(0.2f, 0.2f, 0.2f, 0.8f),
                Models.MessageType.Error => new Color(0.8f, 0.1f, 0.1f, 0.8f),
                Models.MessageType.Warning => new Color(0.8f, 0.6f, 0.1f, 0.8f),
                Models.MessageType.Success => new Color(0.1f, 0.8f, 0.1f, 0.8f),
                Models.MessageType.User => new Color(0.1f, 0.3f, 0.6f, 0.8f),
                Models.MessageType.Normal => new Color(0.1f, 0.1f, 0.1f, 0.8f),
                _ => new Color(0.1f, 0.1f, 0.1f, 0.8f)
            };
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int cacheCount, int maxCacheSize, float cacheUsage) GetCacheStats()
        {
            return (_componentCache.Count, _maxCacheSize, (float)_componentCache.Count / _maxCacheSize);
        }

        /// <summary>
        /// 清理缓存（用于内存管理）
        /// </summary>
        public void CleanupCache()
        {
            // 清理超过最大缓存大小的项
            while (_componentCache.Count > _maxCacheSize)
            {
                var lruKey = _cacheAccessOrder.Last.Value;
                _componentCache.Remove(lruKey);
                _cacheAccessOrder.RemoveLast();
            }
        }

        /// <summary>
        /// 批量绑定多个索引
        /// </summary>
        public void BatchBind(int startIndex, int count, System.Func<int, RectTransform> getItemGetter)
        {
            if (getItemGetter == null) return;

            var endIndex = Mathf.Min(startIndex + count, _messages.Count);

            // 批量处理，减少单独调用开销
            for (int i = startIndex; i < endIndex; i++)
            {
                var item = getItemGetter(i);
                if (item != null)
                {
                    OnBind(i, item);
                }
            }
        }

        /// <summary>
        /// 批量回收多个索引
        /// </summary>
        public void BatchRecycle(int startIndex, int count, System.Func<int, RectTransform> getItemGetter)
        {
            if (getItemGetter == null) return;

            var endIndex = Mathf.Min(startIndex + count, _messages.Count);

            // 批量处理，减少单独调用开销
            for (int i = startIndex; i < endIndex; i++)
            {
                var item = getItemGetter(i);
                if (item != null)
                {
                    OnRecycled(i, item);
                }
            }
        }

        /// <summary>
        /// 批量清理指定范围的缓存
        /// </summary>
        public void BatchClearCache(int startIndex, int count, System.Func<int, RectTransform> getItemGetter)
        {
            if (getItemGetter == null) return;

            var endIndex = Mathf.Min(startIndex + count, _messages.Count);

            // 批量处理，减少单独调用开销
            for (int i = startIndex; i < endIndex; i++)
            {
                var item = getItemGetter(i);
                if (item != null)
                {
                    var instanceId = item.GetInstanceID();
                    _componentCache.Remove(instanceId);

                    // 从访问顺序链表中移除
                    var node = _cacheAccessOrder.Find(instanceId);
                    if (node != null)
                    {
                        _cacheAccessOrder.Remove(node);
                    }
                }
            }
        }

        /// <summary>
        /// 批量预热缓存
        /// </summary>
        public void BatchPreheatCache(int startIndex, int count, System.Func<int, RectTransform> getItemGetter)
        {
            if (getItemGetter == null) return;

            var endIndex = Mathf.Min(startIndex + count, _messages.Count);

            // 批量处理，减少单独调用开销
            for (int i = startIndex; i < endIndex; i++)
            {
                var item = getItemGetter(i);
                if (item != null)
                {
                    var instanceId = item.GetInstanceID();

                    // 如果还没有缓存，则创建缓存
                    if (!_componentCache.ContainsKey(instanceId))
                    {
                        var cache = new ComponentCache
                        {
                            SenderText = item.Find("SenderText").GetComponentInChildren<TextMeshProUGUI>(),
                            ContentText = item.Find("ContentText").GetComponentInChildren<TextMeshProUGUI>(),
                            TimeText = item.Find("TimeText").GetComponentInChildren<TextMeshProUGUI>(),
                        };

                        AddComponentCache(instanceId, cache);
                    }
                }
            }
        }
    }
}
