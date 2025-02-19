﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Mgr;
using Dt.Mgr.Model;
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion

namespace Dt.Sample.ModuleView
{
    public partial class MainList : Mv
    {
        string _query;

        public MainList()
        {
            InitializeComponent();
        }

        public async void Update()
        {
            if (string.IsNullOrEmpty(_query) || _query == "#全部")
            {
                _lv.Data = await AtCm.Query<RoleObj>("角色-所有");
            }
            else if (_query == "#系统角色")
            {
                _lv.Data = await AtCm.Query<RoleObj>("角色-系统角色");
            }
            else
            {
                _lv.Data = await AtCm.Query<RoleObj>("角色-模糊查询", new { name = $"%{_query}%" });
            }
        }

        protected override void OnInit(object p_params)
        {
            Update();
        }

        async void OnToSearch(object sender, Mi e)
        {
            var txt = await Forward<string>(_lzSm.Value);
            if (!string.IsNullOrEmpty(txt))
            {
                _query = txt;
                Title = "主实体列表 - " + txt;
                Update();
            }
        }

        Lazy<SearchMv> _lzSm = new Lazy<SearchMv>(() => new SearchMv
        {
            Placeholder = "名称",
            Fixed = { "全部",  },
        });

        void OnAdd(object sender, Mi e)
        {
            _win.Form.Update(-1);
            NaviToChildren();
        }

        void OnItemClick(object sender, ItemClickArgs e)
        {
            if (e.IsChanged)
                _win.Form.Update(e.Row.ID);
            NaviToChildren();
        }

        void NaviToChildren()
        {
            NaviTo(new List<Mv> { _win.Form, _win.RelatedList });
        }

        MainWin _win => (MainWin)_tab.OwnWin;
    }
}