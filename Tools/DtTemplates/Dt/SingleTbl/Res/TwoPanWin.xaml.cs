﻿#region 文件描述
/******************************************************************************
* 创建: $username$
* 摘要: 
* 日志: $time$ 创建
******************************************************************************/
#endregion

#region 引用命名
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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