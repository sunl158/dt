﻿#region 文件描述
/******************************************************************************
* 创建: $username$
* 摘要: 
* 日志: $time$ 创建
******************************************************************************/
#endregion

#region 引用命名
using System;
#endregion

namespace $ext_safeprojectname$
{
    public class BackgroundJob : IBackgroundJob
    {
        /// <summary>
        /// 后台任务处理，除 AtState、Stub、Kit.Rpc、Kit.Toast 外，不可使用任何UI和外部变量，保证可独立运行！！！
        /// </summary>
        public async Task Run()
        {
            //string tpName = AtState.GetCookie("LoginPhone");
            //var cfg = await AtCm.GetConfig();
            //Kit.Toast(
            //    "样例",
            //    DateTime.Now.ToString(),
            //    new AutoStartInfo { WinType = typeof(LvHome).AssemblyQualifiedName, Title = "列表" });

            await Task.CompletedTask;
        }
    }
}
