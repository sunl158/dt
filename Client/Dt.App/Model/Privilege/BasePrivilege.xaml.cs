﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Core;
using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml;
#endregion

namespace Dt.App.Model
{
    [View("基础权限")]
    public partial class BasePrivilege : Win
    {
        public BasePrivilege()
        {
            InitializeComponent();
            LoadAll();
        }

        async void LoadAll()
        {
            _lvPrv.Data = await AtCm.Query<PrvObj>("权限-所有");
        }

        async void OnSearch(object sender, string e)
        {
            if (e == "#全部")
            {
                LoadAll();
            }
            else if (!string.IsNullOrEmpty(e))
            {
                _lvPrv.Data = await AtCm.Query<PrvObj>("权限-模糊查询", new { id = $"%{e}%" });
            }
            NaviTo("权限列表");
        }

        async void OnAddPrv(object sender, Mi e)
        {
            if (await new EditPrvDlg().Show(null))
                LoadAll();
        }

        async void OnEditPrv(object sender, Mi e)
        {
            if (await new EditPrvDlg().Show(e.Data.To<PrvObj>().ID))
                LoadAll();
        }

        async void OnDelPrv(object sender, Mi e)
        {
            var prv = e.Data.To<PrvObj>();
            if (!await Kit.Confirm($"确认要删除[{prv.ID}]吗？"))
            {
                Kit.Msg("已取消删除！");
                return;
            }

            if (await AtCm.Delete(prv))
                LoadAll();
        }

        void OnItemClick(object sender, ItemClickArgs e)
        {
            if (e.IsChanged)
                RefreshRelation(e.Data.To<PrvObj>().ID);

            NaviTo("授权角色,授权用户");
        }

        async void RefreshRelation(string p_prvid)
        {
            _lvUser.Data = await AtCm.Query("权限-关联用户", new { prvid = p_prvid });
            _lvRole.Data = await AtCm.Query("权限-关联角色", new { prvid = p_prvid });
        }

        void OnDataChanged(object sender, object e)
        {
            _lvUser.Data = null;
            _lvRole.Data = null;
        }

        void OnMultiMode(object sender, Mi e)
        {
            _lvRole.SelectionMode = SelectionMode.Multiple;
            _rMenu.Hide("添加", "选择");
            _rMenu.Show("移除", "全选", "取消");
        }

        void OnCancelMulti(object sender, Mi e)
        {
            _lvRole.SelectionMode = SelectionMode.Single;
            _rMenu.Show("添加", "选择");
            _rMenu.Hide("移除", "全选", "取消");
        }

        void OnSelectAll(object sender, Mi e)
        {
            _lvRole.SelectAll();
        }

        async void OnAddRole(object sender, Mi e)
        {
            string prvID = _lvPrv.SelectedItem.To<PrvObj>().ID;
            SelectRolesDlg dlg = new SelectRolesDlg();

            if (await dlg.Show(RoleRelations.Prv, prvID, e))
            {
                List<RolePrvObj> ls = new List<RolePrvObj>();
                foreach (var row in dlg.SelectedItems.OfType<Row>())
                {
                    ls.Add(new RolePrvObj(row.ID, prvID));
                }
                if (ls.Count > 0 && await AtCm.BatchSave(ls))
                {
                    RefreshRelation(prvID);
                    await AtCm.DeleteDataVer(ls.Select(rm => rm.RoleID).ToList(), "privilege");
                }
            }
        }

        void OnRemoveRole(object sender, Mi e)
        {
            RemoveRole(_lvRole.SelectedRows);
        }

        void OnRemoveRole2(object sender, Mi e)
        {
            if (_lvRole.SelectionMode == SelectionMode.Multiple)
                RemoveRole(_lvRole.SelectedRows);
            else
                RemoveRole(new List<Row> { e.Row });
        }

        async void RemoveRole(IEnumerable<Row> p_rows)
        {
            string prvID = _lvPrv.SelectedItem.To<PrvObj>().ID;
            List<RolePrvObj> ls = new List<RolePrvObj>();
            foreach (var row in p_rows)
            {
                ls.Add(new RolePrvObj(row.Long("roleid"), prvID));
            }
            if (ls.Count > 0 && await AtCm.BatchDelete(ls))
            {
                RefreshRelation(prvID);
                await AtCm.DeleteDataVer(ls.Select(rm => rm.RoleID).ToList(), "privilege");
            }
        }
    }
}