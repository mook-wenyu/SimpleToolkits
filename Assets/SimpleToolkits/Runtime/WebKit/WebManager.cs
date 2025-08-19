using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace SimpleToolkits
{
    /// <summary>
    /// 提供基于 UnityWebRequest 和 UniTask 的网络请求功能。
    /// </summary>
    public class WebManager
    {
        /// <summary>
        /// 默认请求超时时间（秒）。
        /// </summary>
        public int Timeout { get; set; } = 30;

        /// <summary>
        /// 向指定 URL 发送 GET 请求。
        /// </summary>
        /// <param name="url">请求的目标 URL。</param>
        /// <param name="queryParams">可选的查询参数字典。</param>
        /// <param name="headers">可选的请求头字典。</param>
        /// <param name="timeout">请求超时时间（秒），0 表示使用默认值。</param>
        /// <returns>包含 UnityWebRequest 的 UniTask。</returns>
        public UniTask<UnityWebRequest> GetAsync(string url, Dictionary<string, string> queryParams = null, Dictionary<string, string> headers = null, int timeout = 0)
        {
            if (queryParams is {Count: > 0})
            {
                var uriBuilder = new UriBuilder(url);
                var query = new StringBuilder();
                if (uriBuilder.Query.Length > 1)
                {
                    query.Append(uriBuilder.Query[1..] + "&");
                }

                foreach (var param in queryParams)
                {
                    query.Append(Uri.EscapeDataString(param.Key));
                    query.Append("=");
                    query.Append(Uri.EscapeDataString(param.Value));
                    query.Append("&");
                }
                query.Length--; // 移除最后一个 '&'
                uriBuilder.Query = query.ToString();
                url = uriBuilder.ToString();
            }

            return SendRequestAsync(UnityWebRequest.Get(url), null, headers, timeout);
        }

        /// <summary>
        /// 向指定 URL 发送带有字符串数据的 POST 请求。
        /// </summary>
        /// <param name="url">请求的目标 URL。</param>
        /// <param name="postData">要发送的字符串数据。</param>
        /// <param name="headers">可选的请求头字典。</param>
        /// <param name="timeout">请求超时时间（秒），0 表示使用默认值。</param>
        /// <returns>包含 UnityWebRequest 的 UniTask。</returns>
        public UniTask<UnityWebRequest> PostAsync(string url, string postData, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            var bodyRaw = Encoding.UTF8.GetBytes(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (headers == null || !headers.ContainsKey("Content-Type"))
            {
                request.SetRequestHeader("Content-Type", "application/json");
            }
            return SendRequestAsync(request, bodyRaw, headers, timeout);
        }

        /// <summary>
        /// 向指定 URL 发送带有字节数组数据的 POST 请求。
        /// </summary>
        /// <param name="url">请求的目标 URL。</param>
        /// <param name="postData">要发送的字节数组数据。</param>
        /// <param name="headers">可选的请求头字典。</param>
        /// <param name="timeout">请求超时时间（秒），0 表示使用默认值。</param>
        /// <returns>包含 UnityWebRequest 的 UniTask。</returns>
        public UniTask<UnityWebRequest> PostAsync(string url, byte[] postData, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            return SendRequestAsync(request, postData, headers, timeout);
        }

        /// <summary>
        /// 发送一个将对象序列化为 JSON 的 POST 请求。
        /// </summary>
        /// <param name="url">请求的目标 URL。</param>
        /// <param name="dataObject">要序列化并发送的对象。</param>
        /// <param name="headers">可选的请求头字典。</param>
        /// <param name="timeout">请求超时时间（秒），0 表示使用默认值。</param>
        /// <returns>包含 UnityWebRequest 的 UniTask。</returns>
        public UniTask<UnityWebRequest> PostJsonAsync(string url, object dataObject, Dictionary<string, string> headers = null, int timeout = 0)
        {
            var jsonData = JsonConvert.SerializeObject(dataObject);
            return PostAsync(url, jsonData, headers, timeout);
        }

        private async UniTask<UnityWebRequest> SendRequestAsync(UnityWebRequest request, byte[] bodyData, Dictionary<string, string> headers, int timeout)
        {
            if (bodyData != null)
            {
                request.uploadHandler = new UploadHandlerRaw(bodyData);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.SetRequestHeader(header.Key, header.Value);
                }
            }

            request.timeout = timeout > 0 ? timeout : Timeout;

            try
            {
                await request.SendWebRequest().ToUniTask();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebManager] 请求 {request.url} 时发生异常: {ex.Message}");
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[WebManager] URL: {request.url}\n错误: {request.error}\n响应码: {request.responseCode}\n响应内容: {request.downloadHandler?.text}");
            }

            return request;
        }
    }

    /// <summary>
    /// UnityWebRequest 的扩展方法，用于简化数据处理。
    /// </summary>
    public static class UnityWebRequestExtensions
    {
        /// <summary>
        /// 将 JSON 响应反序列化为指定类型。
        /// </summary>
        /// <typeparam name="T">要反序列化的目标类型。</typeparam>
        /// <param name="request">已完成的 UnityWebRequest。</param>
        /// <returns>反序列化后的对象，如果失败则返回默认值。</returns>
        public static T GetJson<T>(this UnityWebRequest request)
        {
            if (request.result != UnityWebRequest.Result.Success || request.downloadHandler == null)
            {
                return default(T);
            }

            try
            {
                return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebManager] JSON 反序列化失败: {ex.Message}\nJSON: {request.downloadHandler.text}");
                return default(T);
            }
        }
    }
}
