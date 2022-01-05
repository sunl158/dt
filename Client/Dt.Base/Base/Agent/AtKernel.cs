﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2021-12-31 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 系统内核Api代理类（自动生成）
    /// </summary>
    public static class AtKernel
    {
        #region SysKernel
        /// <summary>
        /// 获取参数配置，包括服务器时间、所有服务地址、模型文件版本号
        /// </summary>
        /// <returns></returns>
        public static Task<List<object>> GetConfig()
        {
            return Kit.Rpc<List<object>>(
                "cm",
                "SysKernel.GetConfig"
            );
        }

        /// <summary>
        /// 更新模型库文件
        /// </summary>
        /// <returns></returns>
        public static Task<bool> UpdateModelDbFile()
        {
            return Kit.Rpc<bool>(
                "cm",
                "SysKernel.UpdateModelDbFile"
            );
        }
        #endregion

        #region Entry
        /// <summary>
        /// 密码登录
        /// </summary>
        /// <param name="p_phone">手机号</param>
        /// <param name="p_pwd">密码</param>
        /// <returns></returns>
        public static Task<LoginResult> LoginByPwd(string p_phone, string p_pwd)
        {
            return Kit.Rpc<LoginResult>(
                "cm",
                "Entry.LoginByPwd",
                p_phone,
                p_pwd
            );
        }

        /// <summary>
        /// 验证码登录
        /// </summary>
        /// <param name="p_phone">手机号</param>
        /// <param name="p_code">验证码</param>
        /// <returns></returns>
        public static Task<LoginResult> LoginByCode(string p_phone, string p_code)
        {
            return Kit.Rpc<LoginResult>(
                "cm",
                "Entry.LoginByCode",
                p_phone,
                p_code
            );
        }

        /// <summary>
        /// 创建验证码
        /// </summary>
        /// <param name="p_phone"></param>
        /// <returns></returns>
        public static Task<string> CreateVerificationCode(string p_phone)
        {
            return Kit.Rpc<string>(
                "cm",
                "Entry.CreateVerificationCode",
                p_phone
            );
        }
        #endregion
    }
}