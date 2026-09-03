#nullable enable
using System;

namespace SamMul.Loggers
{
    /// <summary>
    /// UnityEngine.Debug 를 감싼 로거. <c>Log.I.Warn(...)</c> 형태로 사용합니다.
    /// </summary>
    public class Log
    {
        public static Log I { get; } = new Log();

        public void Debug(string message) => UnityEngine.Debug.Log(message);

        public void Warn(string message) => UnityEngine.Debug.LogWarning(message);

        public void Error(string message) => UnityEngine.Debug.LogError(message);

        public void Error(string message, Exception exception) => UnityEngine.Debug.LogError($"{message}\n{exception}");
    }
}
