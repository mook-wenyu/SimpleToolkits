using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleToolkits.DialogueKit
{
    /// <summary>
    /// 标签注册管理器
    /// 负责注册、管理和查找标签处理器
    /// </summary>
    public class TagProcessorRegistry
    {
        private static TagProcessorRegistry _instance;
        public static TagProcessorRegistry Instance => _instance ??= new TagProcessorRegistry();

        // 方括号标签处理器字典
        private readonly Dictionary<string, ITagProcessor> _processors;

        private TagProcessorRegistry()
        {
            _processors = new Dictionary<string, ITagProcessor>();
            RegisterBuiltinProcessors();
        }

        /// <summary>
        /// 注册内置标签处理器
        /// </summary>
        private void RegisterBuiltinProcessors()
        {
            // 控制标签
            RegisterProcessor(new SpeedTagProcessor());
            RegisterProcessor(new SpeedRegionTagProcessor());
            RegisterProcessor(new PauseTagProcessor());
            RegisterProcessor(new ClearTagProcessor());
            RegisterProcessor(new LineBreakTagProcessor());
            RegisterProcessor(new NewLineTagProcessor());

            // 样式标签
            RegisterProcessor(new ColorTagProcessor());
            RegisterProcessor(new SizeTagProcessor());
            RegisterProcessor(new BoldTagProcessor());
            RegisterProcessor(new ItalicTagProcessor());
            RegisterProcessor(new UnderlineTagProcessor());

            // 功能标签
            RegisterProcessor(new ActionTagProcessor());
        }

        /// <summary>
        /// 注册标签处理器
        /// </summary>
        /// <param name="processor">标签处理器</param>
        public void RegisterProcessor(ITagProcessor processor)
        {
            if (processor == null)
            {
                Debug.LogError("Cannot register null tag processor");
                return;
            }

            _processors[processor.TagName.ToLower()] = processor;
            Debug.Log($"Registered tag processor: [{processor.TagName}]");
        }

        /// <summary>
        /// 注册多个标签处理器
        /// </summary>
        /// <param name="processors">标签处理器数组</param>
        public void RegisterProcessors(params ITagProcessor[] processors)
        {
            foreach (var processor in processors)
            {
                RegisterProcessor(processor);
            }
        }

        /// <summary>
        /// 取消注册标签处理器
        /// </summary>
        /// <param name="tagName">标签名称</param>
        public void UnregisterProcessor(string tagName)
        {
            if (_processors.Remove(tagName.ToLower()))
            {
                Debug.Log($"Unregistered tag processor: [{tagName}]");
            }
        }

        /// <summary>
        /// 获取标签处理器
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>标签处理器，未找到返回null</returns>
        public ITagProcessor GetProcessor(string tagName)
        {
            return _processors.TryGetValue(tagName.ToLower(), out var processor) ? processor : null;
        }

        /// <summary>
        /// 检查是否存在指定标签的处理器
        /// </summary>
        /// <param name="tagName">标签名称</param>
        /// <returns>是否存在</returns>
        public bool HasProcessor(string tagName)
        {
            return _processors.ContainsKey(tagName.ToLower());
        }

        /// <summary>
        /// 获取所有已注册的标签名称
        /// </summary>
        /// <returns>标签名称列表</returns>
        public string[] GetRegisteredTagNames()
        {
            return _processors.Keys.ToArray();
        }

        /// <summary>
        /// 获取所有已注册的标签处理器
        /// </summary>
        /// <returns>标签处理器列表</returns>
        public ITagProcessor[] GetRegisteredProcessors()
        {
            return _processors.Values.ToArray();
        }

        /// <summary>
        /// 清空所有已注册的标签处理器
        /// </summary>
        public void Clear()
        {
            _processors.Clear();
            Debug.Log("Cleared all tag processors");
        }

        /// <summary>
        /// 重新注册内置处理器
        /// </summary>
        public void ResetToBuiltins()
        {
            Clear();
            RegisterBuiltinProcessors();
            Debug.Log("Reset to builtin tag processors");
        }

        /// <summary>
        /// 获取注册统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            var count = _processors.Count;
            return $"Registered Tag Processors: {count}";
        }
    }
}