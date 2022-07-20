﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2016-02-18
******************************************************************************/
#endregion

#region 引用命名
using Dt.Mgr;
using Dt.Base;
using Dt.Core.Model;
#endregion

namespace Dt.Sample
{
    /// <summary>
    /// 存根
    /// </summary>
    public class AppStub : DefaultStub
    {
        public AppStub()
        {
            Title = "搬运工";
            EnableBgTask = true;
            //InitCmUrl("http://10.10.1.16/dt-cm");
            LogSetting.FileEnabled = true;
            
            MenuKit.FixedMenus = new List<OmMenu>
            {
                new OmMenu(
                    ID: 1110,
                    Name: "通讯录",
                    Icon: "留言",
                    ViewName: "通讯录"),

                new OmMenu(
                    ID: 3000,
                    Name: "任务",
                    Icon: "双绞线",
                    ViewName: "任务",
                    SvcName: "cm:UserRelated.GetMenuTip"),

                new OmMenu(
                    ID: 4000,
                    Name: "文件",
                    Icon: "文件夹",
                    ViewName: "文件"),

                new OmMenu(
                    ID: 5000,
                    Name: "发布",
                    Icon: "公告",
                    ViewName: "发布"),

                new OmMenu(
                    ID: 1,
                    Name: "样例",
                    Icon: "词典",
                    ViewName: "样例"),
            };
        }

        /// <summary>
        /// 系统启动
        /// </summary>
        protected override async Task OnStartup()
        {
            // 初次运行，显示用户协议、隐私政策、向导
            if (AtState.GetCookie("FirstRun") == "")
            {
                await new PrivacyDlg("lob/DtAgreement.html", "lob/DtPrivacy.html").ShowAsync();
                AtState.SaveCookie("FirstRun", "0");
            }

            // 1. 默认启动
            //await StartRun();

            // 2. 自定义启动
            await StartRun(typeof(Sample.SamplesMain), false);
        }

        /// <summary>
        /// 后台任务处理，除 AtState、Stub、Kit.Rpc、Kit.Toast 外，不可使用任何UI和外部变量，保证可独立运行！！！
        /// </summary>
        protected override async Task OnBgTaskRun()
        {
            //string tpName = AtState.GetCookie("LoginPhone");
            //var cfg = await AtCm.GetConfig();
            //await BackgroundLogin();
            //Kit.Toast(
            //    "样例",
            //    tpName + "\r\n" + cfg.Date("now").ToString(),
            //    new AutoStartInfo { WinType = typeof(LvHome).AssemblyQualifiedName, Title = "列表" });

            //Kit.Toast(
            //    "样例",
            //    DateTime.Now.ToString(),
            //    new AutoStartInfo { WinType = typeof(LvHome).AssemblyQualifiedName, Title = "列表" });
            await Task.CompletedTask;
        }

        /// <summary>
        /// 接收分享内容
        /// </summary>
        /// <param name="p_info">分享内容描述</param>
        protected override void OnReceiveShare(ShareInfo p_info)
        {
            Kit.OpenWin(typeof(ReceiveShareWin), "接收分享", Icons.分享, p_info);
        }

        #region 自动生成
        protected override void Init()
        {
            // 视图名称与窗口类型的映射字典，主要菜单项用
            ViewTypes = new Dictionary<string, Type>
            {
                { "通讯录", typeof(Dt.Base.Chat.ChatHome) },
                { "报表", typeof(Dt.Mgr.ReportView) },
                { "流程设计", typeof(Dt.Mgr.Workflow.WorkflowMgr) },
                { "任务", typeof(Dt.Mgr.Workflow.TasksView) },
                { "发布", typeof(Dt.Mgr.Publish.PublishView) },
                { "发布管理", typeof(Dt.Mgr.Publish.PublishMgr) },
                { "基础选项", typeof(Dt.Mgr.Model.BaseOption) },
                { "菜单管理", typeof(Dt.Mgr.Model.MenuWin) },
                { "我的设置", typeof(Dt.Mgr.Model.MyParamsSetting) },
                { "参数定义", typeof(Dt.Mgr.Model.UserParamsWin) },
                { "基础权限", typeof(Dt.Mgr.Model.PrvWin) },
                { "报表设计", typeof(Dt.Mgr.Model.RptWin) },
                { "系统角色", typeof(Dt.Mgr.Model.RoleWin) },
                { "用户账号", typeof(Dt.Mgr.Model.UserAccountWin) },
                { "文件", typeof(Dt.Mgr.File.FileHome) },
                { "样例", typeof(Dt.Sample.SamplesMain) },
                { "ShoppingWin", typeof(Dt.Sample.ModuleView.OneToMany1.ShoppingWin) },
            };

            // 处理服务器推送的类型字典
            PushHandlers = new Dictionary<string, Type>
            {
                { "syspushapi", typeof(Dt.Base.SysPushApi) },
                { "webrtcapi", typeof(Dt.Base.Chat.WebRtcApi) },
            };

            // 本地库的结构信息，键为小写的库文件名(不含扩展名)，值为该库信息，包括版本号和表结构的映射类型
            SqliteDb = new Dictionary<string, SqliteTblsInfo>
            {
                {
                    "state",
                    new SqliteTblsInfo
                    {
                        Version = "047ebd4f0ef4957193958ba8aff3966b",
                        Tables = new List<Type>
                        {
                            typeof(Dt.Core.Model.ClientCookie),
                            typeof(Dt.Core.Model.DataVersion),
                            typeof(Dt.Core.Model.UserParams),
                            typeof(Dt.Core.Model.UserPrivilege),
                            typeof(Dt.Base.Docking.DockLayout),
                            typeof(Dt.Base.ModuleView.SearchHistory),
                            typeof(Dt.Base.FormView.CellLastVal),
                            typeof(Dt.Base.Chat.ChatMember),
                            typeof(Dt.Base.Chat.Letter),
                            typeof(Dt.Mgr.MenuFav),
                            typeof(Dt.Mgr.UserMenu),
                            typeof(Dt.Mgr.File.ReadFileHistory),
                        }
                    }
                },
            };
        }
        #endregion
    }
}