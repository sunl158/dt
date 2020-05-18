﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2018-10-29 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base.FormView;
using Dt.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
#endregion

namespace Dt.Base
{
    /// <summary>
    /// 富文本格
    /// </summary>
    public partial class CHtml : FvCell
    {
        #region 构造方法
        public CHtml()
        {
            DefaultStyleKey = typeof(CHtml);
        }
        #endregion

        #region 事件
        /// <summary>
        /// 保存事件
        /// </summary>
        public event EventHandler Saved;
        #endregion

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            var btn = (Button)GetTemplateChild("BtnEdit");
            btn.Click += OnShowDlg;
        }

        void OnShowDlg(object sender, RoutedEventArgs e)
        {
            if (ReadOnlyBinding)
                return;

            var dlg = new HtmlEditDlg(this);
            if (!AtSys.IsPhoneUI)
            {
                dlg.ShowWinVeil = true;
                dlg.Height = SysVisual.ViewHeight - 140;
                dlg.Width = Math.Min(800, SysVisual.ViewWidth - 200);
            }
            dlg.ShowDlg();
        }

        internal void OnSaved()
        {
            Saved?.Invoke(this, EventArgs.Empty);
        }
    }
}