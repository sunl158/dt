#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2021-09-15 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
#endregion

namespace Dt.Mgr.Model
{
    [View("基础权限")]
    public partial class PrvWin : Win
    {
        public PrvWin()
        {
            InitializeComponent();
        }

        public PrvList List => _list;

        public PrvForm Form => _form;

        public PrvRoleList RoleList => _roleList;

        public PrvUserList UserList => _userList;
    }
}