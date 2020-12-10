﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2013-12-16 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.App;
using Dt.Base;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;
#endregion

namespace Dt.App.Workflow
{
    public partial class CurrentTasks : Win
    {
        public CurrentTasks()
        {
            InitializeComponent();

            _lv.CellEx = typeof(ViewEx1);
            Refresh();
        }

        async void Refresh()
        {
            _lv.Data = await AtCm.Query("流程-待办任务", new { userID = AtUser.ID });
        }

        void OnRefresh(object sender, Mi e)
        {
            Refresh();
        }

        async void OnItemClick(object sender, ItemClickArgs e)
        {
            if (InputManager.IsCtrlPressed)
            {
                AtWf.OpenFormWin(new WfFormInfo(e.Row.Long("prcdid"), e.Row.Long("itemid")));
            }
            else if (e.IsChanged)
            {
                var info = new WfFormInfo(e.Row.Long("prcdid"), e.Row.Long("itemid"));
                var win = await AtWf.CreateFormWin(info);
                info.FormClosed += (s, arg) => Refresh();
                LoadCenter(win);
            }
        }
    }

    public class ViewEx1
    {
        public static Grid title(ViewItem p_item)
        {
            Grid grid = new Grid
            {
                ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                Children =
                        {
                            new TextBlock { Text = p_item.Row.Str("formname"), Margin= new Thickness(0,0,4,0), VerticalAlignment = VerticalAlignment.Center },
                        }
            };

            var rc = new Rectangle { Fill = AtRes.深灰边框 };
            Grid.SetColumn(rc, 1);
            grid.Children.Add(rc);

            var tb = new TextBlock { Text = p_item.Row.Str("atvname"), Margin = new Thickness(4, 2, 4, 2), Foreground = AtRes.WhiteBrush };
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);

            var kind = (WfiItemAssignKind)p_item.Row.Int("AssignKind");
            switch (kind)
            {
                case WfiItemAssignKind.Start:
                    rc = new Rectangle { Fill = AtRes.绿色背景 };
                    Grid.SetColumn(rc, 2);
                    grid.Children.Add(rc);

                    tb = new TextBlock { Text = "发起", Margin = new Thickness(4, 2, 4, 2), Foreground = AtRes.WhiteBrush };
                    Grid.SetColumn(tb, 2);
                    grid.Children.Add(tb);
                    break;

                case WfiItemAssignKind.Back:
                    rc = new Rectangle { Fill = AtRes.醒目红色 };
                    Grid.SetColumn(rc, 2);
                    grid.Children.Add(rc);

                    tb = new TextBlock { Text = "回退", Margin = new Thickness(4, 2, 4, 2), Foreground = AtRes.WhiteBrush };
                    Grid.SetColumn(tb, 2);
                    grid.Children.Add(tb);
                    break;

                case WfiItemAssignKind.Rollback:
                    rc = new Rectangle { Fill = AtRes.醒目蓝色 };
                    Grid.SetColumn(rc, 2);
                    grid.Children.Add(rc);

                    tb = new TextBlock { Text = "追回", Margin = new Thickness(4, 2, 4, 2), Foreground = AtRes.WhiteBrush };
                    Grid.SetColumn(tb, 2);
                    grid.Children.Add(tb);
                    break;

                case WfiItemAssignKind.Jump:
                    rc = new Rectangle { Fill = AtRes.BlackBrush };
                    Grid.SetColumn(rc, 2);
                    grid.Children.Add(rc);

                    tb = new TextBlock { Text = "跳转", Margin = new Thickness(4, 2, 4, 2), Foreground = AtRes.WhiteBrush };
                    Grid.SetColumn(tb, 2);
                    grid.Children.Add(tb);
                    break;
            }
            return grid;
        }
    }
}