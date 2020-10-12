﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2020-10-10 创建
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

namespace Dt.Base.Report
{
    public sealed partial class DbDataWin : UserControl
    {
        RptDesignInfo _info;

        public DbDataWin(RptDesignInfo p_info)
        {
            InitializeComponent();
            _info = p_info;
        }

    }
}
