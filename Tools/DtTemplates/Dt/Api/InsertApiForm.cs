﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Dt
{
    public partial class InsertApiForm : Form
    {
        public InsertApiForm()
        {
            InitializeComponent();
            _ns.Text = Kit.GetNamespace();
        }

        private void _btnOK_Click(object sender, EventArgs e)
        {
            string ns, cls;
            try
            {
                ns = Kit.GetText(_ns);
                cls = Kit.GetText(_cls);
            }
            catch
            {
                _lbl.Text = "当前内容不可为空！";
                return;
            }

            var dt = new Dictionary<string, string>
                {
                    {"$rootnamespace$", ns },
                    {"$safeitemname$", cls },
                    {"$time$", DateTime.Now.ToString("yyyy-MM-dd") },
                    {"$username$", Environment.UserName },
                };
            var path = Kit.GetFolderPath();
            Kit.WritePrjFile(Path.Combine(path, $"{cls}.cs"), "Dt.Api.Class.cs", dt);

            Close();
        }
    }
}
