using System;

namespace BooTools.Core.Models
{
    /// <summary>
    /// 插件操作结果
    /// </summary>
    public class PluginResult
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// 结果消息
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// 异常信息
        /// </summary>
        public Exception? Exception { get; set; }
        
        /// <summary>
        /// 操作数据
        /// </summary>
        public object? Data { get; set; }
        
        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="message">成功消息</param>
        /// <param name="data">返回数据</param>
        /// <returns>成功结果</returns>
        public static PluginResult Success(string message = "", object? data = null)
        {
            return new PluginResult
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }
        
        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常信息</param>
        /// <returns>失败结果</returns>
        public static PluginResult Failure(string errorMessage, Exception? exception = null)
        {
            return new PluginResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Message = errorMessage
            };
        }
    }
    
    /// <summary>
    /// 泛型插件操作结果
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public class PluginResult<T> : PluginResult
    {
        /// <summary>
        /// 强类型数据
        /// </summary>
        public new T? Data { get; set; }
        
        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="data">返回数据</param>
        /// <param name="message">成功消息</param>
        /// <returns>成功结果</returns>
        public static PluginResult<T> Success(T data, string message = "")
        {
            return new PluginResult<T>
            {
                IsSuccess = true,
                Message = message,
                Data = data
            };
        }
        
        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="errorMessage">错误消息</param>
        /// <param name="exception">异常信息</param>
        /// <returns>失败结果</returns>
        public static new PluginResult<T> Failure(string errorMessage, Exception? exception = null)
        {
            return new PluginResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                Message = errorMessage
            };
        }
    }
}
