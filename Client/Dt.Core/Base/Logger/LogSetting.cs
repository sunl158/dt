﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2022-02-28 创建
******************************************************************************/
#endregion

#region 引用命名
using Serilog.Events;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// 日志设置
    /// </summary>
    public class LogSetting
    {
        /// <summary>
        /// 是否将日志输出到Console，默认true
        /// </summary>
        public bool ConsoleEnabled { get; set; } = true;

        /// <summary>
        /// 是否将日志保存到文件，默认false
        /// </summary>
        public bool FileEnabled { get; set; }

        /// <summary>
        /// 是否将日志输出到Trace，默认true
        /// </summary>
        public bool TraceEnabled { get; set; } = true;

        /// <summary>
        /// 日志输出级别，默认Debug
        /// </summary>
        public LogEventLevel LogLevel { get; set; } = LogEventLevel.Debug;
    }
}