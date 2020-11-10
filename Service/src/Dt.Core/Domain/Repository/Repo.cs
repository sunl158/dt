﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2019-11-18 创建
******************************************************************************/
#endregion

#region 引用命名
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// mysql数据仓库，和客户端Repo方法基本相同
    /// </summary>
    public static class Repo
    {
        #region 查询
        /// <summary>
        /// 以参数值方式执行Sql语句，返回实体列表
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyOrSql">Sql字典中的键名(无空格) 或 Sql语句</param>
        /// <param name="p_params">参数值，支持Dict或匿名对象，默认null</param>
        /// <returns>返回实体列表</returns>
        public static Task<Table<TEntity>> Query<TEntity>(string p_keyOrSql, object p_params = null)
            where TEntity : Entity
        {
            return LobContext.Current.Db.Query<TEntity>(p_keyOrSql, p_params);
        }

        /// <summary>
        /// 以参数值方式执行Sql语句，返回实体枚举，高性能
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyOrSql">Sql字典中的键名(无空格) 或 Sql语句</param>
        /// <param name="p_params">参数值，支持Dict或匿名对象，默认null</param>
        /// <returns>返回实体枚举</returns>
        public static Task<IEnumerable<TEntity>> Each<TEntity>(string p_keyOrSql, object p_params = null)
            where TEntity : Entity
        {
            return LobContext.Current.Db.Each<TEntity>(p_keyOrSql, p_params);
        }

        /// <summary>
        /// 以参数值方式执行Sql语句，返回第一个实体对象，实体属性由Sql决定，不存在时返回null
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyOrSql">Sql字典中的键名(无空格) 或 Sql语句</param>
        /// <param name="p_params">参数值，支持Dict或匿名对象，默认null</param>
        /// <returns>返回实体对象或null</returns>
        public static Task<TEntity> Get<TEntity>(string p_keyOrSql, object p_params = null)
            where TEntity : Entity
        {
            return LobContext.Current.Db.Get<TEntity>(p_keyOrSql, p_params);
        }

        /// <summary>
        /// 根据主键获得实体对象(包含所有列值)，主键列名id，仅支持单主键
        /// 不存在时返回null，启用缓存时首先从缓存中获取
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_id">主键</param>
        /// <returns>返回实体对象或null</returns>
        public static Task<TEntity> GetByID<TEntity>(string p_id)
            where TEntity : Entity
        {
            return GetByKey<TEntity>("id", p_id);
        }

        /// <summary>
        /// 根据主键获得实体对象(包含所有列值)，主键列名id，仅支持单主键
        /// 不存在时返回null，启用缓存时首先从缓存中获取
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_id">主键</param>
        /// <returns>返回实体对象或null</returns>
        public static Task<TEntity> GetByID<TEntity>(long p_id)
            where TEntity : Entity
        {
            return GetByKey<TEntity>("id", p_id.ToString());
        }

        /// <summary>
        /// 根据主键或唯一索引列获得实体对象(包含所有列值)，仅支持单主键
        /// 不存在时返回null，启用缓存时首先从缓存中获取
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyName">主键列名</param>
        /// <param name="p_keyVal">主键值</param>
        /// <returns>返回实体对象或null</returns>
        public static async Task<TEntity> GetByKey<TEntity>(string p_keyName, string p_keyVal)
            where TEntity : Entity
        {
            var model = GetModel(typeof(TEntity));
            TEntity entity = null;
            if (model.CacheHandler != null)
                entity = await model.CacheHandler.Get<TEntity>(p_keyName, p_keyVal);

            if (entity == null)
            {
                entity = await LobContext.Current.Db.Get<TEntity>(
                    $"select * from `{model.Schema.Name}` where {p_keyName}=@{p_keyName}",
                    new Dict { { p_keyName, p_keyVal } });

                if (entity != null && model.CacheHandler != null)
                    await model.CacheHandler.Cache(entity);
            }
            return entity;
        }
        #endregion

        #region 保存
        /// <summary>
        /// 保存实体数据
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_entity">待保存的实体</param>
        /// <returns>是否成功</returns>
        public static async Task<bool> Save<TEntity>(TEntity p_entity)
            where TEntity : Entity
        {
            Throw.If(p_entity == null || (!p_entity.IsAdded && !p_entity.IsChanged), _unchangedMsg);

            var model = GetModel(typeof(TEntity));
            if (model.OnSaving != null)
                await OnSaving(model, p_entity);

            Dict dt = model.Schema.GetSaveSql(p_entity);
            if (await LobContext.Current.Db.Exec((string)dt["text"], (Dict)dt["params"]) != 1)
                return false;

            // 实体事件
            GatherSaveEvents(p_entity);

            // 更新实体时删除缓存
            if (model.CacheHandler != null && !p_entity.IsAdded && p_entity.IsChanged)
                await model.CacheHandler.Remove(p_entity);

            p_entity.AcceptChanges();
            return true;
        }

        /// <summary>
        /// 批量保存实体数据，根据实体状态执行增改，Table&lt;Entity&gt;支持删除，方法内部未启动事务！
        /// <para>列表类型支持：</para>
        /// <para>Table&lt;Entity&gt;，单表增删改</para>
        /// <para>List&lt;Entity&gt;，单表增改</para>
        /// <para>IList，多表增删改，成员可为Entity,List&lt;Entity&gt;,Table&lt;Entity&gt;的混合</para>
        /// </summary>
        /// <param name="p_list">待保存</param>
        /// <returns>true 保存成功</returns>
        public static Task<bool> BatchSave(IList p_list)
        {
            Throw.If(p_list == null || p_list.Count == 0, _unchangedMsg);

            Type tp = p_list.GetType();
            if (tp.IsGenericType
                && tp.GetGenericArguments()[0].IsSubclassOf(typeof(Entity)))
            {
                return BatchSaveSameType(p_list);
            }
            return BatchSaveMultiTypes(p_list);
        }

        /// <summary>
        /// 单表增删改，列表中的实体类型相同
        /// </summary>
        /// <param name="p_list"></param>
        /// <returns></returns>
        static async Task<bool> BatchSaveSameType(IList p_list)
        {
            var model = GetModel(p_list.GetType().GetGenericArguments()[0]);
            if (model.OnSaving != null)
            {
                foreach (var item in p_list)
                {
                    await OnSaving(model, item);
                }
            }
            var dts = model.Schema.GetBatchSaveSql(p_list);

            // 不需要保存
            if (dts == null || dts.Count == 0)
                Throw.Msg(_unchangedMsg);

            await BatchExec(dts);

            // 实体事件、缓存
            foreach (var entity in p_list.OfType<Entity>())
            {
                if (entity.IsChanged || entity.IsAdded)
                    await ApplyEventAndCache(entity, model);
            }

            if (p_list is Table tbl)
                tbl.DeletedRows?.Clear();
            return true;
        }

        /// <summary>
        /// 多表增删改
        /// </summary>
        /// <param name="p_list"></param>
        /// <returns></returns>
        static async Task<bool> BatchSaveMultiTypes(IList p_list)
        {
            var dts = new List<Dict>();
            string svc = null;
            foreach (var item in p_list)
            {
                if (item is Entity entity)
                {
                    if (entity.IsAdded || entity.IsChanged)
                    {
                        var model = GetModel(item.GetType());
                        if (model.OnSaving != null)
                            await OnSaving(model, entity);

                        if (svc == null)
                            svc = model.Svc;

                        dts.Add(model.Schema.GetSaveSql(entity));
                    }
                }
                else if (item is IList clist && clist.Count > 0)
                {
                    Type tp = item.GetType();
                    if (tp.IsGenericType && tp.GetGenericArguments()[0].IsSubclassOf(typeof(Entity)))
                    {
                        // IList<Entity> 或 Table<Entity>
                        var model = GetModel(tp.GetGenericArguments()[0]);
                        if (model.OnSaving != null)
                        {
                            foreach (var ci in clist)
                            {
                                await OnSaving(model, ci);
                            }
                        }

                        if (svc == null)
                            svc = model.Svc;

                        var cdts = model.Schema.GetBatchSaveSql(clist);
                        if (cdts != null && cdts.Count > 0)
                            dts.AddRange(cdts);
                    }
                }
            }

            // 不需要保存
            if (dts == null || dts.Count == 0)
                Throw.Msg(_unchangedMsg);

            await BatchExec(dts);

            foreach (var item in p_list)
            {
                if (item is Entity entity)
                {
                    if (entity.IsChanged || entity.IsAdded)
                    {
                        await ApplyEventAndCache(entity, GetModel(item.GetType()));
                    }
                }
                else if (item is IList clist && clist.Count > 0)
                {
                    Type tp = item.GetType();
                    if (tp.IsGenericType && tp.GetGenericArguments()[0].IsSubclassOf(typeof(Entity)))
                    {
                        // IList<Entity> 或 Table<Entity>
                        var model = GetModel(tp.GetGenericArguments()[0]);
                        foreach (var row in clist.OfType<Entity>())
                        {
                            if (row.IsAdded || row.IsChanged)
                            {
                                await ApplyEventAndCache(row, model);
                            }
                        }

                        if (item is Table tbl)
                            tbl.DeletedRows?.Clear();
                    }
                }
            }
            return true;
        }

        static async Task ApplyEventAndCache(Entity p_entity, EntitySchema p_model)
        {
            if (p_entity.IsAdded || p_entity.IsChanged)
            {
                GatherSaveEvents(p_entity);
                p_entity.AcceptChanges();
            }

            if (p_model.CacheHandler != null && !p_entity.IsAdded && p_entity.IsChanged)
                await p_model.CacheHandler.Remove(p_entity);
        }

        /// <summary>
        /// 批量执行多个Sql
        /// 参数列表，每个Dict中包含两个键：text,params，text为sql语句params类型为Dict或List{Dict}
        /// </summary>
        /// <param name="p_dts"></param>
        /// <returns></returns>
        static async Task BatchExec(List<Dict> p_dts)
        {
            var db = LobContext.Current.Db;
            foreach (Dict dt in p_dts)
            {
                string sql = (string)dt["text"];
                if (dt["params"] is List<Dict> ls)
                {
                    foreach (var par in ls)
                    {
                        await db.Exec(sql, par);
                    }
                }
                else if (dt["params"] is Dict par)
                {
                    await db.Exec(sql, par);
                }
            }
        }

        /// <summary>
        /// 保存前外部校验，不合格在外部抛出异常
        /// </summary>
        /// <param name="p_model"></param>
        /// <param name="p_entity"></param>
        /// <returns></returns>
        static async Task OnSaving(EntitySchema p_model, object p_entity)
        {
            if (p_model.OnSaving.ReturnType == typeof(Task))
                await (Task)p_model.OnSaving.Invoke(p_entity, null);
            else
                p_model.OnSaving.Invoke(p_entity, null);
        }
        #endregion

        #region 删除
        /// <summary>
        /// 删除实体，同步删除缓存，依靠数据库的级联删除自动删除子实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_entity">待删除的实体</param>
        /// <returns>实际删除行数</returns>
        public static async Task<int> Delete<TEntity>(TEntity p_entity)
            where TEntity : Entity
        {
            Throw.IfNull(p_entity, _saveError);

            var model = GetModel(typeof(TEntity));
            if (model.OnDeleting != null)
                await OnDeleting(model, p_entity);

            Dict dt = model.Schema.GetDeleteSql(new List<Entity> { p_entity });
            return await BatchExecDelete(dt, new List<Entity> { p_entity }, model);
        }

        /// <summary>
        /// 批量删除实体，单表或多表，列表类型支持：
        /// <para>Table&lt;Entity&gt;，单表删除</para>
        /// <para>List&lt;Entity&gt;，单表删除</para>
        /// <para>IList，多表删除，成员可为Entity,List&lt;Entity&gt;,Table&lt;Entity&gt;的混合</para>
        /// </summary>
        /// <param name="p_list">待删除实体列表</param>
        /// <returns>实际删除行数</returns>
        public static Task<int> BatchDelete(IList p_list)
        {
            Throw.If(p_list == null || p_list.Count == 0, _saveError);

            Type tp = p_list.GetType();
            if (tp.IsGenericType
                && tp.GetGenericArguments()[0].IsSubclassOf(typeof(Entity)))
            {
                return BatchDeleteSameType(p_list);
            }
            return BatchDeleteMultiTypes(p_list);
        }

        /// <summary>
        /// 根据主键删除实体对象，仅支持单主键id，同步删除缓存，依靠数据库的级联删除自动删除子实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_id">主键</param>
        /// <returns>实际删除行数</returns>
        public static Task<int> DelByID<TEntity>(string p_id)
            where TEntity : Entity
        {
            return DelByKey<TEntity>("id", p_id);
        }

        /// <summary>
        /// 根据主键删除实体对象，仅支持单主键id，同步删除缓存，依靠数据库的级联删除自动删除子实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_id">主键</param>
        /// <returns>实际删除行数</returns>
        public static Task<int> DelByID<TEntity>(long p_id)
            where TEntity : Entity
        {
            return DelByKey<TEntity>("id", p_id.ToString());
        }

        /// <summary>
        /// 根据单主键或唯一索引列删除实体，同步删除缓存，依靠数据库的级联删除自动删除子实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyName">主键或唯一索引列名</param>
        /// <param name="p_keyVal">主键值</param>
        /// <returns>实际删除行数</returns>
        public static Task<int> DelByKey<TEntity>(string p_keyName, long p_keyVal)
            where TEntity : Entity
        {
            return DelByKey<TEntity>(p_keyName, p_keyVal.ToString());
        }

        /// <summary>
        /// 根据单主键或唯一索引列删除实体，同步删除缓存，依靠数据库的级联删除自动删除子实体
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_keyName">主键或唯一索引列名</param>
        /// <param name="p_keyVal">主键值</param>
        /// <returns>实际删除行数</returns>
        public static async Task<int> DelByKey<TEntity>(string p_keyName, string p_keyVal)
            where TEntity : Entity
        {
            var model = GetModel(typeof(TEntity));

            // 有缓存或需要触发实体删除事件
            if (model.CacheHandler != null
                || (model.CudEvent & CudEvent.LocalDelete) == CudEvent.LocalDelete
                || (model.CudEvent & CudEvent.RemoteDelete) == CudEvent.RemoteDelete)
            {
                TEntity entity = null;
                if (model.CacheHandler != null)
                    entity = await model.CacheHandler.Get<TEntity>(p_keyName, p_keyVal);

                if (entity == null)
                {
                    entity = await LobContext.Current.Db.Get<TEntity>(
                        $"select * from `{model.Schema.Name}` where {p_keyName}=@{p_keyName}",
                        new Dict { { p_keyName, p_keyVal } });
                }
                if (entity != null)
                    return await Delete(entity);
                return 0;
            }

            // 无缓存无事件直接删除
            return await LobContext.Current.Db.Exec(
                $"delete from `{model.Schema.Name}` where {p_keyName}=@{p_keyName}",
                new Dict { { p_keyName, p_keyVal } });
        }

        /// <summary>
        /// 单表批量删除
        /// </summary>
        /// <param name="p_list"></param>
        /// <returns></returns>
        static async Task<int> BatchDeleteSameType(IList p_list)
        {
            var model = GetModel(p_list.GetType().GetGenericArguments()[0]);
            if (model.OnDeleting != null)
            {
                foreach (var item in p_list)
                {
                    await OnDeleting(model, item);
                }
            }

            Dict dt = model.Schema.GetDeleteSql(p_list);
            return await BatchExecDelete(dt, p_list, model);
        }

        /// <summary>
        /// 多表批量删除
        /// </summary>
        /// <param name="p_list"></param>
        /// <returns></returns>
        static async Task<int> BatchDeleteMultiTypes(IList p_list)
        {
            int cnt = 0;
            foreach (var item in p_list)
            {
                if (item is Entity entity)
                {
                    var model = GetModel(item.GetType());
                    if (model.OnDeleting != null)
                        await OnDeleting(model, item);

                    var ls = new List<Row> { entity };
                    Dict dt = model.Schema.GetDeleteSql(ls);
                    cnt += await BatchExecDelete(dt, ls, model);
                }
                else if (item is IList clist && clist.Count > 0)
                {
                    Type tp = item.GetType();
                    if (tp.IsGenericType && tp.GetGenericArguments()[0].IsSubclassOf(typeof(Entity)))
                    {
                        // IList<Entity> 或 Table<Entity>
                        var model = GetModel(tp.GetGenericArguments()[0]);
                        if (model.OnDeleting != null)
                        {
                            foreach (var ci in clist)
                            {
                                await OnDeleting(model, item);
                            }
                        }

                        Dict dt = model.Schema.GetDeleteSql(clist);
                        cnt += await BatchExecDelete(dt, clist, model);
                    }
                }
            }
            return cnt;
        }

        /// <summary>
        /// 单表批量执行，运行sql删除、收集领域事件、同步缓存
        /// </summary>
        /// <param name="p_dt"></param>
        /// <param name="p_list"></param>
        /// <param name="p_model"></param>
        /// <returns></returns>
        static async Task<int> BatchExecDelete(Dict p_dt, IList p_list, EntitySchema p_model)
        {
            int cnt = 0;
            string sql = (string)p_dt["text"];
            List<Dict> ls = (List<Dict>)p_dt["params"];
            var db = LobContext.Current.Db;

            for (int i = 0; i < p_list.Count; i++)
            {
                if (await db.Exec(sql, ls[i]) == 1)
                {
                    cnt++;
                    Entity entity = (Entity)p_list[i];
                    // 删除实体事件
                    GatherDelEvents(entity);
                    // 删除缓存
                    if (p_model.CacheHandler != null)
                        await p_model.CacheHandler.Remove(entity);
                }
            }
            return cnt;
        }

        /// <summary>
        /// 删除前外部校验，不合格在外部抛出异常
        /// </summary>
        /// <param name="p_model"></param>
        /// <param name="p_entity"></param>
        /// <returns></returns>
        static async Task OnDeleting(EntitySchema p_model, object p_entity)
        {
            if (p_model.OnDeleting.ReturnType == typeof(Task))
                await (Task)p_model.OnDeleting.Invoke(p_entity, null);
            else
                p_model.OnDeleting.Invoke(p_entity, null);
        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 创建空Table
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <returns>空表</returns>
        public static Table<TEntity> NewTable<TEntity>()
            where TEntity : Entity
        {
            var model = GetModel(typeof(TEntity));
            var tbl = new Table<TEntity>();
            foreach (var col in model.Schema.PrimaryKey.Concat(model.Schema.Columns))
            {
                tbl.Columns.Add(new Column(col.Name, col.Type));
            }
            return tbl;
        }
        #endregion

        #region 内部方法
        /// <summary>
        /// 保存实体时收集待发布的领域事件
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_entity"></param>
        static void GatherSaveEvents<TEntity>(TEntity p_entity)
            where TEntity : Entity
        {
            var lob = LobContext.Current;
            var events = p_entity.GetDomainEvents();
            if (events != null)
                lob.AddDomainEvents(events);

            var cudEvent = GetModel(typeof(TEntity)).CudEvent;
            if (cudEvent != CudEvent.None)
            {
                if (p_entity.IsAdded)
                {
                    if ((cudEvent & CudEvent.LocalInsert) == CudEvent.LocalInsert)
                        lob.AddDomainEvent(new DomainEvent(false, new InsertEvent<TEntity> { Entity = p_entity }));
                    if ((cudEvent & CudEvent.RemoteInsert) == CudEvent.RemoteInsert)
                        lob.AddDomainEvent(new DomainEvent(true, new InsertEvent<TEntity> { Entity = p_entity }));
                }
                else if (p_entity.IsChanged)
                {
                    if ((cudEvent & CudEvent.LocalUpdate) == CudEvent.LocalUpdate)
                        lob.AddDomainEvent(new DomainEvent(false, new UpdateEvent<TEntity> { Entity = p_entity }));
                    if ((cudEvent & CudEvent.RemoteUpdate) == CudEvent.RemoteUpdate)
                        lob.AddDomainEvent(new DomainEvent(true, new UpdateEvent<TEntity> { Entity = p_entity }));
                }
            }
        }

        /// <summary>
        /// 删除实体时收集待发布的领域事件
        /// </summary>
        /// <typeparam name="TEntity">实体类型</typeparam>
        /// <param name="p_entity"></param>
        static void GatherDelEvents<TEntity>(TEntity p_entity)
            where TEntity : Entity
        {
            var lob = LobContext.Current;
            var events = p_entity.GetDomainEvents();
            if (events != null)
                lob.AddDomainEvents(events);

            var cudEvent = GetModel(typeof(TEntity)).CudEvent;
            if (cudEvent != CudEvent.None)
            {
                if ((cudEvent & CudEvent.LocalDelete) == CudEvent.LocalDelete)
                    lob.AddDomainEvent(new DomainEvent(false, new DeleteEvent<TEntity> { Entity = p_entity }));
                if ((cudEvent & CudEvent.RemoteDelete) == CudEvent.RemoteDelete)
                    lob.AddDomainEvent(new DomainEvent(true, new DeleteEvent<TEntity> { Entity = p_entity }));
            }
        }

        const string _unchangedMsg = "没有需要保存的数据！";
        const string _saveError = "数据源不可为空！";

        static readonly ConcurrentDictionary<Type, EntitySchema> _models = new ConcurrentDictionary<Type, EntitySchema>();

        static EntitySchema GetModel(Type p_type)
        {
            if (_models.TryGetValue(p_type, out var m))
                return m;

            var model = new EntitySchema(p_type);
            _models[p_type] = model;
            return model;
        }
        #endregion
    }
}
