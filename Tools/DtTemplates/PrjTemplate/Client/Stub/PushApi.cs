﻿#region 文件描述
/******************************************************************************
* 创建: $username$
* 摘要: 
* 日志: $time$ 创建
******************************************************************************/
#endregion

namespace $ext_safeprojectname$
{
    /// <summary>
    /// 服务端推送的处理Api
    /// </summary>
    public class PushApi : IPushApi
    {
        public void Hello(string p_msg)
        {
            Kit.Msg($"【收到服务端推送】\r\n{p_msg}");
        }
    }
}
