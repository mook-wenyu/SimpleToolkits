using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace SimpleToolkits
{
    /// <summary>
    /// 数据存储接口
    /// </summary>
    public interface IDataStorage
    {
        /// <summary>
        /// 存储方式类型
        /// </summary>
        StorageType StorageType { get; }

        /// <summary>
        /// 配置项数量
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 初始化存储
        /// </summary>
        /// <param name="settings">SimpleToolkits设置</param>
        /// <returns>是否初始化成功</returns>
        bool Initialize(SimpleToolkitsSettings settings);

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否加载成功</returns>
        UniTask<bool> LoadAsync(string fileName);

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否保存成功</returns>
        UniTask<bool> SaveAsync(string fileName);

        /// <summary>
        /// 检查是否存在指定键
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns>是否存在</returns>
        bool HasKey(string key);

        /// <summary>
        /// 移除指定键
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns>是否移除成功</returns>
        bool RemoveKey(string key);

        /// <summary>
        /// 清空所有数据
        /// </summary>
        void Clear();

        /// <summary>
        /// 删除指定文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否删除成功</returns>
        UniTask<bool> DeleteAsync(string fileName);

        /// <summary>
        /// 获取所有键名
        /// </summary>
        /// <returns>所有键名</returns>
        string[] GetAllKeys();

        /// <summary>
        /// 获取所有键名
        /// </summary>
        /// <param name="results">结果列表</param>
        void GetAllKeys(List<string> results);

        /// <summary>
        /// 获取布尔值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>布尔值</returns>
        bool GetBool(string key, bool defaultValue = false);

        /// <summary>
        /// 设置布尔值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetBool(string key, bool value);

        /// <summary>
        /// 获取整数值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>整数值</returns>
        int GetInt(string key, int defaultValue = 0);

        /// <summary>
        /// 设置整数值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetInt(string key, int value);

        /// <summary>
        /// 获取浮点数值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>浮点数值</returns>
        float GetFloat(string key, float defaultValue = 0f);

        /// <summary>
        /// 设置浮点数值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetFloat(string key, float value);

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>字符串值</returns>
        string GetString(string key, string defaultValue = "");

        /// <summary>
        /// 设置字符串值
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetString(string key, string value);

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>对象</returns>
        T GetObject<T>(string key, T defaultValue = null) where T : class;

        /// <summary>
        /// 获取对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>对象</returns>
        object GetObject(Type type, string key, object defaultValue = null);

        /// <summary>
        /// 设置对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetObject<T>(string key, T value) where T : class;

        /// <summary>
        /// 设置对象
        /// </summary>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        void SetObject(string key, object value);
    }
}
