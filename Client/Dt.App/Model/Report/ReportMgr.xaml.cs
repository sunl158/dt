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
#endregion

namespace Dt.App.Model
{
    [View("报表设计")]
    public partial class ReportMgr : Win
    {
        public ReportMgr()
        {
            InitializeComponent();
            //LoadAll();
        }

        async void LoadAll()
        {
            _lv.Data = await AtCm.Query<RptObj>("报表-所有");
        }

        async void OnItemClick(object sender, ItemClickArgs e)
        {
            RptObj rpt = _fv.Data.To<RptObj>();
            if (rpt != null && rpt.IsChanged)
            {
                if (!await Kit.Confirm("数据已修改，确认要放弃修改吗？"))
                    return;

                rpt.RejectChanges();
            }

            _fv.Data = await AtCm.First<RptObj>("报表-ID", new { id = e.Data.To<RptObj>().ID });
            SelectTab("编辑");
        }

        async void OnSearch(object sender, string e)
        {
            if (e == "#全部")
            {
                LoadAll();
            }
            else if (e == "#最近修改")
            {
                _lv.Data = await AtCm.Query<RptObj>("报表-最近修改");
            }
            else if (!string.IsNullOrEmpty(e))
            {
                _lv.Data = await AtCm.Query<RptObj>("报表-模糊查询", new { input = $"%{e}%" });
            }
            SelectTab("列表");
        }

        async void OnAdd(object sender, Mi e)
        {
            _fv.Data = new RptObj(
                ID: await AtCm.NewID(),
                Name: "新报表");
        }

        async void OnSave(object sender, Mi e)
        {
            if (await AtCm.Save(_fv.Data.To<RptObj>()))
            {
                _lv.Data = await AtCm.Query<RptObj>("报表-最近修改");
                AtCm.PromptForUpdateModel();
            }
        }

        void OnDel(object sender, Mi e)
        {
            RptObj rpt = _fv.Data.To<RptObj>();
            if (rpt != null)
                DelRpt(rpt);
        }

        void OnDelContext(object sender, Mi e)
        {
            DelRpt(e.Data.To<RptObj>());
        }

        async void DelRpt(RptObj rpt)
        {
            if (rpt.IsAdded && !rpt.IsChanged)
            {
                _fv.Data = null;
                return;
            }

            if (!await Kit.Confirm($"确认要删除报表[{rpt.Name}]吗？"))
            {
                Kit.Msg("已取消删除！");
                return;
            }

            if (rpt.IsAdded)
            {
                _fv.Data = null;
            }
            else if (await AtCm.DelByID<RptObj>(rpt.ID))
            {
                _fv.Data = null;
                LoadAll();
                AtCm.PromptForUpdateModel();
            }
        }

        void OnEditTemplateContext(object sender, Mi e)
        {
            EditTemplate(e.Data.To<RptObj>());
        }

        void OnEditTemplate(object sender, Mi e)
        {
            RptObj rpt = _fv.Data.To<RptObj>();
            if (rpt != null)
                EditTemplate(rpt);
        }

        void EditTemplate(RptObj p_rpt)
        {
            _ = AtRpt.ShowDesign(new AppRptDesignInfo(p_rpt));
        }
    }
}