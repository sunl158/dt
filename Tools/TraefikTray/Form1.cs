﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Tray
{
    public partial class Form1 : Form
    {
        bool _trueClose;

        public Form1()
        {
            InitializeComponent();

            notifyIcon.Icon = new Icon(Assembly.GetExecutingAssembly().GetManifestResourceStream("Tray.logo.ico"));
        }


        private void btnCm_Click(object sender, EventArgs e)
        {
            Exec((Button)sender, tbCm, @"D:\Dt\Service\src\Dt.Cm\bin\Debug\net5.0", "Dt.Cm.exe", "--urls http://*:20201");
        }

        private void btnMsg_Click(object sender, EventArgs e)
        {
            Exec((Button)sender, tbMsg, @"D:\Dt\Service\src\Dt.Msg\bin\Debug\net5.0", "Dt.Msg.exe", "--urls http://*:20202");
        }

        private void btnFsm_Click(object sender, EventArgs e)
        {
            Exec((Button)sender, tbFsm, @"D:\Dt\Service\src\Dt.Fsm\bin\Debug\net5.0", "Dt.Fsm.exe", "--urls http://*:20203");
        }

        private void btnPub_Click(object sender, EventArgs e)
        {
            Exec((Button)sender, tbPub, @"D:\Dt\Service\src\Dt.Pub\bin\Debug\net5.0", "Dt.Pub.exe", "--urls http://*:20204");
        }

        private void btnTraefix_Click(object sender, EventArgs e)
        {
            Exec((Button)sender, tbTraefik, @"E:\Software\Last\Traefik", "traefik.exe", "-c traefik.toml --debug");
        }

        private void startMenuItem_Click(object sender, EventArgs e)
        {
            if (btnTraefix.Tag == null)
            {
                btnTraefix_Click(btnTraefix, null);
                Thread.Sleep(500);
            }

            if (btnCm.Tag == null)
            {
                btnCm_Click(btnCm, null);
                Thread.Sleep(500);
            }

            if (btnMsg.Tag == null)
            {
                btnMsg_Click(btnMsg, null);
                Thread.Sleep(500);
            }

            if (btnFsm.Tag == null)
            {
                btnFsm_Click(btnFsm, null);
                Thread.Sleep(500);
            }

            if (btnPub.Tag == null)
            {
                btnPub_Click(btnPub, null);
                Thread.Sleep(500);
            }
            notifyIcon.ShowBalloonTip(1, "消息", "所有服务启动完毕", ToolTipIcon.Info);
        }

        private void stopMenuItem_Click(object sender, EventArgs e)
        {
            CloseAllSvcs();
        }

        private void CloseAllSvcs()
        {
            if (btnTraefix.Tag != null)
                btnTraefix_Click(btnTraefix, null);

            if (btnCm.Tag != null)
                btnCm_Click(btnCm, null);

            if (btnMsg.Tag != null)
                btnMsg_Click(btnMsg, null);

            if (btnFsm.Tag != null)
                btnFsm_Click(btnFsm, null);

            if (btnPub.Tag != null)
                btnPub_Click(btnPub, null);
        }

        void Exec(Button btn, RichTextBox tb, string path, string exeFile, string exeParams)
        {
            Process pro = btn.Tag as Process;
            if (pro == null)
            {
                pro = new Process();
                pro.StartInfo.WorkingDirectory = path;
                pro.StartInfo.FileName = Path.Combine(path, exeFile);
                pro.StartInfo.Arguments = exeParams;
                pro.StartInfo.RedirectStandardOutput = true;
                pro.StartInfo.RedirectStandardError = true;
                pro.StartInfo.UseShellExecute = false;
                pro.StartInfo.CreateNoWindow = true;

                pro.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data))
                        return;

                    BeginInvoke((MethodInvoker)delegate
                    {
                        tb.AppendText("\r\n");
                        tb.AppendText(e.Data);
                    });
                };

                pro.Start();
                pro.BeginOutputReadLine();
                btn.Tag = pro;
                btn.Text = "停止";
            }
            else
            {
                try
                {
                    pro.Kill();
                }
                catch { }
                btn.Tag = null;
                btn.Text = "启动";
                tb.AppendText("\r\n服务已停止");
            }
        }

        private void Pro_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
                return;

            BeginInvoke((MethodInvoker)delegate
            {
                tbCm.AppendText("\r\n");
                tbCm.AppendText(e.Data);
            });
        }

        private void closeMenuItem_Click(object sender, EventArgs e)
        {
            _trueClose = true;
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_trueClose)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                CloseAllSvcs();

            }
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (WindowState == FormWindowState.Minimized)
                    WindowState = FormWindowState.Normal;
                Show();
                Activate();
            }
        }

    }
}
