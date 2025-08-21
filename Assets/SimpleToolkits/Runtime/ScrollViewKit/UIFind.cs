using UnityEngine;

namespace SimpleToolkits
{
    /// <summary>
    /// UI 查找工具：提供通用的 Transform/组件 查找能力
    /// </summary>
    public static class UIFind
    {
        /// <summary>
        /// 通过相对路径或名称从父节点查找，并获取其下（包含不激活）的组件 T。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="parent">父 Transform</param>
        /// <param name="pathOrName">相对路径或名称（支持"A/B/C"）</param>
        /// <returns>找到的组件，找不到返回 null</returns>
        public static T Find<T>(this Transform parent, string pathOrName) where T : Component
        {
            if (parent == null || string.IsNullOrEmpty(pathOrName)) return null;
            // 明确调用 Unity 的实例方法，避免扩展方法相互调用
            Transform p = parent;
            var t = p != null ? p.Find(pathOrName) : null;
            return t == null ? null : t.GetComponentInChildren<T>(true);
        }
    }
}
