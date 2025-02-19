﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-09-06 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using Dt.Core.Rpc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Dt.Base.Chat;
#if !WASM
using Microsoft.Maui.Devices;
#endif
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 处理服务器推送
    /// </summary>
    class PushHandler
    {
        #region 静态内容
        const int _maxRetry = 4;
        static Dictionary<string, MethodInfo> _methods;
        // 会话标识，区分同一账号多个登录的情况
        static readonly string _sessionID = Guid.NewGuid().ToString().Substring(0, 8);
        static ResponseReader _reader;

        /// <summary>
        /// 连接推送的重试次数
        /// </summary>
        public static int RetryTimes;

        /// <summary>
        /// 处理服务器推送
        /// </summary>
        /// <returns></returns>
        public static async void Register()
        {
            if ((_reader != null && !_reader.IsClosed)
                || !Kit.IsLogon)
            {
                //Kit.Msg("已连接");
                return;
            }

#if WASM
            Dict dt = new Dict
            {
                { "sessionid", _sessionID },
                { "model", "wasm" },
                { "name", "Chrome" },
                { "platform", "Browser" },
                { "version", "11.0" },
            };
#else
            Dict dt = new Dict
            {
                { "sessionid", _sessionID },
                { "model", DeviceInfo.Model },
                { "name", DeviceInfo.Name },
                { "platform", DeviceInfo.Platform.ToString() },
                { "version", DeviceInfo.VersionString },
            };
#endif

            try
            {
                _reader = await AtMsg.Register(dt);
            }
            catch
            {
                _reader = null;
                // 小于最大重试次数重连
                if (RetryTimes < _maxRetry)
                {
                    RetryTimes++;
                    //Kit.Msg($"第{RetryTimes}次重连");
                    _ = Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, RetryTimes))).ContinueWith((t) => Register());
                }
                else
                {
                    Kit.Warn($"已重试{_maxRetry + 1}次，无法接收推送！");
                }
                return;
            }

            // 连接成功，重连次数复位
            RetryTimes = 0;
            bool allowRetry = true;
            try
            {
                while (await _reader.MoveNext())
                {
                    var msg = _reader.Val<string>();
                    if (msg == ":Close")
                        allowRetry = false;
                    else
                        new PushHandler().Call(msg);
                }
            }
            catch { }

            if (allowRetry)
            {
                // 默认允许重连，除非服务端主动取消连接
                _ = Task.Run(() => Register());
            }
            //else
            //{
            //    Kit.Msg("已停止接收推送！");
            //}
        }

        /// <summary>
        /// 主动停止接收推送
        /// </summary>
        public static void StopRecvPush()
        {
            RetryTimes = 0;
            if (_reader != null && !_reader.IsClosed)
            {
                // 只能通过服务端取消连接！！！
                AtMsg.Unregister(Kit.UserID, _sessionID);
            }
        }
#endregion

        public void Call(string p_msg)
        {
            if (string.IsNullOrEmpty(p_msg))
            {
                Log.Warning("服务器推送内容为空");
                return;
            }

            string method = "";
            MethodInfo mi = null;
            object[] args = null;
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(p_msg);
                var reader = new Utf8JsonReader(data);
                // [
                reader.Read();
                method = reader.ReadAsString();

                if (string.IsNullOrEmpty(method) || (mi = GetMethod(method.ToLower())) == null)
                {
                    Log.Warning($"服务器推送方法 {method} 不存在");
                    return;
                }

                var pars = mi.GetParameters();
                if (pars.Length > 0)
                {
                    // 确保和Api的参数个数、类型相同
                    // 类型不同时 执行类型转换 或 直接创建派生类实例！如Row -> User, Table -> Table<User>
                    int index = 0;
                    args = new object[pars.Length];
                    while (index < pars.Length && reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        // 参数支持派生类型！
                        args[index] = JsonRpcSerializer.Deserialize(ref reader, pars[index].ParameterType);
                        index++;
                    }
                }

                // 输出日志信息
                string id = TraceLogs.AddRpcJson(data);
                Log.ForContext("Rpc", "Recv")
                    .ForContext("Json", id)
                    .Debug($"msg推送—> {method}");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "解析服务器推送时异常");
            }

            try
            {
                object tgt = Activator.CreateInstance(mi.DeclaringType);
                Kit.RunAsync(() => mi.Invoke(tgt, args));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, $"调用推送处理方法{method}时异常");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_method">方法名，形如：SysPushApi.ReceiveLetter</param>
        /// <returns></returns>
        MethodInfo GetMethod(string p_method)
        {
            if (_methods == null)
                InitMethods();

            if (_methods.TryGetValue(p_method, out var mi))
                return mi;
            return null;
        }

        void InitMethods()
        {
            _methods = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);

            ExtractApi(typeof(SysPushApi));
            ExtractApi(typeof(WebRtcApi));

            // 外部定义的api
            var apis = Kit.GetServices<IPushApi>();
            foreach (var api in apis)
            {
                ExtractApi(api.GetType());
            }
        }

        /// <summary>
        /// 提取类型中的Api
        /// </summary>
        /// <param name="p_type"></param>
        void ExtractApi(Type p_type)
        {
            MethodInfo[] methods = p_type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (MethodInfo mi in methods)
            {
                _methods[$"{p_type.Name}.{mi.Name}"] = mi;
            }
        }
    }
}
