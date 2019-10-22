﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-05-07 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using Dt.Core.Caches;
using Dt.Core.Sqlite;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion

namespace Dt.Cm
{
    /// <summary>
    /// 入口Api
    /// </summary>
    [Api]
    public class Entry : BaseApi
    {
        const string _prefixCode = "vercode";

        /// <summary>
        /// 获取参数配置，包括模型文件版本号、服务器时间
        /// </summary>
        /// <returns></returns>
        public Dict GetConfig()
        {
            return new Dict { { "ver", Glb.GetSvc<SqliteModelHandler>().Version }, { "now", Glb.Now } };
        }

        /// <summary>
        /// 密码登录
        /// </summary>
        /// <param name="p_phone">手机号</param>
        /// <param name="p_pwd">密码</param>
        /// <returns></returns>
        public async Task<Dict> LoginByPwd(string p_phone, string p_pwd)
        {
            Dict res = new Dict();
            if (string.IsNullOrWhiteSpace(p_phone) || string.IsNullOrWhiteSpace(p_pwd))
            {
                res["valid"] = false;
                res["error"] = "手机号或密码不可为空！";
                return res;
            }

            var repo = new UserRepo();
            User user = await repo.GetByPhone(p_phone);
            if (user == null || !user.ValidatePwd(p_pwd))
            {
                res["valid"] = false;
                res["error"] = "手机号不存在或密码错误！";
                return res;
            }

            res["userid"] = user.ID;
            res["name"] = user.Name;
            res["roles"] = Glb.AnyoneID + ",aca71e2d795d47b6942e4aa5c9df8248";
            return res;
        }

        /// <summary>
        /// 验证码登录
        /// </summary>
        /// <param name="p_phone">手机号</param>
        /// <param name="p_code">验证码</param>
        /// <returns></returns>
        public async Task<Dict> LoginByCode(string p_phone, string p_code)
        {
            Dict res = new Dict();
            if (string.IsNullOrWhiteSpace(p_phone) || string.IsNullOrWhiteSpace(p_code))
            {
                res["valid"] = false;
                res["error"] = "手机号或验证码不可为空！";
                return res;
            }

            //string code = await Cache.StringGet<string>(_prefixCode, p_phone);
            //if (code != p_code)
            //{
            //    res["valid"] = false;
            //    res["error"] = "验证码错误！";
            //    return res;
            //}

            var repo = new UserRepo();
            User user = await repo.GetByPhone(p_phone);
            if (user == null)
            {
                // 初次登录，创建账号，初始密码为手机号后4位
                user = new User
                {
                    Phone = p_phone,
                    Name = p_phone,
                    Pwd = Kit.GetMD5(p_phone.Substring(p_phone.Length - 4)),
                    //Pwd = Kit.GetMD5(new Random().Next(100000, 999999).ToString()),
                };
                await repo.Insert(user);
            }

            res["valid"] = true;
            res["userid"] = "110";
            res["name"] = "test";
            res["roles"] = Glb.AnyoneID + ",aca71e2d795d47b6942e4aa5c9df8248";
            res["pwd"] = "xxx";
            return res;
        }

        /// <summary>
        /// 创建验证码
        /// </summary>
        /// <param name="p_phone"></param>
        /// <returns></returns>
        public string CreateVerificationCode(string p_phone)
        {
            if (string.IsNullOrWhiteSpace(p_phone) || !Regex.IsMatch(p_phone, "^1[34578]\\d{9}$"))
                return string.Empty;

            string code = new Random().Next(1000, 9999).ToString();
            // 60秒失效
            Cache.StringSet(_prefixCode, p_phone, code, TimeSpan.FromSeconds(60));

            // 发送短信

            return code;
        }
    }
}
