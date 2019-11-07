﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-10-21 创建
******************************************************************************/
#endregion

#region 引用命名
using System.Collections.Generic;
#endregion

namespace Dt.Core.Domain
{
    /// <summary>
    /// 聚合根类接口，包含"ID"主键
    /// </summary>
    /// <typeparam name="TKey">主键类型</typeparam>
    public interface IRoot<TKey> : IEntity<TKey>
    { }
}
