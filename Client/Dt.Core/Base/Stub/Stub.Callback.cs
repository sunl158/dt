﻿#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2020-07-21 创建
******************************************************************************/
#endregion

#region 引用命名
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#endregion

namespace Dt.Core
{
    /// <summary>
    /// 系统回调接口存根
    /// </summary>
    public abstract partial class Stub
    {
        /// <summary>
        /// 按默认流程开始运行，加载根页面。初次启动或刷新到根页面时调用
        /// <para>1. 记录主页和登录页的类型，以备登录、注销、自动登录、中途登录时用</para>
        /// <para>2. 不使用dt服务时，直接显示主页</para>
        /// <para>3. 已登录过，先自动登录</para>
        /// <para>4. 未登录或登录失败时，根据 p_loginFirst 显示登录页或主页</para>
        /// </summary>
        /// <param name="p_homePageType">主页类型，null时采用默认主页 DefaultHome</param>
        /// <param name="p_loginFirst">是否强制先登录，默认true</param>
        /// <param name="p_loginPageType">登录页类型，null时采用默认登录页 DefaultLogin</param>
        /// <returns></returns>
        public abstract Task StartRun(Type p_homePageType = null, bool p_loginFirst = true, Type p_loginPageType = null);

        /// <summary>
        /// 显示登录页面
        /// </summary>
        /// <param name="p_isPopup">是否为弹出式</param>
        public abstract void ShowLogin(bool p_isPopup);

        /// <summary>
        /// 注销后重新登录
        /// </summary>
        /// <returns></returns>
        public abstract void Logout();

        /// <summary>
        /// 加载根内容 Desktop/Frame 和主页
        /// </summary>
        public abstract void ShowHome();

        /// <summary>
        /// 当前登录页面类型，未设置时采用 DefaultLogin
        /// </summary>
        public abstract Type LoginPageType { get; }

        /// <summary>
        /// 主页类型
        /// </summary>
        public abstract Type HomePageType { get; }

        /// <summary>
        /// 注册接收服务器推送
        /// </summary>
        public abstract void RegisterSysPush();

        /// <summary>
        /// 主动停止接收推送
        /// </summary>
        public abstract void StopSysPush();

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="p_content">消息内容</param>
        /// <param name="p_title">标题</param>
        /// <returns>true表确认</returns>
        public abstract Task<bool> Confirm(string p_content, string p_title);

        /// <summary>
        /// 显示错误对话框
        /// </summary>
        /// <param name="p_content">消息内容</param>
        /// <param name="p_title">标题</param>
        public abstract void Error(string p_content, string p_title);

        /// <summary>
        /// 根据窗口/视图类型和参数激活旧窗口、打开新窗口 或 自定义启动(IView)
        /// </summary>
        /// <param name="p_type">窗口/视图类型</param>
        /// <param name="p_title">标题</param>
        /// <param name="p_icon">图标</param>
        /// <param name="p_params">初始参数</param>
        /// <returns>返回打开的窗口或视图，null表示打开失败</returns>
        public abstract object OpenWin(Type p_type, string p_title, Icons p_icon, object p_params);

        /// <summary>
        /// 根据视图名称激活旧窗口或打开新窗口
        /// </summary>
        /// <param name="p_viewName">窗口视图名称</param>
        /// <param name="p_title">标题</param>
        /// <param name="p_icon">图标</param>
        /// <param name="p_params">启动参数</param>
        /// <returns>返回打开的窗口或视图，null表示打开失败</returns>
        public abstract object OpenView(string p_viewName, string p_title, Icons p_icon, object p_params);

        /// <summary>
        /// 显示系统日志窗口
        /// </summary>
        public abstract void ShowTraceBox();

        /// <summary>
        /// 挂起时的处理，必须耗时小！
        /// 手机或PC平板模式下不占据屏幕时触发，此时不确定被终止还是可恢复
        /// </summary>
        /// <returns></returns>
        public abstract Task OnSuspending();

        /// <summary>
        /// 恢复会话时的处理，手机或PC平板模式下再次占据屏幕时触发
        /// </summary>
        public abstract void OnResuming();

        /// <summary>
        /// UI模式切换的回调方法，Phone UI 与 PC UI 切换
        /// </summary>
        public abstract void OnUIModeChanged();

        /// <summary>
        /// 选择单个图片
        /// </summary>
        /// <returns></returns>
        public abstract Task<FileData> PickImage();

        /// <summary>
        /// 选择多个图片
        /// </summary>
        /// <returns></returns>
        public abstract Task<List<FileData>> PickImages();

        /// <summary>
        /// 选择单个视频
        /// </summary>
        /// <returns></returns>
        public abstract Task<FileData> PickVideo();

        /// <summary>
        /// 选择多个视频
        /// </summary>
        /// <returns></returns>
        public abstract Task<List<FileData>> PickVideos();

        /// <summary>
        /// 选择单个音频文件
        /// </summary>
        /// <returns></returns>
        public abstract Task<FileData> PickAudio();

        /// <summary>
        /// 选择多个音频文件
        /// </summary>
        /// <returns></returns>
        public abstract Task<List<FileData>> PickAudios();

        /// <summary>
        /// 选择单个媒体文件
        /// </summary>
        /// <returns></returns>
        public abstract Task<FileData> PickMedia();

        /// <summary>
        /// 选择多个媒体文件
        /// </summary>
        /// <returns></returns>
        public abstract Task<List<FileData>> PickMedias();

        /// <summary>
        /// 选择单个文件
        /// </summary>
        /// <param name="p_fileTypes">
        /// uwp文件过滤类型，如 .png .docx，null时不过滤
        /// android文件过滤类型，如 image/png image/*，null时不过滤
        /// ios文件过滤类型，如 UTType.Image，null时不过滤
        /// </param>
        /// <returns></returns>
        public abstract Task<FileData> PickFile(string[] p_fileTypes);

        /// <summary>
        /// 选择多个文件
        /// </summary>
        /// <param name="p_fileTypes">
        /// uwp文件过滤类型，如 .png .docx，null时不过滤
        /// android文件过滤类型，如 image/png image/*，null时不过滤
        /// ios文件过滤类型，如 UTType.Image，null时不过滤
        /// </param>
        /// <returns></returns>
        public abstract Task<List<FileData>> PickFiles(string[] p_fileTypes);

        /// <summary>
        /// 拍照
        /// </summary>
        /// <param name="p_options">选项</param>
        /// <returns>照片文件信息，失败或放弃时返回null</returns>
        public abstract Task<FileData> TakePhoto(CapturePhotoOptions p_options);

        /// <summary>
        /// 录像
        /// </summary>
        /// <param name="p_options">选项</param>
        /// <returns>视频文件信息，失败或放弃时返回null</returns>
        public abstract Task<FileData> TakeVideo(CaptureVideoOptions p_options);

        /// <summary>
        /// 开始录音
        /// </summary>
        /// <param name="p_target">计时对话框居中的目标</param>
        /// <returns>录音文件信息，失败或放弃时返回null</returns>
        public abstract Task<FileData> TakeAudio(FrameworkElement p_target);

        /// <summary>
        /// 加载文件服务的图片，优先加载缓存，支持路径 或 FileList中json格式
        /// </summary>
        /// <param name="p_path">路径或FileList中json格式</param>
        /// <param name="p_img"></param>
        public abstract Task LoadImage(string p_path, Image p_img);
    }
}