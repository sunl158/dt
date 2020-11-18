﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-10-15 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
#endregion

namespace Dt.Cm
{
    #region 自动生成
    [Tbl("cm_userrole")]
    public partial class Userrole : Entity
    {
        #region 构造方法
        Userrole() { }

        public Userrole(
            long UserID,
            long RoleID)
        {
            AddCell<long>("UserID", UserID);
            AddCell<long>("RoleID", RoleID);
            IsAdded = true;
            AttachHook();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 用户标识
        /// </summary>
        public long UserID
        {
            get { return (long)this["UserID"]; }
            set { this["UserID"] = value; }
        }

        /// <summary>
        /// 角色标识
        /// </summary>
        public long RoleID
        {
            get { return (long)this["RoleID"]; }
            set { this["RoleID"] = value; }
        }

        new public long ID { get { return -1; } }
        #endregion
    }
    #endregion

    #region 可复制
    /*
    public partial class Userrole
    {
        async Task OnSaving()
        {
        }
    }

        async Task OnDeleting()
        {
        }

        public static async Task<Userrole> New()
        {
        }

        void SetUserID(long p_value)
        {
        }

        void SetRoleID(long p_value)
        {
        }
    */
    #endregion
}
