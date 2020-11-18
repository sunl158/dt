﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-11-20 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Core;
using System;
using System.Threading.Tasks;
#endregion

namespace Dt.App.Model
{
    public partial class User
    {
        void OnSaving()
        {
            Throw.IfNullOrEmpty(Name, "名称不可为空！");
            Throw.IfNullOrEmpty(Phone, "手机号不可为空！");
        }
    }

    #region 自动生成
    [Tbl("cm_user")]
    public partial class User : Entity
    {
        #region 构造方法
        User() { }

        public User(
            long ID,
            string Phone = default,
            string Name = default,
            string Pwd = default,
            bool Sex = true,
            string Photo = default,
            bool Expired = false,
            DateTime Ctime = default,
            DateTime Mtime = default)
        {
            AddCell<long>("ID", ID);
            AddCell<string>("Phone", Phone);
            AddCell<string>("Name", Name);
            AddCell<string>("Pwd", Pwd);
            AddCell<bool>("Sex", Sex);
            AddCell<string>("Photo", Photo);
            AddCell<bool>("Expired", Expired);
            AddCell<DateTime>("Ctime", Ctime);
            AddCell<DateTime>("Mtime", Mtime);
            IsAdded = true;
            AttachHook();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 手机号，唯一
        /// </summary>
        public string Phone
        {
            get { return (string)this["Phone"]; }
            set { this["Phone"] = value; }
        }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name
        {
            get { return (string)this["Name"]; }
            set { this["Name"] = value; }
        }

        /// <summary>
        /// 密码的md5
        /// </summary>
        public string Pwd
        {
            get { return (string)this["Pwd"]; }
            set { this["Pwd"] = value; }
        }

        /// <summary>
        /// 性别，0女1男
        /// </summary>
        public bool Sex
        {
            get { return (bool)this["Sex"]; }
            set { this["Sex"] = value; }
        }

        /// <summary>
        /// 头像
        /// </summary>
        public string Photo
        {
            get { return (string)this["Photo"]; }
            set { this["Photo"] = value; }
        }

        /// <summary>
        /// 是否停用
        /// </summary>
        public bool Expired
        {
            get { return (bool)this["Expired"]; }
            set { this["Expired"] = value; }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime Ctime
        {
            get { return (DateTime)this["Ctime"]; }
            set { this["Ctime"] = value; }
        }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime Mtime
        {
            get { return (DateTime)this["Mtime"]; }
            set { this["Mtime"] = value; }
        }
        #endregion
    }
    #endregion

    #region 可复制
    /*
    public partial class User
    {
        async Task OnSaving()
        {
        }
    }

        async Task OnDeleting()
        {
        }

        public static async Task<User> New()
        {
        }

        public static async Task<User> Get(long p_id)
        {
        }

        void SetID(long p_value)
        {
        }

        void SetPhone(string p_value)
        {
        }

        void SetName(string p_value)
        {
        }

        void SetPwd(string p_value)
        {
        }

        void SetSex(bool p_value)
        {
        }

        void SetPhoto(string p_value)
        {
        }

        void SetExpired(bool p_value)
        {
        }

        void SetCtime(DateTime p_value)
        {
        }

        void SetMtime(DateTime p_value)
        {
        }
    */
    #endregion
}
