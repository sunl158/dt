﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2016-02-18
******************************************************************************/
#endregion

#region 引用命名
using Dt.App;
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
#endregion

namespace Dt.Shell
{
    /// <summary>
    /// 系统存根
    /// </summary>
    public class Stub : IStub
    {
#if WASM
        readonly string _serverUrl = AtWasm.GetServerUrl();

        /// <summary>
        /// 服务器地址
        /// </summary>
        public string ServerUrl
        {
            get { return _serverUrl; }
        }
#else
        /// <summary>
        /// 服务器地址，末尾不包含'/'
        /// </summary>
        public string ServerUrl => "https://10.10.1.16/fz";
#endif

        /// <summary>
        /// 系统标题
        /// </summary>
        public string Title => "搬运工";

        /// <summary>
        /// 登录页面
        /// </summary>
        /// <returns></returns>
        public UIElement LoginPage => new Login { Desc = "搬运工平台基础样例" };

        /// <summary>
        /// 系统启动
        /// </summary>
        /// <param name="p_info">提示信息</param>
        public async void OnStartup(StartupInfo p_info)
        {
            // 设置固定菜单项
            CreateFixedMenus();
            
            if (ViewTypes.TryGetValue("主页", out var type) && type == typeof(DefaultHome))
            {
                // 联网模式
                // 更新打开模型库
                string error = await AtApp.OpenModelDb("cm");
                if (!string.IsNullOrEmpty(error))
                {
                    p_info.SetMessage(error);
                    return;
                }

                string phone = AtLocal.GetCookie("LoginPhone");
                string pwd = AtLocal.GetCookie("LoginPwd");
                if (!string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(pwd))
                {
                    // 自动登录
                    Dict dt = await AtCm.LoginByPwd(phone, pwd);
                    if (dt.Bool("valid"))
                    {
                        // 登录成功
                        AtApp.LoginSuccess(dt);
                        return;
                    }
                }

                // 未登录或登录失败
                AtSys.Login(false);
            }
            else
            {
                // 单机模式
                AtApp.LoadRootUI();
            }
        }

        /// <summary>
        /// 系统注销时的处理
        /// </summary>
        public Task OnLogout()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 挂起时的处理，必须耗时小！
        /// 手机或PC平板模式下不占据屏幕时触发，此时不确定被终止还是可恢复
        /// </summary>
        /// <returns></returns>
        public Task OnSuspending()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 恢复会话时的处理，手机或PC平板模式下再次占据屏幕时触发
        /// </summary>
        public void OnResuming()
        {
        }

        void CreateFixedMenus()
        {
            MenuKit.FixedMenus = new List<OmMenu>
            {
                new OmMenu
                {
                    ID = 1110,
                    Name = "通讯录",
                    Icon = "留言",
                    ViewName = "通讯录"
                },
                new OmMenu
                {
                    ID = 3000,
                    Name = "任务",
                    Icon = "双绞线",
                    ViewName = "任务",
                    SvcName = "cm"
                },
                new OmMenu
                {
                    ID = 4000,
                    Name = "文件",
                    Icon = "文件夹",
                    ViewName = "文件"
                },
                new OmMenu
                {
                    ID = 5000,
                    Name = "发布",
                    Icon = "公告",
                    ViewName = "发布"
                },
                new OmMenu
                {
                    ID = 1,
                    Name = "控件样例",
                    Icon = "词典",
                    ViewName = "控件样例"
                },
            };
        }

        #region 自动生成
        /// <summary>
        /// 获取视图字典
        /// </summary>
        public Dictionary<string, Type> ViewTypes => new Dictionary<string, Type>
        {
            { "主页", typeof(Dt.App.DefaultHome) },
            { "报表", typeof(Dt.App.ReportView) },
            { "流程设计", typeof(Dt.App.Workflow.WorkflowMgr) },
            { "任务", typeof(Dt.App.Workflow.TasksView) },
            { "发布管理", typeof(Dt.App.Publish.PublishMgr) },
            { "发布", typeof(Dt.App.Publish.PublishView) },
            { "基础选项", typeof(Dt.App.Model.BaseOption) },
            { "菜单管理", typeof(Dt.App.Model.SysMenu) },
            { "我的设置", typeof(Dt.App.Model.MyParamsSetting) },
            { "参数定义", typeof(Dt.App.Model.UserParamsMgr) },
            { "基础权限", typeof(Dt.App.Model.BasePrivilege) },
            { "报表设计", typeof(Dt.App.Model.ReportMgr) },
            { "系统角色", typeof(Dt.App.Model.SysRole) },
            { "用户账号", typeof(Dt.App.Model.UserAccount) },
            { "文件", typeof(Dt.App.File.FileHome) },
            { "通讯录", typeof(Dt.App.Chat.ChatHome) },
            { "控件样例", typeof(Dt.Sample.SamplesMain) },
        };

        /// <summary>
        /// 处理服务器推送的类型字典
        /// </summary>
        public Dictionary<string, Type> PushHandlers => new Dictionary<string, Type>
        {
            { "syspushapi", typeof(Dt.Base.SysPushApi) },
        };

        /// <summary>
        /// 获取自定义可序列化类型字典
        /// </summary>
        public Dictionary<string, Type> SerializeTypes => new Dictionary<string, Type>
        {
            { "产品", typeof(Dt.Sample.Product) },
            { "学生", typeof(Dt.Sample.Student) },
            { "部门", typeof(Dt.Sample.Department) },
        };

        /// <summary>
        /// 获取状态库表类型
        /// </summary>
        public Dictionary<string, Type> StateTbls => new Dictionary<string, Type>
        {
            { "menufav", typeof(Dt.App.MenuFav) },
            { "usermenu", typeof(Dt.App.UserMenu) },
            { "readfilehistory", typeof(Dt.App.File.ReadFileHistory) },
            { "chatmember", typeof(Dt.Base.ChatMember) },
            { "letter", typeof(Dt.Base.Letter) },
            { "docklayout", typeof(Dt.Base.Docking.DockLayout) },
            { "celllastval", typeof(Dt.Base.FormView.CellLastVal) },
            { "searchfvhis", typeof(Dt.Base.FormView.SearchFvHis) },
            { "clientlog", typeof(Dt.Core.Model.ClientLog) },
            { "clientcookie", typeof(Dt.Core.Model.ClientCookie) },
            { "dataversion", typeof(Dt.Core.Model.DataVersion) },
            { "userparams", typeof(Dt.Core.Model.UserParams) },
            { "userprivilege", typeof(Dt.Core.Model.UserPrivilege) },
        };

        /// <summary>
        /// 获取状态库版本号，和本地不同时自动更新
        /// </summary>
        public string StateDbVer => "3910e85d001b57a07dccb63e2ca29f44";
        #endregion
    }
}