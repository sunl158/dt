﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2018-08-23 创建
******************************************************************************/
#endregion

#region 引用命名
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dt.Base;
using Dt.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endregion

namespace Dt.Sample
{
    public sealed partial class MvHome : Win
    {
        public MvHome()
        {
            InitializeComponent();
            _lv.Data = new Nl<MainInfo>
            {
                new MainInfo { Type = typeof(MvNavi), Title = "Tab内导航", Desc = "Mv之间导航时输入输出参数、带遮罩的模式视图" },
                new MainInfo { Type = typeof(SearchMvWin), Title = "搜索面板", Desc = "通用搜索功能，包括固定搜索项、历史搜索项、统一搜索事件、统一导航等功能" },
            };
        }
    }
}
