﻿using Dt.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dt.Sample
{
    public static class AtTest
    {
        #region TestSerialize
        /// <summary>
        /// 返回字符串
        /// </summary>
        /// <returns></returns>
        public static Task<string> GetString()
        {
            return Kit.Rpc<string>(
                "cm",
                "TestSerialize.GetString"
            );
        }

        /// <summary>
        /// 字符串参数
        /// </summary>
        /// <param name="p_str"></param>
        public static Task<bool> SetString(string p_str)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetString",
                p_str
            );
        }

        /// <summary>
        /// 返回bool值
        /// </summary>
        /// <returns></returns>
        public static Task<bool> GetBool()
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.GetBool"
            );
        }

        /// <summary>
        /// bool参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<bool> SetBool(bool p_val)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetBool",
                p_val
            );
        }

        /// <summary>
        /// 返回int值
        /// </summary>
        /// <returns></returns>
        public static Task<int> GetInt()
        {
            return Kit.Rpc<int>(
                "cm",
                "TestSerialize.GetInt"
            );
        }

        /// <summary>
        /// int参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<int> SetInt(int p_val)
        {
            return Kit.Rpc<int>(
                "cm",
                "TestSerialize.SetInt",
                p_val
            );
        }

        /// <summary>
        /// 返回long值
        /// </summary>
        /// <returns></returns>
        public static Task<long> GetLong()
        {
            return Kit.Rpc<long>(
                "cm",
                "TestSerialize.GetLong"
            );
        }

        /// <summary>
        /// long参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<long> SetLong(long p_val)
        {
            return Kit.Rpc<long>(
                "cm",
                "TestSerialize.SetLong",
                p_val
            );
        }

        /// <summary>
        /// 返回double值
        /// </summary>
        /// <returns></returns>
        public static Task<Double> GetDouble()
        {
            return Kit.Rpc<Double>(
                "cm",
                "TestSerialize.GetDouble"
            );
        }

        /// <summary>
        /// double参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<Double> SetDouble(Double p_val)
        {
            return Kit.Rpc<Double>(
                "cm",
                "TestSerialize.SetDouble",
                p_val
            );
        }

        /// <summary>
        /// 返回DateTime值
        /// </summary>
        /// <returns></returns>
        public static Task<DateTime> GetDateTime()
        {
            return Kit.Rpc<DateTime>(
                "cm",
                "TestSerialize.GetDateTime"
            );
        }

        /// <summary>
        /// DateTime参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<DateTime> SetDateTime(DateTime p_val)
        {
            return Kit.Rpc<DateTime>(
                "cm",
                "TestSerialize.SetDateTime",
                p_val
            );
        }

        /// <summary>
        /// 返回byte[]值
        /// </summary>
        /// <returns></returns>
        public static Task<Byte[]> GetByteArray()
        {
            return Kit.Rpc<Byte[]>(
                "cm",
                "TestSerialize.GetByteArray"
            );
        }

        /// <summary>
        /// byte[]参数
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<Byte[]> SetByteArray(Byte[] p_val)
        {
            return Kit.Rpc<Byte[]>(
                "cm",
                "TestSerialize.SetByteArray",
                p_val
            );
        }

        /// <summary>
        /// 返回MsgInfo
        /// </summary>
        /// <returns></returns>
        public static Task<MsgInfo> GetMsgInfo()
        {
            return Kit.Rpc<MsgInfo>(
                "cm",
                "TestSerialize.GetMsgInfo"
            );
        }

        /// <summary>
        /// MsgInfo参数
        /// </summary>
        /// <param name="p_msg"></param>
        public static Task<MsgInfo> SetMsgInfo(MsgInfo p_msg)
        {
            return Kit.Rpc<MsgInfo>(
                "cm",
                "TestSerialize.SetMsgInfo",
                p_msg
            );
        }

        /// <summary>
        /// 返回字符串数组
        /// </summary>
        /// <returns></returns>
        public static Task<List<string>> GetStringList()
        {
            return Kit.Rpc<List<string>>(
                "cm",
                "TestSerialize.GetStringList"
            );
        }

        /// <summary>
        /// 字符串列表
        /// </summary>
        /// <param name="p_ls"></param>
        public static Task<bool> SetStringList(List<string> p_ls)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetStringList",
                p_ls
            );
        }

        /// <summary>
        /// 返回bool值列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<bool>> GetBoolList()
        {
            return Kit.Rpc<List<bool>>(
                "cm",
                "TestSerialize.GetBoolList"
            );
        }

        /// <summary>
        /// bool值列表
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<List<bool>> SetBoolList(List<bool> p_val)
        {
            return Kit.Rpc<List<bool>>(
                "cm",
                "TestSerialize.SetBoolList",
                p_val
            );
        }

        /// <summary>
        /// 返回int值列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<int>> GetIntList()
        {
            return Kit.Rpc<List<int>>(
                "cm",
                "TestSerialize.GetIntList"
            );
        }

        /// <summary>
        /// int列表
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<List<int>> SetIntList(List<int> p_val)
        {
            return Kit.Rpc<List<int>>(
                "cm",
                "TestSerialize.SetIntList",
                p_val
            );
        }

        /// <summary>
        /// 返回long值列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<long>> GetLongList()
        {
            return Kit.Rpc<List<long>>(
                "cm",
                "TestSerialize.GetLongList"
            );
        }

        /// <summary>
        /// long列表
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<List<long>> SetLongList(List<long> p_val)
        {
            return Kit.Rpc<List<long>>(
                "cm",
                "TestSerialize.SetLongList",
                p_val
            );
        }

        /// <summary>
        /// 返回double值列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<double>> GetDoubleList()
        {
            return Kit.Rpc<List<double>>(
                "cm",
                "TestSerialize.GetDoubleList"
            );
        }

        /// <summary>
        /// double列表
        /// </summary>
        /// <param name="p_val"></param>
        public static Task<List<double>> SetDoubleList(List<double> p_val)
        {
            return Kit.Rpc<List<double>>(
                "cm",
                "TestSerialize.SetDoubleList",
                p_val
            );
        }

        /// <summary>
        /// DateTime列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<DateTime>> GetDateTimeList()
        {
            return Kit.Rpc<List<DateTime>>(
                "cm",
                "TestSerialize.GetDateTimeList"
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_times"></param>
        /// <returns></returns>
        public static Task<bool> SetDateTimeList(List<DateTime> p_times)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetDateTimeList",
                p_times
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Task<List<object>> GetObjectList()
        {
            return Kit.Rpc<List<object>>(
                "cm",
                "TestSerialize.GetObjectList"
            );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_ls"></param>
        /// <returns></returns>
        public static Task<bool> SetObjectList(params object[] p_ls)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetObjectList",
                (p_ls == null || p_ls.Length == 0) ? null : p_ls.ToList()
            );
        }

        /// <summary>
        /// 返回Table到客户端
        /// </summary>
        /// <returns></returns>
        public static Task<Table> GetTable()
        {
            return Kit.Rpc<Table>(
                "cm",
                "TestSerialize.GetTable"
            );
        }

        /// <summary>
        /// 由外部传递Table
        /// </summary>
        /// <param name="p_tbl"></param>
        public static Task<Table> SetTable(Table p_tbl)
        {
            return Kit.Rpc<Table>(
                "cm",
                "TestSerialize.SetTable",
                p_tbl
            );
        }

        /// <summary>
        /// 返回Row到客户端
        /// </summary>
        /// <returns></returns>
        public static Task<Row> GetRow()
        {
            return Kit.Rpc<Row>(
                "cm",
                "TestSerialize.GetRow"
            );
        }

        /// <summary>
        /// 由外部传递Row
        /// </summary>
        /// <param name="p_row"></param>
        public static Task<Row> SetRow(Row p_row)
        {
            return Kit.Rpc<Row>(
                "cm",
                "TestSerialize.SetRow",
                p_row
            );
        }

        public static Task<Table<CustomEntityObj>> GetEntityTable()
        {
            return Kit.Rpc<Table<CustomEntityObj>>(
                "cm",
                "TestSerialize.GetEntityTable"
            );
        }

        public static Task<bool> SetEntityTable(Table p_tbl)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetEntityTable",
                p_tbl
            );
        }

        public static Task<CustomEntityObj> GetEntity()
        {
            return Kit.Rpc<CustomEntityObj>(
                "cm",
                "TestSerialize.GetEntity"
            );
        }

        public static Task<bool> SetEntity(Row p_entity)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetEntity",
                p_entity
            );
        }

        /// <summary>
        /// 返回多个Table到客户端
        /// </summary>
        /// <returns></returns>
        public static Task<Dict> GetTableDict()
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestSerialize.GetTableDict"
            );
        }

        /// <summary>
        /// 由外部传递多个Table
        /// </summary>
        /// <param name="p_dict"></param>
        public static Task<Dict> SetTableDict(Dict p_dict)
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestSerialize.SetTableDict",
                p_dict
            );
        }

        /// <summary>
        /// 返回多个Table到客户端
        /// </summary>
        /// <returns></returns>
        public static Task<List<Table>> GetTableList()
        {
            return Kit.Rpc<List<Table>>(
                "cm",
                "TestSerialize.GetTableList"
            );
        }

        /// <summary>
        /// 由外部传递多个Table
        /// </summary>
        /// <param name="p_ls"></param>
        public static Task<List<Table>> SetTableList(List<Table> p_ls)
        {
            return Kit.Rpc<List<Table>>(
                "cm",
                "TestSerialize.SetTableList",
                p_ls
            );
        }

        /// <summary>
        /// 返回基本数据类型的Dict
        /// </summary>
        /// <returns></returns>
        public static Task<Dict> GetBaseDict()
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestSerialize.GetBaseDict"
            );
        }

        /// <summary>
        /// 返回基本数据类型的Dict
        /// </summary>
        /// <returns></returns>
        public static Task<Dict> GetCombineDict()
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestSerialize.GetCombineDict"
            );
        }

        /// <summary>
        /// 本数据类型的Dict
        /// </summary>
        /// <param name="p_dict"></param>
        public static Task<Dict> SendDict(Dict p_dict)
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestSerialize.SendDict",
                p_dict
            );
        }

        /// <summary>
        /// 返回Dict列表
        /// </summary>
        /// <returns></returns>
        public static Task<List<Dict>> GetDictList()
        {
            return Kit.Rpc<List<Dict>>(
                "cm",
                "TestSerialize.GetDictList"
            );
        }

        /// <summary>
        /// 发送Dict列表
        /// </summary>
        /// <param name="p_dicts"></param>
        public static Task<bool> SendDictList(List<Dict> p_dicts)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SendDictList",
                p_dicts
            );
        }

        /// <summary>
        /// 返回基础自定义类型
        /// </summary>
        /// <returns></returns>
        public static Task<Product> GetCustomBase()
        {
            return Kit.Rpc<Product>(
                "cm",
                "TestSerialize.GetCustomBase"
            );
        }

        /// <summary>
        /// 由外部传递基础自定义类型
        /// </summary>
        /// <param name="p_product"></param>
        /// <returns></returns>
        public static Task<bool> SetCustomBase(Product p_product)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetCustomBase",
                p_product
            );
        }

        /// <summary>
        /// 返回复杂自定义类型
        /// </summary>
        /// <returns></returns>
        public static Task<Student> GetCustomCombine()
        {
            return Kit.Rpc<Student>(
                "cm",
                "TestSerialize.GetCustomCombine"
            );
        }

        /// <summary>
        /// 由外部传递复杂自定义类型
        /// </summary>
        /// <param name="p_person"></param>
        /// <returns></returns>
        public static Task<bool> SetCustomCombine(Student p_person)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetCustomCombine",
                p_person
            );
        }

        /// <summary>
        /// 返回嵌套自定义类型
        /// </summary>
        /// <returns></returns>
        public static Task<Department> GetContainCustom()
        {
            return Kit.Rpc<Department>(
                "cm",
                "TestSerialize.GetContainCustom"
            );
        }

        /// <summary>
        /// 由外部传递嵌套自定义类型
        /// </summary>
        /// <param name="p_dept"></param>
        /// <returns></returns>
        public static Task<bool> SetContainCustom(Department p_dept)
        {
            return Kit.Rpc<bool>(
                "cm",
                "TestSerialize.SetContainCustom",
                p_dept
            );
        }

        public static Task AsyncVoid(string p_msg)
        {
            return Kit.Rpc<object>(
                "cm",
                "TestSerialize.AsyncVoid",
                p_msg
            );
        }

        public static Task<Table> AsyncDb()
        {
            return Kit.Rpc<Table>(
                "cm",
                "TestSerialize.AsyncDb"
            );
        }

        public static Task<int> AsyncWait()
        {
            return Kit.Rpc<int>(
                "cm",
                "TestSerialize.AsyncWait"
            );
        }
        #endregion

        #region TestException
        public static Task<string> ThrowException()
        {
            return Kit.Rpc<string>(
                "cm",
                "TestException.ThrowException"
            );
        }

        public static Task<string> ThrowBusinessException()
        {
            return Kit.Rpc<string>(
                "cm",
                "TestException.ThrowBusinessException"
            );
        }

        public static Task<string> ThrowPostionException()
        {
            return Kit.Rpc<string>(
                "cm",
                "TestException.ThrowPostionException"
            );
        }

        public static Task<Dict> ThrowSerializeException()
        {
            return Kit.Rpc<Dict>(
                "cm",
                "TestException.ThrowSerializeException"
            );
        }
        #endregion
    }
}
