﻿#region 文件描述
/******************************************************************************
* 创建: $username$
* 摘要: 
* 日志: $time$ 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endregion

namespace $rootnamespace$
{
    [View("$entityname$Win")]
    public partial class $entityname$Win : Win
    {
        public $entityname$Win()
        {
            InitializeComponent();
        }

        public $entityname$List List => _list;

        public $entityname$Form Form => _form;
    }
}