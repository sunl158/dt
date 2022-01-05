﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Core;
using Dt.Core.Rpc;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
#endregion

namespace Dt.Sample
{
    public partial class RpcDemo : Win
    {
        public RpcDemo()
        {
            InitializeComponent();
        }

        async void OnGetString(object sender, RoutedEventArgs e)
        {
            _tbInfo.Text = "返回：" + await AtTestRpc.GetString();
        }

        async void OnSetString(object sender, RoutedEventArgs e)
        {
            if (await AtTestRpc.SetString("abc"))
                _tbInfo.Text = "OnSetString成功";
        }

        async void OnServerStream(object sender, RoutedEventArgs e)
        {
            _tbInfo.Text = "ServerStream模式：";
            var _reader = await AtTestRpc.OnServerStream("hello");
            while (await _reader.MoveNext())
            {
                _tbInfo.Text += $"{Environment.NewLine}收到：{_reader.Val<string>()}";
            }
            _tbInfo.Text += Environment.NewLine + "结束";
        }

    }

    internal static class AtTestRpc
    {
        #region TestRpc
        public static Task<string> GetString()
        {
            return Kit.Rpc<string>(
                "cm",
                "TestRpc.GetString"
            );
        }

        public static Task<bool> SetString(string p_str)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestRpc.SetString",
                p_str
            );
        }

        public static Task<ResponseReader> OnServerStream(string p_title)
        {
            return Kit.ServerStreamRpc(
                "cm",
                "TestRpc.OnServerStream",
                p_title
            );
        }

        public static Task<RequestWriter> OnClientStream(string p_title)
        {
            return Kit.ClientStreamRpc(
                "cm",
                "TestRpc.OnClientStream",
                p_title
            );
        }

        public static Task<DuplexStream> OnDuplexStream(string p_title)
        {
            return Kit.DuplexStreamRpc(
                "cm",
                "TestRpc.OnDuplexStream",
                p_title
            );
        }
        #endregion
    }
}