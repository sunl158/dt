#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2021-09-17 创建
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

namespace Dt.Mgr.Publish
{
    public sealed partial class PostAlbumList : Mv
    {
        long _id;

        public PostAlbumList()
        {
            InitializeComponent();
        }

        public void Update(long p_id)
        {
            _id = p_id;
            Menu["添加"].IsEnabled = true;
            Refresh();
        }

        public void Clear()
        {
            _id = -1;
            Menu["添加"].IsEnabled = false;
            _lv.Data = null;
        }

        async void Refresh()
        {
            _lv.Data = await AtCm.Query("文章-已选专辑", new { postid = _id });
        }

        async void OnAdd(object sender, Mi e)
        {
            if (await new SelectAlbumDlg().Show(_id))
                Refresh();
        }
        async void OnDel(object sender, Mi e)
        {
            var pa = new PubPostalbumObj(PostID: _id, AlbumID: e.Row.ID);
            pa.IsAdded = false;
            if (await AtCm.Delete(pa))
                Refresh();
        }

        PostWin _win => (PostWin)_tab.OwnWin;
    }
}
