#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2014-07-01 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Charts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#endregion

namespace Dt.Base
{
    [ContentProperty(Name = "Children")]
    public partial class Chart : ContentControl, IDisposable
    {
        #region 静态内容
        public static readonly DependencyProperty AggregateProperty = DependencyProperty.Register(
            "Aggregate",
            typeof(Aggregate),
            typeof(Chart),
            new PropertyMetadata(Aggregate.None, new PropertyChangedCallback(Chart.OnAggregateChanged)));

        public static readonly DependencyProperty ChartTypeProperty = Utils.RegisterProperty(
            "ChartType",
            typeof(ChartType),
            typeof(Chart),
            new PropertyChangedCallback(Chart.OnChartTypeChanged), ChartType.Column);

        public static readonly DependencyProperty ClipToBoundsProperty = DependencyProperty.Register(
            "ClipToBounds",
            typeof(bool),
            typeof(Chart),
            new PropertyMetadata((bool)false, new PropertyChangedCallback(Chart.OnClipToBoundsChanged)));

        public static readonly DependencyProperty CustomPaletteProperty = DependencyProperty.Register(
            "CustomPalette",
            typeof(IEnumerable),
            typeof(Chart),
            new PropertyMetadata(null, new PropertyChangedCallback(Chart.OnCustomPaletteChanged)));

        public static readonly DependencyProperty DataProperty = Utils.RegisterProperty(
            "Data",
            typeof(ChartData),
            typeof(Chart),
            new PropertyChangedCallback(Chart.OnDataChanged));

        static readonly DependencyProperty ForegroundInternalProperty = DependencyProperty.Register(
            "ForegroundInternal",
            typeof(Brush),
            typeof(Chart),
            new PropertyMetadata(null, new PropertyChangedCallback(Chart.OnForegroundInternalChanged)));

        public static readonly DependencyProperty LegendItemsProperty = DependencyProperty.Register(
            "LegendItems",
            typeof(LegendItemCollection),
            typeof(Chart),
            new PropertyMetadata(null, new PropertyChangedCallback(Chart.OnLegendItemsChanged)));

        /// <summary>
        /// 标题
        /// </summary>
        public readonly static DependencyProperty HeaderProperty = DependencyProperty.Register(
            "Header",
            typeof(object),
            typeof(Chart),
            new PropertyMetadata(null));

        public static readonly DependencyProperty ViewProperty = Utils.RegisterProperty(
            "View",
            typeof(ChartView),
            typeof(Chart),
            new PropertyChangedCallback(Chart.OnViewChanged));


        static void OnAggregateChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            if (chart.Data != null)
            {
                using (IEnumerator<DataSeries> enumerator = chart.Data.Children.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        enumerator.Current.Dirty = true;
                    }
                }
            }
            chart.dataChanged = true;
            chart.forceRebuild = true;
            chart.InvalidateChart();
        }


        internal static void OnAttachedPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = obj as Chart;
            if (chart != null)
            {
                if (args.NewValue != args.OldValue)
                {
                    chart.forceRebuild = true;
                    chart.InvalidateChart();
                }
            }
            else
            {
                DataSeries series = obj as DataSeries;
                if (series != null)
                {
                    series.FirePropertyChanged(args.Property.ToString());
                }
            }
        }

        static void OnChartTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            ChartType newValue = (ChartType)args.NewValue;
            if (chart._loaded)
            {
                chart.ApplyChartType(newValue);
            }
        }

        static void OnClipToBoundsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((Chart)obj).InvalidateArrange();
        }

        static void OnCustomPaletteChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            chart.StyleGenerator.Reset();
            object newValue = args.NewValue;
            IList<Windows.UI.Color> list = newValue as IList<Windows.UI.Color>;
            if (list != null)
            {
                Brush[] brushArray = new Brush[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    brushArray[i] = new SolidColorBrush(list[i]);
                }
                newValue = brushArray;
            }
            Brush[] brushArray2 = newValue as Brush[];
            if ((brushArray2 != null) && (brushArray2.Length > 0))
            {
                chart.StyleGenerator.CustomBrushes = brushArray2;
            }
            else if (brushArray2 != null)
            {
                IList<Brush> list2 = (IList<Brush>)brushArray2;
                int num2 = list2.Count;
                if (num2 > 0)
                {
                    Brush[] brushArray3 = new Brush[num2];
                    for (int j = 0; j < num2; j++)
                    {
                        brushArray3[j] = list2[j];
                    }
                    chart.StyleGenerator.CustomBrushes = brushArray3;
                }
            }
            else
            {
                ResourceDictionary dictionary = newValue as ResourceDictionary;
                if (dictionary != null)
                {
                    List<Brush> list3 = new List<Brush>();
                    foreach (object obj3 in Enumerable.OrderBy<object, object>((IEnumerable<object>)dictionary.Keys, delegate (object key)
                    {
                        return key;
                    }))
                    {
                        if (dictionary[obj3] is Brush)
                        {
                            list3.Add((Brush)dictionary[obj3]);
                        }
                    }
                    chart.StyleGenerator.CustomBrushes = (list3.Count > 0) ? list3.ToArray() : null;
                }
                else
                {
                    IEnumerable enumerable2 = newValue as IEnumerable;
                    if (enumerable2 != null)
                    {
                        IEnumerator enumerator = enumerable2.GetEnumerator();
                        List<Brush> list4 = new List<Brush>();
                        while (enumerator.MoveNext())
                        {
                            object current = enumerator.Current;
                            if (current is Brush)
                            {
                                list4.Add((Brush)current);
                            }
                            else if (current is Windows.UI.Color)
                            {
                                list4.Add(new SolidColorBrush((Windows.UI.Color)current));
                            }
                        }
                        chart.StyleGenerator.CustomBrushes = (list4.Count > 0) ? list4.ToArray() : null;
                    }
                    else
                    {
                        chart.StyleGenerator.CustomBrushes = null;
                    }
                }
            }
            chart.InvalidateChart();
        }

        static void OnDataChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            chart.ClearAllINP();
            ChartData oldValue = (ChartData)args.OldValue;
            if (oldValue != null)
            {
                if (((oldValue.Renderer == chart.Renderers.Pie) || (oldValue.Renderer == chart.Renderers.Radar)) || (oldValue.Renderer == chart.Renderers.Renderer2D))
                {
                    oldValue.Renderer = null;
                }
                oldValue.DataChanged -= new EventHandler(chart._data_DataChanged);
            }
            oldValue = (ChartData)args.NewValue;
            if (oldValue != null)
            {
                oldValue.DataChanged += new EventHandler(chart._data_DataChanged);
            }
            chart.StyleGenerator.Reset();
            if (oldValue != null)
            {
                oldValue.Renderer = null;
            }
            chart.InvalidateChart();
        }


        static void OnForegroundInternalChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((Chart)obj).InvalidateChart();
        }

        static void OnLegendItemsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            if (args.NewValue != chart._litemsRO)
            {
                chart.LegendItems = chart._litemsRO;
            }
        }

        static void OnViewChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Chart chart = (Chart)obj;
            ChartView oldValue = (ChartView)args.OldValue;
            if (oldValue != null)
            {
                oldValue.Chart = null;
            }
            oldValue = (ChartView)args.NewValue;
            if (oldValue != null)
            {
                oldValue.Chart = chart;
            }
            chart.InvalidateChart();
        }

        static void ClearValue(FrameworkElement fe, DependencyProperty dp)
        {
            if (fe != null)
            {
                object obj2 = fe.ReadLocalValue(dp);
                if (dp == CustomPaletteProperty)
                {
                    fe.ClearValue(dp);
                }
                if (obj2 is BindingBase)
                {
                    fe.ClearValue(dp);
                }
            }
        }

        #endregion

        #region 成员变量
        readonly ChartObservableCollection _children = new ChartObservableCollection();
        Grid _contentGrid;
        ContentPresenter _viewPresenter;
        ActionCollection _actions;
        bool _autoSeries;
        ChartBindings _bindings;
        ResourceDictionary _customTheme = new ResourceDictionary();
        GradientMethod _gradientMethod;
        bool _inBuild;
        List<INotifyPropertyChanged> _inps = new List<INotifyPropertyChanged>();
        LegendItems _litems;
        LegendItemCollection _litemsRO;
        bool _loaded;
        Renderers _renderers;
        StyleGenerator _stgen;
        int _updateCount;
        FrameworkElement _viewElement;
        bool dataChanged = true;
        internal bool forceRebuild = true;
        Point pinchCenter = new Point();
        #endregion

        #region 构造方法
        public Chart()
        {
            DefaultStyleKey = typeof(Chart);

            BubbleOptions.SetMinSize(this, Size.Empty);
            BubbleOptions.SetMaxSize(this, Size.Empty);
            Data = new ChartData();
            View = new ChartView();

            Binding binding = new Binding();
            binding.Source = this;
            binding.Path = new PropertyPath("Foreground");
            SetBinding(ForegroundInternalProperty, binding);

            _litems = new LegendItems();
            _litemsRO = new LegendItemCollection(_litems);
            LegendItems = _litemsRO;

            PointerPressed += C1Chart_PointerPressed;
            PointerReleased += C1Chart_PointerReleased;
            PointerMoved += C1Chart_PointerMoved;
            ManipulationStarted += C1Chart_ManipulationStarted;
            ManipulationDelta += C1Chart_ManipulationDelta;
            ManipulationCompleted += C1Chart_ManipulationCompleted;
            Loaded += C1Chart_Loaded;
        }
        #endregion

        #region 事件
        /// <summary>
        /// 
        /// </summary>
        public event EventHandler ActionEnter;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler ActionLeave;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler LegendChanged;
        #endregion

        #region 属性
        /// <summary>
        /// 获取设置图表类型
        /// </summary>
        public ChartType ChartType
        {
            get { return (ChartType)GetValue(ChartTypeProperty); }
            set { SetValue(ChartTypeProperty, value); }
        }

        /// <summary>
        /// 获取设置图表数据
        /// </summary>
        public ChartData Data
        {
            get { return (ChartData)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        /// <summary>
        /// 获取设置图表标题内容
        /// </summary>
        public object Header
        {
            get { return GetValue(HeaderProperty); }
            set { SetValue(HeaderProperty, value); }
        }

        /// <summary>
        /// 获取设置图表调色板
        /// </summary>
        [DefaultValue(0)]
        public Palette Palette
        {
            get { return StyleGenerator.Palette; }
            set
            {
                StyleGenerator.Palette = value;
                forceRebuild = true;
                InvalidateChart();
            }
        }

        internal ActionCollection Actions
        {
            get
            {
                if (_actions == null)
                {
                    _actions = new ActionCollection(this);
                }
                return _actions;
            }
        }

        [DefaultValue(40)]
        public double ActionUpdateDelay
        {
            get { return Actions.UpdateDelay; }
            set { Actions.UpdateDelay = value; }
        }

        public Aggregate Aggregate
        {
            get { return (Aggregate)GetValue(AggregateProperty); }
            set { SetValue(AggregateProperty, value); }
        }

        public bool AutoGenerateSeries
        {
            get { return _autoSeries; }
            set
            {
                if (_autoSeries != value)
                {
                    _autoSeries = value;
                    dataChanged = true;
                    InvalidateChart();
                }
            }
        }

        public ChartBindings Bindings
        {
            get { return _bindings; }
            set
            {
                if (_bindings != value)
                {
                    _bindings = value;
                    dataChanged = true;
                    InvalidateChart();
                }
            }
        }

        public ObservableCollection<UIElement> Children
        {
            get { return _children; }
        }

        public bool ClipToBounds
        {
            get { return (bool)GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        public IEnumerable CustomPalette
        {
            get { return (IEnumerable)GetValue(CustomPaletteProperty); }
            set { SetValue(CustomPaletteProperty, value); }
        }

        public GestureDoubleTapAction GestureDoubleTap { get; set; }

        public GesturePinchAction GesturePinch { get; set; }

        public GestureSlideAction GestureSlide { get; set; }

        public GradientMethod GradientMethod
        {
            get { return _gradientMethod; }
            set
            {
                if (_gradientMethod != value)
                {
                    _gradientMethod = value;
                    InvalidateChart();
                }
            }
        }

        public LegendItemCollection LegendItems
        {
            get { return (LegendItemCollection)GetValue(LegendItemsProperty); }
            set { SetValue(LegendItemsProperty, value); }
        }

        internal LegendItems LegendItemsInternal
        {
            get { return _litems; }
        }

        internal Renderers Renderers
        {
            get
            {
                if (_renderers == null)
                {
                    _renderers = new Renderers();
                }
                return _renderers;
            }
        }

        StyleGenerator StyleGenerator
        {
            get
            {
                if (_stgen == null)
                {
                    _stgen = new StyleGenerator();
                    _stgen.CustomBrushConverter = new Dt.Charts.Converter<Brush, Brush>(BrushConverter);
                }
                return _stgen;
            }
        }

        internal int UpdateCount
        {
            get { return _updateCount; }
            set
            {
                if (value <= 0)
                {
                    _updateCount = 0;
                    InvalidateChart();
                }
                else
                {
                    _updateCount = value;
                }
            }
        }

        public ChartView View
        {
            get { return (ChartView)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        internal FrameworkElement ViewElement
        {
            get { return _viewElement; }
            set
            {
                try
                {
                    if (_viewElement != value)
                    {
                        _viewElement = value;
                        Content = _viewElement;
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        #endregion

        void _data_DataChanged(object sender, EventArgs e)
        {
            if ((Data != null) && (Data.Children.Count == 0))
            {
                StyleGenerator.Reset();
            }
            forceRebuild = true;
            dataChanged = true;
            InvalidateChart();
        }

        void AddINP(INotifyPropertyChanged inp)
        {
            inp.PropertyChanged += inp_PropertyChanged;
            _inps.Add(inp);
        }

        internal void ApplyChartType(ChartType chartType)
        {
            ApplyChartType(chartType.ToString());
        }

        void ApplyChartType(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                ChartSubtype subtype = ChartTypes.GetSubtype(type);
                if (subtype != null)
                {
                    subtype.Apply(this);
                }
            }
        }

        public void BeginUpdate()
        {
            UpdateCount++;
        }

        Brush BrushConverter(Brush brush)
        {
            if (GradientMethod == GradientMethod.None)
            {
                return brush;
            }
            return brush;
        }

        void C1Chart_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if ((GesturePinch == GesturePinchAction.Scale) && (Data.Renderer is Renderer2D))
            {
                if (Actions.State == ActionType.Pinch)
                {
                    Actions.State = ActionType.None;
                    e.Handled = true;
                    Actions.FireLeave();
                }
            }
            else if (((GestureSlide == GestureSlideAction.Translate) && (Data.Renderer is Renderer2D)) && (Actions.State == ActionType.Translate))
            {
                Actions.State = ActionType.None;
                e.Handled = true;
                Actions.FireLeave();
            }
        }

        void C1Chart_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.Delta.Scale != 1.0)
            {
                if ((GesturePinch == GesturePinchAction.Scale) && (Data.Renderer is Renderer2D))
                {
                    Actions.FireEnter();
                    Actions.State = ActionType.Pinch;
                    float scale = e.Delta.Scale;
                    Actions.PerformScale(pinchCenter, (double)scale, (double)scale);
                    e.Handled = true;
                }
            }
            else if (((e.Delta.Translation.X != 0.0) || (e.Delta.Translation.Y != 0.0)) && ((GestureSlide == GestureSlideAction.Translate) && (Data.Renderer is Renderer2D)))
            {
                Actions.FireEnter();
                Actions.State = ActionType.Translate;
                Actions.PerformTranslate(new Point(), e.Delta.Translation);
            }
        }

        void C1Chart_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            pinchCenter = e.Position;
        }

        void C1Chart_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (GestureSlide == GestureSlideAction.Zoom)
            {
                Actions.OnMouseMove(e);
            }
        }

        void C1Chart_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (GestureSlide == GestureSlideAction.Zoom)
            {
                Actions.OnMouseDown(e);
            }
        }

        void C1Chart_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (GestureSlide == GestureSlideAction.Zoom)
            {
                Actions.OnMouseUp(e);
            }
        }

        void ClearAllINP()
        {
            int num = _inps.Count;
            if (num > 0)
            {
                for (int i = 0; i < num; i++)
                {
                    _inps[i].PropertyChanged -= inp_PropertyChanged;
                }
                _inps.Clear();
            }
        }

        void EffectChanged(object sender, EventArgs e)
        {
            InvalidateChart();
        }

        public void EndUpdate()
        {
            UpdateCount--;
        }

        public object FindPlotElement(string name)
        {
            return ViewElement.FindName(name);
        }

        internal void FireActionEnter(object sender, EventArgs args)
        {
            if (ActionEnter != null)
            {
                ActionEnter(sender, args);
            }
        }

        internal void FireActionLeave(object sender, EventArgs args)
        {
            if (ActionLeave != null)
            {
                ActionLeave(sender, args);
            }
        }

        void inp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _data_DataChanged(this, EventArgs.Empty);
        }

        void C1Chart_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= C1Chart_Loaded;
            _loaded = true;
            UpdateChildren();

            _children.OnClear += _children_Clear;
            _children.CollectionChanged += _children_CollectionChanged;
        }

        #region 重写
        protected override void OnApplyTemplate()
        {
            _contentGrid = (Grid)GetTemplateChild("grid");

            if (_viewPresenter != null)
                _viewPresenter.SizeChanged -= OnPresenterSizeChanged;
            _viewPresenter = (ContentPresenter)GetTemplateChild("ViewPresenter");
            if (_viewPresenter != null)
                _viewPresenter.SizeChanged += OnPresenterSizeChanged;
        }

        protected override void OnDoubleTapped(DoubleTappedRoutedEventArgs e)
        {
            base.OnDoubleTapped(e);
            if ((GestureDoubleTap == GestureDoubleTapAction.Scale) && (Data.Renderer is Renderer2D))
            {
                Actions.FireEnter();
                Actions.PerformScale(e.GetPosition(this));
                Actions.FireLeave();
            }
        }
        #endregion

        #region 绘制

        internal void InvalidateChart()
        {
            if (UpdateCount <= 0 && _loaded)
            {
                forceRebuild = true;
                RebuildChart();
            }
        }

        void RebuildChart()
        {
            if (_inBuild)
                return;

            try
            {
                BaseRenderer renderer;
                _inBuild = true;
                if (Data != null)
                {
                    IRenderer renderer2 = Data.Renderer;
                    if (renderer2 == null)
                    {
                        ApplyChartType(ChartType);
                        renderer2 = Data.Renderer;
                        if (renderer2 == null)
                        {
                            Data.Renderer = renderer2 = new Renderer2D();
                        }
                    }
                    renderer = renderer2 as BaseRenderer;
                    if (renderer != null)
                    {
                        renderer.StyleGen = StyleGenerator;
                    }
                    renderer2.Visual = this;
                    if (renderer2.Dirty)
                    {
                        forceRebuild = true;
                    }
                }
                if (forceRebuild)
                {
                    forceRebuild = false;
                    if (Data != null)
                    {
                        IRenderer renderer3 = Data.Renderer;
                        if (renderer3 != null)
                        {
                            renderer = renderer3 as BaseRenderer;
                            if (renderer != null)
                            {
                                renderer.ChartType = ChartType;
                                renderer.StyleGen.Reset();
                            }
                            renderer3.Dirty = false;
                            View.Renderer = renderer3;
                            if (dataChanged)
                            {
                                dataChanged = false;
                                renderer3.Clear();
                                RebuildRenderer(renderer3);
                            }
                            if (renderer != null)
                            {
                                object[] itemNamesInternal = Data.ItemNamesInternal;
                                if ((Data.Aggregate != Aggregate.None) && (itemNamesInternal != null))
                                {
                                    renderer.ItemNames = Enumerable.ToArray<object>(Enumerable.Distinct<object>(itemNamesInternal));
                                }
                                else
                                {
                                    renderer.ItemNames = itemNamesInternal;
                                }
                                renderer.UpdateLegend((IList<LegendItem>)LegendItemsInternal);
                            }
                        }
                    }
                    if (View != null)
                    {
                        ViewElement = View.ViewElement;
                        if (_viewPresenter != null)
                        {
                            double actualWidth = _viewPresenter.ActualWidth;
                            double actualHeight = _viewPresenter.ActualHeight;
                            ViewElement.Width = actualWidth;
                            ViewElement.Height = actualHeight;
                            View.Rebuild(actualWidth, actualHeight);
                        }
                        LegendChanged?.Invoke(this, EventArgs.Empty);
                        ViewElement.InvalidateArrange();
                    }
                }
            }
            finally
            {
                _inBuild = false;
            }
        }

        /// <summary>
        /// 绘制图表
        /// </summary>
        /// <param name="renderer"></param>
        void RebuildRenderer(IRenderer renderer)
        {
            IEnumerable itemsSource = Data.ItemsSource;
            ClearAllINP();
            if (itemsSource == null)
            {
                itemsSource = DataContext as IEnumerable;
            }

            if (itemsSource != null)
            {
                DataBindingHelper.AutoCreateSeries(this, itemsSource);
                List<object> list = null;
                Binding itemNameBinding = Data.ItemNameBinding;
                if ((itemNameBinding == null) && (Bindings != null))
                {
                    itemNameBinding = Bindings.ItemNameBinding;
                }
                if ((itemNameBinding != null) && (Data.ItemNames == null))
                {
                    list = new List<object>();
                }

                int num = Data.Children.Count;
                Dictionary<IDataSeriesInfo, Binding[]> dictionary = new Dictionary<IDataSeriesInfo, Binding[]>();
                int num2 = 0;
                for (int j = 0; j < num; j++)
                {
                    IDataSeriesInfo info = Data.Children[j];
                    Binding[] memberPaths = info.MemberPaths;
                    if ((memberPaths != null) && (Data.Children[j].ItemsSource == null))
                    {
                        dictionary.Add(info, memberPaths);
                        num2 += memberPaths.Length;
                    }
                }

                IEnumerator enumerator = itemsSource.GetEnumerator();
                DataUtils.TryReset(enumerator);
                List<object>[] listArray = new List<object>[num2];
                int index = 0;
                while (index < listArray.Length)
                {
                    listArray[index] = new List<object>();
                    index++;
                }

                DataBindingProxy proxy = new DataBindingProxy();
                while (enumerator.MoveNext())
                {
                    object current = enumerator.Current;
                    index = 0;
                    proxy.DataContext = current;
                    if (list != null)
                    {
                        list.Add(proxy.GetValue(itemNameBinding));
                    }

                    using (Dictionary<IDataSeriesInfo, Binding[]>.Enumerator enumerator2 = dictionary.GetEnumerator())
                    {
                        while (enumerator2.MoveNext())
                        {
                            Binding[] bindingArray2 = enumerator2.Current.Value;
                            for (int k = 0; k < bindingArray2.Length; k++)
                            {
                                if (bindingArray2[k] != null)
                                {
                                    listArray[index++].Add(proxy.GetValue(bindingArray2[k]));
                                }
                            }
                        }
                    }

                    proxy.DataContext = null;
                    if (current is INotifyPropertyChanged)
                    {
                        AddINP((INotifyPropertyChanged)current);
                    }
                }

                index = 0;
                foreach (KeyValuePair<IDataSeriesInfo, Binding[]> pair2 in dictionary)
                {
                    Binding[] bindingArray3 = pair2.Value;
                    for (int m = 0; m < bindingArray3.Length; m++)
                    {
                        if (bindingArray3[m] != null)
                        {
                            pair2.Key.SetResolvedValues(m, listArray[index++].ToArray());
                        }
                    }
                }

                if ((list != null) && (list.Count > 0))
                {
                    Data.ItemNamesInternal = list.ToArray();
                }
            }

            Data.Aggregate = Aggregate;
            for (int i = 0; i < Data.Children.Count; i++)
            {
                DataSeries seriesInfo = Data.Children[i];
                if (seriesInfo.ItemsSource != null)
                {
                    seriesInfo.PerformBinding(new Action<INotifyPropertyChanged>(AddINP));
                }
                renderer.AddSeries(seriesInfo);
            }
        }

        void OnPresenterSizeChanged(object sender, SizeChangedEventArgs e)
        {
            InvalidateChart();
        }
        #endregion

        public void Reset(bool clearData)
        {
            BeginUpdate();
            _autoSeries = false;
            Actions.Clear();
            ActionUpdateDelay = 40.0;
            if ((Data != null) && clearData)
            {
                Data.Reset();
                Bindings = null;
                StyleGenerator.Reset();
            }
            if (View != null)
            {
                View.ResetInternal();
            }
            BarColumnOptions.Reset(this);
            PieOptions.Reset(this);
            LineAreaOptions.Reset(this);
            PolarRadarOptions.Reset(this);
            EndUpdate();
        }

        public void ResetData()
        {
            Data = null;
        }

        public bool ShouldSerializeData()
        {
            return (Data != null);
        }

        void IDisposable.Dispose()
        {
            ClearAllINP();
        }

        #region 同步Children
        void _children_Clear(object sender, EventArgs e)
        {
            RemoveChildren((IList)Children);
        }

        void _children_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != ((NotifyCollectionChangedAction)((int)NotifyCollectionChangedAction.Reset)))
            {
                RemoveChildren(e.OldItems);
            }
            UpdateChildren();
        }

        void RemoveChildren(IList p_items)
        {
            if (p_items != null && _contentGrid != null)
            {
                foreach (UIElement element in p_items)
                {
                    _contentGrid.Children.Remove(element);
                }
            }
        }

        void UpdateChildren()
        {
            if (_contentGrid == null)
                return;

            foreach (var elem in Children.OfType<FrameworkElement>())
            {
                ChartLegend legend = elem as ChartLegend;
                if (legend != null)
                {
                    legend.Chart = this;
                }
                else
                {
                    Grid.SetColumn(elem, 1);
                    Grid.SetRow(elem, 1);
                }

                if (!_contentGrid.Children.Contains(elem))
                {
                    if (elem.Parent is Panel pnl)
                        pnl.Children.Remove(elem);
                    _contentGrid.Children.Add(elem);
                }
            }
        }
        #endregion
    }
}

