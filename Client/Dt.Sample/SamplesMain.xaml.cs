﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Mgr;
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
#endregion

namespace Dt.Sample
{
    /// <summary>
    /// 样例
    /// </summary>
    [View("样例")]
    public sealed partial class SamplesMain : Win
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        public SamplesMain()
        {
            InitializeComponent();
            LoadControlList();
            LoadModuleList();
        }

        void LoadControlList()
        {
            Nl<GroupData<Nav>> ds = new Nl<GroupData<Nav>>();

            #region 框架结构
            var group = new GroupData<Nav>
            {
                new Nav("空白窗口", typeof(BlankWin)) { Desc = "窗口内容为空" },
                new Nav("主区窗口", typeof(SingleViewWin)) { Desc = "有标题栏的空白窗口" },
                new Nav("动态主区窗口", typeof(ToggleWinCenter)) { Desc = "左区联动切换主区内容" },
                new Nav("三区窗口", typeof(ThreePanelWin)) { Desc = "左区、主区、右区" },
                new Nav("窗口布局", typeof(WinLayout)) { Desc = "Windows模式：所有Tab可拖动并自动停靠" },
                new Nav("窗口内导航", typeof(WinNavi)) { Desc = "Phone模式：所有Tab可互相导航" },
                new Nav("对话框", typeof(DlgDemo)) { To = NavTarget.WinMain, Desc = "模拟传统对话框" },
                new Nav("提示信息", typeof(NotifyDemo)) { To = NavTarget.WinMain, Desc = "普通信息、警告信息、Toast通知" },
            };
            group.Title = "框架结构";
            ds.Add(group);
            #endregion

            #region 基础控件
            group = new GroupData<Nav>
            {
                new Nav("表单Fv", typeof(FvHome)) { Desc = "表单、单元格、编辑器、自动布局" },
                new Nav("列表Lv", typeof(LvHome)) { Desc = "三种视图、两类数据源、各种变形" },
                new Nav("树", typeof(TvHome)) { Desc = "传统树，自定义节点样式、节点内容" },
                new Nav("数据图表", typeof(ChartHome)) { Desc = "柱线饼等9大类40种不同图表" },
                new Nav("Excel", typeof(ExcelHome)) { Desc = "模拟Excel的常用功能" },
                new Nav("报表", typeof(RptDemo)) { Desc = "报表模板设计、预览、导出、打印" },
                new Nav("菜单", typeof(MenuHome)) { Desc = "菜单、工具栏、上下文菜单" },
                new Nav("杂项", typeof(MiscHome)) { Desc = "分隔栏、可停靠面板等" },
            };
            group.Title = "基础控件";
            ds.Add(group);
            #endregion

            #region 样式资源
            group = new GroupData<Nav>
            {
                new Nav("字体", typeof(FontDemo)) { To = NavTarget.WinMain, Desc = "常用字体大小" },
                new Nav("常用画刷", typeof(BrushDemo)) { To = NavTarget.WinMain, Desc = "内部标准画刷" },
                new Nav("按钮", typeof(BtnDemo)) { To = NavTarget.WinMain, Desc = "常用按钮样式" },
                new Nav("标准控件", typeof(StyleHome)) { Desc = "常用系统控件的样式" },
            };
            group.Title = "样式资源";
            ds.Add(group);
            #endregion

            #region 临时
#if DEBUG
            group = new GroupData<Nav>
            {
                new Nav("测试1", typeof(TestDemo1)),
                new Nav("测试2", typeof(TestDemo2)),
            };
            group.Title = "临时";
            ds.Add(group);
#endif
            #endregion

            _navControl.Data = ds;
        }

        void LoadModuleList()
        {
            Nl<GroupData<Nav>> ds = new Nl<GroupData<Nav>>();

            #region 工具
            var group = new GroupData<Nav>
            {
                new Nav("查找图标", typeof(IconDemo)) { Desc = "内置的矢量文字，可用作图标、提示" },
#if WIN
                new Nav("生成App图片", typeof(AppIcon)) { To = NavTarget.WinMain, Desc = "生成 android 和 iOS 中用到的app图片" },
#endif
                new Nav("日志", typeof(LogDemo)) { Desc = "可通过AppStub.LogSetting设置日志输出，支持输出到Console、Trace或保存到文件" },
            };
            group.Title = "工具";
            ds.Add(group);
            #endregion

            #region 模块视图
            group = new GroupData<Nav>
            {
                new Nav("Mv之间导航", typeof(MvNavi)) { Desc = "导航时的参数传递、带遮罩的模式视图等" },
                new Nav("功能列表视图", typeof(NavListDemo)) { Desc = "通过功能项打开新窗口或切换主区内容" },
                new Nav("通用搜索视图", typeof(SearchMvWin)) { Desc = "包括固定搜索项、历史搜索项、搜索事件、导航等功能" },
            };

            if (Kit.IsUsingSvc)
            {
                group.AddRange(new List<Nav>
                {
                    new Nav("单表模板", typeof(MyEntityWin)) { Desc = "单表增删改模板，需要联网" },
                    new Nav("多对多模板", typeof(ModuleView.MainWin)) { Desc = "多对多增删改模板，需要联网" },
                    new Nav("一对多，三栏", typeof(ModuleView.OneToMany1.ShoppingWin)) { Desc = "一对多增删改模板，需要联网" },
                    new Nav("一对多，两栏", typeof(ModuleView.OneToMany2.ShoppingWin)) { Desc = "一对多增删改模板，需要联网" },
                });
            }
            group.Title = "模块视图";
            ds.Add(group);
            #endregion

            #region 综合
            if (Kit.IsUsingSvc)
            {
                group = new GroupData<Nav>
                {
                    new Nav("文件", typeof(FileHome)) { Desc = "跨平台文件选择、上传下载文件、不同类型图像资源，外网无法访问" },
                    new Nav("数据访问与异常", typeof(DataAccessHome)) { Desc = "创建数据对象、序列化、远程/本地数据的增删改查、远程过程调用等，外网无法访问" },
                    new Nav("切换到默认主页") { Callback = OpenHomeWin, Desc = "平台提供的默认主页，需要登录后才可加载，需要联网" },
                };
                group.Title = "综合";
                ds.Add(group);
            }
            #endregion

            _navModule.Data = ds;
        }

        async void OpenHomeWin(Win p_win, Nav p_nav)
        {
            var tp = typeof(DefaultHome);
            if (Kit.HomePageType != tp)
            {
                await Kit.StartRun();
            }
            else
            {
                Kit.Msg("当前已加载默认主页！");
            }
        }
    }
}