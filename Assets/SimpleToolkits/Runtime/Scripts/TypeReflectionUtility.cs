using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Scripting;

namespace SimpleToolkits
{
    /// <summary>
    /// 类型反射工具类
    /// </summary>
    public static class TypeReflectionUtility
    {
        #region 私有字段
        /// <summary>
        /// 类型缓存字典，键为类型全名，值为类型对象
        /// 使用 ConcurrentDictionary 确保线程安全
        /// </summary>
        private static readonly ConcurrentDictionary<string, Type> _typeCache = new();

        /// <summary>
        /// 程序集缓存字典，键为程序集名称，值为程序集对象
        /// </summary>
        private static readonly ConcurrentDictionary<string, Assembly> _assemblyCache = new();

        /// <summary>
        /// 已加载程序集的缓存，避免重复获取
        /// </summary>
        private static Assembly[] _loadedAssembliesCache;

        /// <summary>
        /// 缓存更新时间戳，用于判断是否需要刷新程序集缓存
        /// </summary>
        private static DateTime _lastCacheUpdate = DateTime.MinValue;

        /// <summary>
        /// 缓存刷新间隔（秒）
        /// </summary>
        private const double CacheRefreshInterval = 30.0;

        /// <summary>
        /// 反射查找的绑定标志，包括公共、非公共、实例、静态成员，并展平继承层次结构
        /// </summary>
        private const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic |
                                                         BindingFlags.Instance | BindingFlags.Static |
                                                         BindingFlags.FlattenHierarchy;
        #endregion

        /// <summary>
        /// 从所有已加载的程序集中查找指定名称的类型
        /// 支持完整类型名和简单类型名两种查找方式
        /// </summary>
        /// <param name="typeName">类型名称，可以是完整名称或简单名称</param>
        /// <param name="ignoreCase">是否忽略大小写，默认为 false</param>
        /// <returns>找到的类型，如果未找到则返回 null</returns>
        [Preserve]
        public static Type FindType(string typeName, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("[TypeReflectionUtility] 类型名称不能为空");
                return null;
            }

            // 生成缓存键
            var cacheKey = ignoreCase ? typeName.ToLowerInvariant() : typeName;

            // 先从缓存中查找
            if (_typeCache.TryGetValue(cacheKey, out var cachedType))
            {
                return cachedType;
            }

            // 缓存中没有，开始查找
            Type foundType = null;

            // 首先尝试直接通过 Type.GetType 查找（适用于完整类型名）
            foundType = Type.GetType(typeName, false, ignoreCase);
            if (foundType != null)
            {
                _typeCache.TryAdd(cacheKey, foundType);
                return foundType;
            }

            // 如果直接查找失败，遍历所有已加载的程序集
            var assemblies = GetLoadedAssemblies();
            foreach (var assembly in assemblies)
            {
                foundType = FindTypeInAssembly(typeName, assembly, ignoreCase);
                if (foundType == null) continue;
                _typeCache.TryAdd(cacheKey, foundType);
                return foundType;
            }

            // 未找到类型，记录警告并缓存 null 结果以避免重复查找
            Debug.LogWarning($"[TypeReflectionUtility] 未找到类型: {typeName}");
            _typeCache.TryAdd(cacheKey, null);
            return null;
        }

        /// <summary>
        /// 从指定程序集中查找类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="assemblyName">程序集名称</param>
        /// <param name="ignoreCase">是否忽略大小写，默认为 false</param>
        /// <returns>找到的类型，如果未找到则返回 null</returns>
        [Preserve]
        public static Type FindType(string typeName, string assemblyName, bool ignoreCase = false)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                Debug.LogWarning("[TypeReflectionUtility] 类型名称不能为空");
                return null;
            }

            if (string.IsNullOrEmpty(assemblyName))
            {
                Debug.LogWarning("[TypeReflectionUtility] 程序集名称不能为空");
                return null;
            }

            // 生成缓存键
            var cacheKey = $"{assemblyName}::{(ignoreCase ? typeName.ToLowerInvariant() : typeName)}";

            // 先从缓存中查找
            if (_typeCache.TryGetValue(cacheKey, out var cachedType))
            {
                return cachedType;
            }

            // 获取指定程序集
            var assembly = GetAssemblyByName(assemblyName);
            if (assembly == null)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 未找到程序集: {assemblyName}");
                _typeCache.TryAdd(cacheKey, null);
                return null;
            }

            // 在指定程序集中查找类型
            var foundType = FindTypeInAssembly(typeName, assembly, ignoreCase);
            _typeCache.TryAdd(cacheKey, foundType);

            if (foundType == null)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 在程序集 {assemblyName} 中未找到类型: {typeName}");
            }

            return foundType;
        }

        /// <summary>
        /// 通过泛型参数查找类型（主要用于缓存优化）
        /// </summary>
        /// <typeparam name="T">要查找的类型</typeparam>
        /// <returns>类型对象</returns>
        [Preserve]
        public static Type FindType<T>() where T : class
        {
            return typeof(T);
        }

        /// <summary>
        /// 获取所有已加载的程序集列表
        /// </summary>
        /// <returns>程序集数组</returns>
        [Preserve]
        public static Assembly[] GetLoadedAssemblies()
        {
            // 检查是否需要刷新缓存
            var now = DateTime.Now;
            if (_loadedAssembliesCache == null ||
                (now - _lastCacheUpdate).TotalSeconds > CacheRefreshInterval)
            {
                _loadedAssembliesCache = AppDomain.CurrentDomain.GetAssemblies();
                _lastCacheUpdate = now;
            }

            return _loadedAssembliesCache;
        }

        /// <summary>
        /// 根据名称获取程序集
        /// </summary>
        /// <param name="assemblyName">程序集名称</param>
        /// <returns>程序集对象，如果未找到则返回 null</returns>
        [Preserve]
        public static Assembly GetAssemblyByName(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName))
                return null;

            // 先从缓存中查找
            if (_assemblyCache.TryGetValue(assemblyName, out var cachedAssembly))
            {
                return cachedAssembly;
            }

            // 缓存中没有，开始查找
            var assemblies = GetLoadedAssemblies();
            var assembly = assemblies.FirstOrDefault(a =>
                string.Equals(a.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a.FullName, assemblyName, StringComparison.OrdinalIgnoreCase));

            // 缓存结果
            _assemblyCache.TryAdd(assemblyName, assembly);
            return assembly;
        }

        #region 字段访问方法
        /// <summary>
        /// 获取指定类型的字段信息
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>字段信息，如果未找到则返回 null</returns>
        [Preserve]
        public static FieldInfo GetField(Type type, string fieldName, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return null;
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                Debug.LogError("[TypeReflectionUtility] 字段名称不能为空");
                return null;
            }

            try
            {
                // 查找字段
                var field = type.GetField(fieldName, bindingFlags);

                if (field == null)
                {
                    Debug.LogWarning($"[TypeReflectionUtility] 在类型 {type.Name} 中未找到字段: {fieldName}");
                }

                return field;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取字段 {fieldName} 时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取对象指定字段的值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="obj">目标对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>字段值，如果获取失败则返回默认值</returns>
        [Preserve]
        public static T GetFieldValue<T>(object obj, string fieldName, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return default(T);
            }

            var field = GetField(obj.GetType(), fieldName, bindingFlags);
            if (field == null)
            {
                return default(T);
            }

            try
            {
                var value = field.GetValue(obj);

                // 类型转换
                if (value == null)
                {
                    return default(T);
                }

                if (value is T directValue)
                {
                    return directValue;
                }

                // 尝试类型转换
                if (typeof(T).IsAssignableFrom(value.GetType()))
                {
                    return (T)value;
                }

                Debug.LogWarning($"[TypeReflectionUtility] 字段 {fieldName} 的值类型 {value.GetType().Name} 无法转换为 {typeof(T).Name}");
                return default(T);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取字段 {fieldName} 的值时发生异常: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 设置对象指定字段的值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="fieldName">字段名称</param>
        /// <param name="value">要设置的值</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>设置是否成功</returns>
        [Preserve]
        public static bool SetFieldValue(object obj, string fieldName, object value, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return false;
            }

            var field = GetField(obj.GetType(), fieldName, bindingFlags);
            if (field == null)
            {
                return false;
            }

            if (field.IsInitOnly || field.IsLiteral)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 字段 {fieldName} 是只读的，无法设置值");
                return false;
            }

            try
            {
                // 类型检查
                if (value != null && !field.FieldType.IsAssignableFrom(value.GetType()))
                {
                    Debug.LogWarning($"[TypeReflectionUtility] 值类型 {value.GetType().Name} 与字段类型 {field.FieldType.Name} 不兼容");
                    return false;
                }

                field.SetValue(obj, value);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 设置字段 {fieldName} 的值时发生异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 属性访问方法
        /// <summary>
        /// 获取指定类型的属性信息
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>属性信息，如果未找到则返回 null</returns>
        [Preserve]
        public static PropertyInfo GetProperty(Type type, string propertyName, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return null;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                Debug.LogError("[TypeReflectionUtility] 属性名称不能为空");
                return null;
            }

            try
            {
                // 查找属性
                var property = type.GetProperty(propertyName, bindingFlags);

                if (property == null)
                {
                    Debug.LogWarning($"[TypeReflectionUtility] 在类型 {type.Name} 中未找到属性: {propertyName}");
                }

                return property;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取属性 {propertyName} 时发生异常: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取对象指定属性的值
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>属性值，如果获取失败则返回默认值</returns>
        [Preserve]
        public static T GetPropertyValue<T>(object obj, string propertyName, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return default(T);
            }

            var property = GetProperty(obj.GetType(), propertyName, bindingFlags);
            if (property == null)
            {
                return default(T);
            }

            if (!property.CanRead)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 属性 {propertyName} 不可读");
                return default(T);
            }

            try
            {
                var value = property.GetValue(obj);

                // 类型转换
                if (value == null)
                {
                    return default(T);
                }

                if (value is T directValue)
                {
                    return directValue;
                }

                // 尝试类型转换
                if (typeof(T).IsAssignableFrom(value.GetType()))
                {
                    return (T)value;
                }

                Debug.LogWarning($"[TypeReflectionUtility] 属性 {propertyName} 的值类型 {value.GetType().Name} 无法转换为 {typeof(T).Name}");
                return default(T);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取属性 {propertyName} 的值时发生异常: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 设置对象指定属性的值
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="propertyName">属性名称</param>
        /// <param name="value">要设置的值</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>设置是否成功</returns>
        [Preserve]
        public static bool SetPropertyValue(object obj, string propertyName, object value, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return false;
            }

            var property = GetProperty(obj.GetType(), propertyName, bindingFlags);
            if (property == null)
            {
                return false;
            }

            if (!property.CanWrite)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 属性 {propertyName} 不可写");
                return false;
            }

            try
            {
                // 类型检查
                if (value != null && !property.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    Debug.LogWarning($"[TypeReflectionUtility] 值类型 {value.GetType().Name} 与属性类型 {property.PropertyType.Name} 不兼容");
                    return false;
                }

                property.SetValue(obj, value);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 设置属性 {propertyName} 的值时发生异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 方法访问方法
        /// <summary>
        /// 获取指定类型的方法信息
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameterTypes">参数类型数组，如果为 null 则查找第一个匹配名称的方法</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>方法信息，如果未找到则返回 null</returns>
        [Preserve]
        public static MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes = null, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return null;
            }

            if (string.IsNullOrEmpty(methodName))
            {
                Debug.LogError("[TypeReflectionUtility] 方法名称不能为空");
                return null;
            }

            try
            {
                MethodInfo method;

                if (parameterTypes != null)
                {
                    // 根据参数类型精确查找
                    method = type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);
                }
                else
                {
                    // 只根据名称查找
                    method = type.GetMethod(methodName, bindingFlags);
                }

                if (method == null)
                {
                    Debug.LogWarning($"[TypeReflectionUtility] 在类型 {type.Name} 中未找到方法: {methodName}");
                }

                return method;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取方法 {methodName} 时发生异常: {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// 调用对象的指定方法
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="obj">目标对象</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法参数</param>
        /// <returns>方法返回值，如果调用失败则返回默认值</returns>
        [Preserve]
        public static T InvokeMethod<T>(object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return default(T);
            }

            // 获取参数类型
            var parameterTypes = parameters?.Select(p => p?.GetType()).ToArray();
            var method = GetMethod(obj.GetType(), methodName, parameterTypes);

            if (method == null)
            {
                return default(T);
            }

            try
            {
                var result = method.Invoke(obj, parameters);

                // 类型转换
                if (result == null)
                {
                    return default(T);
                }

                if (result is T directValue)
                {
                    return directValue;
                }

                // 尝试类型转换
                if (typeof(T).IsAssignableFrom(result.GetType()))
                {
                    return (T)result;
                }

                Debug.LogWarning($"[TypeReflectionUtility] 方法 {methodName} 的返回值类型 {result.GetType().Name} 无法转换为 {typeof(T).Name}");
                return default(T);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 调用方法 {methodName} 时发生异常: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// 调用对象的指定方法（无返回值）
        /// </summary>
        /// <param name="obj">目标对象</param>
        /// <param name="methodName">方法名称</param>
        /// <param name="parameters">方法参数</param>
        /// <returns>调用是否成功</returns>
        [Preserve]
        public static bool InvokeMethod(object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
            {
                Debug.LogError("[TypeReflectionUtility] 对象参数不能为 null");
                return false;
            }

            // 获取参数类型
            var parameterTypes = parameters?.Select(p => p?.GetType()).ToArray();
            var method = GetMethod(obj.GetType(), methodName, parameterTypes);

            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(obj, parameters);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 调用方法 {methodName} 时发生异常: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 批量操作方法
        /// <summary>
        /// 获取类型的所有字段
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>字段信息数组</returns>
        [Preserve]
        public static FieldInfo[] GetAllFields(Type type, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return Array.Empty<FieldInfo>();
            }

            try
            {
                var fields = type.GetFields(bindingFlags);

                return fields;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取类型 {type.Name} 的所有字段时发生异常: {ex.Message}");
                return Array.Empty<FieldInfo>();
            }
        }

        /// <summary>
        /// 获取类型的所有属性
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>属性信息数组</returns>
        [Preserve]
        public static PropertyInfo[] GetAllProperties(Type type, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return Array.Empty<PropertyInfo>();
            }
            try
            {
                var properties = type.GetProperties(bindingFlags);
                return properties;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取类型 {type.Name} 的所有属性时发生异常: {ex.Message}");
                return Array.Empty<PropertyInfo>();
            }
        }

        /// <summary>
        /// 获取类型的所有方法
        /// 支持继承层次结构中的成员查找（包括基类成员）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <param name="bindingFlags">绑定标志，默认为公共实例成员并展平继承层次</param>
        /// <returns>方法信息数组</returns>
        [Preserve]
        public static MethodInfo[] GetAllMethods(Type type, BindingFlags bindingFlags = DefaultBindingFlags)
        {
            if (type == null)
            {
                Debug.LogError("[TypeReflectionUtility] 类型参数不能为 null");
                return Array.Empty<MethodInfo>();
            }
            try
            {
                var methods = type.GetMethods(bindingFlags);
                return methods;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 获取类型 {type.Name} 的所有方法时发生异常: {ex.Message}");
                return Array.Empty<MethodInfo>();
            }
        }
        #endregion

        #region 缓存管理方法
        /// <summary>
        /// 清理所有缓存
        /// </summary>
        [Preserve]
        public static void Clear()
        {
            _typeCache.Clear();
            _assemblyCache.Clear();
            _loadedAssembliesCache = null;
            _lastCacheUpdate = DateTime.MinValue;
            Debug.Log("[TypeReflectionUtility] 所有缓存已清理");
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 在指定程序集中查找类型
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <param name="assembly">程序集</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>找到的类型，如果未找到则返回 null</returns>
        private static Type FindTypeInAssembly(string typeName, Assembly assembly, bool ignoreCase = false)
        {
            if (assembly == null)
                return null;

            try
            {
                // 首先尝试精确匹配
                var type = assembly.GetType(typeName, false, ignoreCase);
                if (type != null)
                    return type;

                // 如果精确匹配失败，尝试通过简单名称匹配
                var types = assembly.GetTypes();
                var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                // 优先匹配简单名称
                type = types.FirstOrDefault(t => string.Equals(t.Name, typeName, comparison));
                if (type != null)
                    return type;

                // 最后尝试匹配完整名称
                type = types.FirstOrDefault(t => string.Equals(t.FullName, typeName, comparison));
                return type;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogWarning($"[TypeReflectionUtility] 加载程序集 {assembly.FullName} 中的类型时发生异常: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TypeReflectionUtility] 在程序集 {assembly.FullName} 中查找类型 {typeName} 时发生异常: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
