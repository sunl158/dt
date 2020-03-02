﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2018-10-08 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.Core;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
#endregion

namespace Dt.App.Chat
{
    /// <summary>
    /// 聊天人员列表
    /// </summary>
    public sealed partial class ChatMemberList : UserControl
    {
        const string _refreshKey = "LastRefreshChatMember";


        public event EventHandler<long> ItemClick;

        public ChatMemberList()
        {
            InitializeComponent();

            // 头像视图扩展
            _lv.ViewEx = typeof(MemberIconViewEx);
            // 按姓名排序
            _lv.SortDesc = new SortDescription("name", ListSortDirection.Ascending);
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;

            // 超过10小时需要刷新
            bool refresh = true;
            string val = AtLocal.GetCookie(_refreshKey);
            if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out var last))
                refresh = (AtSys.Now - last).TotalHours >= 10;

            if (refresh)
                RefreshList();
            else
                LoadLocalList();
        }

        async void RefreshList()
        {
            var newTbl = await AtCm.Query("select * from cm_user");
            _lv.Data = newTbl;

            if (newTbl != null && newTbl.Count > 0)
            {
                foreach (Row row in newTbl)
                {
                    long id = row.ID;
                    var mem = AtLocal.GetFirst<ChatMember>("select id,mtime from ChatMember where id=@id", new Dict { { "id", id } });
                    // 最后修改时间不同，删除缓存的头像文件
                    if (mem != null && mem.Mtime != row.Date("mtime"))
                        AtLocal.DeleteFile($"{id}.png");
                }
            }

            // 将新列表缓存到本地库
            AtLocal.Execute("delete from ChatMember");
            if (newTbl != null && newTbl.Count > 0)
                AtLocal.Save(newTbl, "ChatMember");

            // 记录刷新时间
            AtLocal.SaveCookie(_refreshKey, AtSys.Now.ToString());
        }

        void LoadLocalList()
        {
            _lv.Data = AtLocal.Query("select id, name from ChatMember");
        }

        void OnItemClick(object sender, ItemClickArgs e)
        {
            ItemClick?.Invoke(this, e.Row.Long("id"));
        }
    }
}
