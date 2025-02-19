﻿using System.Text;
using System.Windows.Forms;

namespace Dt.Editor
{
    public partial class CBool : UserControl, ICellControl
    {
        public CBool()
        {
            InitializeComponent();
        }

        string ICellControl.GetText()
        {
            StringBuilder sb = new StringBuilder("<a:CBool");
            _header.GetText(sb);

            var txt = _trueVal.Text.Trim();
            if (txt != "")
                sb.Append($" TrueVal=\"{txt}\"");

            txt = _falseVal.Text.Trim();
            if (txt != "")
                sb.Append($" FalseVal=\"{txt}\"");

            if (_isSwitch.Checked)
                sb.Append(" IsSwitch=\"True\"");

            _footer.GetText(sb);
            sb.AppendLine(" />");
            return sb.ToString();
        }

        void ICellControl.Reset()
        {
            _header.Reset();
            _footer.Reset();
            _trueVal.Text = "";
            _falseVal.Text = "";
            _isSwitch.Checked = false;
        }
    }
}
