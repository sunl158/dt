#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2014-07-03 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Base;
using Dt.CalcEngine;
using Dt.CalcEngine.Expressions;
using Dt.Cells.Data;
using Dt.Cells.UndoRedo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Input;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
#endregion

namespace Dt.Cells.UI
{
    /// <summary>
    /// Represents the GcSpreadSheet worksheet viewer used to present and handle worksheet operations.
    /// </summary>
    public partial class SheetView : Panel, IXmlSerializable
    {
        internal SheetView(Control host)
        {
            _host = host;
            _formulaSelectionFeature = new FormulaSelectionFeature(this);
            InitInput();
            Init();
        }

        void _dataValidationListPopUp_Closed(object sender, object e)
        {
            FocusInternal();
        }

        void _dataValidationListPopUp_Opened(object sender, object e)
        {
        }

        /// <summary>
        /// Adds a cell or cells to the selection.
        /// </summary>
        /// <param name="row">The row index of the first cell to add.</param>
        /// <param name="column">The column index of the first cell to add.</param>
        /// <param name="rowCount">The number of rows to add.</param>
        /// <param name="columnCount">The number of columns to add.</param>
        public void AddSelection(int row, int column, int rowCount, int columnCount)
        {
            Worksheet.AddSelection(row, column, rowCount, columnCount);
        }

        void AddSortItems(ColumnDropDownList dropdown, FilterButtonInfo info)
        {
            DropDownItemControl control = new DropDownItemControl();
            control.Content = ResourceStrings.SortDropdownItemSortAscend;
            control.Icon = Dt.Cells.UI.SR.GetImage("SortAscending.png");
            control.Command = new SortCommand(this, info, true);
            dropdown.Items.Add(control);
            DropDownItemControl control2 = new DropDownItemControl();
            control2.Content = ResourceStrings.SortDropdownItemSortDescend;
            control2.Icon = Dt.Cells.UI.SR.GetImage("SortDescending.png");
            control2.Command = new SortCommand(this, info, false);
            dropdown.Items.Add(control2);
        }

        /// <summary>
        /// Adjusts the adjacent column viewport's width.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index to adjust, it adjusts the column viewport and its next column viewport.</param>
        /// <param name="deltaViewportWidth">The column width adjusted offset.</param>
        public void AdjustColumnViewport(int columnViewportIndex, double deltaViewportWidth)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            if ((columnViewportIndex < 0) || (columnViewportIndex > (viewportInfo.ColumnViewportCount - 1)))
            {
                throw new ArgumentOutOfRangeException("columnViewportIndex");
            }
            if ((viewportInfo.ColumnViewportCount > 1) && (columnViewportIndex != (viewportInfo.ColumnViewportCount - 1)))
            {
                int index = columnViewportIndex + 1;
                viewportInfo.ViewportWidth[columnViewportIndex] = DoubleUtil.Formalize(GetViewportWidth(columnViewportIndex) + deltaViewportWidth) / ((double)ZoomFactor);
                viewportInfo.ViewportWidth[index] = DoubleUtil.Formalize(GetViewportWidth(index) - deltaViewportWidth) / ((double)ZoomFactor);
                if (viewportInfo.ViewportWidth[index] == 0.0)
                {
                    Worksheet.RemoveColumnViewport(index);
                }
                if (viewportInfo.ViewportWidth[columnViewportIndex] == 0.0)
                {
                    Worksheet.RemoveColumnViewport(columnViewportIndex);
                }
                viewportInfo = GetViewportInfo();
                viewportInfo.ViewportWidth[viewportInfo.ColumnViewportCount - 1] = -1.0;
                SetViewportInfo(Worksheet, viewportInfo);
                InvalidateLayout();
                base.InvalidateMeasure();
            }
        }

        CellRange AdjustFillRange(CellRange fillRange)
        {
            int row = (fillRange.Row != -1) ? fillRange.Row : 0;
            int column = (fillRange.Column != -1) ? fillRange.Column : 0;
            int rowCount = (fillRange.RowCount != -1) ? fillRange.RowCount : Worksheet.RowCount;
            return new CellRange(row, column, rowCount, (fillRange.ColumnCount != -1) ? fillRange.ColumnCount : Worksheet.ColumnCount);
        }

        /// <summary>
        /// Adjusts the adjacent row viewport's height.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index to adjust, it adjusts the row viewport and its next row viewport.</param>
        /// <param name="deltaViewportHeight">The row height adjusted offset.</param>
        public void AdjustRowViewport(int rowViewportIndex, double deltaViewportHeight)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            if ((rowViewportIndex < 0) || (rowViewportIndex > (viewportInfo.RowViewportCount - 1)))
            {
                throw new ArgumentOutOfRangeException("rowViewportIndex");
            }
            if ((viewportInfo.RowViewportCount > 1) && (rowViewportIndex != (viewportInfo.RowViewportCount - 1)))
            {
                int index = rowViewportIndex + 1;
                viewportInfo.ViewportHeight[rowViewportIndex] = DoubleUtil.Formalize(GetViewportHeight(rowViewportIndex) + deltaViewportHeight) / ((double)ZoomFactor);
                viewportInfo.ViewportHeight[index] = DoubleUtil.Formalize(GetViewportHeight(index) - deltaViewportHeight) / ((double)ZoomFactor);
                if (viewportInfo.ViewportHeight[index] == 0.0)
                {
                    Worksheet.RemoveRowViewport(rowViewportIndex + 1);
                }
                if (viewportInfo.ViewportHeight[rowViewportIndex] == 0.0)
                {
                    Worksheet.RemoveRowViewport(rowViewportIndex);
                }
                viewportInfo = GetViewportInfo();
                viewportInfo.ViewportHeight[viewportInfo.RowViewportCount - 1] = -1.0;
                SetViewportInfo(Worksheet, viewportInfo);
                InvalidateLayout();
                base.InvalidateMeasure();
            }
        }

        CellRange AdjustViewportRange(int rowViewport, int columnViewport, CellRange range)
        {
            int row = (range.Row != -1) ? range.Row : GetViewportTopRow(rowViewport);
            int column = (range.Column != -1) ? range.Column : GetViewportLeftColumn(columnViewport);
            int rowCount = (range.RowCount != -1) ? range.RowCount : Worksheet.RowCount;
            return new CellRange(row, column, rowCount, (range.ColumnCount != -1) ? range.ColumnCount : Worksheet.ColumnCount);
        }

        bool AllowEnterEditing(KeyRoutedEventArgs e)
        {
            bool flag;
            bool flag2;
            bool flag3;
            _isIMEEnterEditing = false;
            KeyboardHelper.GetMetaKeyState(out flag, out flag2, out flag3);
            if (flag2 || flag3)
            {
                return false;
            }
            if (((((e.Key != VirtualKey.Space) && (((VirtualKey.Search | VirtualKey.Shift) > e.Key) || (e.Key > ((VirtualKey)0xc0)))) && (((VirtualKey.Scroll | VirtualKey.J) > e.Key) || (e.Key > (VirtualKey.NumberKeyLock | VirtualKey.N)))) && (((VirtualKey.Number0 > e.Key) || (e.Key > VirtualKey.Number9)) && ((VirtualKey.A > e.Key) || (e.Key > VirtualKey.Z)))) && ((VirtualKey.NumberPad0 > e.Key) || (e.Key > VirtualKey.NumberPad9)))
            {
                return ((VirtualKey.Multiply <= e.Key) && (e.Key <= VirtualKey.Divide));
            }
            return true;
        }

        internal DataValidationResult ApplyEditingValue(bool cancel = false)
        {
            if (IsEditing && EditorDirty)
            {
                GcViewport editingViewport = EditingViewport;
                if (((editingViewport != null) && editingViewport.IsEditing()) && !cancel)
                {
                    int editingRowIndex = editingViewport.EditingContainer.EditingRowIndex;
                    int editingColumnIndex = editingViewport.EditingContainer.EditingColumnIndex;
                    string editorValue = (string)(editingViewport.GetEditorValue() as string);
                    CellEditExtent extent = new CellEditExtent(editingRowIndex, editingColumnIndex, editorValue);
                    CellEditUndoAction command = new CellEditUndoAction(Worksheet, extent);
                    DoCommand(command);
                    return command.ApplyResult;
                }
            }
            return DataValidationResult.ForceApply;
        }

        Point ArrangeDragFillTooltip(CellRange range, FillDirection direction)
        {
            int row = -1;
            int column = -1;
            switch (direction)
            {
                case FillDirection.Left:
                    row = (range.Row + range.RowCount) - 1;
                    column = range.Column;
                    break;

                case FillDirection.Right:
                case FillDirection.Down:
                    row = (range.Row + range.RowCount) - 1;
                    column = (range.Column + range.ColumnCount) - 1;
                    break;

                case FillDirection.Up:
                    row = range.Row;
                    column = (range.Column + range.ColumnCount) - 1;
                    break;
            }
            RowLayout layout = GetViewportRowLayoutModel(_dragToRowViewport).FindRow(row);
            ColumnLayout layout2 = GetViewportColumnLayoutModel(_dragToColumnViewport).FindColumn(column);
            if ((layout != null) && (layout2 != null))
            {
                switch (direction)
                {
                    case FillDirection.Left:
                        return new Point(layout2.X + 2.0, (layout.Y + layout.Height) + 2.0);

                    case FillDirection.Right:
                    case FillDirection.Down:
                        return new Point((layout2.X + layout2.Width) + 2.0, (layout.Y + layout.Height) + 2.0);

                    case FillDirection.Up:
                        return new Point((layout2.X + layout2.Width) + 2.0, layout.Y + 2.0);
                }
            }
            return new Point();
        }

        /// <summary>
        /// Positions child elements and determines a size for a <see cref="T:System.Windows.FrameworkElement" /> derived class, when overridden in a derived class.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>
        /// The actual size used.
        /// </returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            double headerX;
            double headerY;
            SheetLayout sheetLayout = GetSheetLayout();
            ViewportInfo viewportInfo = GetViewportInfo();
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            int rowViewportCount = viewportInfo.RowViewportCount;
            TrackersContainer.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
            ShapeDrawingContainer.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
            CursorsContainer.Arrange(new Rect(0.0, 0.0, finalSize.Width, finalSize.Height));
            if ((_cornerPresenter != null) && (_cornerPresenter.Parent != null))
            {
                headerX = sheetLayout.HeaderX;
                headerY = sheetLayout.HeaderY;
                if ((_cornerPresenter.Width != sheetLayout.HeaderWidth) || (_cornerPresenter.Height != sheetLayout.HeaderHeight))
                {
                    _cornerPresenter.Arrange(new Rect(headerX, headerY, sheetLayout.HeaderWidth, sheetLayout.HeaderHeight));
                }
            }
            if (_columnHeaderPresenters != null)
            {
                for (int i = -1; i <= columnViewportCount; i++)
                {
                    headerX = sheetLayout.GetViewportX(i);
                    headerY = sheetLayout.HeaderY;
                    double viewportWidth = sheetLayout.GetViewportWidth(i);
                    double headerHeight = sheetLayout.HeaderHeight;
                    GcViewport viewport = _columnHeaderPresenters[i + 1];
                    if (((viewport != null) && (viewport.Parent != null)) && ((viewport.Width != viewportWidth) || (viewport.Height != headerHeight)))
                    {
                        viewport.InvalidateArrange();
                        viewport.Arrange(new Rect(headerX, headerY, viewportWidth, headerHeight));
                    }
                }
            }
            if (_rowHeaderPresenters != null)
            {
                for (int j = -1; j <= rowViewportCount; j++)
                {
                    headerX = sheetLayout.HeaderX;
                    headerY = sheetLayout.GetViewportY(j);
                    double headerWidth = sheetLayout.HeaderWidth;
                    double viewportHeight = sheetLayout.GetViewportHeight(j);
                    GcViewport viewport2 = _rowHeaderPresenters[j + 1];
                    if (((viewport2 != null) && (viewport2.Parent != null)) && ((viewport2.Width != headerWidth) || (viewport2.Height != viewportHeight)))
                    {
                        viewport2.InvalidateArrange();
                        viewport2.Arrange(new Rect(headerX, headerY, headerWidth, viewportHeight));
                    }
                }
            }
            if (_viewportPresenters != null)
            {
                for (int k = -1; k <= columnViewportCount; k++)
                {
                    headerX = sheetLayout.GetViewportX(k);
                    double width = sheetLayout.GetViewportWidth(k);
                    for (int m = -1; m <= rowViewportCount; m++)
                    {
                        headerY = sheetLayout.GetViewportY(m);
                        double height = sheetLayout.GetViewportHeight(m);
                        GcViewport viewport3 = _viewportPresenters[m + 1, k + 1];
                        if (viewport3 != null)
                        {
                            viewport3.Arrange(new Rect(headerX, headerY, width, height));
                        }
                    }
                }
            }
            ArrangeRangeGroup(rowViewportCount, columnViewportCount, sheetLayout);
            return finalSize;
        }

        internal void ArrangeRangeGroup(int rowPaneCount, int columnPaneCount, SheetLayout layout)
        {
            double x;
            double y;
            GroupLayout groupLayout = GetGroupLayout();
            if ((_groupCornerPresenter != null) && (_groupCornerPresenter.Parent != null))
            {
                x = groupLayout.X;
                y = groupLayout.Y;
                if ((_groupCornerPresenter.Width != groupLayout.Width) || (_groupCornerPresenter.Height != groupLayout.Height))
                {
                    _groupCornerPresenter.Arrange(new Rect(x, y, groupLayout.Width, groupLayout.Height));
                }
            }
            if ((_rowGroupHeaderPresenter != null) && (_rowGroupHeaderPresenter.Parent != null))
            {
                x = groupLayout.X;
                y = groupLayout.Y + groupLayout.Height;
                double width = groupLayout.Width;
                double headerHeight = layout.HeaderHeight;
                _rowGroupHeaderPresenter.Arrange(new Rect(x, y, width, headerHeight));
            }
            if ((_columnGroupHeaderPresenter != null) && (_columnGroupHeaderPresenter.Parent != null))
            {
                x = groupLayout.X + groupLayout.Width;
                y = groupLayout.Y;
                double headerWidth = layout.HeaderWidth;
                double height = groupLayout.Height;
                _columnGroupHeaderPresenter.Arrange(new Rect(x, y, headerWidth, height));
            }
            if (_rowGroupPresenters != null)
            {
                for (int i = -1; i <= rowPaneCount; i++)
                {
                    GcRangeGroup group = _rowGroupPresenters[i + 1];
                    if (group != null)
                    {
                        x = groupLayout.X;
                        y = layout.GetViewportY(i);
                        double num8 = groupLayout.Width;
                        double viewportHeight = layout.GetViewportHeight(i);
                        if (!IsTouching || (i != _touchStartHitTestInfo.RowViewportIndex))
                        {
                            group.Arrange(new Rect(x, y, num8, viewportHeight));
                            group.Clip = null;
                        }
                        else
                        {
                            group.Arrange(new Rect(x, y + _translateOffsetY, num8, viewportHeight));
                            if (_translateOffsetY < 0.0)
                            {
                                RectangleGeometry geometry = new RectangleGeometry();
                                geometry.Rect = new Rect(x, Math.Abs(_translateOffsetY), num8, viewportHeight);
                                group.Clip = geometry;
                            }
                            else if (_translateOffsetY > 0.0)
                            {
                                RectangleGeometry geometry2 = new RectangleGeometry();
                                geometry2.Rect = new Rect(x, 0.0, num8, Math.Max((double)0.0, (double)(viewportHeight - Math.Abs(_translateOffsetY))));
                                group.Clip = geometry2;
                            }
                        }
                    }
                }
            }
            if (_columnGroupPresenters != null)
            {
                for (int j = -1; j <= columnPaneCount; j++)
                {
                    GcRangeGroup group2 = _columnGroupPresenters[j + 1];
                    if (group2 != null)
                    {
                        x = layout.GetViewportX(j);
                        y = groupLayout.Y;
                        double viewportWidth = layout.GetViewportWidth(j);
                        double num12 = groupLayout.Height;
                        if (!IsTouching || (j != _touchStartHitTestInfo.ColumnViewportIndex))
                        {
                            group2.Arrange(new Rect(x, y, viewportWidth, num12));
                            group2.Clip = null;
                        }
                        else
                        {
                            group2.Arrange(new Rect(x + _translateOffsetX, y, viewportWidth, num12));
                            if (_translateOffsetX < 0.0)
                            {
                                RectangleGeometry geometry3 = new RectangleGeometry();
                                geometry3.Rect = new Rect(Math.Abs(_translateOffsetX), y, viewportWidth, num12);
                                group2.Clip = geometry3;
                            }
                            else if (_translateOffsetX > 0.0)
                            {
                                RectangleGeometry geometry4 = new RectangleGeometry();
                                geometry4.Rect = new Rect(0.0, y, Math.Max((double)0.0, (double)(viewportWidth - Math.Abs(_translateOffsetX))), num12);
                                group2.Clip = geometry4;
                            }
                        }
                    }
                }
            }
        }

        void AutoFitColumn()
        {
            ColumnLayout viewportResizingColumnLayoutFromX;
            if (IsResizingColumns)
            {
                EndColumnResizing();
            }
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation.HitTestType == HitTestType.ColumnHeader)
            {
                viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                bool flag = false;
                if (viewportResizingColumnLayoutFromX == null)
                {
                    if (savedHitTestInformation.ColumnViewportIndex == 0)
                    {
                        viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(-1, savedHitTestInformation.HitPoint.X);
                    }
                    if ((viewportResizingColumnLayoutFromX == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                    {
                        viewportResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(savedHitTestInformation.HitPoint.X);
                        flag = true;
                    }
                }
                if (viewportResizingColumnLayoutFromX != null)
                {
                    int column = viewportResizingColumnLayoutFromX.Column;
                    if (!flag)
                    {
                        AutoFitColumnInternal(column, true, false);
                    }
                    else
                    {
                        ColumnAutoFitUndoAction command = new ColumnAutoFitUndoAction(Worksheet, new ColumnAutoFitExtent[] { new ColumnAutoFitExtent(column) }, true);
                        DoCommand(command);
                    }
                }
            }
            else if (savedHitTestInformation.HitTestType == HitTestType.Corner)
            {
                viewportResizingColumnLayoutFromX = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                if (viewportResizingColumnLayoutFromX != null)
                {
                    int num2 = viewportResizingColumnLayoutFromX.Column;
                    ColumnAutoFitUndoAction action2 = new ColumnAutoFitUndoAction(Worksheet, new ColumnAutoFitExtent[] { new ColumnAutoFitExtent(num2) }, true);
                    DoCommand(action2);
                }
            }
        }

        /// <summary>
        /// Automatically fits the viewport column.
        /// </summary>
        /// <param name="column">The column index to automatically fit.</param>
        public void AutoFitColumn(int column)
        {
            if ((column < 0) || (column >= Worksheet.ColumnCount))
            {
                throw new ArgumentOutOfRangeException("column");
            }
            AutoFitColumn(column, false);
        }

        /// <summary>
        /// Automatically fits the viewport column.
        /// </summary>
        /// <param name="column">The column index to automatically fit.</param>
        /// <param name="isRowHeader">The flag indicates whether sheetArea is a row header.</param>
        public void AutoFitColumn(int column, bool isRowHeader)
        {
            if ((column < 0) || (column >= Worksheet.ColumnCount))
            {
                throw new ArgumentOutOfRangeException("column");
            }
            AutoFitColumnInternal(column, false, isRowHeader);
        }

        void AutoFitColumnForTouch(HitTestInformation hi)
        {
            ColumnLayout viewportResizingColumnLayoutFromXForTouch;
            if (IsTouchResizingColumns)
            {
                EndTouchColumnResizing();
            }
            if (hi.HitTestType == HitTestType.ColumnHeader)
            {
                viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(hi.ColumnViewportIndex, hi.HitPoint.X);
                bool flag = false;
                if (viewportResizingColumnLayoutFromXForTouch == null)
                {
                    if (hi.ColumnViewportIndex == 0)
                    {
                        viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(-1, hi.HitPoint.X);
                    }
                    if ((viewportResizingColumnLayoutFromXForTouch == null) && ((hi.ColumnViewportIndex == 0) || (hi.ColumnViewportIndex == -1)))
                    {
                        viewportResizingColumnLayoutFromXForTouch = GetRowHeaderResizingColumnLayoutFromXForTouch(hi.HitPoint.X);
                        flag = true;
                    }
                }
                if (viewportResizingColumnLayoutFromXForTouch != null)
                {
                    int column = viewportResizingColumnLayoutFromXForTouch.Column;
                    if (!flag)
                    {
                        AutoFitColumnInternal(column, true, false);
                    }
                    else
                    {
                        ColumnAutoFitUndoAction command = new ColumnAutoFitUndoAction(Worksheet, new ColumnAutoFitExtent[] { new ColumnAutoFitExtent(column) }, true);
                        DoCommand(command);
                    }
                }
            }
            else if (hi.HitTestType == HitTestType.Corner)
            {
                viewportResizingColumnLayoutFromXForTouch = GetRowHeaderColumnLayoutModel().FindColumn(hi.HeaderInfo.ResizingColumn);
                if (viewportResizingColumnLayoutFromXForTouch != null)
                {
                    int num2 = viewportResizingColumnLayoutFromXForTouch.Column;
                    ColumnAutoFitUndoAction action2 = new ColumnAutoFitUndoAction(Worksheet, new ColumnAutoFitExtent[] { new ColumnAutoFitExtent(num2) }, true);
                    DoCommand(action2);
                }
            }
        }

        void AutoFitColumnInternal(int columnIndex, bool supportUndo, bool isRowHeader)
        {
            List<ColumnAutoFitExtent> list = new List<ColumnAutoFitExtent>();
            if (Worksheet.IsSelected(-1, columnIndex))
            {
                foreach (CellRange range in Worksheet.Selections)
                {
                    if (range.Row == -1)
                    {
                        int num = (range.Column == -1) ? 0 : range.Column;
                        int num2 = (range.Column == -1) ? Worksheet.ColumnCount : range.ColumnCount;
                        for (int i = num; i < (num + num2); i++)
                        {
                            list.Add(new ColumnAutoFitExtent(i));
                        }
                    }
                }
            }
            else
            {
                list.Add(new ColumnAutoFitExtent(columnIndex));
            }
            ColumnAutoFitExtent[] columns = new ColumnAutoFitExtent[list.Count];
            list.CopyTo(columns);
            ColumnAutoFitUndoAction command = new ColumnAutoFitUndoAction(Worksheet, columns, isRowHeader);
            if (supportUndo)
            {
                DoCommand(command);
            }
            else
            {
                command.Execute(this);
            }
        }

        void AutoFitRow()
        {
            RowLayout viewportResizingRowLayoutFromY;
            if (IsResizingRows)
            {
                EndRowResizing();
            }
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation.HitTestType == HitTestType.RowHeader)
            {
                bool flag = false;
                viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                if (viewportResizingRowLayoutFromY == null)
                {
                    if (savedHitTestInformation.RowViewportIndex == 0)
                    {
                        viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(-1, savedHitTestInformation.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromY == null) && ((savedHitTestInformation.RowViewportIndex == -1) || (savedHitTestInformation.RowViewportIndex == 0)))
                    {
                        viewportResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(savedHitTestInformation.HitPoint.Y);
                        flag = true;
                    }
                }
                if (viewportResizingRowLayoutFromY != null)
                {
                    int row = viewportResizingRowLayoutFromY.Row;
                    if (!flag)
                    {
                        AutoFitRowInternal(row, true, false);
                    }
                    else
                    {
                        RowAutoFitUndoAction command = new RowAutoFitUndoAction(Worksheet, new RowAutoFitExtent[] { new RowAutoFitExtent(row) }, true);
                        DoCommand(command);
                    }
                }
            }
            else if (savedHitTestInformation.HitTestType == HitTestType.Corner)
            {
                viewportResizingRowLayoutFromY = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                if (viewportResizingRowLayoutFromY != null)
                {
                    int num2 = viewportResizingRowLayoutFromY.Row;
                    RowAutoFitUndoAction action2 = new RowAutoFitUndoAction(Worksheet, new RowAutoFitExtent[] { new RowAutoFitExtent(num2) }, true);
                    DoCommand(action2);
                }
            }
        }

        /// <summary>
        /// Automatically fits the viewport row.
        /// </summary>
        /// <param name="row">The row index.</param>
        public void AutoFitRow(int row)
        {
            if ((row < 0) || (row > Worksheet.RowCount))
            {
                throw new ArgumentOutOfRangeException("row");
            }
            AutoFitRow(row, false);
        }

        /// <summary>
        /// Automatically fits the viewport row.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="isColumnHeader">The flag indicates whether sheetArea is a column header.</param>
        public void AutoFitRow(int row, bool isColumnHeader)
        {
            if ((row < 0) || (row > Worksheet.RowCount))
            {
                throw new ArgumentOutOfRangeException("row");
            }
            AutoFitRowInternal(row, false, isColumnHeader);
        }

        void AutoFitRowForTouch(HitTestInformation hi)
        {
            RowLayout viewportResizingRowLayoutFromYForTouch;
            if (IsTouchResizingRows)
            {
                EndTouchRowResizing();
            }
            if (hi.HitTestType == HitTestType.RowHeader)
            {
                bool flag = false;
                viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(hi.RowViewportIndex, hi.HitPoint.Y);
                if (viewportResizingRowLayoutFromYForTouch == null)
                {
                    if (hi.RowViewportIndex == 0)
                    {
                        viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(-1, hi.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromYForTouch == null) && ((hi.RowViewportIndex == -1) || (hi.RowViewportIndex == 0)))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetColumnHeaderResizingRowLayoutFromYForTouch(hi.HitPoint.Y);
                        flag = true;
                    }
                }
                if (viewportResizingRowLayoutFromYForTouch != null)
                {
                    int row = viewportResizingRowLayoutFromYForTouch.Row;
                    if (!flag)
                    {
                        AutoFitRowInternal(row, true, false);
                    }
                    else
                    {
                        RowAutoFitUndoAction command = new RowAutoFitUndoAction(Worksheet, new RowAutoFitExtent[] { new RowAutoFitExtent(row) }, true);
                        DoCommand(command);
                    }
                }
            }
            else if (hi.HitTestType == HitTestType.Corner)
            {
                viewportResizingRowLayoutFromYForTouch = GetColumnHeaderRowLayoutModel().FindRow(hi.HeaderInfo.ResizingRow);
                if (viewportResizingRowLayoutFromYForTouch != null)
                {
                    int num2 = viewportResizingRowLayoutFromYForTouch.Row;
                    RowAutoFitUndoAction action2 = new RowAutoFitUndoAction(Worksheet, new RowAutoFitExtent[] { new RowAutoFitExtent(num2) }, true);
                    DoCommand(action2);
                }
            }
        }

        void AutoFitRowInternal(int rowIndex, bool supportUndo, bool isColumnHeader)
        {
            List<RowAutoFitExtent> list = new List<RowAutoFitExtent>();
            if (Worksheet.IsSelected(rowIndex, -1))
            {
                foreach (CellRange range in Worksheet.Selections)
                {
                    if (range.Column == -1)
                    {
                        int num = (range.Row == -1) ? 0 : range.Row;
                        int num2 = (range.Row == -1) ? Worksheet.RowCount : range.RowCount;
                        for (int i = num; i < (num + num2); i++)
                        {
                            list.Add(new RowAutoFitExtent(i));
                        }
                    }
                }
            }
            else
            {
                list.Add(new RowAutoFitExtent(rowIndex));
            }
            RowAutoFitExtent[] rows = new RowAutoFitExtent[list.Count];
            list.CopyTo(rows);
            RowAutoFitUndoAction command = new RowAutoFitUndoAction(Worksheet, rows, isColumnHeader);
            if (supportUndo)
            {
                DoCommand(command);
            }
            else
            {
                command.Execute(this);
            }
        }

        /// <summary>
        /// Enters a state where the user can select formulas with the mouse or keyboard.
        /// </summary>
        public void BeginFormulaSelection(object editor = null)
        {
            _formulaSelectionFeature.BeginFormulaSelection(editor);
        }

        void CachFloatingObjectsMovingResizingLayoutModels()
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            int rowViewportCount = viewportInfo.RowViewportCount;
            _cachedFloatingObjectMovingResizingLayoutModel = new FloatingObjectLayoutModel[rowViewportCount + 2, columnViewportCount + 2];
            for (int i = -1; i <= rowViewportCount; i++)
            {
                for (int j = -1; j <= columnViewportCount; j++)
                {
                    _cachedFloatingObjectMovingResizingLayoutModel[i + 1, j + 1] = new FloatingObjectLayoutModel(GetViewportFloatingObjectLayoutModel(i, j));
                }
            }
        }

        Point CalcMoveOffset(int moveStartRowViewport, int moveStartColumnViewport, int moveStartRow, int moveStartColumn, Point startPoint, int moveEndRowViewport, int moveEndColumnViewport, int moveEndRow, int moveEndColumn, Point endPoint)
        {
            RowLayout layout = GetViewportRowLayoutModel(moveEndRowViewport).FindRow(moveEndRow);
            ColumnLayout layout2 = GetViewportColumnLayoutModel(moveEndColumnViewport).FindColumn(moveEndColumn);
            if ((layout == null) || (layout2 == null))
            {
                return new Point(0.0, 0.0);
            }
            Rect rect = _floatingObjectsMovingResizingStartPointCellBounds;
            Rect rect2 = new Rect(layout2.X, layout.Y, layout2.Width, layout.Height);
            bool flag = true;
            if (moveEndRow < moveStartRow)
            {
                flag = false;
                int num = moveStartRow;
                moveStartRow = moveEndRow;
                moveEndRow = num;
                double y = startPoint.Y;
                startPoint.Y = endPoint.Y;
                endPoint.Y = y;
                y = rect.Y;
                rect.Y = rect2.Y;
                rect2.Y = y;
                y = rect.Height;
                rect.Height = rect2.Height;
                rect2.Height = y;
            }
            double num3 = 0.0;
            for (int i = moveStartRow; i <= moveEndRow; i++)
            {
                num3 += Math.Ceiling((double)(Worksheet.GetActualRowHeight(i, SheetArea.Cells) * ZoomFactor));
            }
            num3 -= startPoint.Y - rect.Y;
            num3 -= (rect2.Y + rect2.Height) - endPoint.Y;
            if (!flag)
            {
                num3 = -num3;
            }
            bool flag2 = true;
            if (moveEndColumn < moveStartColumn)
            {
                flag2 = false;
                int num5 = moveStartColumn;
                moveStartColumn = moveEndColumn;
                moveEndColumn = num5;
                double width = startPoint.X;
                startPoint.X = endPoint.X;
                endPoint.X = width;
                width = rect.X;
                rect.X = rect2.X;
                rect2.X = width;
                width = rect.Width;
                rect.Width = rect2.Width;
                rect2.Width = width;
            }
            double x = 0.0;
            for (int j = moveStartColumn; j <= moveEndColumn; j++)
            {
                x += Math.Ceiling((double)(Worksheet.GetActualColumnWidth(j, SheetArea.Cells) * ZoomFactor));
            }
            x -= startPoint.X - rect.X;
            x -= (rect2.X + rect2.Width) - endPoint.X;
            if (!flag2)
            {
                x = -x;
            }
            x = Math.Floor((double)(x / ((double)ZoomFactor)));
            return new Point(x, Math.Floor((double)(num3 / ((double)ZoomFactor))));
        }

        internal bool CanCommitAndNavigate()
        {
            if (!IsEditing)
            {
                return false;
            }
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
            if ((viewportRowsPresenter != null) && (((viewportRowsPresenter.EditingContainer != null) && (viewportRowsPresenter.EditingContainer.Editor != null)) && (viewportRowsPresenter.EditingContainer.EditorStatus == EditorStatus.Edit)))
            {
                return false;
            }
            return true;
        }

        internal bool CheckPastedRange(Dt.Cells.Data.Worksheet fromSheet, CellRange fromRange, CellRange toRange, bool isCutting, string clipboardText, out CellRange pastedRange, out bool pasteInternal)
        {
            pasteInternal = false;
            pastedRange = null;
            CellRange exceptedRange = isCutting ? fromRange : null;
            if ((fromSheet == null) && string.IsNullOrEmpty(clipboardText))
            {
                return false;
            }
            pasteInternal = IsPastedInternal(fromSheet, fromRange, Worksheet, clipboardText);
            Dt.Cells.Data.Worksheet toSheet = Worksheet;
            if (pasteInternal)
            {
                bool flag;
                string str;
                if ((isCutting && fromSheet.Protect) && IsAnyCellInRangeLocked(fromSheet, fromRange.Row, fromRange.Column, fromRange.RowCount, fromRange.ColumnCount))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewPasteSouceSheetCellsAreLocked, null, null);
                    return false;
                }
                pastedRange = GetPastedRange(fromSheet, fromRange, toSheet, toRange, isCutting);
                if (RaiseValidationPasting(fromSheet, fromRange, Worksheet, toRange, pastedRange, isCutting, out flag, out str))
                {
                    pastedRange = GetPastedRange(fromSheet, fromRange, toSheet, toRange, isCutting);
                    return !flag;
                }
            }
            else
            {
                bool flag3;
                string str2;
                pastedRange = GetPastedRange(toRange, clipboardText);
                if (RaiseValidationPasting(null, null, Worksheet, toRange, pastedRange, isCutting, out flag3, out str2))
                {
                    return !flag3;
                }
            }
            if (pastedRange == null)
            {
                RaiseInvalidOperation(ResourceStrings.SheetViewTheCopyAreaAndPasteAreaAreNotTheSameSize, null, null);
                return false;
            }
            if (toSheet.Protect && IsAnyCellInRangeLocked(toSheet, pastedRange.Row, pastedRange.Column, pastedRange.RowCount, pastedRange.ColumnCount))
            {
                RaiseInvalidOperation(ResourceStrings.SheetViewPasteDestinationSheetCellsAreLocked, null, null);
                return false;
            }
            if (pasteInternal)
            {
                if (HasPartSpans(fromSheet, fromRange.Row, fromRange.Column, fromRange.RowCount, fromRange.ColumnCount))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangeMergeCell, "Paste", new ClipboardPastingEventArgs(fromSheet, fromRange, toSheet, pastedRange, _clipBoardOptions, isCutting));
                    return false;
                }
                if (HasPartArrayFormulas(fromSheet, fromRange.Row, fromRange.Column, fromRange.RowCount, fromRange.ColumnCount, exceptedRange))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangePartOfArrayFormula, null, null);
                    return false;
                }
                int rowCount = (pastedRange.Row < 0) ? toSheet.RowCount : pastedRange.RowCount;
                int columnCount = (pastedRange.Column < 0) ? toSheet.ColumnCount : pastedRange.ColumnCount;
                int num3 = (fromRange.Row < 0) ? fromSheet.RowCount : fromRange.RowCount;
                int num4 = (fromRange.Column < 0) ? fromSheet.ColumnCount : fromRange.ColumnCount;
                if ((rowCount <= num3) && (columnCount <= num4))
                {
                    if (HasPartSpans(toSheet, pastedRange.Row, pastedRange.Column, pastedRange.RowCount, pastedRange.ColumnCount))
                    {
                        RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangeMergeCell, "Paste", new ClipboardPastingEventArgs(fromSheet, fromRange, toSheet, pastedRange, _clipBoardOptions, isCutting));
                        return false;
                    }
                    if (HasPartArrayFormulas(toSheet, pastedRange.Row, pastedRange.Column, pastedRange.RowCount, pastedRange.ColumnCount, exceptedRange))
                    {
                        RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangePartOfArrayFormula, null, null);
                        return false;
                    }
                }
                else
                {
                    int row = toRange.Row;
                    int column = toRange.Column;
                    if ((toRange.Row < 0) && (num3 < toSheet.RowCount))
                    {
                        row = 0;
                    }
                    if ((toRange.Column < 0) && (num4 < toSheet.ColumnCount))
                    {
                        column = 0;
                    }
                    if (((rowCount % num3) != 0) || ((columnCount % num4) != 0))
                    {
                        rowCount = num3;
                        columnCount = num4;
                        pastedRange = new CellRange(row, column, rowCount, columnCount);
                    }
                    int num7 = rowCount / num3;
                    int num8 = columnCount / num4;
                    for (int i = 0; i < num7; i++)
                    {
                        for (int j = 0; j < num8; j++)
                        {
                            if (HasPartSpans(toSheet, (row < 0) ? -1 : (row + (i * num3)), (column < 0) ? -1 : (column + (j * num4)), (row < 0) ? -1 : num3, (column < 0) ? -1 : num4))
                            {
                                RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangeMergeCell, "Paste", new ClipboardPastingEventArgs(fromSheet, fromRange, toSheet, pastedRange, _clipBoardOptions, isCutting));
                                return false;
                            }
                            if (HasPartArrayFormulas(toSheet, (row < 0) ? -1 : (row + (i * num3)), (column < 0) ? -1 : (column + (j * num4)), (row < 0) ? -1 : num3, (column < 0) ? -1 : num4, exceptedRange))
                            {
                                RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangePartOfArrayFormula, null, null);
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {
                if (HasPartSpans(toSheet, pastedRange.Row, pastedRange.Column, pastedRange.RowCount, pastedRange.ColumnCount))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangeMergeCell, "Paste", new ClipboardPastingEventArgs(fromSheet, fromRange, toSheet, pastedRange, _clipBoardOptions, isCutting));
                    return false;
                }
                if (HasPartArrayFormulas(toSheet, pastedRange.Row, pastedRange.Column, pastedRange.RowCount, pastedRange.ColumnCount, exceptedRange))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewPasteChangePartOfArrayFormula, null, null);
                    return false;
                }
                if (((pastedRange.Row + pastedRange.RowCount) > toSheet.RowCount) || ((pastedRange.Column + pastedRange.ColumnCount) > toSheet.ColumnCount))
                {
                    RaiseInvalidOperation(ResourceStrings.SheetViewTheCopyAreaAndPasteAreaAreNotTheSameSize, null, null);
                    return false;
                }
            }
            return true;
        }

        internal virtual void ClearMouseLeftButtonDownStates()
        {
            if (IsResizingColumns)
            {
                EndColumnResizing();
            }
            if (IsResizingRows)
            {
                EndRowResizing();
            }
            if (_formulaSelectionFeature.IsDragging)
            {
                _formulaSelectionFeature.EndDragging();
            }
            if (IsSelectingCells)
            {
                EndCellSelecting();
            }
            if (IsSelectingColumns)
            {
                EndColumnSelecting();
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if ((savedHitTestInformation != null) && (savedHitTestInformation.HitTestType == HitTestType.ColumnHeader))
                {
                    GcViewport columnHeaderRowsPresenter = GetColumnHeaderRowsPresenter(savedHitTestInformation.ColumnViewportIndex);
                    if (columnHeaderRowsPresenter != null)
                    {
                        RowPresenter row = columnHeaderRowsPresenter.GetRow(savedHitTestInformation.HeaderInfo.Row);
                        if (row != null)
                        {
                            CellPresenterBase cell = row.GetCell(savedHitTestInformation.HeaderInfo.Column);
                            if (cell != null)
                            {
                                cell.ApplyState();
                            }
                        }
                    }
                }
            }
            if (IsSelectingRows)
            {
                EndRowSelecting();
                HitTestInformation information2 = GetHitInfo();
                if ((information2 != null) && (information2.HitTestType == HitTestType.RowHeader))
                {
                    GcViewport rowHeaderRowsPresenter = GetRowHeaderRowsPresenter(information2.RowViewportIndex);
                    if (rowHeaderRowsPresenter != null)
                    {
                        RowPresenter presenter2 = rowHeaderRowsPresenter.GetRow(information2.HeaderInfo.Row);
                        if (presenter2 != null)
                        {
                            CellPresenterBase base3 = presenter2.GetCell(information2.HeaderInfo.Column);
                            if (base3 != null)
                            {
                                base3.ApplyState();
                            }
                        }
                    }
                }
            }
            if (IsDragDropping)
            {
                EndDragDropping();
            }
            if (IsDraggingFill)
            {
                EndDragFill();
            }
            if (IsMovingFloatingOjects)
            {
                EndFloatingObjectsMoving();
            }
            if (IsResizingFloatingObjects)
            {
                EndFloatingObjectResizing();
            }
        }

        /// <summary>
        /// Clears all undo and redo actions in the current UndoManager. 
        /// </summary>
        public void ClearUndoManager()
        {
            if (_undoManager != null)
            {
                _undoManager.UndoList.Clear();
                _undoManager.RedoList.Clear();
            }
        }

        /// <summary>
        /// Copies the text of a cell range to the Clipboard.
        /// </summary>
        /// <param name="range">The copied cell range.</param>
        public void ClipboardCopy(CellRange range)
        {
            if (Worksheet != null)
            {
                if (range == null)
                {
                    throw new ArgumentNullException("range");
                }
                if (!IsValidRange(range.Row, range.Column, range.RowCount, range.ColumnCount, Worksheet.RowCount, Worksheet.ColumnCount))
                {
                    throw new ArgumentException(ResourceStrings.SheetViewClipboardArgumentException);
                }
                CopyToClipboard(range, false);
            }
        }

        /// <summary>
        /// Cuts the text of a cell range to the Clipboard.
        /// </summary>
        /// <param name="range">The cut cell range.</param>
        public void ClipboardCut(CellRange range)
        {
            if (Worksheet != null)
            {
                if (range == null)
                {
                    throw new ArgumentNullException("range");
                }
                if (!IsValidRange(range.Row, range.Column, range.RowCount, range.ColumnCount, Worksheet.RowCount, Worksheet.ColumnCount))
                {
                    throw new ArgumentException(ResourceStrings.SheetViewClipboardArgumentException);
                }
                CopyToClipboard(range, true);
            }
        }

        /// <summary>
        /// Pastes content from the Clipboard to a cell range on the sheet.
        /// </summary>
        /// <param name="range">The pasted cell range on the sheet.</param>
        public void ClipboardPaste(CellRange range)
        {
            ClipboardPaste(range, ClipboardPasteOptions.All);
        }

        /// <summary>
        /// Pastes content from the Clipboard to a cell range on the sheet.
        /// </summary>
        /// <param name="range">The pasted cell range.</param>
        /// <param name="option">The Clipboard paste option that indicates which content type to paste.</param>
        public void ClipboardPaste(CellRange range, ClipboardPasteOptions option)
        {
            if (Worksheet != null)
            {
                CellRange range1;
                bool flag2;
                if (range == null)
                {
                    throw new ArgumentNullException("range");
                }
                if (!IsValidRange(range.Row, range.Column, range.RowCount, range.ColumnCount, Worksheet.RowCount, Worksheet.ColumnCount))
                {
                    throw new ArgumentException(ResourceStrings.SheetViewClipboardArgumentException);
                }
                Worksheet fromSheet = SpreadXClipboard.Worksheet;
                CellRange fromRange = SpreadXClipboard.Range;
                string clipboardText = ClipboardHelper.GetClipboardData();
                bool isCutting = SpreadXClipboard.IsCutting;
                if (((isCutting && (fromSheet != null)) && ((fromRange != null) && fromSheet.Protect)) && IsAnyCellInRangeLocked(fromSheet, fromRange.Row, fromRange.Column, fromRange.RowCount, fromRange.ColumnCount))
                {
                    isCutting = false;
                }
                if (CheckPastedRange(fromSheet, fromRange, range, isCutting, clipboardText, out range1, out flag2))
                {
                    if (isCutting)
                    {
                        option = ClipboardPasteOptions.All;
                    }
                    if (flag2)
                    {
                        ClipboardPaste(fromSheet, fromRange, Worksheet, range1, isCutting, clipboardText, option);
                    }
                    else
                    {
                        ClipboardPaste(null, null, Worksheet, range1, isCutting, clipboardText, option);
                    }
                    SetSelection(range1.Row, range1.Column, range1.RowCount, range1.ColumnCount);
                    SetActiveCell((range.Row < 0) ? 0 : range.Row, (range.Column < 0) ? 0 : range.Column, false);
                    InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                }
            }
        }

        internal static void ClipboardPaste(Dt.Cells.Data.Worksheet fromSheet, CellRange fromRange, Dt.Cells.Data.Worksheet toSheet, CellRange toRange, bool isCutting, string clipboardText, ClipboardPasteOptions option)
        {
            if (((fromSheet != null) && (fromSheet.Workbook != null)) && (object.ReferenceEquals(toSheet.Workbook, fromSheet.Workbook) && !toSheet.Workbook.Sheets.Contains(fromSheet)))
            {
                ClipboardHelper.ClearClipboard();
            }
            else if ((fromSheet != null) && (fromRange != null))
            {
                if (isCutting)
                {
                    Workbook.MoveTo(fromSheet, fromRange.Row, fromRange.Column, toSheet, toRange.Row, toRange.Column, fromRange.RowCount, fromRange.ColumnCount, ConvertPasteOption(option));
                    ClipboardHelper.ClearClipboard();
                }
                else
                {
                    int num = (toRange.Row < 0) ? toSheet.RowCount : toRange.RowCount;
                    int num2 = (toRange.Column < 0) ? toSheet.ColumnCount : toRange.ColumnCount;
                    int num3 = (fromRange.Row < 0) ? fromSheet.RowCount : fromRange.RowCount;
                    int num4 = (fromRange.Column < 0) ? fromSheet.ColumnCount : fromRange.ColumnCount;
                    if ((num > num3) || (num2 > num4))
                    {
                        int row = toRange.Row;
                        int column = toRange.Column;
                        if ((toRange.Row < 0) && (num3 < toSheet.RowCount))
                        {
                            row = 0;
                        }
                        if ((toRange.Column < 0) && (num4 < toSheet.ColumnCount))
                        {
                            column = 0;
                        }
                        if (((num % num3) != 0) || ((num2 % num4) != 0))
                        {
                            num = num3;
                            num2 = num4;
                        }
                        int num7 = num / num3;
                        int num8 = num2 / num4;
                        fromSheet.SuspendCalcService();
                        toSheet.SuspendCalcService();
                        try
                        {
                            for (int i = 0; i < num7; i++)
                            {
                                for (int j = 0; j < num8; j++)
                                {
                                    Workbook.CopyTo(fromSheet, fromRange.Row, fromRange.Column, toSheet, (row < 0) ? -1 : (row + (i * num3)), (column < 0) ? -1 : (column + (j * num4)), (row < 0) ? -1 : num3, (column < 0) ? -1 : num4, ConvertPasteOption(option));
                                }
                            }
                            return;
                        }
                        finally
                        {
                            fromSheet.ResumeCalcService();
                            toSheet.ResumeCalcService();
                        }
                    }
                    Workbook.CopyTo(fromSheet, fromRange.Row, fromRange.Column, toSheet, toRange.Row, toRange.Column, fromRange.RowCount, fromRange.ColumnCount, ConvertPasteOption(option));
                }
            }
            else
            {
                int num11 = toRange.Row;
                int num12 = toRange.Column;
                int rowCount = toRange.RowCount;
                int columnCount = toRange.ColumnCount;
                IEnumerator enumerator = toSheet.SpanModel.GetEnumerator(num11, num12, rowCount, columnCount);
                while (enumerator.MoveNext())
                {
                    CellRange current = enumerator.Current as CellRange;
                    if (current != null)
                    {
                        toSheet.SpanModel.Remove(current.Row, current.Column);
                    }
                }
                if (string.IsNullOrEmpty(clipboardText))
                {
                    for (int k = 0; k < rowCount; k++)
                    {
                        for (int m = 0; m < columnCount; m++)
                        {
                            toSheet.SetValue(num11 + k, num12 + m, null);
                        }
                    }
                }
                else
                {
                    toSheet.SetCsv(num11, num12, clipboardText, "\r\n", "\t", "\"", TextFileOpenFlags.ImportFormula);
                }
            }
        }

        internal void CloseAutoFilterIndicator()
        {
            _autoFillIndicatorContainer.Width = 0.0;
            _autoFillIndicatorContainer.Height = 0.0;
            _autoFillIndicatorContainer.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
            _autoFillIndicatorContainer.InvalidateMeasure();
            AutoFillIndicatorRec = null;
        }

        internal void CloseDragFillPopup()
        {
            if (_dragFillPopup != null)
            {
                _dragFillPopup.Close();
            }
            if (_dragFillSmartTag != null)
            {
                _dragFillSmartTag.AutoFilterTypeChanged -= new EventHandler(DragFillSmartTag_AutoFilterTypeChanged);
                _dragFillSmartTag.CloseDragFillSmartTagPopup();
                _dragFillSmartTag = null;
            }
        }

        internal void CloseTooltip()
        {
            TooltipHelper.CloseTooltip();
        }

        internal void CloseTouchToolbar()
        {
            if ((_touchToolbarPopup != null) && _touchToolbarPopup.IsOpen)
            {
                _touchToolbarPopup.IsOpen = false;
            }
        }

        internal bool ContainsFilterButton(int row, int column, SheetArea sheetArea)
        {
            return (GetFilterButtonInfo(row, column, sheetArea) != null);
        }

        void ContinueCellSelecting()
        {
            if ((IsWorking && IsSelectingCells) && (MousePosition != _lastClickPoint))
            {
                int activeColumnViewportIndex = GetActiveColumnViewportIndex();
                int activeRowViewportIndex = GetActiveRowViewportIndex();
                ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(activeColumnViewportIndex, MousePosition.X);
                RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(activeRowViewportIndex, MousePosition.Y);
                CellLayout layout3 = GetViewportCellLayoutModel(activeRowViewportIndex, activeColumnViewportIndex).FindPoint(MousePosition.X, MousePosition.Y);
                CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                if (layout3 != null)
                {
                    ExtendSelection(layout3.Row, layout3.Column);
                }
                else if ((viewportColumnLayoutNearX != null) && (viewportRowLayoutNearY != null))
                {
                    ExtendSelection(viewportRowLayoutNearY.Row, viewportColumnLayoutNearX.Column);
                }
                RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                ProcessScrollTimer();
            }
        }

        void ContinueColumnResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            ColumnLayout viewportResizingColumnLayoutFromX = null;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingColumnLayoutFromX = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                    break;

                case HitTestType.ColumnHeader:
                    viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                    if (viewportResizingColumnLayoutFromX == null)
                    {
                        viewportResizingColumnLayoutFromX = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if (viewportResizingColumnLayoutFromX == null)
                        {
                            if (savedHitTestInformation.ColumnViewportIndex == 0)
                            {
                                viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(-1, savedHitTestInformation.HitPoint.X);
                            }
                            if ((viewportResizingColumnLayoutFromX == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                            {
                                viewportResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(savedHitTestInformation.HitPoint.X);
                            }
                        }
                    }
                    break;
            }
            if (viewportResizingColumnLayoutFromX != null)
            {
                double x = viewportResizingColumnLayoutFromX.X;
                if (MousePosition.X > _resizingTracker.X1)
                {
                    _resizingTracker.X1 = Math.Min(base.ActualWidth, MousePosition.X) - 0.5;
                }
                else
                {
                    _resizingTracker.X1 = Math.Max(x, MousePosition.X) - 0.5;
                }
                _resizingTracker.X2 = _resizingTracker.X1;
                if ((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Column)))
                {
                    UpdateResizeToolTip(GetHorizontalResizeTip(Math.Max((double)0.0, (double)(MousePosition.X - x))), true);
                }
            }
        }

        void ContinueColumnSelecting()
        {
            if ((IsWorking && (IsSelectingColumns || IsTouchSelectingColumns)) && (MousePosition != _lastClickPoint))
            {
                int activeColumnViewportIndex = GetActiveColumnViewportIndex();
                ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(activeColumnViewportIndex, MousePosition.X);
                if (viewportColumnLayoutNearX != null)
                {
                    CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                    if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                    {
                        IsContinueTouchOperation = true;
                    }
                    ExtendSelection(-1, viewportColumnLayoutNearX.Column);
                    RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                    ProcessScrollTimer();
                }
            }
        }

        void ContinueDragDropping()
        {
            if (IsDragDropping && (_dragDropFromRange != null))
            {
                DoContinueDragDropping();
            }
        }

        void ContinueDragFill()
        {
            if (IsDraggingFill && (_dragFillStartRange != null))
            {
                DoContinueDragFill();
            }
        }

        void ContinueFloatingObjectsMoving()
        {
            if (IsTouching)
            {
                if (!IsTouchingMovingFloatingObjects)
                {
                    return;
                }
            }
            else if (!IsMovingFloatingOjects)
            {
                return;
            }
            if ((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length != 0))
            {
                UpdateFloatingObjectsMovingResizingToViewports();
                UpdateFloatingObjectsMovingResizingToCoordicates();
                RefreshViewportFloatingObjectsContainerMoving();
                ProcessScrollTimer();
            }
        }

        void ContinueFloatingObjectsResizing()
        {
            if (IsTouching)
            {
                if (!IsTouchingResizingFloatingObjects)
                {
                    return;
                }
            }
            else if (!IsResizingFloatingObjects)
            {
                return;
            }
            if ((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length != 0))
            {
                UpdateFloatingObjectsMovingResizingToViewports();
                UpdateFloatingObjectsMovingResizingToCoordicates();
                RefreshViewportFloatingObjectsContainerResizing();
                ProcessScrollTimer();
            }
        }

        void ContinueRowResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            RowLayout viewportResizingRowLayoutFromY = null;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingRowLayoutFromY = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    break;

                case HitTestType.RowHeader:
                    viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                    if (((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                    {
                        viewportResizingRowLayoutFromY = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    }
                    if ((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.RowViewportIndex == 0))
                    {
                        viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(-1, savedHitTestInformation.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromY == null) && ((savedHitTestInformation.RowViewportIndex == -1) || (savedHitTestInformation.RowViewportIndex == 0)))
                    {
                        viewportResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(savedHitTestInformation.HitPoint.Y);
                    }
                    break;
            }
            if (viewportResizingRowLayoutFromY != null)
            {
                double y = viewportResizingRowLayoutFromY.Y;
                if (MousePosition.Y > _resizingTracker.Y1)
                {
                    _resizingTracker.Y1 = Math.Min(base.ActualHeight, MousePosition.Y) - 0.5;
                }
                else
                {
                    _resizingTracker.Y1 = Math.Max(y, MousePosition.Y) - 0.5;
                }
                _resizingTracker.Y2 = _resizingTracker.Y1;
                if ((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Row)))
                {
                    UpdateResizeToolTip(GetVerticalResizeTip(Math.Max((double)0.0, (double)(MousePosition.Y - y))), false);
                }
            }
        }

        void ContinueRowSelecting()
        {
            if ((IsWorking && (IsSelectingRows || IsTouchSelectingRows)) && (MousePosition != _lastClickPoint))
            {
                int activeRowViewportIndex = GetActiveRowViewportIndex();
                RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(activeRowViewportIndex, MousePosition.Y);
                if (viewportRowLayoutNearY != null)
                {
                    CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                    if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                    {
                        IsContinueTouchOperation = true;
                    }
                    ExtendSelection(viewportRowLayoutNearY.Row, -1);
                    RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                    ProcessScrollTimer();
                }
            }
        }

        void ContinueTouchColumnResizing()
        {
            _DoTouchResizing = true;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            ColumnLayout viewportResizingColumnLayoutFromXForTouch = null;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingColumnLayoutFromXForTouch = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                    break;

                case HitTestType.ColumnHeader:
                    viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                    if (viewportResizingColumnLayoutFromXForTouch == null)
                    {
                        viewportResizingColumnLayoutFromXForTouch = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if ((viewportResizingColumnLayoutFromXForTouch == null) && (savedHitTestInformation.ColumnViewportIndex == 0))
                        {
                            viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(-1, savedHitTestInformation.HitPoint.X);
                        }
                        if ((viewportResizingColumnLayoutFromXForTouch == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                        {
                            viewportResizingColumnLayoutFromXForTouch = GetRowHeaderResizingColumnLayoutFromXForTouch(savedHitTestInformation.HitPoint.X);
                        }
                    }
                    break;
            }
            if (viewportResizingColumnLayoutFromXForTouch != null)
            {
                double x = viewportResizingColumnLayoutFromXForTouch.X;
                if (MousePosition.X > _resizingTracker.X1)
                {
                    _resizingTracker.X1 = Math.Min(base.ActualWidth, MousePosition.X) - 0.5;
                }
                else
                {
                    _resizingTracker.X1 = Math.Max(x, MousePosition.X) - 0.5;
                }
                _resizingTracker.X2 = _resizingTracker.X1;
                if ((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Column)))
                {
                    UpdateResizeToolTip(GetHorizontalResizeTip(Math.Max((double)0.0, (double)(MousePosition.X - x))), true);
                }
            }
        }

        void ContinueTouchDragDropping()
        {
            if (IsTouchDrapDropping && (_dragDropFromRange != null))
            {
                DoContinueDragDropping();
            }
        }

        void ContinueTouchDragFill()
        {
            if (IsTouchDragFilling && (_dragFillStartRange != null))
            {
                DoContinueDragFill();
            }
        }

        void ContinueTouchRowResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            RowLayout viewportResizingRowLayoutFromYForTouch = null;
            _DoTouchResizing = true;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingRowLayoutFromYForTouch = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    break;

                case HitTestType.RowHeader:
                    viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                    if (((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    }
                    if ((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.RowViewportIndex == 0))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(-1, savedHitTestInformation.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromYForTouch == null) && ((savedHitTestInformation.RowViewportIndex == -1) || (savedHitTestInformation.RowViewportIndex == 0)))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetColumnHeaderResizingRowLayoutFromYForTouch(savedHitTestInformation.HitPoint.Y);
                    }
                    break;
            }
            if (viewportResizingRowLayoutFromYForTouch != null)
            {
                double y = viewportResizingRowLayoutFromYForTouch.Y;
                if (MousePosition.Y > _resizingTracker.Y1)
                {
                    _resizingTracker.Y1 = Math.Min(base.ActualHeight, MousePosition.Y) - 0.5;
                }
                else
                {
                    _resizingTracker.Y1 = Math.Max(y, MousePosition.Y) - 0.5;
                }
                _resizingTracker.Y2 = _resizingTracker.Y1;
                if ((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Row)))
                {
                    UpdateResizeToolTip(GetVerticalResizeTip(Math.Max((double)0.0, (double)(MousePosition.Y - y))), false);
                }
            }
        }

        void ContinueTouchSelectingCells(Point touchPoint)
        {
            IsContinueTouchOperation = true;
            int activeColumnViewportIndex = GetActiveColumnViewportIndex();
            int activeRowViewportIndex = GetActiveRowViewportIndex();
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(activeColumnViewportIndex, touchPoint.X);
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(activeRowViewportIndex, touchPoint.Y);
            CellLayout layout3 = GetViewportCellLayoutModel(activeRowViewportIndex, activeColumnViewportIndex).FindPoint(touchPoint.X, touchPoint.Y);
            if ((CachedGripperLocation != null) && CachedGripperLocation.TopLeft.Expand(10, 10).Contains(touchPoint))
            {
                CellRange range = Worksheet.Selections[0];
                if ((Worksheet.ActiveRowIndex != ((range.Row + range.RowCount) - 1)) || (Worksheet.ActiveColumnIndex != ((range.Column + range.ColumnCount) - 1)))
                {
                    Worksheet.Workbook.SuspendEvent();
                    Worksheet.SetActiveCell((range.Row + range.RowCount) - 1, (range.Column + range.ColumnCount) - 1, false);
                    Worksheet.Workbook.ResumeEvent();
                }
            }
            CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
            if (layout3 != null)
            {
                ExtendSelection(layout3.Row, layout3.Column);
            }
            else if ((viewportColumnLayoutNearX != null) && (viewportRowLayoutNearY != null))
            {
                ExtendSelection(viewportRowLayoutNearY.Row, viewportColumnLayoutNearX.Column);
            }
            RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
            ProcessScrollTimer();
        }

        internal static CopyToOption ConvertPasteOption(ClipboardPasteOptions pasteOption)
        {
            CopyToOption option = 0;
            if ((pasteOption & ClipboardPasteOptions.Values) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Value;
            }
            if ((pasteOption & ClipboardPasteOptions.Formatting) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Style;
            }
            if ((pasteOption & ClipboardPasteOptions.Formulas) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Formula;
            }
            if ((pasteOption & ClipboardPasteOptions.FloatingObjects) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.FloatingObject;
            }
            if ((pasteOption & ClipboardPasteOptions.RangeGroup) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.RangeGroup;
            }
            if ((pasteOption & ClipboardPasteOptions.Sparkline) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Sparkline;
            }
            if ((pasteOption & ClipboardPasteOptions.Span) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Span;
            }
            if ((pasteOption & ClipboardPasteOptions.Tags) > ((ClipboardPasteOptions)0))
            {
                option |= CopyToOption.Tag;
            }
            return option;
        }

        void CopyToClipboard(CellRange range, bool isCutting)
        {
            SpreadXClipboard.Range = range;
            SpreadXClipboard.FloatingObjects = null;
            SpreadXClipboard.IsCutting = isCutting;
            SpreadXClipboard.Worksheet = Worksheet;
            ClipboardHelper.SetClipboardData(Worksheet.GetCsv(range.Row, range.Column, range.RowCount, range.ColumnCount, "\r\n", "\t", "\"", false));
        }

        AutoFilterDropDownItemControl CreateAutoFilter(FilterButtonInfo info)
        {
            HideRowFilter rowFilter = info.RowFilter;
            int column = info.Column;
            AutoFilterDropDownItemControl depObj = new AutoFilterDropDownItemControl();
            depObj.SuspendAllHandlers();
            AutoFilterItem item = new AutoFilterItem
            {
                IsChecked = null,
                Criterion = ResourceStrings.Filter_SelectAll
            };
            depObj.FilterItems.Add(item);
            ReadOnlyCollection<object> filterableDataItems = rowFilter.GetFilterableDataItems(column);
            bool flag = false;
            if ((filterableDataItems != null) && (filterableDataItems.Count > 0))
            {
                flag = filterableDataItems[filterableDataItems.Count - 1] == RowFilterBase.BlankItem;
            }
            List<object> filteredInDateItems = new List<object>();
            if (rowFilter.IsColumnFiltered(column))
            {
                filteredInDateItems = GetFilteredInDateItems(column, rowFilter);
            }
            else
            {
                filteredInDateItems = Enumerable.ToList<object>((IEnumerable<object>)filterableDataItems);
            }
            HashSet<object> set = new HashSet<object>();
            foreach (object obj2 in filteredInDateItems)
            {
                set.Add(obj2);
            }
            bool flag2 = true;
            bool flag3 = true;
            AutoFilterItem item2 = null;
            for (int i = 0; i < filterableDataItems.Count; i++)
            {
                object obj3 = filterableDataItems[i];
                bool flag4 = set.Contains(obj3);
                if ((obj3 == null) || string.IsNullOrEmpty(obj3.ToString()))
                {
                    if (item2 == null)
                    {
                        item2 = new AutoFilterItem
                        {
                            IsChecked = new bool?(flag4),
                            Criterion = BlankFilterItem.Blank
                        };
                    }
                }
                else
                {
                    AutoFilterItem item4 = new AutoFilterItem
                    {
                        IsChecked = new bool?(flag4),
                        Criterion = obj3
                    };
                    depObj.FilterItems.Add(item4);
                }
                flag2 = flag2 && flag4;
                flag3 = flag3 && !flag4;
            }
            if (flag && (item2 == null))
            {
                bool flag5 = false;
                if (rowFilter.IsColumnFiltered(column))
                {
                    foreach (object obj4 in filteredInDateItems)
                    {
                        if ((obj4 == null) || string.IsNullOrEmpty(obj4.ToString()))
                        {
                            flag5 = true;
                            break;
                        }
                    }
                }
                else
                {
                    flag5 = true;
                }
                item2 = new AutoFilterItem
                {
                    IsChecked = new bool?(flag5),
                    Criterion = BlankFilterItem.Blank
                };
                flag2 = flag2 && flag5;
                flag3 = flag3 && !flag5;
            }
            if (item2 != null)
            {
                depObj.FilterItems.Add(item2);
            }
            if (flag2)
            {
                item.IsChecked = true;
            }
            else if (flag3)
            {
                item.IsChecked = false;
            }
            else
            {
                item.IsChecked = null;
            }
            depObj.Command = new FilterCommand(this, info, column);
            depObj.ResumeAllHandlers();
            return depObj;
        }

        CellClickEventArgs CreateCellClickEventArgs(int row, int column, SheetSpanModel spanModel, SheetArea area, MouseButtonType button)
        {
            CellRange range = spanModel.Find(row, column);
            if (range != null)
            {
                row = range.Row;
                column = range.Column;
            }
            return new CellClickEventArgs(area, row, column, button);
        }

        internal CellLayoutModel CreateColumnHeaderCellLayoutModel(int columnViewportIndex)
        {
            ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(columnViewportIndex);
            RowLayoutModel columnHeaderRowLayoutModel = GetColumnHeaderRowLayoutModel();
            CellLayoutModel model3 = new CellLayoutModel();
            if ((viewportColumnLayoutModel.Count > 0) && (columnHeaderRowLayoutModel.Count > 0))
            {
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                int row = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)columnHeaderRowLayoutModel, 0).Row;
                int column = viewportColumnLayoutModel[0].Column;
                int num3 = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)columnHeaderRowLayoutModel, columnHeaderRowLayoutModel.Count - 1).Row;
                int num4 = viewportColumnLayoutModel[viewportColumnLayoutModel.Count - 1].Column;
                IEnumerator enumerator = Worksheet.ColumnHeaderSpanModel.GetEnumerator(row, column, (num3 - row) + 1, (num4 - column) + 1);
                float zoomFactor = ZoomFactor;
                while (enumerator.MoveNext())
                {
                    double num6 = 0.0;
                    double num7 = 0.0;
                    double width = 0.0;
                    double height = 0.0;
                    CellRange current = (CellRange)enumerator.Current;
                    for (int i = current.Row; i < row; i++)
                    {
                        num7 -= Math.Ceiling((double)(worksheet.GetActualRowHeight(i, SheetArea.ColumnHeader) * zoomFactor));
                    }
                    for (int j = row; j < current.Row; j++)
                    {
                        num7 += Math.Ceiling((double)(worksheet.GetActualRowHeight(j, SheetArea.ColumnHeader) * zoomFactor));
                    }
                    for (int k = current.Column; k < column; k++)
                    {
                        num6 -= Math.Ceiling((double)(worksheet.GetActualColumnWidth(k, SheetArea.Cells) * zoomFactor));
                    }
                    for (int m = column; m < current.Column; m++)
                    {
                        num6 += Math.Ceiling((double)(worksheet.GetActualColumnWidth(m, SheetArea.Cells) * zoomFactor));
                    }
                    for (int n = current.Row; n < (current.Row + current.RowCount); n++)
                    {
                        if (n < worksheet.ColumnHeader.RowCount)
                        {
                            height += Math.Ceiling((double)(worksheet.GetActualRowHeight(n, SheetArea.ColumnHeader) * zoomFactor));
                        }
                    }
                    for (int num15 = current.Column; num15 < (current.Column + current.ColumnCount); num15++)
                    {
                        if (num15 < worksheet.ColumnCount)
                        {
                            width += Math.Ceiling((double)(worksheet.GetActualColumnWidth(num15, SheetArea.Cells) * zoomFactor));
                        }
                    }
                    model3.Add(new CellLayout(current.Row, current.Column, current.RowCount, current.ColumnCount, viewportColumnLayoutModel[0].X + num6, columnHeaderRowLayoutModel[0].Y + num7, width, height));
                }
            }
            return model3;
        }

        internal virtual RowLayoutModel CreateColumnHeaderRowLayoutModel()
        {
            RowLayoutModel model = new RowLayoutModel();
            SheetLayout sheetLayout = GetSheetLayout();
            if (Worksheet != null)
            {
                float zoomFactor = ZoomFactor;
                double headerY = sheetLayout.HeaderY;
                for (int i = 0; i < Worksheet.ColumnHeader.RowCount; i++)
                {
                    double height = Math.Ceiling((double)(Worksheet.GetActualRowHeight(i, SheetArea.ColumnHeader) * zoomFactor));
                    model.Add(new RowLayout(i, headerY, height));
                    headerY += height;
                }
            }
            return model;
        }

        internal virtual ColumnLayoutModel CreateEnhancedResizeToZeroColumnHeaderViewportColumnLayoutModel(int columnViewportIndex)
        {
            return CreateViewportColumnLayoutModel(columnViewportIndex);
        }

        internal virtual RowLayoutModel CreateEnhancedResizeToZeroRowHeaderViewportRowLayoutModel(int rowViewportIndex)
        {
            return CreateViewportRowLayoutModel(rowViewportIndex);
        }

        FilterButtonInfoModel CreateFilterButtonInfoModel()
        {
            FilterButtonInfoModel model = new FilterButtonInfoModel();
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet != null)
            {
                HideRowFilter rowFilter = worksheet.RowFilter as HideRowFilter;
                if (((rowFilter != null) && (rowFilter.Range != null)) && rowFilter.ShowFilterButton)
                {
                    CellRange range = rowFilter.Range;
                    if (range.Row < 1)
                    {
                        int num = (range.Column < 0) ? 0 : range.Column;
                        int num2 = (range.Column < 0) ? (worksheet.ColumnCount - 1) : ((range.Column + range.ColumnCount) - 1);
                        int row = worksheet.ColumnHeader.RowCount - 1;
                        if (row >= 0)
                        {
                            int column = num;
                            while (column <= num2)
                            {
                                FilterButtonInfo info = new FilterButtonInfo(rowFilter)
                                {
                                    SheetArea = SheetArea.ColumnHeader,
                                    Row = row
                                };
                                CellRange range2 = worksheet.GetSpanCell(row, column, SheetArea.ColumnHeader);
                                if (range2 != null)
                                {
                                    info.Row = range2.Row;
                                    info.Column = range2.Column;
                                    column += range2.ColumnCount;
                                }
                                else
                                {
                                    info.Column = column;
                                    column++;
                                }
                                model.Add(info);
                            }
                        }
                    }
                    else
                    {
                        int num5 = (range.Column < 0) ? 0 : range.Column;
                        int num6 = (range.Column < 0) ? (worksheet.ColumnCount - 1) : ((range.Column + range.ColumnCount) - 1);
                        int num7 = range.Row - 1;
                        int num8 = num5;
                        while (num8 <= num6)
                        {
                            FilterButtonInfo info2 = new FilterButtonInfo(rowFilter)
                            {
                                SheetArea = SheetArea.Cells,
                                Row = num7
                            };
                            CellRange range3 = worksheet.GetSpanCell(num7, num8, SheetArea.Cells);
                            if (range3 != null)
                            {
                                info2.Row = range3.Row;
                                info2.Column = range3.Column;
                                num8 += range3.ColumnCount;
                            }
                            else
                            {
                                info2.Column = num8;
                                num8++;
                            }
                            model.Add(info2);
                        }
                    }
                }
                foreach (SheetTable table in Worksheet.GetTables())
                {
                    if (((table != null) && table.ShowHeader) && table.RowFilter.ShowFilterButton)
                    {
                        int headerIndex = table.HeaderIndex;
                        for (int i = 0; i < table.Range.ColumnCount; i++)
                        {
                            int num11 = table.Range.Column + i;
                            FilterButtonInfo info3 = new FilterButtonInfo(table.RowFilter as HideRowFilter, headerIndex, num11, SheetArea.Cells);
                            model.Add(info3);
                        }
                    }
                }
            }
            return model;
        }

        internal Line CreateFreezeLine()
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            Line line2 = new Line();
            line2.StrokeThickness = 1.0;
            line2.Stroke = brush;
            Line element = line2;
            element.TypeSafeSetStyle(FreezeLineStyle);
            return element;
        }

        internal GroupLayout CreateGroupLayout()
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            GroupLayout layout = new GroupLayout
            {
                X = 0.0,
                Y = 0.0
            };
            if (worksheet != null)
            {
                if (ShowRowRangeGroup && (worksheet.RowRangeGroup != null))
                {
                    int maxLevel = worksheet.RowRangeGroup.GetMaxLevel();
                    if (maxLevel >= 0)
                    {
                        double num2 = Math.Min((double)16.0, (double)(16.0 * ZoomFactor));
                        layout.Width = (num2 * (maxLevel + 2)) + 4.0;
                    }
                }
                if (ShowColumnRangeGroup && (worksheet.ColumnRangeGroup != null))
                {
                    int num3 = worksheet.ColumnRangeGroup.GetMaxLevel();
                    if (num3 >= 0)
                    {
                        double num4 = Math.Min((double)16.0, (double)(16.0 * ZoomFactor));
                        layout.Height = (num4 * (num3 + 2)) + 4.0;
                    }
                }
            }
            return layout;
        }

        internal CellLayoutModel CreateRowHeaderCellLayoutModel(int rowViewportIndex)
        {
            ColumnLayoutModel rowHeaderColumnLayoutModel = GetRowHeaderColumnLayoutModel();
            RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(rowViewportIndex);
            CellLayoutModel model3 = new CellLayoutModel();
            if ((rowHeaderColumnLayoutModel.Count > 0) && (viewportRowLayoutModel.Count > 0))
            {
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                int row = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, 0).Row;
                int column = rowHeaderColumnLayoutModel[0].Column;
                int num3 = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, viewportRowLayoutModel.Count - 1).Row;
                int num4 = rowHeaderColumnLayoutModel[rowHeaderColumnLayoutModel.Count - 1].Column;
                IEnumerator enumerator = Worksheet.RowHeaderSpanModel.GetEnumerator(row, column, (num3 - row) + 1, (num4 - column) + 1);
                float zoomFactor = ZoomFactor;
                while (enumerator.MoveNext())
                {
                    double num6 = 0.0;
                    double num7 = 0.0;
                    double width = 0.0;
                    double height = 0.0;
                    CellRange current = (CellRange)enumerator.Current;
                    for (int i = current.Row; i < row; i++)
                    {
                        num7 -= Math.Ceiling((double)(worksheet.GetActualRowHeight(i, SheetArea.Cells) * zoomFactor));
                    }
                    for (int j = row; j < current.Row; j++)
                    {
                        num7 += Math.Ceiling((double)(worksheet.GetActualRowHeight(j, SheetArea.Cells) * zoomFactor));
                    }
                    for (int k = current.Column; k < column; k++)
                    {
                        num6 -= Math.Ceiling((double)(worksheet.GetActualColumnWidth(k, SheetArea.CornerHeader | SheetArea.RowHeader) * zoomFactor));
                    }
                    for (int m = column; m < current.Column; m++)
                    {
                        num6 += Math.Ceiling((double)(worksheet.GetActualColumnWidth(m, SheetArea.CornerHeader | SheetArea.RowHeader) * zoomFactor));
                    }
                    for (int n = current.Row; n < (current.Row + current.RowCount); n++)
                    {
                        if (n < worksheet.RowCount)
                        {
                            height += Math.Ceiling((double)(worksheet.GetActualRowHeight(n, SheetArea.Cells) * zoomFactor));
                        }
                    }
                    for (int num15 = current.Column; num15 < (current.Column + current.ColumnCount); num15++)
                    {
                        if (num15 < worksheet.RowHeader.ColumnCount)
                        {
                            width += Math.Ceiling((double)(worksheet.GetActualColumnWidth(num15, SheetArea.CornerHeader | SheetArea.RowHeader) * zoomFactor));
                        }
                    }
                    model3.Add(new CellLayout(current.Row, current.Column, current.RowCount, current.ColumnCount, rowHeaderColumnLayoutModel[0].X + num6, viewportRowLayoutModel[0].Y + num7, width, height));
                }
            }
            return model3;
        }

        internal virtual ColumnLayoutModel CreateRowHeaderColumnLayoutModel()
        {
            ColumnLayoutModel model = new ColumnLayoutModel();
            SheetLayout sheetLayout = GetSheetLayout();
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet != null)
            {
                double headerX = sheetLayout.HeaderX;
                float zoomFactor = ZoomFactor;
                for (int i = 0; i < Worksheet.RowHeader.ColumnCount; i++)
                {
                    double width = Math.Ceiling((double)(worksheet.GetActualColumnWidth(i, SheetArea.CornerHeader | SheetArea.RowHeader) * zoomFactor));
                    model.Add(new ColumnLayout(i, headerX, width));
                    headerX += width;
                }
            }
            return model;
        }

        SheetLayout CreateSheetLayout()
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            double width = AvailableSize.Width;
            double height = AvailableSize.Height;
            SheetLayout layout = new SheetLayout
            {
                X = 0.0,
                Y = 0.0
            };
            if ((worksheet != null) && worksheet.Visible)
            {
                GroupLayout groupLayout = GetGroupLayout();
                layout.HeaderX = layout.X + groupLayout.Width;
                layout.HeaderY = layout.Y + groupLayout.Height;
                float zoomFactor = ZoomFactor;
                if (worksheet.RowHeader.IsVisible)
                {
                    for (int n = 0; n < worksheet.RowHeader.Columns.Count; n++)
                    {
                        layout.HeaderWidth += worksheet.GetActualColumnWidth(n, SheetArea.CornerHeader | SheetArea.RowHeader) * zoomFactor;
                    }
                }
                if (worksheet.ColumnHeader.IsVisible)
                {
                    for (int num5 = 0; num5 < worksheet.ColumnHeader.Rows.Count; num5++)
                    {
                        layout.HeaderHeight += worksheet.GetActualRowHeight(num5, SheetArea.ColumnHeader) * zoomFactor;
                    }
                }
                layout.FrozenX = layout.HeaderX + layout.HeaderWidth;
                layout.FrozenY = layout.HeaderY + layout.HeaderHeight;
                for (int i = 0; i < worksheet.FrozenColumnCount; i++)
                {
                    layout.FrozenWidth += Math.Ceiling((double)(worksheet.GetActualColumnWidth(i, SheetArea.Cells) * zoomFactor));
                }
                for (int j = 0; j < worksheet.FrozenRowCount; j++)
                {
                    layout.FrozenHeight += Math.Ceiling((double)(worksheet.GetActualRowHeight(j, SheetArea.Cells) * zoomFactor));
                }
                for (int k = Math.Max(worksheet.FrozenColumnCount, worksheet.ColumnCount - worksheet.FrozenTrailingColumnCount); k < worksheet.ColumnCount; k++)
                {
                    layout.FrozenTrailingWidth += Math.Ceiling((double)(worksheet.GetActualColumnWidth(k, SheetArea.Cells) * zoomFactor));
                }
                for (int m = Math.Max(worksheet.FrozenRowCount, worksheet.RowCount - worksheet.FrozenTrailingRowCount); m < worksheet.RowCount; m++)
                {
                    layout.FrozenTrailingHeight += Math.Ceiling((double)(worksheet.GetActualRowHeight(m, SheetArea.Cells) * zoomFactor));
                }
                width -= layout.HeaderX;
                width -= layout.HeaderWidth;
                width -= layout.FrozenWidth;
                width -= layout.FrozenTrailingWidth;
                width = Math.Max(0.0, width);
                height -= layout.HeaderY;
                height -= layout.HeaderHeight;
                height -= layout.FrozenHeight;
                height -= layout.FrozenTrailingHeight;
                height = Math.Max(0.0, height);
                width = Math.Max(0.0, width);
                height = Math.Max(0.0, height);
                layout.SetViewportWidth(0, width);
                layout.SetViewportHeight(0, height);
                layout.SetViewportX(0, (layout.HeaderX + layout.HeaderWidth) + layout.FrozenWidth);
                layout.SetViewportY(0, (layout.HeaderY + layout.HeaderHeight) + layout.FrozenHeight);
            }
            return layout;
        }

        internal CellLayoutModel CreateViewportCellLayoutModel(int rowViewportIndex, int columnViewportIndex)
        {
            ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(columnViewportIndex);
            RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(rowViewportIndex);
            CellLayoutModel model3 = new CellLayoutModel();
            if ((viewportColumnLayoutModel.Count > 0) && (viewportRowLayoutModel.Count > 0))
            {
                int row = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, 0).Row;
                int column = viewportColumnLayoutModel[0].Column;
                int num3 = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, viewportRowLayoutModel.Count - 1).Row;
                int num4 = viewportColumnLayoutModel[viewportColumnLayoutModel.Count - 1].Column;
                IEnumerator enumerator = Worksheet.SpanModel.GetEnumerator(row, column, (num3 - row) + 1, (num4 - column) + 1);
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                SheetArea cells = SheetArea.Cells;
                float zoomFactor = ZoomFactor;
                Dictionary<int, double> dictionary = new Dictionary<int, double>();
                Dictionary<int, double> dictionary2 = new Dictionary<int, double>();
                while (enumerator.MoveNext() && (worksheet != null))
                {
                    double num6 = 0.0;
                    double num7 = 0.0;
                    double width = 0.0;
                    double height = 0.0;
                    CellRange current = (CellRange)enumerator.Current;
                    for (int i = current.Row; i < row; i++)
                    {
                        double num11 = 0.0;
                        if (!dictionary2.TryGetValue(i, out num11))
                        {
                            num11 = Math.Ceiling((double)(worksheet.GetActualRowHeight(i, cells) * zoomFactor));
                            dictionary2[i] = num11;
                        }
                        num7 -= num11;
                    }
                    for (int j = row; j < current.Row; j++)
                    {
                        double num13 = 0.0;
                        if (!dictionary2.TryGetValue(j, out num13))
                        {
                            num13 = Math.Ceiling((double)(worksheet.GetActualRowHeight(j, cells) * zoomFactor));
                            dictionary2[j] = num13;
                        }
                        num7 += num13;
                    }
                    for (int k = current.Column; k < column; k++)
                    {
                        double num15 = 0.0;
                        if (!dictionary.TryGetValue(k, out num15))
                        {
                            num15 = Math.Ceiling((double)(worksheet.GetActualColumnWidth(k, cells) * zoomFactor));
                            dictionary.Add(k, num15);
                        }
                        num6 -= num15;
                    }
                    for (int m = column; m < current.Column; m++)
                    {
                        double num17 = 0.0;
                        if (!dictionary.TryGetValue(m, out num17))
                        {
                            num17 = Math.Ceiling((double)(worksheet.GetActualColumnWidth(m, cells) * zoomFactor));
                            dictionary.Add(m, num17);
                        }
                        num6 += num17;
                    }
                    for (int n = current.Row; n < (current.Row + current.RowCount); n++)
                    {
                        if (n < worksheet.RowCount)
                        {
                            double num19 = 0.0;
                            if (!dictionary2.TryGetValue(n, out num19))
                            {
                                num19 = Math.Ceiling((double)(worksheet.GetActualRowHeight(n, cells) * zoomFactor));
                                dictionary2[n] = num19;
                            }
                            height += num19;
                        }
                    }
                    for (int num20 = current.Column; num20 < (current.Column + current.ColumnCount); num20++)
                    {
                        if (num20 < worksheet.ColumnCount)
                        {
                            double num21 = 0.0;
                            if (!dictionary.TryGetValue(num20, out num21))
                            {
                                num21 = Math.Ceiling((double)(worksheet.GetActualColumnWidth(num20, cells) * zoomFactor));
                                dictionary.Add(num20, num21);
                            }
                            width += num21;
                        }
                    }
                    model3.Add(new CellLayout(current.Row, current.Column, current.RowCount, current.ColumnCount, viewportColumnLayoutModel[0].X + num6, viewportRowLayoutModel[0].Y + num7, width, height));
                }
            }
            return model3;
        }

        FloatingObjectLayoutModel CreateViewportChartShapeLayoutMode(int rowViewportIndex, int columnViewportIndex)
        {
            FloatingObjectLayoutModel model = new FloatingObjectLayoutModel();
            FloatingObject[] allFloatingObjects = GetAllFloatingObjects();
            if (allFloatingObjects.Length != 0)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                double viewportX = sheetLayout.GetViewportX(columnViewportIndex);
                double viewportY = sheetLayout.GetViewportY(rowViewportIndex);
                Point viewportTopLeftCoordinates = GetViewportTopLeftCoordinates(rowViewportIndex, columnViewportIndex);
                for (int i = 0; i < allFloatingObjects.Length; i++)
                {
                    FloatingObject obj2 = allFloatingObjects[i];
                    double x = 0.0;
                    for (int j = 0; j < obj2.StartColumn; j++)
                    {
                        double num6 = Math.Ceiling((double)(Worksheet.GetActualColumnWidth(j, SheetArea.Cells) * ZoomFactor));
                        x += num6;
                    }
                    x += obj2.StartColumnOffset * ZoomFactor;
                    double y = 0.0;
                    for (int k = 0; k < obj2.StartRow; k++)
                    {
                        double num9 = Math.Ceiling((double)(Worksheet.GetActualRowHeight(k, SheetArea.Cells) * ZoomFactor));
                        y += num9;
                    }
                    y += obj2.StartRowOffset * ZoomFactor;
                    double with = Math.Ceiling((double)(obj2.Size.Width * ZoomFactor));
                    double height = Math.Ceiling((double)(obj2.Size.Height * ZoomFactor));
                    x -= viewportTopLeftCoordinates.X;
                    y -= viewportTopLeftCoordinates.Y;
                    x += viewportX;
                    y += viewportY;
                    model.Add(new FloatingObjectLayout(obj2.Name, x, y, with, height));
                }
            }
            return model;
        }

        internal virtual ColumnLayoutModel CreateViewportColumnLayoutModel(int columnViewportIndex)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            ColumnLayoutModel model = new ColumnLayoutModel();
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet != null)
            {
                float zoomFactor = ZoomFactor;
                if (columnViewportIndex == -1)
                {
                    double x = sheetLayout.HeaderX + sheetLayout.HeaderWidth;
                    for (int i = 0; i < Worksheet.FrozenColumnCount; i++)
                    {
                        double width = Math.Ceiling((double)(worksheet.GetActualColumnWidth(i, SheetArea.Cells) * zoomFactor));
                        model.Add(new ColumnLayout(i, x, width));
                        x += width;
                    }
                    return model;
                }
                if ((columnViewportIndex >= 0) && (columnViewportIndex < columnViewportCount))
                {
                    double viewportX = sheetLayout.GetViewportX(columnViewportIndex);
                    double viewportWidth = sheetLayout.GetViewportWidth(columnViewportIndex);
                    for (int j = GetViewportLeftColumn(columnViewportIndex); ((viewportWidth > 0.0) && (j != -1)) && (j < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)); j++)
                    {
                        double num9 = Math.Ceiling((double)(worksheet.GetActualColumnWidth(j, SheetArea.Cells) * zoomFactor));
                        model.Add(new ColumnLayout(j, viewportX, num9));
                        viewportX += num9;
                        viewportWidth -= num9;
                    }
                    return model;
                }
                if (columnViewportIndex == columnViewportCount)
                {
                    double num10 = sheetLayout.GetViewportX(columnViewportCount - 1) + sheetLayout.GetViewportWidth(columnViewportCount - 1);
                    for (int k = Math.Max(Worksheet.FrozenColumnCount, Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount); k < Worksheet.ColumnCount; k++)
                    {
                        double num12 = Math.Ceiling((double)(worksheet.GetActualColumnWidth(k, SheetArea.Cells) * zoomFactor));
                        model.Add(new ColumnLayout(k, num10, num12));
                        num10 += num12;
                    }
                }
            }
            return model;
        }

        internal virtual RowLayoutModel CreateViewportRowLayoutModel(int rowViewportIndex)
        {
            RowLayoutModel model = new RowLayoutModel();
            SheetLayout sheetLayout = GetSheetLayout();
            int rowViewportCount = GetViewportInfo().RowViewportCount;
            if (Worksheet != null)
            {
                float zoomFactor = ZoomFactor;
                if (rowViewportIndex == -1)
                {
                    double y = sheetLayout.HeaderY + sheetLayout.HeaderHeight;
                    int frozenRowCount = Worksheet.FrozenRowCount;
                    if (Worksheet.RowCount < frozenRowCount)
                    {
                        frozenRowCount = Worksheet.RowCount;
                    }
                    for (int i = 0; i < frozenRowCount; i++)
                    {
                        double height = Math.Ceiling((double)(Worksheet.GetActualRowHeight(i, SheetArea.Cells) * zoomFactor));
                        model.Add(new RowLayout(i, y, height));
                        y += height;
                    }
                    return model;
                }
                if ((rowViewportIndex >= 0) && (rowViewportIndex < rowViewportCount))
                {
                    double viewportY = sheetLayout.GetViewportY(rowViewportIndex);
                    double viewportHeight = sheetLayout.GetViewportHeight(rowViewportIndex);
                    for (int j = GetViewportTopRow(rowViewportIndex); ((viewportHeight > 0.0) && (j != -1)) && (j < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)); j++)
                    {
                        double num10 = Math.Ceiling((double)(Worksheet.GetActualRowHeight(j, SheetArea.Cells) * zoomFactor));
                        model.Add(new RowLayout(j, viewportY, num10));
                        viewportY += num10;
                        viewportHeight -= num10;
                    }
                    return model;
                }
                if (rowViewportIndex == rowViewportCount)
                {
                    double num11 = sheetLayout.GetViewportY(rowViewportCount - 1) + sheetLayout.GetViewportHeight(rowViewportCount - 1);
                    for (int k = Math.Max(Worksheet.FrozenRowCount, Worksheet.RowCount - Worksheet.FrozenTrailingRowCount); k < Worksheet.RowCount; k++)
                    {
                        double num13 = Math.Ceiling((double)(Worksheet.GetActualRowHeight(k, SheetArea.Cells) * zoomFactor));
                        model.Add(new RowLayout(k, num11, num13));
                        num11 += num13;
                    }
                }
            }
            return model;
        }

        internal bool DoCommand(ICommand command)
        {
            return UndoManager.Do(command);
        }

        void DoContinueDragDropping()
        {
            UpdateMouseCursorLocation();
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragStartRowViewport, MousePosition.Y);
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragStartColumnViewport, MousePosition.X);
            if (((viewportRowLayoutNearY != null) && (viewportColumnLayoutNearX != null)) && ((viewportRowLayoutNearY.Height > 0.0) && (viewportColumnLayoutNearX.Width > 0.0)))
            {
                bool flag;
                bool flag2;
                int row = viewportRowLayoutNearY.Row;
                int column = viewportColumnLayoutNearX.Column;
                int rowViewportIndex = _dragStartRowViewport;
                int columnViewportIndex = _dragStartColumnViewport;
                if (GetViewportRowLayoutModel(rowViewportIndex).FindRow(row) == null)
                {
                    double y = GetHitInfo().HitPoint.Y;
                    int rowViewportCount = GetViewportInfo().RowViewportCount;
                    if (MousePosition.Y < y)
                    {
                        if ((_dragStartRowViewport == 0) && (row < Worksheet.FrozenRowCount))
                        {
                            rowViewportIndex = -1;
                        }
                        else if ((_dragStartRowViewport == rowViewportCount) && (row < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                        {
                            rowViewportIndex = rowViewportCount - 1;
                        }
                    }
                    else if ((_dragStartRowViewport == -1) && (row >= Worksheet.FrozenRowCount))
                    {
                        rowViewportIndex = 0;
                    }
                    else if ((_dragStartRowViewport == (rowViewportCount - 1)) && (row >= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                    {
                        rowViewportIndex = rowViewportCount;
                    }
                }
                if (GetViewportColumnLayoutModel(columnViewportIndex).FindColumn(column) == null)
                {
                    double x = GetHitInfo().HitPoint.X;
                    int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                    if (MousePosition.X < x)
                    {
                        if ((_dragStartColumnViewport == 0) && (column < Worksheet.FrozenColumnCount))
                        {
                            columnViewportIndex = -1;
                        }
                        else if ((_dragStartColumnViewport == columnViewportCount) && (column < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                        {
                            columnViewportIndex = columnViewportCount - 1;
                        }
                    }
                    else if ((_dragStartColumnViewport == -1) && (column >= Worksheet.FrozenColumnCount))
                    {
                        columnViewportIndex = 0;
                    }
                    else if ((_dragStartColumnViewport == (columnViewportCount - 1)) && (column >= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                    {
                        columnViewportIndex = columnViewportCount;
                    }
                }
                _dragToRowViewport = rowViewportIndex;
                _dragToColumnViewport = columnViewportIndex;
                _dragToRow = row;
                _dragToColumn = column;
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                _isDragInsert = flag;
                _isDragCopy = flag2;
                if (_isDragInsert && ((_dragDropFromRange.Row == -1) || (_dragDropFromRange.Column == -1)))
                {
                    RefreshDragDropInsertIndicator(rowViewportIndex, columnViewportIndex, row, column);
                }
                else
                {
                    RefreshDragDropIndicator(rowViewportIndex, columnViewportIndex, row, column);
                }
            }
            ProcessScrollTimer();
        }

        void DoContinueDragFill()
        {
            UpdateDragToViewports();
            UpdateDragToCoordicates();
            if (((_dragToRow >= 0) || (_dragToColumn >= 0)) && !IsMouseInDragFillIndicator(MousePosition.X, MousePosition.Y, _dragStartRowViewport, _dragStartColumnViewport, false))
            {
                UpdateMouseCursorLocation();
                UpdateCurrentFillSettings();
                UpdateCurrentFillRange();
                RefreshDragFill();
                RefreshSelectionBorder();
                ProcessScrollTimer();
                int row = (_currentFillRange.Row + _currentFillRange.RowCount) - 1;
                int column = (_currentFillRange.Column + _currentFillRange.ColumnCount) - 1;
                FillDirection currentFillDirection = GetCurrentFillDirection();
                switch (currentFillDirection)
                {
                    case FillDirection.Left:
                    case FillDirection.Up:
                        row = _currentFillRange.Row;
                        column = _currentFillRange.Column;
                        break;
                }
                string str = Worksheet.GetFillText(row, column, GetDragAutoFillType(), currentFillDirection);
                if (str == null)
                {
                    TooltipHelper.CloseTooltip();
                }
                if (!string.IsNullOrWhiteSpace(str))
                {
                    Point point = ArrangeDragFillTooltip(_currentFillRange, currentFillDirection);
                    if (IsTouchDragFilling)
                    {
                        if (ShowDragFillTip)
                        {
                            TooltipHelper.ShowTooltip(str, point.X + 40.0, point.Y);
                        }
                    }
                    else if (ShowDragFillTip)
                    {
                        TooltipHelper.ShowTooltip(str, point.X, point.Y);
                    }
                }
            }
        }

        void DoDragFloatingObjects()
        {
            SuspendFloatingObjectsInvalidate();
            _floatingObjectsMovingResizingOffset = CalcMoveOffset(_dragStartRowViewport, _dragStartColumnViewport, _floatingObjectsMovingResizingStartRow, _floatingObjectsMovingResizingStartColumn, _floatingObjectsMovingResizingStartPoint, _dragToRowViewport, _dragToColumnViewport, _dragToRow, _dragToColumn, MousePosition);
            if ((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length > 0))
            {
                List<string> list = new List<string>();
                foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
                {
                    list.Add(obj2.Name);
                }
                MoveFloatingObjectExtent extent = new MoveFloatingObjectExtent(list.ToArray(), _floatingObjectsMovingResizingOffset.X, _floatingObjectsMovingResizingOffset.Y);
                DoCommand(new DragFloatingObjectUndoAction(Worksheet, extent));
            }
            ResumeFloatingObjectsInvalidate();
        }

        void DoEndDragDropping(ref bool isInvalid, ref string invalidMessage, ref bool doCommand)
        {
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragStartRowViewport, MousePosition.Y);
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragStartColumnViewport, MousePosition.X);
            if ((viewportRowLayoutNearY != null) && (viewportColumnLayoutNearX != null))
            {
                int row = _dragDropFromRange.Row;
                int column = _dragDropFromRange.Column;
                int rowCount = _dragDropFromRange.RowCount;
                int columnCount = _dragDropFromRange.ColumnCount;
                int toRow = (viewportRowLayoutNearY.Height > 0.0) ? viewportRowLayoutNearY.Row : _dragToRow;
                int toColumn = (viewportColumnLayoutNearX.Width > 0.0) ? viewportColumnLayoutNearX.Column : _dragToColumn;
                CellRange exceptedRange = _isDragCopy ? null : _dragDropFromRange;
                if (_isDragInsert && ((row == -1) || (column == -1)))
                {
                    if ((row < 0) || (column < 0))
                    {
                        if (column < 0)
                        {
                            if (row >= 0)
                            {
                                if (MousePosition.Y > (viewportRowLayoutNearY.Y + (viewportRowLayoutNearY.Height / 2.0)))
                                {
                                    toRow = Math.Min(Worksheet.RowCount, toRow + 1);
                                }
                                if ((_isDragCopy && ((toRow <= row) || (toRow >= (row + rowCount)))) || (!_isDragCopy && ((toRow < row) || (toRow > (row + rowCount)))))
                                {
                                    if (!RaiseValidationDragDropBlock(row, column, toRow, toColumn, rowCount, columnCount, _isDragCopy, true, out isInvalid, out invalidMessage))
                                    {
                                        if (HasPartSpans(Worksheet, row, -1, rowCount, -1) || HasPartSpans(Worksheet, toRow, -1, 0, -1))
                                        {
                                            isInvalid = true;
                                            invalidMessage = ResourceStrings.SheetViewDragDropChangePartOfMergedCell;
                                        }
                                        if (!isInvalid && (HasPartArrayFormulas(Worksheet, row, -1, rowCount, -1, exceptedRange) || HasPartArrayFormulas(Worksheet, toRow, -1, 0, -1, exceptedRange)))
                                        {
                                            isInvalid = true;
                                            invalidMessage = ResourceStrings.SheetViewPasteChangePartOfArrayFormula;
                                        }
                                        if (!isInvalid && Worksheet.Protect)
                                        {
                                            isInvalid = true;
                                            invalidMessage = ResourceStrings.SheetViewDragDropChangeProtectRow;
                                        }
                                        if ((!isInvalid && !_isDragCopy) && HasTable(Worksheet, row, -1, rowCount, -1, true))
                                        {
                                            isInvalid = true;
                                            invalidMessage = ResourceStrings.SheetViewDragDropChangePartOfTable;
                                        }
                                    }
                                    if (!isInvalid)
                                    {
                                        DragDropExtent dragMoveExtent = new DragDropExtent(row, -1, toRow, -1, rowCount, -1);
                                        CopyToOption all = CopyToOption.All;
                                        if (!RaiseDragDropBlock(dragMoveExtent.FromRow, dragMoveExtent.FromColumn, dragMoveExtent.ToRow, dragMoveExtent.ToColumn, dragMoveExtent.RowCount, dragMoveExtent.ColumnCount, _isDragCopy, true, CopyToOption.All, out all))
                                        {
                                            DragDropUndoAction command = new DragDropUndoAction(Worksheet, dragMoveExtent, _isDragCopy, true, all);
                                            DoCommand(command);
                                            RaiseDragDropBlockCompleted(dragMoveExtent.FromRow, dragMoveExtent.FromColumn, dragMoveExtent.ToRow, dragMoveExtent.ToColumn, dragMoveExtent.RowCount, dragMoveExtent.ColumnCount, _isDragCopy, true, all);
                                            doCommand = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (MousePosition.X > (viewportColumnLayoutNearX.X + (viewportColumnLayoutNearX.Width / 2.0)))
                            {
                                toColumn = Math.Min(Worksheet.ColumnCount, toColumn + 1);
                            }
                            if ((_isDragCopy && ((toColumn <= column) || (toColumn >= (column + columnCount)))) || (!_isDragCopy && ((toColumn < column) || (toColumn > (column + columnCount)))))
                            {
                                if (!RaiseValidationDragDropBlock(row, column, toRow, toColumn, rowCount, columnCount, _isDragCopy, true, out isInvalid, out invalidMessage))
                                {
                                    if (HasPartSpans(Worksheet, -1, column, -1, columnCount) || HasPartSpans(Worksheet, -1, toColumn, -1, 0))
                                    {
                                        isInvalid = true;
                                        invalidMessage = ResourceStrings.SheetViewDragDropChangePartOfMergedCell;
                                    }
                                    if (!isInvalid && (HasPartArrayFormulas(Worksheet, -1, column, -1, columnCount, exceptedRange) || HasPartArrayFormulas(Worksheet, -1, toColumn, -1, 0, exceptedRange)))
                                    {
                                        isInvalid = true;
                                        invalidMessage = ResourceStrings.SheetViewDragDropChangePartOChangePartOfAnArray;
                                    }
                                    if (!isInvalid && Worksheet.Protect)
                                    {
                                        isInvalid = true;
                                        invalidMessage = ResourceStrings.SheetViewDragDropChangeProtectColumn;
                                    }
                                    if (!isInvalid && HasTable(Worksheet, -1, toColumn, -1, 1, true))
                                    {
                                        isInvalid = true;
                                        invalidMessage = ResourceStrings.SheetViewDragDropShiftTableCell;
                                    }
                                    if ((!isInvalid && !_isDragCopy) && HasTable(Worksheet, -1, column, -1, columnCount, true))
                                    {
                                        isInvalid = true;
                                        invalidMessage = ResourceStrings.SheetViewDragDropChangePartOfTable;
                                    }
                                }
                                if (!isInvalid)
                                {
                                    DragDropExtent extent = new DragDropExtent(-1, column, -1, toColumn, -1, columnCount);
                                    CopyToOption newCopyOption = CopyToOption.All;
                                    if (!RaiseDragDropBlock(extent.FromRow, extent.FromColumn, extent.ToRow, extent.ToColumn, extent.RowCount, extent.ColumnCount, _isDragCopy, true, CopyToOption.All, out newCopyOption))
                                    {
                                        DragDropUndoAction action = new DragDropUndoAction(Worksheet, extent, _isDragCopy, true, newCopyOption);
                                        DoCommand(action);
                                        RaiseDragDropBlockCompleted(extent.FromRow, extent.FromColumn, extent.ToRow, extent.ToColumn, extent.RowCount, extent.ColumnCount, _isDragCopy, true, newCopyOption);
                                        doCommand = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    toRow = (_dragDropFromRange.Row < 0) ? -1 : Math.Max(0, Math.Min((int)(Worksheet.RowCount - rowCount), (int)(toRow - _dragDropRowOffset)));
                    toColumn = (_dragDropFromRange.Column < 0) ? -1 : Math.Max(0, Math.Min((int)(Worksheet.ColumnCount - columnCount), (int)(toColumn - _dragDropColumnOffset)));
                    if ((toRow != row) || (toColumn != column))
                    {
                        if (!RaiseValidationDragDropBlock(row, column, toRow, toColumn, rowCount, columnCount, _isDragCopy, true, out isInvalid, out invalidMessage))
                        {
                            if (HasPartSpans(Worksheet, row, column, rowCount, columnCount) || HasPartSpans(Worksheet, toRow, toColumn, rowCount, columnCount))
                            {
                                isInvalid = true;
                                invalidMessage = ResourceStrings.SheetViewDragDropChangePartOfMergedCell;
                            }
                            if (!isInvalid && (HasPartArrayFormulas(Worksheet, row, column, rowCount, columnCount, exceptedRange) || HasPartArrayFormulas(Worksheet, toRow, toColumn, rowCount, columnCount, exceptedRange)))
                            {
                                isInvalid = true;
                                invalidMessage = ResourceStrings.SheetViewPasteChangePartOfArrayFormula;
                            }
                            if ((!isInvalid && Worksheet.Protect) && ((!_isDragCopy && IsAnyCellInRangeLocked(Worksheet, row, column, rowCount, columnCount)) || IsAnyCellInRangeLocked(Worksheet, toRow, toColumn, rowCount, columnCount)))
                            {
                                isInvalid = true;
                                invalidMessage = ResourceStrings.SheetViewDragDropChangeProtectCell;
                            }
                        }
                        if (!isInvalid)
                        {
                            DragDropExtent extent3 = new DragDropExtent(row, column, toRow, toColumn, rowCount, columnCount);
                            CopyToOption option3 = CopyToOption.All;
                            if (!RaiseDragDropBlock(extent3.FromRow, extent3.FromColumn, extent3.ToRow, extent3.ToColumn, extent3.RowCount, extent3.ColumnCount, _isDragCopy, false, CopyToOption.All, out option3))
                            {
                                DragDropUndoAction action3 = new DragDropUndoAction(Worksheet, extent3, _isDragCopy, false, option3);
                                DoCommand(action3);
                                RaiseDragDropBlockCompleted(extent3.FromRow, extent3.FromColumn, extent3.ToRow, extent3.ToColumn, extent3.RowCount, extent3.ColumnCount, _isDragCopy, false, option3);
                                doCommand = true;
                            }
                        }
                    }
                }
            }
        }

        void DoMoveFloatingObjects()
        {
            SuspendFloatingObjectsInvalidate();
            _floatingObjectsMovingResizingOffset = CalcMoveOffset(_dragStartRowViewport, _dragStartColumnViewport, _floatingObjectsMovingResizingStartRow, _floatingObjectsMovingResizingStartColumn, _floatingObjectsMovingResizingStartPoint, _dragToRowViewport, _dragToColumnViewport, _dragToRow, _dragToColumn, MousePosition);
            if ((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length > 0))
            {
                List<string> list = new List<string>();
                foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
                {
                    list.Add(obj2.Name);
                }
                MoveFloatingObjectExtent extent = new MoveFloatingObjectExtent(list.ToArray(), _floatingObjectsMovingResizingOffset.X, _floatingObjectsMovingResizingOffset.Y);
                DoCommand(new MoveFloatingObjectUndoAction(Worksheet, extent));
            }
            ResumeFloatingObjectsInvalidate();
        }

        void DoResizeFloatingObjects()
        {
            SuspendFloatingObjectsInvalidate();
            if ((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length > 0))
            {
                int activeRowViewportIndex = GetActiveRowViewportIndex();
                int activeColumnViewportIndex = GetActiveColumnViewportIndex();
                Rect[] floatingObjectsResizingRects = GetFloatingObjectsResizingRects(activeRowViewportIndex, activeColumnViewportIndex);
                List<string> list = new List<string>();
                List<Rect> list2 = new List<Rect>();
                for (int i = 0; (i < _movingResizingFloatingObjects.Length) && (i < floatingObjectsResizingRects.Length); i++)
                {
                    FloatingObject obj2 = _movingResizingFloatingObjects[i];
                    Rect rect = new Rect(floatingObjectsResizingRects[i].X, floatingObjectsResizingRects[i].Y, floatingObjectsResizingRects[i].Width, floatingObjectsResizingRects[i].Height);
                    RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(activeRowViewportIndex, rect.Y);
                    if (viewportRowLayoutNearY == null)
                    {
                        viewportRowLayoutNearY = GetViewportRowLayoutNearY(-1, rect.Y);
                    }
                    int row = 0;
                    if (viewportRowLayoutNearY != null)
                    {
                        row = viewportRowLayoutNearY.Row;
                    }
                    double num5 = rect.Y - viewportRowLayoutNearY.Y;
                    double y = 0.0;
                    for (int j = 0; j < row; j++)
                    {
                        double num8 = Math.Ceiling((double)(Worksheet.GetActualRowHeight(j, SheetArea.Cells) * ZoomFactor));
                        y += num8;
                    }
                    y += num5;
                    ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(activeColumnViewportIndex, rect.X);
                    if (viewportColumnLayoutNearX == null)
                    {
                        viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(-1, rect.X);
                    }
                    double column = 0.0;
                    if (viewportColumnLayoutNearX != null)
                    {
                        column = viewportColumnLayoutNearX.Column;
                    }
                    double num10 = rect.X - viewportColumnLayoutNearX.X;
                    double x = 0.0;
                    for (int k = 0; k < column; k++)
                    {
                        double num13 = Math.Ceiling((double)(Worksheet.GetActualColumnWidth(k, SheetArea.Cells) * ZoomFactor));
                        x += num13;
                    }
                    x += num10;
                    x = Math.Floor((double)(x / ((double)ZoomFactor)));
                    y = Math.Floor((double)(y / ((double)ZoomFactor)));
                    double width = Math.Floor((double)(rect.Width / ((double)ZoomFactor)));
                    double height = Math.Floor((double)(rect.Height / ((double)ZoomFactor)));
                    list.Add(obj2.Name);
                    list2.Add(new Rect(x, y, width, height));
                }
                ResizeFloatingObjectExtent extent = new ResizeFloatingObjectExtent(list.ToArray(), list2.ToArray());
                DoCommand(new ResizeFloatingObjectUndoAction(Worksheet, extent));
            }
            ResumeFloatingObjectsInvalidate();
        }

        void DoubleClickStartCellEditing(int row, int column)
        {
            CellRange spanCell = Worksheet.GetSpanCell(row, column);
            if (spanCell != null)
            {
                row = spanCell.Row;
                column = spanCell.Column;
            }
            if ((row == _currentActiveRowIndex) && (column == _currentActiveColumnIndex))
            {
                object formula = Worksheet.GetValue(row, column);
                if (formula == null)
                {
                    formula = Worksheet.GetFormula(row, column);
                }
                EditorStatus enter = EditorStatus.Enter;
                if ((formula != null) && (formula.ToString() != ""))
                {
                    enter = EditorStatus.Edit;
                }
                StartCellEditing(false, null, enter);
            }
        }

        void DoubleTapStartCellEediting(int row, int column)
        {
            DoubleClickStartCellEditing(row, column);
        }

        void DragFillSmartTag_AutoFilterTypeChanged(object sender, EventArgs e)
        {
            if (IsDragFill)
            {
                DragFillSmartTag tag = sender as DragFillSmartTag;
                AutoFillType autoFilterType = tag.AutoFilterType;
                if (_preFillCellsInfo != null)
                {
                    try
                    {
                        SuspendFloatingObjectsInvalidate();
                        CellRange range = AdjustFillRange(_currentFillRange);
                        CopyMoveHelper.UndoCellsInfo(Worksheet, _preFillCellsInfo, range.Row, range.Column, SheetArea.Cells);
                    }
                    finally
                    {
                        ResumeFloatingObjectsInvalidate();
                    }
                }
                FillDirection currentFillDirection = GetCurrentFillDirection();
                if (!RaiseDragFillBlock(_currentFillRange, currentFillDirection, autoFilterType))
                {
                    DragFillExtent dragFillExtent = new DragFillExtent(_dragFillStartRange, _currentFillRange, autoFilterType, currentFillDirection);
                    DragFillUndoAction command = new DragFillUndoAction(Worksheet, dragFillExtent);
                    DoCommand(command);
                    RaiseDragFillBlockCompleted(dragFillExtent.FillRange, dragFillExtent.FillDirection, dragFillExtent.AutoFillType);
                }
            }
        }

        void DragFillSmartTagPopup_Closed(object sender, object e)
        {
            if (!IsDraggingFill)
            {
                _dragFillStartRange = null;
                _preFillCellsInfo = null;
                _currentFillDirection = DragFillDirection.Down;
                _currentFillRange = null;
                _dragFillPopup = null;
            }
        }

        void DragFillSmartTagPopup_Opened(object sender, EventArgs e)
        {
        }

        void EndCellSelecting()
        {
            IsWorking = false;
            IsSelectingCells = false;
            StopScrollTimer();
            if (SavedOldSelections != null)
            {
                if (!IsRangesEqual(SavedOldSelections, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections)))
                {
                    RaiseSelectionChanged();
                }
                SavedOldSelections = null;
            }
        }

        void EndColumnResizing()
        {
            IsWorking = false;
            IsResizingColumns = false;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation.HitPoint.X == MousePosition.X)
            {
                _resizingTracker.Visibility = Visibility.Collapsed;
                CloseTooltip();
            }
            else
            {
                ColumnLayout viewportResizingColumnLayoutFromX;
                switch (savedHitTestInformation.HitTestType)
                {
                    case HitTestType.Corner:
                        viewportResizingColumnLayoutFromX = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if (viewportResizingColumnLayoutFromX != null)
                        {
                            double num6 = (_resizingTracker.X1 - viewportResizingColumnLayoutFromX.X) - viewportResizingColumnLayoutFromX.Width;
                            int column = viewportResizingColumnLayoutFromX.Column;
                            double size = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.RowHeader.Columns[column].ActualWidth + (num6 / ((double)ZoomFactor)))));
                            ColumnResizeExtent[] columns = new ColumnResizeExtent[] { new ColumnResizeExtent(column, column) };
                            ColumnResizeUndoAction command = new ColumnResizeUndoAction(Worksheet, columns, size, true);
                            DoCommand(command);
                        }
                        break;

                    case HitTestType.ColumnHeader:
                        {
                            viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                            bool flag = false;
                            if (viewportResizingColumnLayoutFromX == null)
                            {
                                viewportResizingColumnLayoutFromX = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                                if (viewportResizingColumnLayoutFromX == null)
                                {
                                    if (savedHitTestInformation.ColumnViewportIndex == 0)
                                    {
                                        viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(-1, savedHitTestInformation.HitPoint.X);
                                    }
                                    if ((viewportResizingColumnLayoutFromX == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                                    {
                                        viewportResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(savedHitTestInformation.HitPoint.X);
                                        flag = true;
                                    }
                                }
                            }
                            if (viewportResizingColumnLayoutFromX != null)
                            {
                                double num = (_resizingTracker.X1 - viewportResizingColumnLayoutFromX.X) - viewportResizingColumnLayoutFromX.Width;
                                int num2 = viewportResizingColumnLayoutFromX.Column;
                                double num3 = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.Columns[num2].ActualWidth + (num / ((double)ZoomFactor)))));
                                if (!flag)
                                {
                                    List<ColumnResizeExtent> list = new List<ColumnResizeExtent>();
                                    if (Worksheet.IsSelected(-1, num2))
                                    {
                                        foreach (CellRange range in Worksheet.Selections)
                                        {
                                            if (range.Row == -1)
                                            {
                                                int firstColumn = (range.Column == -1) ? 0 : range.Column;
                                                int num5 = ((range.Column == -1) && (range.ColumnCount == -1)) ? Worksheet.ColumnCount : range.ColumnCount;
                                                list.Add(new ColumnResizeExtent(firstColumn, (firstColumn + num5) - 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        list.Add(new ColumnResizeExtent(num2, num2));
                                    }
                                    ColumnResizeExtent[] extentArray = new ColumnResizeExtent[list.Count];
                                    list.CopyTo(extentArray);
                                    ColumnResizeUndoAction action = new ColumnResizeUndoAction(Worksheet, extentArray, num3, false);
                                    DoCommand(action);
                                }
                                else
                                {
                                    ColumnResizeExtent[] extentArray2 = new ColumnResizeExtent[] { new ColumnResizeExtent(num2, num2) };
                                    ColumnResizeUndoAction action2 = new ColumnResizeUndoAction(Worksheet, extentArray2, num3, true);
                                    DoCommand(action2);
                                }
                            }
                            break;
                        }
                }
                _resizingTracker.Visibility = Visibility.Collapsed;
                CloseTooltip();
            }
        }

        void EndColumnSelecting()
        {
            IsWorking = false;
            IsTouchSelectingColumns = false;
            IsSelectingColumns = false;
            StopScrollTimer();
            if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
            {
                CellRange activeSelection = GetActiveSelection();
                if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
                {
                    activeSelection = Worksheet.Selections[0];
                }
                GetHitInfo();
                int viewportTopRow = GetViewportTopRow(GetActiveRowViewportIndex());
                if ((Worksheet.ActiveRowIndex != viewportTopRow) || (activeSelection.Column != Worksheet.ActiveColumnIndex))
                {
                    Worksheet.SetActiveCell(viewportTopRow, activeSelection.Column, false);
                }
                RefreshSelection();
            }
            if (SavedOldSelections != null)
            {
                if (!IsRangesEqual(SavedOldSelections, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections)))
                {
                    RaiseSelectionChanged();
                }
                SavedOldSelections = null;
            }
        }

        void EndDragDropping()
        {
            ResetMouseCursor();
            bool isInvalid = false;
            string invalidMessage = string.Empty;
            bool doCommand = false;
            if (IsDragDropping && (GetHitInfo().HitPoint != MousePosition))
            {
                DoEndDragDropping(ref isInvalid, ref invalidMessage, ref doCommand);
            }
            if (doCommand)
            {
                SetActiveportIndexAfterDragDrop();
            }
            IsDragDropping = false;
            ResetFlagasAfterDragDropping();
            StopScrollTimer();
            TooltipHelper.CloseTooltip();
            if (isInvalid)
            {
                RaiseInvalidOperation(invalidMessage, null, null);
            }
        }

        void EndDragFill()
        {
            if (_currentFillRange == null)
            {
                IsDraggingFill = false;
            }
            else if (!IsDraggingFill)
            {
                ResetDragFill();
            }
            else
            {
                IsDraggingFill = false;
                if (IsMouseInDragFillIndicator(MousePosition.X, MousePosition.Y, _dragStartRowViewport, _dragStartColumnViewport, false))
                {
                    ResetDragFill();
                }
                else
                {
                    CellRange dragFillFrameRange = GetDragFillFrameRange();
                    if (!ValidateFillRange(dragFillFrameRange))
                    {
                        ResetDragFill();
                        RefreshSelection();
                    }
                    else
                    {
                        AutoFillType dragAutoFillType = GetDragAutoFillType();
                        bool flag3 = ExecuteDragFillAction(_currentFillRange, dragAutoFillType);
                        if (flag3)
                        {
                            ResetDragFill();
                            string sheetViewDragFillInvalidOperation = ResourceStrings.SheetViewDragFillInvalidOperation;
                            RaiseInvalidOperation(sheetViewDragFillInvalidOperation, "DragFill", null);
                        }
                        if (!flag3 && IsDragFill)
                        {
                            ShowDragFillSmartTag(_currentFillRange, dragAutoFillType);
                            ResetDragFill();
                        }
                        else
                        {
                            ResetDragFill();
                        }
                        RefreshSelection();
                    }
                }
            }
        }

        void EndFloatingObjectResizing()
        {
            if ((((_dragStartRowViewport == -2) || (_dragStartColumnViewport == -2)) || ((_floatingObjectsMovingResizingStartRow == -2) || (_floatingObjectsMovingResizingStartColumn == -2))) || (((_dragToRowViewport == -2) || (_dragToColumnViewport == -2)) || ((_dragToRow == -2) || (_dragToColumn == -2))))
            {
                ResetFloatingObjectsMovingResizing();
                StopScrollTimer();
            }
            else
            {
                DoResizeFloatingObjects();
                InvalidateFloatingObjectsLayoutModel();
                RefreshViewportFloatingObjectsLayout();
                ResetFloatingObjectsMovingResizing();
                StopScrollTimer();
            }
        }

        void EndFloatingObjectsMoving()
        {
            if ((((_dragStartRowViewport == -2) || (_dragStartColumnViewport == -2)) || ((_floatingObjectsMovingResizingStartRow == -2) || (_floatingObjectsMovingResizingStartColumn == -2))) || (((_dragToRowViewport == -2) || (_dragToColumnViewport == -2)) || ((_dragToRow == -2) || (_dragToColumn == -2))))
            {
                ResetFloatingObjectsMovingResizing();
                StopScrollTimer();
            }
            else
            {
                bool ctrl = false;
                bool shift = false;
                KeyboardHelper.GetMetaKeyState(out shift, out ctrl);
                if (ctrl)
                {
                    DoDragFloatingObjects();
                }
                else
                {
                    DoMoveFloatingObjects();
                }
                InvalidateFloatingObjectsLayoutModel();
                RefreshViewportFloatingObjectsLayout();
                ResetFloatingObjectsMovingResizing();
                StopScrollTimer();
                if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                {
                    base.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Exits the state where the user can select formulas with the mouse or keyboard.
        /// </summary>
        public void EndFormulaSelection()
        {
            _formulaSelectionFeature.EndFormulaSelection();
        }

        internal virtual bool EndMouseClick(DoubleTappedRoutedEventArgs e)
        {
            IsMouseLeftButtonPressed = false;
            base.ReleasePointerCaptures();
            return true;
        }

        void EndRowResizing()
        {
            IsWorking = false;
            IsResizingRows = false;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation.HitPoint.Y == MousePosition.Y)
            {
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
            }
            else
            {
                RowLayout viewportResizingRowLayoutFromY = null;
                switch (savedHitTestInformation.HitTestType)
                {
                    case HitTestType.Corner:
                        viewportResizingRowLayoutFromY = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                        if (viewportResizingRowLayoutFromY != null)
                        {
                            double num6 = (_resizingTracker.Y1 - viewportResizingRowLayoutFromY.Y) - viewportResizingRowLayoutFromY.Height;
                            int row = viewportResizingRowLayoutFromY.Row;
                            double size = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.ColumnHeader.Rows[row].ActualHeight + (num6 / ((double)ZoomFactor)))));
                            RowResizeExtent[] rows = new RowResizeExtent[] { new RowResizeExtent(row, row) };
                            RowResizeUndoAction command = new RowResizeUndoAction(Worksheet, rows, size, true);
                            DoCommand(command);
                        }
                        break;

                    case HitTestType.RowHeader:
                        {
                            viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                            bool flag = false;
                            if (((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                            {
                                viewportResizingRowLayoutFromY = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                            }
                            if ((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.RowViewportIndex == 0))
                            {
                                viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(-1, savedHitTestInformation.HitPoint.Y);
                            }
                            if ((viewportResizingRowLayoutFromY == null) && ((savedHitTestInformation.RowViewportIndex == -1) || (savedHitTestInformation.RowViewportIndex == 0)))
                            {
                                viewportResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(savedHitTestInformation.HitPoint.Y);
                                flag = true;
                            }
                            if (viewportResizingRowLayoutFromY != null)
                            {
                                double num = (_resizingTracker.Y1 - viewportResizingRowLayoutFromY.Y) - viewportResizingRowLayoutFromY.Height;
                                int firstRow = viewportResizingRowLayoutFromY.Row;
                                double num3 = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.Rows[firstRow].ActualHeight + (num / ((double)ZoomFactor)))));
                                if (flag)
                                {
                                    RowResizeExtent[] extentArray2 = new RowResizeExtent[] { new RowResizeExtent(firstRow, firstRow) };
                                    RowResizeUndoAction action2 = new RowResizeUndoAction(Worksheet, extentArray2, num3, true);
                                    DoCommand(action2);
                                    break;
                                }
                                List<RowResizeExtent> list = new List<RowResizeExtent>();
                                if (Worksheet.IsSelected(firstRow, -1))
                                {
                                    foreach (CellRange range in Worksheet.Selections)
                                    {
                                        if (range.Column == -1)
                                        {
                                            int num4 = (range.Row == -1) ? 0 : range.Row;
                                            int num5 = ((range.Row == -1) && (range.RowCount == -1)) ? Worksheet.RowCount : range.RowCount;
                                            list.Add(new RowResizeExtent(num4, (num4 + num5) - 1));
                                        }
                                    }
                                }
                                else
                                {
                                    list.Add(new RowResizeExtent(firstRow, firstRow));
                                }
                                RowResizeExtent[] extentArray = new RowResizeExtent[list.Count];
                                list.CopyTo(extentArray);
                                RowResizeUndoAction action = new RowResizeUndoAction(Worksheet, extentArray, num3, false);
                                DoCommand(action);
                            }
                            break;
                        }
                }
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
            }
        }

        void EndRowSelecting()
        {
            IsWorking = false;
            IsSelectingRows = false;
            IsTouchSelectingRows = false;
            StopScrollTimer();
            if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
            {
                CellRange activeSelection = GetActiveSelection();
                if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
                {
                    activeSelection = Worksheet.Selections[0];
                }
                GetHitInfo();
                int viewportLeftColumn = GetViewportLeftColumn(GetActiveColumnViewportIndex());
                if ((Worksheet.ActiveColumnIndex != viewportLeftColumn) || (activeSelection.Row != Worksheet.ActiveRowIndex))
                {
                    Worksheet.SetActiveCell(activeSelection.Row, viewportLeftColumn, false);
                }
                RefreshSelection();
            }
            if (SavedOldSelections != null)
            {
                if (!IsRangesEqual(SavedOldSelections, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections)))
                {
                    RaiseSelectionChanged();
                }
                SavedOldSelections = null;
            }
        }

        void EndTouchColumnResizing()
        {
            IsWorking = false;
            IsTouchResizingColumns = false;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if ((savedHitTestInformation.HitPoint.X == MousePosition.X) || !_DoTouchResizing)
            {
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
            }
            else
            {
                ColumnLayout viewportResizingColumnLayoutFromXForTouch;
                switch (savedHitTestInformation.HitTestType)
                {
                    case HitTestType.Corner:
                        viewportResizingColumnLayoutFromXForTouch = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if (viewportResizingColumnLayoutFromXForTouch != null)
                        {
                            double num6 = (_resizingTracker.X1 - viewportResizingColumnLayoutFromXForTouch.X) - viewportResizingColumnLayoutFromXForTouch.Width;
                            int column = viewportResizingColumnLayoutFromXForTouch.Column;
                            double size = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.RowHeader.Columns[column].ActualWidth + (num6 / ((double)ZoomFactor)))));
                            ColumnResizeExtent[] columns = new ColumnResizeExtent[] { new ColumnResizeExtent(column, column) };
                            ColumnResizeUndoAction command = new ColumnResizeUndoAction(Worksheet, columns, size, true);
                            DoCommand(command);
                        }
                        break;

                    case HitTestType.ColumnHeader:
                        {
                            viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                            bool flag = false;
                            if (viewportResizingColumnLayoutFromXForTouch == null)
                            {
                                viewportResizingColumnLayoutFromXForTouch = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                                if ((viewportResizingColumnLayoutFromXForTouch == null) && (savedHitTestInformation.ColumnViewportIndex == 0))
                                {
                                    viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(-1, savedHitTestInformation.HitPoint.X);
                                }
                                if ((viewportResizingColumnLayoutFromXForTouch == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                                {
                                    viewportResizingColumnLayoutFromXForTouch = GetRowHeaderResizingColumnLayoutFromXForTouch(savedHitTestInformation.HitPoint.X);
                                    flag = true;
                                }
                            }
                            if (viewportResizingColumnLayoutFromXForTouch != null)
                            {
                                double num = (_resizingTracker.X1 - viewportResizingColumnLayoutFromXForTouch.X) - viewportResizingColumnLayoutFromXForTouch.Width;
                                int num2 = viewportResizingColumnLayoutFromXForTouch.Column;
                                double num3 = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.Columns[num2].ActualWidth + (num / ((double)ZoomFactor)))));
                                if (!flag)
                                {
                                    List<ColumnResizeExtent> list = new List<ColumnResizeExtent>();
                                    if (Worksheet.IsSelected(-1, num2))
                                    {
                                        foreach (CellRange range in Worksheet.Selections)
                                        {
                                            if (range.Row == -1)
                                            {
                                                int firstColumn = (range.Column == -1) ? 0 : range.Column;
                                                int num5 = ((range.Column == -1) && (range.ColumnCount == -1)) ? Worksheet.ColumnCount : range.ColumnCount;
                                                list.Add(new ColumnResizeExtent(firstColumn, (firstColumn + num5) - 1));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        list.Add(new ColumnResizeExtent(num2, num2));
                                    }
                                    ColumnResizeExtent[] extentArray = new ColumnResizeExtent[list.Count];
                                    list.CopyTo(extentArray);
                                    ColumnResizeUndoAction action = new ColumnResizeUndoAction(Worksheet, extentArray, num3, false);
                                    DoCommand(action);
                                }
                                else
                                {
                                    ColumnResizeExtent[] extentArray2 = new ColumnResizeExtent[] { new ColumnResizeExtent(num2, num2) };
                                    ColumnResizeUndoAction action2 = new ColumnResizeUndoAction(Worksheet, extentArray2, num3, true);
                                    DoCommand(action2);
                                }
                            }
                            break;
                        }
                }
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
                _DoTouchResizing = false;
            }
        }

        void EndTouchDragDropping()
        {
            ResetMouseCursor();
            bool isInvalid = false;
            string invalidMessage = string.Empty;
            bool doCommand = false;
            if (IsTouchDrapDropping && (GetHitInfo().HitPoint != MousePosition))
            {
                DoEndDragDropping(ref isInvalid, ref invalidMessage, ref doCommand);
            }
            if (doCommand)
            {
                SetActiveportIndexAfterDragDrop();
            }
            ResetFlagasAfterDragDropping();
            StopScrollTimer();
            if (isInvalid)
            {
                RaiseInvalidOperation(invalidMessage, null, null);
            }
            TooltipHelper.CloseTooltip();
        }

        void EndTouchDragFill()
        {
            if (_currentFillRange != null)
            {
                if (!IsTouchDragFilling)
                {
                    ResetTouchDragFill();
                }
                else if (IsMouseInDragFillIndicator(MousePosition.X, MousePosition.Y, _dragStartRowViewport, _dragStartColumnViewport, false))
                {
                    ResetTouchDragFill();
                }
                else
                {
                    CellRange dragFillFrameRange = GetDragFillFrameRange();
                    if (!ValidateFillRange(dragFillFrameRange))
                    {
                        ResetTouchDragFill();
                        RefreshSelection();
                    }
                    else
                    {
                        AutoFillType dragAutoFillType = GetDragAutoFillType();
                        if (!ExecuteDragFillAction(_currentFillRange, dragAutoFillType) && IsTouchDragFilling)
                        {
                            ShowDragFillSmartTag(_currentFillRange, dragAutoFillType);
                        }
                        ResetTouchDragFill();
                        RefreshSelection();
                    }
                }
            }
        }

        void EndTouchRowResizing()
        {
            IsWorking = false;
            IsTouchResizingRows = false;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if ((savedHitTestInformation.HitPoint.Y == MousePosition.Y) || !_DoTouchResizing)
            {
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
            }
            else
            {
                RowLayout viewportResizingRowLayoutFromYForTouch = null;
                switch (savedHitTestInformation.HitTestType)
                {
                    case HitTestType.Corner:
                        viewportResizingRowLayoutFromYForTouch = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                        if (viewportResizingRowLayoutFromYForTouch != null)
                        {
                            double num6 = (_resizingTracker.Y1 - viewportResizingRowLayoutFromYForTouch.Y) - viewportResizingRowLayoutFromYForTouch.Height;
                            int row = viewportResizingRowLayoutFromYForTouch.Row;
                            double size = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.ColumnHeader.Rows[row].ActualHeight + (num6 / ((double)ZoomFactor)))));
                            RowResizeExtent[] rows = new RowResizeExtent[] { new RowResizeExtent(row, row) };
                            RowResizeUndoAction command = new RowResizeUndoAction(Worksheet, rows, size, true);
                            DoCommand(command);
                        }
                        break;

                    case HitTestType.RowHeader:
                        {
                            viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                            bool flag = false;
                            if ((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.RowViewportIndex == 0))
                            {
                                viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(-1, savedHitTestInformation.HitPoint.Y);
                            }
                            if (((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                            {
                                viewportResizingRowLayoutFromYForTouch = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                            }
                            if ((viewportResizingRowLayoutFromYForTouch == null) && ((savedHitTestInformation.RowViewportIndex == -1) || (savedHitTestInformation.RowViewportIndex == 0)))
                            {
                                viewportResizingRowLayoutFromYForTouch = GetColumnHeaderResizingRowLayoutFromYForTouch(savedHitTestInformation.HitPoint.Y);
                                flag = true;
                            }
                            if (viewportResizingRowLayoutFromYForTouch != null)
                            {
                                double num = (_resizingTracker.Y1 - viewportResizingRowLayoutFromYForTouch.Y) - viewportResizingRowLayoutFromYForTouch.Height;
                                int firstRow = viewportResizingRowLayoutFromYForTouch.Row;
                                double num3 = Math.Ceiling(Math.Max((double)0.0, (double)(Worksheet.Rows[firstRow].ActualHeight + (num / ((double)ZoomFactor)))));
                                if (flag)
                                {
                                    RowResizeExtent[] extentArray2 = new RowResizeExtent[] { new RowResizeExtent(firstRow, firstRow) };
                                    RowResizeUndoAction action2 = new RowResizeUndoAction(Worksheet, extentArray2, num3, true);
                                    DoCommand(action2);
                                    break;
                                }
                                List<RowResizeExtent> list = new List<RowResizeExtent>();
                                if (Worksheet.IsSelected(firstRow, -1))
                                {
                                    foreach (CellRange range in Worksheet.Selections)
                                    {
                                        if (range.Column == -1)
                                        {
                                            int num4 = (range.Row == -1) ? 0 : range.Row;
                                            int num5 = ((range.Row == -1) && (range.RowCount == -1)) ? Worksheet.RowCount : range.RowCount;
                                            list.Add(new RowResizeExtent(num4, (num4 + num5) - 1));
                                        }
                                    }
                                }
                                else
                                {
                                    list.Add(new RowResizeExtent(firstRow, firstRow));
                                }
                                RowResizeExtent[] extentArray = new RowResizeExtent[list.Count];
                                list.CopyTo(extentArray);
                                RowResizeUndoAction action = new RowResizeUndoAction(Worksheet, extentArray, num3, false);
                                DoCommand(action);
                            }
                            break;
                        }
                }
                TooltipHelper.CloseTooltip();
                _resizingTracker.Visibility = Visibility.Collapsed;
                _DoTouchResizing = false;
            }
        }

        void EndTouchSelectingCells()
        {
            IsWorking = false;
            IsTouchSelectingCells = false;
            StopScrollTimer();
            if (SavedOldSelections != null)
            {
                if (!IsRangesEqual(SavedOldSelections, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections)))
                {
                    RaiseSelectionChanged();
                }
                SavedOldSelections = null;
            }
            CellRange activeSelection = GetActiveSelection();
            if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
            {
                activeSelection = Worksheet.Selections[0];
            }
            if ((activeSelection != null) && ((Worksheet.ActiveColumnIndex != activeSelection.Column) || (Worksheet.ActiveRowIndex != activeSelection.Row)))
            {
                Worksheet.SetActiveCell(activeSelection.Row, activeSelection.Column, false);
            }
        }

        internal virtual bool EndTouchTap(Point point)
        {
            return true;
        }

        bool ExecuteDragFillAction(CellRange fillRange, AutoFillType autoFillType)
        {
            CellRange range = AdjustFillRange(fillRange);
            object[,] objArray = Worksheet.FindFormulas(range.Row, range.Column, range.RowCount, range.ColumnCount);
            for (int i = 0; i < objArray.GetLength(0); i++)
            {
                string str = (string)(objArray[i, 1] as string);
                if (str.StartsWith("{"))
                {
                    return true;
                }
            }
            _preFillCellsInfo = new CopyMoveCellsInfo(range.RowCount, range.ColumnCount);
            CopyMoveHelper.SaveViewportInfo(Worksheet, _preFillCellsInfo, range.Row, range.Column, CopyToOption.All);
            FillDirection currentFillDirection = GetCurrentFillDirection();
            if (RaiseDragFillBlock(fillRange, currentFillDirection, autoFillType))
            {
                return true;
            }
            DragFillExtent dragFillExtent = new DragFillExtent(_dragFillStartRange, fillRange, autoFillType, currentFillDirection);
            DragFillUndoAction command = new DragFillUndoAction(Worksheet, dragFillExtent);
            if (!DoCommand(command))
            {
                command.Undo(this);
                return true;
            }
            RaiseDragFillBlockCompleted(dragFillExtent.FillRange, dragFillExtent.FillDirection, dragFillExtent.AutoFillType);
            return false;
        }

        /// <summary>
        /// Specifies the last cell in the cell selection. 
        /// </summary>  
        /// <param name="row">The row index of the extended selection.</param>
        /// <param name="column">The column index of the extended selection.</param>
        public void ExtendSelection(int row, int column)
        {
            Worksheet.ExtendSelection(row, column);
        }

        void FilterPopup_Closed(object sender, object e)
        {
            FocusInternal();
            if (_hitFilterInfo != null)
            {
                UpdateHitFilterCellState();
                _hitFilterInfo.RowViewportIndex = -2;
                _hitFilterInfo.ColumnViewportIndex = -2;
                _hitFilterInfo = null;
            }
        }

        void FilterPopup_Opened(object sender, object e)
        {
            if (_hitFilterInfo != null)
            {
                UpdateHitFilterCellState();
            }
        }

        internal void FocusInternal()
        {
#if UWP || WASM
            // 手机上不设置输入焦点
            if (_host != null)
            {
                _host.IsTabStop = true;

                if (_viewportPresenters != null)
                {
                    GcViewport viewport = _viewportPresenters[1, 1];
                    if ((viewport != null) && !viewport.FocusContent())
                    {
                        _host.Focus(FocusState.Programmatic);
                    }
                }
            }
#endif
        }

        CellRange GetActiveCell()
        {
            int activeRowIndex = Worksheet.ActiveRowIndex;
            int activeColumnIndex = Worksheet.ActiveColumnIndex;
            CellRange range = new CellRange(activeRowIndex, activeColumnIndex, 1, 1);
            CellRange range2 = Worksheet.SpanModel.Find(activeRowIndex, activeColumnIndex);
            if (range2 != null)
            {
                range = range2;
            }
            return range;
        }

        internal int GetActiveColumnViewportIndex()
        {
            return Worksheet.GetActiveColumnViewportIndex();
        }

        internal int GetActiveRowViewportIndex()
        {
            return Worksheet.GetActiveRowViewportIndex();
        }

        internal CellRange GetActiveSelection()
        {
            CellRange activeCell = GetActiveCell();
            ReadOnlyCollection<CellRange> selections = Worksheet.Selections;
            int num = selections.Count;
            if (num > 0)
            {
                for (int i = num - 1; i >= 0; i--)
                {
                    CellRange range2 = selections[i];
                    if (range2.Contains(activeCell))
                    {
                        return range2;
                    }
                }
            }
            return null;
        }

        internal Rect GetActiveSelectionBounds()
        {
            CellRange activeSelection = GetActiveSelection();
            if (activeSelection == null)
            {
                activeSelection = GetActiveCell();
            }
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
            SheetLayout sheetLayout = GetSheetLayout();
            if (viewportRowsPresenter != null)
            {
                double viewportX = sheetLayout.GetViewportX(GetActiveColumnViewportIndex());
                double viewportY = sheetLayout.GetViewportY(GetActiveRowViewportIndex());
                Rect rangeBounds = viewportRowsPresenter.GetRangeBounds(activeSelection);
                if (!double.IsInfinity(rangeBounds.Width) && !double.IsInfinity(rangeBounds.Height))
                {
                    return new Rect(viewportX + rangeBounds.X, viewportY + rangeBounds.Y, rangeBounds.Width, rangeBounds.Height);
                }
            }
            return Rect.Empty;
        }

        internal FloatingObject[] GetAllFloatingObjects()
        {
            List<FloatingObject> list = new List<FloatingObject>();
            if (Worksheet != null)
            {
                if (Worksheet.FloatingObjects.Count > 0)
                {
                    list.AddRange((IEnumerable<FloatingObject>)Worksheet.FloatingObjects);
                }
                if (Worksheet.Pictures.Count > 0)
                {
                    foreach (Picture picture in Worksheet.Pictures)
                    {
                        list.Add(picture);
                    }
                }
                if (Worksheet.Charts.Count > 0)
                {
                    foreach (FloatingObject obj2 in Worksheet.Charts)
                    {
                        list.Add(obj2);
                    }
                }
            }
            return list.ToArray();
        }

        internal FloatingObject[] GetAllSelectedFloatingObjects()
        {
            List<FloatingObject> list = new List<FloatingObject>();
            foreach (FloatingObject obj2 in GetAllFloatingObjects())
            {
                if (obj2.IsSelected)
                {
                    list.Add(obj2);
                }
            }
            return list.ToArray();
        }

        Rect GetAutoFillIndicatorRect(GcViewport vp, CellRange activeSelection)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            double viewportX = sheetLayout.GetViewportX(vp.ColumnViewportIndex);
            double viewportY = sheetLayout.GetViewportY(vp.RowViewportIndex);
            Rect rangeBounds = vp._cachedSelectionFrameLayout;
            if (!vp.SelectionContainer.IsAnchorCellInSelection)
            {
                rangeBounds = vp._cachedFocusCellLayout;
            }
            if (vp.Sheet.Worksheet.Selections.Count > 0)
            {
                rangeBounds = vp.GetRangeBounds(activeSelection);
            }
            Rect rect3 = rangeBounds;
            return new Rect(((rect3.Width + viewportX) + rangeBounds.X) - 16.0, (rect3.Height + viewportY) + rangeBounds.Y, 16.0, 16.0);
        }

        FloatingObjectLayoutModel GetCacheFloatingObjectsMovingResizingLayoutModels(int rowViewport, int columnViewport)
        {
            return _cachedFloatingObjectMovingResizingLayoutModel[rowViewport + 1, columnViewport + 1];
        }

        Cell GetCanSelectedCell(int row, int column, int rowCount, int columnCount)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            int num = (row < 0) ? 0 : row;
            int num2 = (column < 0) ? 0 : column;
            int num3 = (row < 0) ? worksheet.RowCount : (row + rowCount);
            int num4 = (column < 0) ? worksheet.ColumnCount : (column + columnCount);
            for (int i = num; i < num3; i++)
            {
                if (worksheet.GetActualRowVisible(i, SheetArea.Cells))
                {
                    for (int j = num2; j < num4; j++)
                    {
                        CellRange spanCell = worksheet.GetSpanCell(i, j);
                        if (spanCell == null)
                        {
                            if (worksheet.GetActualStyleInfo(i, j, SheetArea.Cells).Focusable && worksheet.GetActualColumnVisible(j, SheetArea.Cells))
                            {
                                return worksheet.Cells[i, j];
                            }
                            j++;
                        }
                        else
                        {
                            if (worksheet.GetActualStyleInfo(spanCell.Row, spanCell.Column, SheetArea.Cells).Focusable && (worksheet.GetActualColumnWidth(spanCell.Column, spanCell.ColumnCount, SheetArea.Cells) > 0.0))
                            {
                                return worksheet.Cells[spanCell.Row, spanCell.Column];
                            }
                            j = spanCell.Column + spanCell.ColumnCount;
                        }
                    }
                }
                i++;
            }
            return null;
        }

        Cell GetCanSelectedCellInColumn(int startRow, int column)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            int row = startRow;
            while (row < worksheet.RowCount)
            {
                CellRange spanCell = worksheet.GetSpanCell(row, column);
                if (spanCell == null)
                {
                    if (worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable && worksheet.GetActualRowVisible(row, SheetArea.Cells))
                    {
                        return worksheet.Cells[row, column];
                    }
                    row++;
                }
                else
                {
                    if (((spanCell.ColumnCount == 1) || ((spanCell.Row + spanCell.RowCount) == worksheet.RowCount)) && (worksheet.GetActualStyleInfo(spanCell.Row, column, SheetArea.Cells).Focusable && worksheet.GetActualRowVisible(spanCell.Row, SheetArea.Cells)))
                    {
                        return worksheet.Cells[spanCell.Row, column];
                    }
                    row = spanCell.Row + spanCell.RowCount;
                }
            }
            return null;
        }

        Cell GetCanSelectedCellInRow(int row, int startColumn)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            int column = startColumn;
            while (column < worksheet.ColumnCount)
            {
                CellRange spanCell = worksheet.GetSpanCell(row, column);
                if (spanCell == null)
                {
                    if (worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable && worksheet.GetActualColumnVisible(column, SheetArea.Cells))
                    {
                        return worksheet.Cells[row, column];
                    }
                    column++;
                }
                else
                {
                    if (((spanCell.RowCount == 1) || ((spanCell.Column + spanCell.ColumnCount) == worksheet.ColumnCount)) && (worksheet.GetActualStyleInfo(row, spanCell.Column, SheetArea.Cells).Focusable && worksheet.GetActualColumnVisible(spanCell.Column, SheetArea.Cells)))
                    {
                        return worksheet.Cells[row, spanCell.Column];
                    }
                    column = spanCell.Column + spanCell.ColumnCount;
                }
            }
            return null;
        }

        internal CellLayoutModel GetCellLayoutModel(int rowViewportIndex, int columnViewportIndex, SheetArea sheetArea)
        {
            switch (sheetArea)
            {
                case SheetArea.Cells:
                    return GetViewportCellLayoutModel(rowViewportIndex, columnViewportIndex);

                case (SheetArea.CornerHeader | SheetArea.RowHeader):
                    return GetRowHeaderCellLayoutModel(rowViewportIndex);

                case SheetArea.ColumnHeader:
                    return GetColumnHeaderCellLayoutModel(columnViewportIndex);
            }
            return null;
        }

        CellRange GetCellRangeEx(CellRange cellRange, ICellsSupport dataContext)
        {
            return new CellRange((cellRange.Row < 0) ? 0 : cellRange.Row, (cellRange.Column < 0) ? 0 : cellRange.Column, (cellRange.RowCount < 0) ? dataContext.Rows.Count : cellRange.RowCount, (cellRange.ColumnCount < 0) ? dataContext.Columns.Count : cellRange.ColumnCount);
        }

        internal double GetColumnAutoFitValue(int column, bool rowHeader)
        {
            string str = string.Empty;
            double num = -1.0;
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            int rowCount = worksheet.RowCount;
            Cell cell = null;
            FontFamily fontFamily = null;
            fontFamily = InheritedControlFontFamily;
            object textFormattingMode = null;
            SheetArea sheetArea = rowHeader ? (SheetArea.CornerHeader | SheetArea.RowHeader) : SheetArea.Cells;
            IDictionary<MeasureInfo, Dictionary<string, object>> dictionary = (IDictionary<MeasureInfo, Dictionary<string, object>>)new Dictionary<MeasureInfo, Dictionary<string, object>>();
            int activeRowViewportIndex = GetActiveRowViewportIndex();
            int viewportTopRow = GetViewportTopRow(activeRowViewportIndex);
            if (viewportTopRow < 0)
            {
                viewportTopRow = 0;
            }
            int num5 = 500;
            for (int i = 0; i < rowCount; i++)
            {
                cell = rowHeader ? worksheet.RowHeader.Cells[i, column] : worksheet.Cells[i, column];
                string str2 = Worksheet.GetText(i, column, sheetArea);
                if (!string.IsNullOrEmpty(str2))
                {
                    CellRange range = worksheet.GetSpanCell(i, column, sheetArea);
                    if ((range == null) || ((range.Column >= column) && (range.ColumnCount <= 1)))
                    {
                        double height = 0.0;
                        if (range == null)
                        {
                            height = worksheet.GetRowHeight(i, sheetArea);
                        }
                        else
                        {
                            for (int j = 0; j < range.RowCount; j++)
                            {
                                height += worksheet.GetRowHeight(i, sheetArea);
                            }
                        }
                        Size maxSize = MeasureHelper.ConvertExcelCellSizeToTextSize(new Size(double.PositiveInfinity, height), 1.0);
                        if ((viewportTopRow <= i) && (i < (viewportTopRow + num5)))
                        {
                            num = Math.Max(num, MeasureCellText(cell, i, column, maxSize, fontFamily, textFormattingMode, base.UseLayoutRounding));
                        }
                        else
                        {
                            MeasureInfo info = new MeasureInfo(cell, maxSize);
                            if (dictionary.Keys.Contains(info))
                            {
                                str = (string)(dictionary[info]["t"] as string);
                                if (str2.Length > str.Length)
                                {
                                    dictionary[info]["t"] = str2;
                                    dictionary[info]["r"] = (int)i;
                                }
                            }
                            else
                            {
                                Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
                                dictionary2.Add("t", str2);
                                dictionary2.Add("r", (int)i);
                                dictionary.Add(info, dictionary2);
                            }
                        }
                        if (range != null)
                        {
                            i += range.RowCount - 1;
                        }
                    }
                }
            }
            foreach (MeasureInfo info2 in dictionary.Keys)
            {
                int row = (int)((int)dictionary[info2]["r"]);
                double rowHeight = 0.0;
                cell = rowHeader ? worksheet.RowHeader.Cells[row, column] : worksheet.Cells[row, column];
                CellRange range2 = worksheet.GetSpanCell(row, column, sheetArea);
                if (range2 == null)
                {
                    rowHeight = worksheet.GetRowHeight(row, sheetArea);
                }
                else
                {
                    for (int k = 0; k < range2.RowCount; k++)
                    {
                        rowHeight += worksheet.GetRowHeight(row, sheetArea);
                    }
                }
                Size size2 = MeasureHelper.ConvertExcelCellSizeToTextSize(new Size(double.PositiveInfinity, rowHeight), 1.0);
                num = Math.Max(num, MeasureCellText(cell, row, column, size2, fontFamily, textFormattingMode, base.UseLayoutRounding));
            }
            return num;
        }

        internal CellLayoutModel GetColumnHeaderCellLayoutModel(int columnViewportIndex)
        {
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            if (_cachedColumnHeaderCellLayoutModel == null)
            {
                _cachedColumnHeaderCellLayoutModel = new CellLayoutModel[columnViewportCount + 2];
            }
            if (_cachedColumnHeaderCellLayoutModel[columnViewportIndex + 1] == null)
            {
                _cachedColumnHeaderCellLayoutModel[columnViewportIndex + 1] = CreateColumnHeaderCellLayoutModel(columnViewportIndex);
            }
            return _cachedColumnHeaderCellLayoutModel[columnViewportIndex + 1];
        }

        Rect GetColumnHeaderRectangle(int columnViewportIndex)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            double viewportX = sheetLayout.GetViewportX(columnViewportIndex);
            double headerY = sheetLayout.HeaderY;
            double width = sheetLayout.GetViewportWidth(columnViewportIndex) - 1.0;
            double height = sheetLayout.HeaderHeight - 1.0;
            if ((width >= 0.0) && (height >= 0.0))
            {
                return new Rect(viewportX, headerY, width, height);
            }
            return Rect.Empty;
        }

        RowLayout GetColumnHeaderResizingRowLayoutFromY(double y)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet.ColumnCount > 0)
            {
                RowLayoutModel columnHeaderRowLayoutModel = GetColumnHeaderRowLayoutModel();
                for (int i = columnHeaderRowLayoutModel.Count - 1; i >= 0; i--)
                {
                    RowLayout layout = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)columnHeaderRowLayoutModel, i);
                    if (((y >= Math.Max((layout.Y + layout.Height) - 4.0, layout.Y)) && (y < ((layout.Y + layout.Height) + 4.0))) && worksheet.ColumnHeader.Rows[layout.Row].CanUserResize)
                    {
                        return layout;
                    }
                }
            }
            return null;
        }

        RowLayout GetColumnHeaderResizingRowLayoutFromYForTouch(double y)
        {
            RowLayout columnHeaderResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(y);
            if (columnHeaderResizingRowLayoutFromY == null)
            {
                for (int i = -5; i < 5; i++)
                {
                    columnHeaderResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(y);
                    if (columnHeaderResizingRowLayoutFromY != null)
                    {
                        return columnHeaderResizingRowLayoutFromY;
                    }
                }
            }
            return columnHeaderResizingRowLayoutFromY;
        }

        RowLayout GetColumnHeaderRowLayout(int row)
        {
            return GetColumnHeaderRowLayoutModel().FindRow(row);
        }

        RowLayout GetColumnHeaderRowLayoutFromY(double y)
        {
            return GetColumnHeaderRowLayoutModel().FindY(y);
        }

        internal RowLayoutModel GetColumnHeaderRowLayoutModel()
        {
            if (_cachedColumnHeaderRowLayoutModel == null)
            {
                _cachedColumnHeaderRowLayoutModel = CreateColumnHeaderRowLayoutModel();
            }
            return _cachedColumnHeaderRowLayoutModel;
        }

        internal GcViewport GetColumnHeaderRowsPresenter(int columnViewportIndex)
        {
            if (_columnHeaderPresenters == null)
            {
                return null;
            }
            return _columnHeaderPresenters[columnViewportIndex + 1];
        }

        internal ColumnLayoutModel GetColumnHeaderViewportColumnLayoutModel(int columnViewportIndex)
        {
            if (_cachedColumnHeaderViewportColumnLayoutModel == null)
            {
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                _cachedColumnHeaderViewportColumnLayoutModel = new ColumnLayoutModel[columnViewportCount + 2];
            }
            if (_cachedColumnHeaderViewportColumnLayoutModel[columnViewportIndex + 1] == null)
            {
                if (ResizeZeroIndicator == Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
                {
                    _cachedColumnHeaderViewportColumnLayoutModel[columnViewportIndex + 1] = CreateEnhancedResizeToZeroColumnHeaderViewportColumnLayoutModel(columnViewportIndex);
                }
                else
                {
                    _cachedColumnHeaderViewportColumnLayoutModel[columnViewportIndex + 1] = CreateViewportColumnLayoutModel(columnViewportIndex);
                }
            }
            return _cachedColumnHeaderViewportColumnLayoutModel[columnViewportIndex + 1];
        }

        internal ColumnLayoutModel GetColumnLayoutModel(int columnViewportIndex, SheetArea sheetArea)
        {
            switch (sheetArea)
            {
                case SheetArea.Cells:
                    return GetViewportColumnLayoutModel(columnViewportIndex);

                case (SheetArea.CornerHeader | SheetArea.RowHeader):
                    return GetRowHeaderColumnLayoutModel();

                case SheetArea.ColumnHeader:
                    if (ResizeZeroIndicator != Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
                    {
                        return GetViewportColumnLayoutModel(columnViewportIndex);
                    }
                    return GetColumnHeaderViewportColumnLayoutModel(columnViewportIndex);
            }
            return null;
        }

        internal GcViewport GetCornerPresenter()
        {
            return _cornerPresenter;
        }

        Rect GetCornerRectangle()
        {
            SheetLayout sheetLayout = GetSheetLayout();
            double headerX = sheetLayout.HeaderX;
            double headerY = sheetLayout.HeaderY;
            double width = sheetLayout.HeaderWidth - 1.0;
            double height = sheetLayout.HeaderHeight - 1.0;
            if ((width >= 0.0) && (height >= 0.0))
            {
                return new Rect(headerX, headerY, width, height);
            }
            return Rect.Empty;
        }

        ColumnLayout GetCurrentDragToColumnLayout()
        {
            return GetViewportColumnLayoutModel(_dragToColumnViewport).FindColumn(_dragToColumn);
        }

        RowLayout GetCurrentDragToRowLayout()
        {
            return GetViewportRowLayoutModel(_dragToRowViewport).FindRow(_dragToRow);
        }

        FillDirection GetCurrentFillDirection()
        {
            switch (_currentFillDirection)
            {
                case DragFillDirection.Left:
                    return FillDirection.Left;

                case DragFillDirection.Right:
                    return FillDirection.Right;

                case DragFillDirection.Up:
                    return FillDirection.Up;

                case DragFillDirection.Down:
                    return FillDirection.Down;

                case DragFillDirection.LeftClear:
                    return FillDirection.Left;

                case DragFillDirection.UpClear:
                    return FillDirection.Up;
            }
            return FillDirection.Down;
        }

        CellRange GetCurrentFillRange()
        {
            int row = -1;
            int column = -1;
            int rowCount = -1;
            int columnCount = -1;
            switch (_currentFillDirection)
            {
                case DragFillDirection.Left:
                    if (!IsDragFillWholeColumns)
                    {
                        row = DragFillStartTopRow;
                        rowCount = _dragFillStartRange.RowCount;
                        break;
                    }
                    row = -1;
                    rowCount = -1;
                    break;

                case DragFillDirection.Right:
                    if (!IsDragFillWholeColumns)
                    {
                        row = DragFillStartTopRow;
                        rowCount = _dragFillStartRange.RowCount;
                    }
                    else
                    {
                        row = -1;
                        rowCount = -1;
                    }
                    column = DragFillStartRightColumn + 1;
                    columnCount = (_dragToColumn - column) + 1;
                    goto Label_0184;

                case DragFillDirection.Up:
                    row = _dragToRow;
                    rowCount = DragFillStartTopRow - row;
                    if (!IsDragFillWholeRows)
                    {
                        column = DragFillStartLeftColumn;
                        columnCount = _dragFillStartRange.ColumnCount;
                    }
                    else
                    {
                        column = -1;
                        columnCount = -1;
                    }
                    goto Label_0184;

                case DragFillDirection.Down:
                    row = DragFillStartBottomRow + 1;
                    rowCount = (_dragToRow - row) + 1;
                    if (!IsDragFillWholeRows)
                    {
                        column = DragFillStartLeftColumn;
                        columnCount = _dragFillStartRange.ColumnCount;
                    }
                    else
                    {
                        column = -1;
                        columnCount = -1;
                    }
                    goto Label_0184;

                case DragFillDirection.LeftClear:
                    if (!IsDragFillWholeColumns)
                    {
                        row = _dragFillStartRange.Row;
                        rowCount = _dragFillStartRange.RowCount;
                    }
                    else
                    {
                        row = -1;
                        rowCount = -1;
                    }
                    column = _dragToColumn;
                    columnCount = (DragFillStartRightColumn - column) + 1;
                    goto Label_0184;

                case DragFillDirection.UpClear:
                    row = _dragToRow;
                    rowCount = (DragFillStartBottomRow - row) + 1;
                    if (!IsDragFillWholeRows)
                    {
                        column = DragFillStartLeftColumn;
                        columnCount = _dragFillStartRange.ColumnCount;
                    }
                    else
                    {
                        column = -1;
                        columnCount = -1;
                    }
                    goto Label_0184;

                default:
                    goto Label_0184;
            }
            column = _dragToColumn;
            columnCount = DragFillStartLeftColumn - column;
        Label_0184:
            return new CellRange(row, column, rowCount, columnCount);
        }

        internal DataValidationListButtonInfo GetDataValidationListButtonInfo(int row, int column, SheetArea sheetArea)
        {
            if (((sheetArea != SheetArea.Cells) || (Worksheet.ActiveColumnIndex != column)) || (Worksheet.ActiveRowIndex != row))
            {
                return null;
            }
            DataValidator actualDataValidator = Worksheet.ActiveCell.ActualDataValidator;
            if (((actualDataValidator == null) || (actualDataValidator.Type != CriteriaType.List)) || !actualDataValidator.InCellDropdown)
            {
                return null;
            }
            ViewportInfo viewportInfo = GetViewportInfo();
            List<int> list = Enumerable.ToList<int>(Enumerable.Distinct<int>(viewportInfo.LeftColumns));
            list.Add(Worksheet.ColumnCount);
            int num = column + 1;
            CellRange spanCell = Worksheet.GetSpanCell(row, column);
            if ((spanCell != null) && (spanCell.ColumnCount > 1))
            {
                num = column + spanCell.ColumnCount;
            }
            if (!list.Contains(num))
            {
                return new DataValidationListButtonInfo(actualDataValidator, row, column, SheetArea.Cells) { DisplayColumn = column + 1, ColumnViewportIndex = viewportInfo.ActiveColumnViewport, RowViewportIndex = viewportInfo.ActiveRowViewport };
            }
            return new DataValidationListButtonInfo(actualDataValidator, row, column, SheetArea.Cells) { DisplayColumn = column, ColumnViewportIndex = viewportInfo.ActiveColumnViewport, RowViewportIndex = viewportInfo.ActiveRowViewport };
        }

        double GetDataValidationListDropdownWidth(int row, int column, int columnViewportIndex)
        {
            double num = 0.0;
            CellRange range = Worksheet.GetSpanCell(row, column, SheetArea.Cells);
            if (range != null)
            {
                for (int i = 0; i < range.ColumnCount; i++)
                {
                    ColumnLayout layout = GetViewportColumnLayoutModel(columnViewportIndex).Find(column + i);
                    if (layout != null)
                    {
                        num += layout.Width;
                    }
                }
                return num;
            }
            ColumnLayout layout2 = GetViewportColumnLayoutModel(columnViewportIndex).Find(column);
            if (layout2 != null)
            {
                num += layout2.Width;
            }
            return num;
        }

        AutoFillType GetDragAutoFillType()
        {
            bool flag;
            bool flag2;
            if (DefaultAutoFillType.HasValue)
            {
                return DefaultAutoFillType.Value;
            }
            if (IsDragClear)
            {
                return AutoFillType.ClearValues;
            }
            KeyboardHelper.GetMetaKeyState(out flag, out flag2);
            if ((((_dragFillStartRange.RowCount == 1) && (_dragFillStartRange.ColumnCount == 1)) && !IsDragFillWholeColumns) && !IsDragFillWholeRows)
            {
                if (flag2)
                {
                    return AutoFillType.FillSeries;
                }
                return AutoFillType.CopyCells;
            }
            if (flag2)
            {
                return AutoFillType.CopyCells;
            }
            return AutoFillType.FillSeries;
        }

        internal CellRange GetDragClearRange()
        {
            if (IsDragClear)
            {
                return _currentFillRange;
            }
            return null;
        }

        internal CellRange GetDragFillFrameRange()
        {
            if (IsDragClear)
            {
                return _dragFillStartRange;
            }
            int row = 0;
            int rowCount = 0;
            int column = 0;
            int columnCount = 0;
            if (IsVerticalDragFill)
            {
                row = (_currentFillDirection == DragFillDirection.Up) ? _currentFillRange.Row : _dragFillStartRange.Row;
                rowCount = _dragFillStartRange.RowCount + _currentFillRange.RowCount;
                column = _dragFillStartRange.Column;
                columnCount = _dragFillStartRange.ColumnCount;
            }
            else
            {
                row = _dragFillStartRange.Row;
                rowCount = _dragFillStartRange.RowCount;
                column = (_currentFillDirection == DragFillDirection.Left) ? _currentFillRange.Column : _dragFillStartRange.Column;
                columnCount = _dragFillStartRange.ColumnCount + _currentFillRange.ColumnCount;
            }
            return new CellRange(row, column, rowCount, columnCount);
        }

        internal FilterButtonInfo GetFilterButtonInfo(int row, int column, SheetArea sheetArea)
        {
            return GetFilterButtonInfoModel().Find(row, column, sheetArea);
        }

        FilterButtonInfoModel GetFilterButtonInfoModel()
        {
            if (_cachedFilterButtonInfoModel == null)
            {
                _cachedFilterButtonInfoModel = CreateFilterButtonInfoModel();
            }
            return _cachedFilterButtonInfoModel;
        }

        List<object> GetFilteredInDateItems(int columnIndex, RowFilterBase filter)
        {
            List<object> list = new List<object>();
            if ((filter != null) && filter.IsColumnFiltered(columnIndex))
            {
                int num = (filter.Range.Row == -1) ? 0 : filter.Range.Row;
                int num2 = (filter.Range.RowCount == -1) ? filter.Sheet.RowCount : filter.Range.RowCount;
                for (int i = num; i < (num + num2); i++)
                {
                    if (!filter.IsRowFilteredOut(i))
                    {
                        object obj2 = filter.Sheet.GetValue(i, columnIndex);
                        object text = null;
                        if ((obj2 is DateTime) || (obj2 is TimeSpan))
                        {
                            text = obj2;
                        }
                        else
                        {
                            text = filter.Sheet.GetText(i, columnIndex);
                        }
                        if (!list.Contains(text))
                        {
                            list.Add(text);
                        }
                    }
                }
            }
            return list;
        }

        Rect[] GetFloatingObjectsBottomCenterResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X + layout.Width, layout.Y + layout.Height);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X, layout2.Y);
                double y = Math.Min(point3.Y, point4.Y);
                double height = Math.Abs((double)(point3.Y - point4.Y));
                double width = layout2.Width;
                Rect rect = new Rect(layout2.X, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsBottomLeftResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X, layout.Y + layout.Height);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X + layout2.Width, layout2.Y);
                double x = Math.Min(point3.X, point4.X);
                double y = Math.Min(point3.Y, point4.Y);
                double width = Math.Abs((double)(point4.X - point3.X));
                double height = Math.Abs((double)(point4.Y - point3.Y));
                Rect rect = new Rect(x, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsBottomRighResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X + layout.Width, layout.Y + layout.Height);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X, layout2.Y);
                double x = Math.Min(point3.X, point4.X);
                double y = Math.Min(point3.Y, point4.Y);
                double width = Math.Abs((double)(point3.X - point4.X));
                double height = Math.Abs((double)(point3.Y - point4.Y));
                Rect rect = new Rect(x, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsMiddleLeftResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X, layout.Y);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X + layout2.Width, layout2.Y + layout2.Height);
                double x = Math.Min(point3.X, point4.X);
                double width = Math.Abs((double)(point4.X - point3.X));
                double height = layout2.Height;
                Rect rect = new Rect(x, layout2.Y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsMiddleRightResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X + layout.Width, layout.Y + layout.Height);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X, layout2.Y);
                double x = Math.Min(point3.X, point4.X);
                double width = Math.Abs((double)(point3.X - point4.X));
                double height = layout2.Height;
                Rect rect = new Rect(x, layout2.Y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        internal Rect[] GetFloatingObjectsMovingFrameRects(int rowViewport, int columnViewport)
        {
            FloatingObject[] allSelectedFloatingObjects = GetAllSelectedFloatingObjects();
            if ((allSelectedFloatingObjects == null) || (allSelectedFloatingObjects.Length == 0))
            {
                return null;
            }
            List<Rect> list = new List<Rect>();
            Point mousePosition = MousePosition;
            new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            foreach (FloatingObject obj2 in allSelectedFloatingObjects)
            {
                bool flag;
                bool flag2;
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                Point point2 = new Point(_floatingObjectsMovingResizingStartPoint.X - layout.X, _floatingObjectsMovingResizingStartPoint.Y - layout.Y);
                double x = mousePosition.X - point2.X;
                double y = mousePosition.Y - point2.Y;
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                if (flag)
                {
                    double num3 = x - layout.X;
                    double num4 = y - layout.Y;
                    if (Math.Abs(num3) > Math.Abs(num4))
                    {
                        y = layout.Y;
                    }
                    else
                    {
                        x = layout.X;
                    }
                }
                list.Add(new Rect(x, y, layout.Width, layout.Height));
            }
            return list.ToArray();
        }

        internal Rect[] GetFloatingObjectsResizingRects(int rowViewport, int columnViewport)
        {
            if ((_movingResizingFloatingObjects == null) || (_movingResizingFloatingObjects.Length == 0))
            {
                return null;
            }
            Point mousePosition = MousePosition;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (IsTouchingResizingFloatingObjects || IsTouchingMovingFloatingObjects)
            {
                savedHitTestInformation = _touchStartHitTestInfo;
            }
            if (savedHitTestInformation.FloatingObjectInfo == null)
            {
                Debugger.Break();
            }
            if (savedHitTestInformation.FloatingObjectInfo.InTopNWSEResize)
            {
                return GetFloatingObjectsTopleftResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InTopNSResize)
            {
                return GetFloatingObjectsTopCenterResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InTopNESWResize)
            {
                return GetFloatingObjectsTopRightResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InLeftWEResize)
            {
                return GetFloatingObjectsMiddleLeftResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InRightWEResize)
            {
                return GetFloatingObjectsMiddleRightResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InBottomNESWResize)
            {
                return GetFloatingObjectsBottomLeftResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InBottomNSResize)
            {
                return GetFloatingObjectsBottomCenterResizingRects(rowViewport, columnViewport, mousePosition);
            }
            if (savedHitTestInformation.FloatingObjectInfo.InBottomNWSEResize)
            {
                return GetFloatingObjectsBottomRighResizingRects(rowViewport, columnViewport, mousePosition);
            }
            return new List<Rect>().ToArray();
        }

        Rect[] GetFloatingObjectsTopCenterResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X, layout.Y);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X + layout2.Width, layout2.Y + layout2.Height);
                double y = Math.Min(point3.Y, point4.Y);
                double height = Math.Abs((double)(point4.Y - point3.Y));
                double width = layout2.Width;
                Rect rect = new Rect(layout2.X, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsTopleftResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X, layout.Y);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X + layout2.Width, layout2.Y + layout2.Height);
                double x = Math.Min(point3.X, point4.X);
                double y = Math.Min(point3.Y, point4.Y);
                double width = Math.Abs((double)(point4.X - point3.X));
                double height = Math.Abs((double)(point4.Y - point3.Y));
                Rect rect = new Rect(x, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        Rect[] GetFloatingObjectsTopRightResizingRects(int rowViewport, int columnViewport, Point mousePosition)
        {
            List<Rect> list = new List<Rect>();
            FloatingObjectLayoutModel cacheFloatingObjectsMovingResizingLayoutModels = GetCacheFloatingObjectsMovingResizingLayoutModels(rowViewport, columnViewport);
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewport, columnViewport);
            Point point = new Point(mousePosition.X - _floatingObjectsMovingResizingStartPoint.X, mousePosition.Y - _floatingObjectsMovingResizingStartPoint.Y);
            foreach (FloatingObject obj2 in _movingResizingFloatingObjects)
            {
                FloatingObjectLayout layout = cacheFloatingObjectsMovingResizingLayoutModels.Find(obj2.Name);
                FloatingObjectLayout layout2 = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                Point point2 = new Point(layout.X + layout.Width, layout.Y);
                Point point3 = new Point(point2.X + point.X, point2.Y + point.Y);
                Point point4 = new Point(layout2.X, layout2.Y + layout2.Height);
                double x = Math.Min(point3.X, point4.X);
                double y = Math.Min(point3.Y, point4.Y);
                double width = Math.Abs((double)(point4.X - point3.X));
                double height = Math.Abs((double)(point4.Y - point3.Y));
                Rect rect = new Rect(x, y, width, height);
                list.Add(rect);
            }
            return list.ToArray();
        }

        /// <summary>
        /// Gets the index of the floating object Z.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public int GetFloatingObjectZIndex(string name)
        {
            int activeRowViewportIndex = GetActiveRowViewportIndex();
            int activeColumnViewportIndex = GetActiveColumnViewportIndex();
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(activeRowViewportIndex, activeColumnViewportIndex);
            if (viewportRowsPresenter != null)
            {
                return viewportRowsPresenter.GetFlotingObjectZIndex(name);
            }
            return -1;
        }

        CellRange GetFromRange()
        {
            CellRange range = null;
            if (Worksheet.Selections.Count > 1)
            {
                return range;
            }
            if (Worksheet.Selections.Count == 1)
            {
                return Worksheet.Selections[0];
            }
            CellRange spanCell = Worksheet.GetSpanCell(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
            if (spanCell != null)
            {
                return spanCell;
            }
            return new CellRange(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, 1, 1);
        }

        internal Windows.UI.Color GetGripperFillColor()
        {
            if (Worksheet == null)
            {
                return Colors.White;
            }
            if (!string.IsNullOrWhiteSpace(Worksheet.TouchSelectionGripperBackgroundThemeColor))
            {
                return Worksheet.Workbook.GetThemeColor(Worksheet.TouchSelectionGripperBackgroundThemeColor);
            }
            return Worksheet.TouchSelectionGripperBackgroundColor;
        }

        internal Windows.UI.Color GetGripperStrokeColor()
        {
            if (Worksheet == null)
            {
                return Windows.UI.Color.FromArgb(220, 0, 0, 0);
            }
            if (!string.IsNullOrWhiteSpace(Worksheet.SelectionBorderThemeColor))
            {
                return Worksheet.Workbook.GetThemeColor(Worksheet.SelectionBorderThemeColor);
            }
            return Worksheet.SelectionBorderColor;
        }

        internal GroupLayout GetGroupLayout()
        {
            if (_cachedGroupLayout == null)
            {
                _cachedGroupLayout = CreateGroupLayout();
            }
            return _cachedGroupLayout;
        }

        string GetHorizentalScrollTip(int column)
        {
            return string.Format(ResourceStrings.HorizentalScroll, (object[])new object[] { ((Worksheet.ColumnHeader.AutoText == HeaderAutoText.Numbers) ? ((int)column).ToString() : IndexToLetter(column)) });
        }

        string GetHorizontalResizeTip(double size)
        {
            object[] args = new object[1];
            double num = size / ((double)ZoomFactor);
            args[0] = ((double)num).ToString("0");
            return string.Format(ResourceStrings.ColumnResize, args);
        }

        internal ImageSource GetImageSource(string image)
        {
            if (_cachedToolbarImageSources.ContainsKey(image))
            {
                return _cachedToolbarImageSources[image];
            }
            string name = IntrospectionExtensions.GetTypeInfo((Type)typeof(SheetView)).Assembly.GetName().Name;
            Uri uri = new Uri(string.Format("ms-appx:///{0}/Icons/{1}", (object[])new object[] { name, image }), (UriKind)UriKind.RelativeOrAbsolute);
            BitmapImage image2 = new BitmapImage(uri);
            _cachedResizerGipper[image] = image2;
            return image2;
        }

        internal int GetMaxBottomScrollableRow()
        {
            int frozenRowCount = Worksheet.FrozenRowCount;
            int num2 = (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1;
            while (num2 > frozenRowCount)
            {
                if (Worksheet.Rows[num2].ActualVisible)
                {
                    return num2;
                }
                num2--;
            }
            return num2;
        }

        internal int GetMaxLeftScrollableColumn()
        {
            int frozenColumnCount = Worksheet.FrozenColumnCount;
            int num2 = (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1;
            while (frozenColumnCount < num2)
            {
                if (Worksheet.Columns[frozenColumnCount].ActualVisible)
                {
                    return frozenColumnCount;
                }
                frozenColumnCount++;
            }
            return frozenColumnCount;
        }

        internal int GetMaxRightScrollableColumn()
        {
            int frozenColumnCount = Worksheet.FrozenColumnCount;
            int num2 = (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1;
            while (num2 > frozenColumnCount)
            {
                if (Worksheet.Columns[num2].ActualVisible)
                {
                    return num2;
                }
                num2--;
            }
            return num2;
        }

        internal int GetMaxTopScrollableRow()
        {
            int frozenRowCount = Worksheet.FrozenRowCount;
            int num2 = (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1;
            while (frozenRowCount < num2)
            {
                if (Worksheet.Rows[frozenRowCount].ActualVisible)
                {
                    return frozenRowCount;
                }
                frozenRowCount++;
            }
            return frozenRowCount;
        }

        DataValidationListButtonInfo GetMouseDownDataValidationButton(HitTestInformation hi, bool touching = false)
        {
            DataValidationListButtonInfo info = null;
            RowLayout columnHeaderRowLayoutFromY = null;
            ColumnLayout viewportColumnLayoutFromX = null;
            SheetArea cells = SheetArea.Cells;
            if (hi.HitTestType == HitTestType.ColumnHeader)
            {
                columnHeaderRowLayoutFromY = GetColumnHeaderRowLayoutFromY(hi.HitPoint.Y);
                viewportColumnLayoutFromX = GetViewportColumnLayoutFromX(hi.ColumnViewportIndex, hi.HitPoint.X);
                cells = SheetArea.ColumnHeader;
                return null;
            }
            if (hi.HitTestType == HitTestType.Viewport)
            {
                ViewportInfo viewportInfo = GetViewportInfo();
                if ((hi.RowViewportIndex != viewportInfo.ActiveRowViewport) || (hi.ColumnViewportIndex != viewportInfo.ActiveColumnViewport))
                {
                    return null;
                }
                columnHeaderRowLayoutFromY = GetViewportRowLayoutFromY(hi.RowViewportIndex, hi.HitPoint.Y);
                viewportColumnLayoutFromX = GetViewportColumnLayoutFromX(hi.ColumnViewportIndex, hi.HitPoint.X);
                cells = SheetArea.Cells;
            }
            if ((columnHeaderRowLayoutFromY != null) && (viewportColumnLayoutFromX != null))
            {
                int row = columnHeaderRowLayoutFromY.Row;
                int column = viewportColumnLayoutFromX.Column - 1;
                while (column >= 0)
                {
                    CellRange range = Worksheet.GetSpanCell(row, column, cells);
                    if (range != null)
                    {
                        row = range.Row;
                        column = range.Column;
                    }
                    info = GetDataValidationListButtonInfo(row, column, cells);
                    if (info != null)
                    {
                        ColumnLayout layout3 = GetColumnLayoutModel(hi.ColumnViewportIndex, SheetArea.Cells).Find(column);
                        if ((layout3 != null) && (Math.Abs((double)(layout3.Width - 0.0)) >= 1E-06))
                        {
                            break;
                        }
                        info = null;
                        column--;
                    }
                    else
                    {
                        ColumnLayout layout4 = GetColumnLayoutModel(hi.ColumnViewportIndex, SheetArea.Cells).Find(column);
                        if ((layout4 == null) || ((Math.Abs((double)(layout4.Width - 0.0)) >= 1E-06) && (layout4.Width > 16.0)))
                        {
                            break;
                        }
                        column--;
                    }
                }
                if ((column >= 0) && (info == null))
                {
                    ColumnLayout layout5 = GetColumnLayoutModel(hi.ColumnViewportIndex, SheetArea.Cells).Find(column);
                    if (layout5 != null)
                    {
                        CellRange range2 = Worksheet.GetSpanCell(columnHeaderRowLayoutFromY.Row, layout5.Column - 1, cells);
                        if (range2 != null)
                        {
                            row = range2.Row;
                            column = range2.Column;
                        }
                        info = GetDataValidationListButtonInfo(row, column, cells);
                    }
                }
                if (info == null)
                {
                    row = columnHeaderRowLayoutFromY.Row;
                    column = viewportColumnLayoutFromX.Column;
                    CellRange range3 = Worksheet.GetSpanCell(columnHeaderRowLayoutFromY.Row, viewportColumnLayoutFromX.Column - 1, cells);
                    if (range3 != null)
                    {
                        row = range3.Row;
                        column = range3.Column;
                    }
                    info = GetDataValidationListButtonInfo(row, column, cells);
                }
                if (info != null)
                {
                    double x = hi.HitPoint.X;
                    double y = hi.HitPoint.Y;
                    double num5 = Math.Min(16.0, columnHeaderRowLayoutFromY.Height);
                    if (info.Column == info.DisplayColumn)
                    {
                        double num6 = Math.Min(16.0, viewportColumnLayoutFromX.Width);
                        if (!touching)
                        {
                            if (((x >= (((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - num6) - 2.0)) && (x < ((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - 2.0))) && ((y >= (((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num5) - 2.0)) && (y < ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - 2.0))))
                            {
                                info.RowViewportIndex = hi.RowViewportIndex;
                                info.ColumnViewportIndex = hi.ColumnViewportIndex;
                                return info;
                            }
                        }
                        else if (((x >= (((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - num6) - 6.0)) && (x < ((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) + 4.0))) && ((y >= (((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num5) - 6.0)) && (y < ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) + 4.0))))
                        {
                            info.RowViewportIndex = hi.RowViewportIndex;
                            info.ColumnViewportIndex = hi.ColumnViewportIndex;
                            return info;
                        }
                    }
                    else
                    {
                        double num7 = 16.0;
                        double num8 = 0.0;
                        viewportColumnLayoutFromX = GetColumnLayoutModel(hi.ColumnViewportIndex, SheetArea.Cells).Find(info.Column);
                        if (viewportColumnLayoutFromX != null)
                        {
                            num8 += viewportColumnLayoutFromX.Width;
                        }
                        CellRange range4 = Worksheet.GetSpanCell(columnHeaderRowLayoutFromY.Row, info.Column, cells);
                        if ((range4 != null) && (range4.ColumnCount > 1))
                        {
                            for (int i = 1; i < range4.ColumnCount; i++)
                            {
                                viewportColumnLayoutFromX = GetColumnLayoutModel(hi.ColumnViewportIndex, SheetArea.Cells).Find(info.Column + i);
                                if (viewportColumnLayoutFromX != null)
                                {
                                    num8 += viewportColumnLayoutFromX.Width;
                                }
                            }
                        }
                        if (viewportColumnLayoutFromX != null)
                        {
                            if (touching)
                            {
                                if (((x >= ((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - 4.0)) && (x < (((viewportColumnLayoutFromX.X + num8) + num7) + 4.0))) && ((y >= (((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num5) - 6.0)) && (y < ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) + 4.0))))
                                {
                                    info.RowViewportIndex = hi.RowViewportIndex;
                                    info.ColumnViewportIndex = hi.ColumnViewportIndex;
                                    return info;
                                }
                            }
                            else if (((x >= (viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width)) && (x < ((viewportColumnLayoutFromX.X + num8) + num7))) && ((y >= (((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num5) - 2.0)) && (y < ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - 2.0))))
                            {
                                info.RowViewportIndex = hi.RowViewportIndex;
                                info.ColumnViewportIndex = hi.ColumnViewportIndex;
                                return info;
                            }
                        }
                    }
                }
            }
            return null;
        }

        internal FilterButtonInfo GetMouseDownFilterButton()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation != null)
            {
                return GetMouseDownFilterButton(savedHitTestInformation, false);
            }
            return null;
        }

        FilterButtonInfo GetMouseDownFilterButton(HitTestInformation hi, bool touching = false)
        {
            FilterButtonInfo info = null;
            RowLayout columnHeaderRowLayoutFromY = null;
            ColumnLayout viewportColumnLayoutFromX = null;
            SheetArea cells = SheetArea.Cells;
            if (hi.HitTestType == HitTestType.ColumnHeader)
            {
                columnHeaderRowLayoutFromY = GetColumnHeaderRowLayoutFromY(hi.HitPoint.Y);
                viewportColumnLayoutFromX = GetViewportColumnLayoutFromX(hi.ColumnViewportIndex, hi.HitPoint.X);
                cells = SheetArea.ColumnHeader;
            }
            else if (hi.HitTestType == HitTestType.Viewport)
            {
                columnHeaderRowLayoutFromY = GetViewportRowLayoutFromY(hi.RowViewportIndex, hi.HitPoint.Y);
                viewportColumnLayoutFromX = GetViewportColumnLayoutFromX(hi.ColumnViewportIndex, hi.HitPoint.X);
                cells = SheetArea.Cells;
            }
            if ((columnHeaderRowLayoutFromY != null) && (viewportColumnLayoutFromX != null))
            {
                int row = columnHeaderRowLayoutFromY.Row;
                int column = viewportColumnLayoutFromX.Column;
                CellRange range = Worksheet.GetSpanCell(columnHeaderRowLayoutFromY.Row, viewportColumnLayoutFromX.Column, cells);
                if (range != null)
                {
                    if ((columnHeaderRowLayoutFromY.Row != ((range.Row + range.RowCount) - 1)) || (viewportColumnLayoutFromX.Column != ((range.Column + range.ColumnCount) - 1)))
                    {
                        return null;
                    }
                    row = range.Row;
                    column = range.Column;
                }
                info = GetFilterButtonInfo(row, column, cells);
                if (info != null)
                {
                    double x = hi.HitPoint.X;
                    double y = hi.HitPoint.Y;
                    double num5 = Math.Min(16.0, viewportColumnLayoutFromX.Width);
                    double num6 = Math.Min(16.0, columnHeaderRowLayoutFromY.Height);
                    if (touching)
                    {
                        double num7 = ((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - num5) - 6.0;
                        double num8 = ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num6) - 6.0;
                        double num9 = (viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) + 4.0;
                        double num10 = (columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) + 4.0;
                        if (((x >= num7) && (x < num9)) && ((y >= num8) && (y < num10)))
                        {
                            info.RowViewportIndex = hi.RowViewportIndex;
                            info.ColumnViewportIndex = hi.ColumnViewportIndex;
                            return info;
                        }
                    }
                    else if (((x >= (((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - num5) - 2.0)) && (x < ((viewportColumnLayoutFromX.X + viewportColumnLayoutFromX.Width) - 2.0))) && ((y >= (((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - num6) - 2.0)) && (y < ((columnHeaderRowLayoutFromY.Y + columnHeaderRowLayoutFromY.Height) - 2.0))))
                    {
                        info.RowViewportIndex = hi.RowViewportIndex;
                        info.ColumnViewportIndex = hi.ColumnViewportIndex;
                        return info;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the column count when scrolling right one page.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index one page to the right.</param>
        /// <returns>The column count when scrolling right one page.</returns>
        public int GetNextPageColumnCount(int columnViewportIndex)
        {
            return GetNextPageColumnCount(Worksheet, columnViewportIndex);
        }

        int GetNextPageColumnCount(Dt.Cells.Data.Worksheet sheet, int columnViewportIndex)
        {
            if (sheet == null)
            {
                return 0;
            }
            float zoomFactor = ZoomFactor;
            int viewportLeftColumn = sheet.GetViewportLeftColumn(columnViewportIndex);
            double viewportWidth = GetViewportWidth(sheet, columnViewportIndex);
            if ((viewportLeftColumn < ((sheet.ColumnCount - sheet.FrozenTrailingColumnCount) - 1)) && ((sheet.Columns[viewportLeftColumn].ActualWidth * zoomFactor) >= viewportWidth))
            {
                return 1;
            }
            int num4 = 0;
            double num5 = 0.0;
            int column = viewportLeftColumn;
            while (column < (sheet.ColumnCount - sheet.FrozenTrailingColumnCount))
            {
                double num7 = sheet.GetActualColumnWidth(column, SheetArea.Cells) * zoomFactor;
                if ((num5 + num7) > viewportWidth)
                {
                    break;
                }
                num5 += num7;
                num4++;
                column++;
            }
            if (column == (sheet.ColumnCount - sheet.FrozenTrailingColumnCount))
            {
                num4 = 0;
            }
            return num4;
        }

        /// <summary>
        /// Gets the row count when scrolling down one page.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index one page down.</param>
        /// <returns>The row count when scrolling down one page.</returns>
        public int GetNextPageRowCount(int rowViewportIndex)
        {
            return GetNextPageRowCount(Worksheet, rowViewportIndex);
        }

        int GetNextPageRowCount(Dt.Cells.Data.Worksheet sheet, int rowViewportIndex)
        {
            if (sheet == null)
            {
                return 0;
            }
            float zoomFactor = ZoomFactor;
            int viewportTopRow = GetViewportTopRow(sheet, rowViewportIndex);
            double viewportHeight = GetViewportHeight(sheet, rowViewportIndex);
            if ((viewportTopRow < ((sheet.RowCount - sheet.FrozenTrailingRowCount) - 1)) && ((sheet.Rows[viewportTopRow].ActualHeight * zoomFactor) >= viewportHeight))
            {
                return 1;
            }
            int num4 = 0;
            double num5 = 0.0;
            int row = viewportTopRow;
            while (row < (sheet.RowCount - sheet.FrozenTrailingRowCount))
            {
                double num7 = sheet.GetActualRowHeight(row, SheetArea.Cells) * zoomFactor;
                if ((num5 + num7) > viewportHeight)
                {
                    break;
                }
                num5 += num7;
                num4++;
                row++;
            }
            if (row == (sheet.RowCount - sheet.FrozenTrailingRowCount))
            {
                num4 = 0;
            }
            return num4;
        }

        internal int GetNextScrollableColumn(int startColumn)
        {
            int num = startColumn + 1;
            int num2 = Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount;
            while (num < num2)
            {
                if (Worksheet.Columns[num].ActualVisible)
                {
                    return num;
                }
                num++;
            }
            return -1;
        }

        internal int GetNextScrollableRow(int startRow)
        {
            int num = startRow + 1;
            int num2 = Worksheet.RowCount - Worksheet.FrozenTrailingRowCount;
            while (num < num2)
            {
                if (Worksheet.Rows[num].ActualVisible)
                {
                    return num;
                }
                num++;
            }
            return -1;
        }

        static CellRange GetPastedRange(CellRange toRange, string clipboadText)
        {
            CellRange range = null;
            string[,] strArray = Dt.Cells.Data.Worksheet.ParseCsv(clipboadText, "\r\n", "\t", "\"");
            if (strArray != null)
            {
                int row = (toRange.Row < 0) ? 0 : toRange.Row;
                int column = (toRange.Column < 0) ? 0 : toRange.Column;
                int length = strArray.GetLength(0);
                int columnCount = strArray.GetLength(1);
                range = new CellRange(row, column, length, columnCount);
            }
            return range;
        }

        static CellRange GetPastedRange(Dt.Cells.Data.Worksheet fromSheet, CellRange fromRange, Dt.Cells.Data.Worksheet toSheet, CellRange toRange, bool isCutting)
        {
            int row = (fromRange.Row < 0) ? 0 : fromRange.Row;
            int column = (fromRange.Column < 0) ? 0 : fromRange.Column;
            int rowCount = (fromRange.Row < 0) ? fromSheet.RowCount : fromRange.RowCount;
            int columnCount = (fromRange.Column < 0) ? fromSheet.ColumnCount : fromRange.ColumnCount;
            int num5 = (toRange.Row < 0) ? 0 : toRange.Row;
            int num6 = (toRange.Column < 0) ? 0 : toRange.Column;
            int num7 = (toRange.Row < 0) ? toSheet.RowCount : toRange.RowCount;
            int num8 = (toRange.Column < 0) ? toSheet.ColumnCount : toRange.ColumnCount;
            if ((isCutting || ((num7 % rowCount) != 0)) || ((num8 % columnCount) != 0))
            {
                num7 = rowCount;
                num8 = columnCount;
            }
            if (!IsValidRange(row, column, rowCount, columnCount, fromSheet.RowCount, fromSheet.ColumnCount))
            {
                return null;
            }
            if (!IsValidRange(num5, num6, num7, num8, toSheet.RowCount, toSheet.ColumnCount))
            {
                return null;
            }
            CellRange range = new CellRange(num5, num6, num7, num8);
            if (!isCutting && object.ReferenceEquals(fromSheet, toSheet))
            {
                if (range.Contains(row, column, rowCount, columnCount))
                {
                    if ((((row - num5) % rowCount) != 0) || (((column - num6) % columnCount) != 0))
                    {
                        return null;
                    }
                }
                else if (range.Intersects(row, column, rowCount, columnCount) && ((num7 > rowCount) || (num8 > columnCount)))
                {
                    return null;
                }
            }
            if (toRange.Row == -1)
            {
                num5 = -1;
                num7 = -1;
            }
            if (toRange.Column == -1)
            {
                num6 = -1;
                num8 = -1;
            }
            return new CellRange(num5, num6, num7, num8);
        }

        /// <summary>
        /// Gets the column count when scrolling left one page.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index one page to the left.</param>
        /// <returns>The column count when scrolling left one page.</returns>
        public int GetPrePageColumnCount(int columnViewportIndex)
        {
            return GetPrePageColumnCount(Worksheet, columnViewportIndex);
        }

        int GetPrePageColumnCount(Dt.Cells.Data.Worksheet sheet, int columnViewportIndex)
        {
            if (sheet == null)
            {
                return 0;
            }
            float zoomFactor = ZoomFactor;
            int viewportLeftColumn = sheet.GetViewportLeftColumn(columnViewportIndex);
            double viewportWidth = GetViewportWidth(sheet, columnViewportIndex);
            int column = viewportLeftColumn - 1;
            if ((column > sheet.FrozenColumnCount) && ((sheet.Columns[column].ActualWidth * zoomFactor) >= viewportWidth))
            {
                return 1;
            }
            double num5 = 0.0;
            int num6 = 0;
            while ((column >= sheet.FrozenColumnCount) && (num5 < viewportWidth))
            {
                double num7 = sheet.GetActualColumnWidth(column, SheetArea.Cells) * zoomFactor;
                if ((num5 + num7) > viewportWidth)
                {
                    return num6;
                }
                num5 += num7;
                num6++;
                column--;
            }
            return num6;
        }

        /// <summary>
        /// Gets the row count when scrolling up one page.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index one page up.</param>
        /// <returns>The row count when scrolling up one page.</returns>
        public int GetPrePageRowCount(int rowViewportIndex)
        {
            return GetPrePageRowCount(Worksheet, rowViewportIndex);
        }

        int GetPrePageRowCount(Dt.Cells.Data.Worksheet sheet, int rowViewportIndex)
        {
            if (sheet == null)
            {
                return 0;
            }
            float zoomFactor = ZoomFactor;
            int viewportTopRow = GetViewportTopRow(sheet, rowViewportIndex);
            double viewportHeight = GetViewportHeight(sheet, rowViewportIndex);
            int row = viewportTopRow - 1;
            if ((row > sheet.FrozenRowCount) && ((sheet.Rows[row].ActualHeight * zoomFactor) >= viewportHeight))
            {
                return 1;
            }
            double num5 = 0.0;
            int num6 = 0;
            while (row >= sheet.FrozenRowCount)
            {
                double num7 = sheet.GetActualRowHeight(row, SheetArea.Cells) * zoomFactor;
                if ((num5 + num7) > viewportHeight)
                {
                    return num6;
                }
                num5 += num7;
                num6++;
                row--;
            }
            return num6;
        }

        string GetRangeString(CellRange range)
        {
            CalcExpression expression;
            int row = range.Row;
            int column = range.Column;
            int rowCount = range.RowCount;
            int columnCount = range.ColumnCount;
            CalcParser parser = new CalcParser();
            if ((range.RowCount == 1) && (range.ColumnCount == 1))
            {
                expression = new CalcCellExpression(row, column, true, true);
            }
            else
            {
                new CalcCellIdentity(row, column);
                if (((rowCount == -1) && (columnCount == -1)) || ((row == -1) && (column == -1)))
                {
                    expression = new CalcRangeExpression();
                }
                else if ((columnCount == -1) || (column == -1))
                {
                    expression = new CalcRangeExpression(row, (row + rowCount) - 1, true, true, true);
                }
                else if ((rowCount == -1) || (row == -1))
                {
                    expression = new CalcRangeExpression(column, (column + columnCount) - 1, true, true, false);
                }
                else
                {
                    expression = new CalcRangeExpression(row, column, (row + rowCount) - 1, (column + columnCount) - 1, true, true, true, true);
                }
            }
            CalcParserContext context = new CalcParserContext(Worksheet.ReferenceStyle == ReferenceStyle.R1C1, 0, 0, null);
            return parser.Unparse(expression, context);
        }

        internal BitmapImage GetResizerBitmapImage(bool rowHeaderResizer)
        {
            string str = "";
            if (rowHeaderResizer)
            {
                str = "ResizeGripperVer.png";
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    str = "ResizeGripperVer_dark.png";
                }
            }
            else
            {
                str = "ResizeGripperHor.png";
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    str = "ResizeGripperHor_dark.png";
                }
            }
            if (_cachedResizerGipper.ContainsKey(str))
            {
                return _cachedResizerGipper[str];
            }
            string name = IntrospectionExtensions.GetTypeInfo((Type)typeof(SheetView)).Assembly.GetName().Name;
            Uri uri = new Uri(string.Format("ms-appx:///{0}/Icons/{1}", (object[])new object[] { name, str }), (UriKind)UriKind.RelativeOrAbsolute);
            BitmapImage image = new BitmapImage(uri);
            _cachedResizerGipper[str] = image;
            return image;
        }

        internal double GetRowAutoFitValue(int row, bool columnHeader)
        {
            double num = -1.0;
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            int columnCount = worksheet.ColumnCount;
            Cell cell = null;
            FontFamily unknownFontfamily = null;
            unknownFontfamily = InheritedControlFontFamily;
            object textFormattingMode = null;
            for (int i = 0; i < columnCount; i++)
            {
                cell = columnHeader ? worksheet.ColumnHeader.Cells[row, i] : worksheet.Cells[row, i];
                if (!string.IsNullOrEmpty(cell.Text))
                {
                    CellRange range = worksheet.GetSpanCell(row, i, cell.SheetArea);
                    if ((range == null) || ((range.Row >= row) && (range.RowCount <= 1)))
                    {
                        double width = 0.0;
                        if (range == null)
                        {
                            width = worksheet.GetColumnWidth(i, cell.SheetArea);
                        }
                        else
                        {
                            for (int j = 0; j < range.ColumnCount; j++)
                            {
                                width += worksheet.GetColumnWidth(i + j, cell.SheetArea);
                            }
                        }
                        Size maxSize = MeasureHelper.ConvertExcelCellSizeToTextSize(new Size(width, double.PositiveInfinity), 1.0);
                        Size size3 = MeasureHelper.ConvertTextSizeToExcelCellSize(MeasureHelper.MeasureTextInCell(cell, maxSize, 1.0, unknownFontfamily, textFormattingMode, base.UseLayoutRounding), 1.0);
                        num = Math.Max(num, size3.Height);
                        if (range != null)
                        {
                            i += range.ColumnCount - 1;
                        }
                    }
                }
            }
            return num;
        }

        internal CellLayoutModel GetRowHeaderCellLayoutModel(int rowViewportIndex)
        {
            int rowViewportCount = GetViewportInfo().RowViewportCount;
            if (_cachedRowHeaderCellLayoutModel == null)
            {
                _cachedRowHeaderCellLayoutModel = new CellLayoutModel[rowViewportCount + 2];
            }
            if (_cachedRowHeaderCellLayoutModel[rowViewportIndex + 1] == null)
            {
                _cachedRowHeaderCellLayoutModel[rowViewportIndex + 1] = CreateRowHeaderCellLayoutModel(rowViewportIndex);
            }
            return _cachedRowHeaderCellLayoutModel[rowViewportIndex + 1];
        }

        ColumnLayout GetRowHeaderColumnLayout(int column)
        {
            return GetRowHeaderColumnLayoutModel().FindColumn(column);
        }

        ColumnLayout GetRowHeaderColumnLayoutFromX(double x)
        {
            return GetRowHeaderColumnLayoutModel().FindX(x);
        }

        internal ColumnLayoutModel GetRowHeaderColumnLayoutModel()
        {
            if (_cachedRowHeaderColumnLayoutModel == null)
            {
                _cachedRowHeaderColumnLayoutModel = CreateRowHeaderColumnLayoutModel();
            }
            return _cachedRowHeaderColumnLayoutModel;
        }

        Rect GetRowHeaderRectangle(int rowViewportIndex)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            double headerX = sheetLayout.HeaderX;
            double viewportY = sheetLayout.GetViewportY(rowViewportIndex);
            double width = sheetLayout.HeaderWidth - 1.0;
            double height = sheetLayout.GetViewportHeight(rowViewportIndex) - 1.0;
            if ((width >= 0.0) && (height >= 0.0))
            {
                return new Rect(headerX, viewportY, width, height);
            }
            return Rect.Empty;
        }

        ColumnLayout GetRowHeaderResizingColumnLayoutFromX(double x)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            ColumnLayoutModel rowHeaderColumnLayoutModel = GetRowHeaderColumnLayoutModel();
            if (worksheet.RowCount > 0)
            {
                for (int i = rowHeaderColumnLayoutModel.Count - 1; i >= 0; i--)
                {
                    ColumnLayout layout = Enumerable.ElementAt<ColumnLayout>((IEnumerable<ColumnLayout>)rowHeaderColumnLayoutModel, i);
                    if (((x >= Math.Max(layout.X, (layout.X + layout.Width) - 4.0)) && (x < ((layout.X + layout.Width) + 4.0))) && worksheet.RowHeader.Columns[layout.Column].CanUserResize)
                    {
                        return layout;
                    }
                }
            }
            return null;
        }

        ColumnLayout GetRowHeaderResizingColumnLayoutFromXForTouch(double x)
        {
            ColumnLayout rowHeaderResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(x);
            if (rowHeaderResizingColumnLayoutFromX == null)
            {
                for (int i = -5; i < 5; i++)
                {
                    rowHeaderResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(x + i);
                    if (rowHeaderResizingColumnLayoutFromX != null)
                    {
                        return rowHeaderResizingColumnLayoutFromX;
                    }
                }
            }
            return rowHeaderResizingColumnLayoutFromX;
        }

        internal GcViewport GetRowHeaderRowsPresenter(int rowViewportIndex)
        {
            if (_rowHeaderPresenters == null)
            {
                return null;
            }
            return _rowHeaderPresenters[rowViewportIndex + 1];
        }

        internal RowLayoutModel GetRowHeaderViewportRowLayoutModel(int rowViewportIndex)
        {
            if (_cachedRowHeaderViewportRowLayoutModel == null)
            {
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                _cachedRowHeaderViewportRowLayoutModel = new RowLayoutModel[rowViewportCount + 2];
            }
            if (_cachedRowHeaderViewportRowLayoutModel[rowViewportIndex + 1] == null)
            {
                if (ResizeZeroIndicator == Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
                {
                    _cachedRowHeaderViewportRowLayoutModel[rowViewportIndex + 1] = CreateEnhancedResizeToZeroRowHeaderViewportRowLayoutModel(rowViewportIndex);
                }
                else
                {
                    _cachedRowHeaderViewportRowLayoutModel[rowViewportIndex + 1] = CreateViewportRowLayoutModel(rowViewportIndex);
                }
            }
            return _cachedRowHeaderViewportRowLayoutModel[rowViewportIndex + 1];
        }

        internal RowLayoutModel GetRowLayoutModel(int rowViewportIndex, SheetArea sheetArea)
        {
            switch (sheetArea)
            {
                case SheetArea.Cells:
                    return GetViewportRowLayoutModel(rowViewportIndex);

                case (SheetArea.CornerHeader | SheetArea.RowHeader):
                    if (ResizeZeroIndicator != Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
                    {
                        return GetViewportRowLayoutModel(rowViewportIndex);
                    }
                    return GetRowHeaderViewportRowLayoutModel(rowViewportIndex);

                case SheetArea.ColumnHeader:
                    return GetColumnHeaderRowLayoutModel();
            }
            return null;
        }

        internal static object[,] GetsArrayFormulas(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount)
        {
            object[,] objArray = sheet.FindFormulas(row, column, rowCount, columnCount);
            if ((objArray != null) && (objArray.Length > 0))
            {
                List<string> list = new List<string>();
                List<CellRange> list2 = new List<CellRange>();
                int length = objArray.GetLength(0);
                for (int i = 0; i < length; i++)
                {
                    string str = (string)(objArray[i, 1] as string);
                    if ((!string.IsNullOrEmpty(str) && str.StartsWith("{")) && str.EndsWith("}"))
                    {
                        list2.Add((CellRange)objArray[i, 0]);
                        list.Add(str);
                    }
                }
                if (list.Count > 0)
                {
                    object[,] objArray2 = new object[list.Count, 2];
                    for (int j = 0; j < list.Count; j++)
                    {
                        objArray2[j, 0] = list2[j];
                        objArray2[j, 1] = list[j];
                    }
                    return objArray2;
                }
            }
            return null;
        }

        internal virtual SheetLayout GetSheetLayout()
        {
            SheetLayout layout = _cachedLayout;
            if (layout == null)
            {
                _cachedLayout = layout = CreateSheetLayout();
            }
            return layout;
        }

        /// <summary>
        /// Ges the spread chart view.
        /// </summary>
        /// <param name="chartName">Name of the chart.</param>
        /// <returns></returns>
        public SpreadChartView GetSpreadChartView(string chartName)
        {
            int activeRowViewportIndex = GetActiveRowViewportIndex();
            int activeColumnViewportIndex = GetActiveColumnViewportIndex();
            GcViewport viewport = _viewportPresenters[activeRowViewportIndex + 1, activeColumnViewportIndex + 1];
            if (viewport != null)
            {
                return viewport.GetSpreadChartView(chartName);
            }
            return null;
        }

        ColumnLayout GetValidHorDragToColumnLayout()
        {
            if (IsIncreaseFill)
            {
                if (IsDragToColumnInView)
                {
                    return GetCurrentDragToColumnLayout();
                }
                return DragFillToViewportRightColumnLayout;
            }
            if (IsDragFillStartRightColumnInView)
            {
                return DragFillStartRightColumnLayout;
            }
            return DragFillStartViewportRightColumnLayout;
        }

        RowLayout GetValidVerDragToRowLayout()
        {
            if (IsIncreaseFill)
            {
                if (IsDragToRowInView)
                {
                    return GetCurrentDragToRowLayout();
                }
                return DragFillToViewportBottomRowLayout;
            }
            if (IsDragFillStartBottomRowInView)
            {
                return DragFillStartBottomRowLayout;
            }
            return DragFillStartViewportBottomRowLayout;
        }

        string GetVericalScrollTip(int row)
        {
            return string.Format(ResourceStrings.VerticalScroll, (object[])new object[] { ((int)row) });
        }

        string GetVerticalResizeTip(double size)
        {
            object[] args = new object[1];
            double num = size / ((double)ZoomFactor);
            args[0] = ((double)num).ToString("0");
            return string.Format(ResourceStrings.RowResize, args);
        }

        /// <summary>
        /// Gets the row viewport's bottom row index.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index.</param>
        /// <returns>The bottom row index in the row viewport.</returns>
        public int GetViewportBottomRow(int rowViewportIndex)
        {
            return GetViewportBottomRow(Worksheet, rowViewportIndex);
        }

        int GetViewportBottomRow(Dt.Cells.Data.Worksheet sheet, int rowViewportIndex)
        {
            if (rowViewportIndex == GetViewportInfo(sheet).RowViewportCount)
            {
                return (sheet.RowCount - 1);
            }
            int viewportTopRow = GetViewportTopRow(rowViewportIndex);
            double viewportHeight = GetViewportHeight(sheet, rowViewportIndex);
            double num3 = 0.0;
            int num4 = 0;
            float zoomFactor = ZoomFactor;
            int row = viewportTopRow;
            while ((row < (sheet.RowCount - sheet.FrozenTrailingRowCount)) && (num3 < viewportHeight))
            {
                num3 += Math.Ceiling((double)(sheet.GetActualRowHeight(row, SheetArea.Cells) * zoomFactor));
                row++;
                num4++;
            }
            return ((viewportTopRow + num4) - 1);
        }

        CellPresenterBase GetViewportCell(int rowViewportIndex, int columnViewportIndex, int rowIndex, int columnIndex)
        {
            CellPresenterBase cell = null;
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(rowViewportIndex, columnViewportIndex);
            if (viewportRowsPresenter != null)
            {
                RowPresenter row = viewportRowsPresenter.GetRow(rowIndex);
                if (row != null)
                {
                    cell = row.GetCell(columnIndex);
                }
            }
            if (((cell == null) && (viewportRowsPresenter.CurrentRow != null)) && (rowIndex == Worksheet.ActiveRowIndex))
            {
                cell = viewportRowsPresenter.CurrentRow.GetCell(columnIndex);
            }
            return cell;
        }

        internal CellLayoutModel GetViewportCellLayoutModel(int rowViewportIndex, int columnViewportIndex)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            int rowViewportCount = viewportInfo.RowViewportCount;
            if (_cachedViewportCellLayoutModel == null)
            {
                _cachedViewportCellLayoutModel = new CellLayoutModel[rowViewportCount + 2, columnViewportCount + 2];
            }
            if (_cachedViewportCellLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1] == null)
            {
                _cachedViewportCellLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1] = CreateViewportCellLayoutModel(rowViewportIndex, columnViewportIndex);
            }
            return _cachedViewportCellLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1];
        }

        ColumnLayout GetViewportColumnLayoutFromX(int columnViewportIndex, double x)
        {
            if (ResizeZeroIndicator != Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
            {
                return GetViewportColumnLayoutModel(columnViewportIndex).FindX(x);
            }
            ColumnLayoutModel columnHeaderViewportColumnLayoutModel = GetColumnHeaderViewportColumnLayoutModel(columnViewportIndex);
            ColumnLayout layout = columnHeaderViewportColumnLayoutModel.FindX(x);
            if ((InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch) && (layout != null))
            {
                if (Worksheet.GetActualColumnWidth(layout.Column, SheetArea.Cells).IsZero())
                {
                    return layout;
                }
                if ((layout.Column <= 0) || !Worksheet.GetActualColumnWidth(layout.Column - 1, SheetArea.Cells).IsZero())
                {
                    return layout;
                }
                ColumnLayout layout2 = columnHeaderViewportColumnLayoutModel.FindColumn(layout.Column - 1);
                if ((layout2 != null) && (((layout2.X + layout2.Width) + 3.0) >= x))
                {
                    return layout2;
                }
            }
            return layout;
        }

        internal ColumnLayoutModel GetViewportColumnLayoutModel(int columnViewportIndex)
        {
            if (_cachedViewportColumnLayoutModel == null)
            {
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                _cachedViewportColumnLayoutModel = new ColumnLayoutModel[columnViewportCount + 2];
            }
            if (_cachedViewportColumnLayoutModel[columnViewportIndex + 1] == null)
            {
                _cachedViewportColumnLayoutModel[columnViewportIndex + 1] = CreateViewportColumnLayoutModel(columnViewportIndex);
            }
            return _cachedViewportColumnLayoutModel[columnViewportIndex + 1];
        }

        internal ColumnLayout GetViewportColumnLayoutNearX(int columnViewportIndex, double x)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            ColumnLayout layout2 = null;
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            if ((columnViewportIndex == -1) && (x > (sheetLayout.GetViewportX(-1) + sheetLayout.GetViewportWidth(-1))))
            {
                layout2 = GetViewportColumnLayoutModel(0).FindNearX(x);
            }
            else if (((columnViewportIndex == 0) && (x < sheetLayout.GetViewportX(0))) && (GetViewportLeftColumn(0) == Worksheet.FrozenColumnCount))
            {
                layout2 = GetViewportColumnLayoutModel(-1).FindNearX(x);
            }
            else if (((columnViewportIndex == (columnViewportCount - 1)) && (x > sheetLayout.GetViewportX(columnViewportCount))) && (GetViewportRightColumn(columnViewportCount - 1) == ((Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1)))
            {
                layout2 = GetViewportColumnLayoutModel(columnViewportCount).FindNearX(x);
            }
            else if ((columnViewportIndex == columnViewportCount) && (x < sheetLayout.GetViewportX(columnViewportCount)))
            {
                layout2 = GetViewportColumnLayoutModel(columnViewportCount - 1).FindNearX(x);
            }
            if (layout2 == null)
            {
                layout2 = GetViewportColumnLayoutModel(columnViewportIndex).FindNearX(x);
            }
            return layout2;
        }

        internal FloatingObjectLayoutModel GetViewportFloatingObjectLayoutModel(int rowViewportIndex, int columnViewportIndex)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            int rowViewportCount = viewportInfo.RowViewportCount;
            if (_cachedFloatingObjectLayoutModel == null)
            {
                _cachedFloatingObjectLayoutModel = new FloatingObjectLayoutModel[rowViewportCount + 2, columnViewportCount + 2];
            }
            if (_cachedFloatingObjectLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1] == null)
            {
                _cachedFloatingObjectLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1] = CreateViewportChartShapeLayoutMode(rowViewportIndex, columnViewportIndex);
            }
            return _cachedFloatingObjectLayoutModel[rowViewportIndex + 1, columnViewportIndex + 1];
        }

        internal double GetViewportHeight(int rowViewportIndex)
        {
            return GetViewportHeight(Worksheet, rowViewportIndex);
        }

        double GetViewportHeight(Dt.Cells.Data.Worksheet sheet, int rowViewportIndex)
        {
            return GetSheetLayout().GetViewportHeight(rowViewportIndex);
        }

        internal ViewportInfo GetViewportInfo()
        {
            return GetViewportInfo(Worksheet);
        }

        internal virtual ViewportInfo GetViewportInfo(Dt.Cells.Data.Worksheet sheet)
        {
            if (sheet == null)
            {
                return new ViewportInfo();
            }
            return sheet.GetViewportInfo();
        }

        /// <summary>
        /// Gets the column viewport's left column index.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index.</param>
        /// <returns>The left column index in the column viewport.</returns>
        public int GetViewportLeftColumn(int columnViewportIndex)
        {
            return Worksheet.GetViewportLeftColumn(columnViewportIndex);
        }

        Rect GetViewportRectangle(int rowViewportIndex, int columnViewportIndex)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            double viewportX = sheetLayout.GetViewportX(columnViewportIndex);
            double viewportY = sheetLayout.GetViewportY(rowViewportIndex);
            double width = sheetLayout.GetViewportWidth(columnViewportIndex) - 1.0;
            double height = sheetLayout.GetViewportHeight(rowViewportIndex) - 1.0;
            if ((width >= 0.0) && (height >= 0.0))
            {
                return new Rect(viewportX, viewportY, width, height);
            }
            return Rect.Empty;
        }

        ColumnLayout GetViewportResizingColumnLayoutFromX(int columnViewportIndex, double x)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(columnViewportIndex);
            for (int i = viewportColumnLayoutModel.Count - 1; i >= 0; i--)
            {
                ColumnLayout layout = Enumerable.ElementAt<ColumnLayout>((IEnumerable<ColumnLayout>)viewportColumnLayoutModel, i);
                if (((layout != null) && (x >= Math.Max(layout.X, (layout.X + layout.Width) - 4.0))) && ((x < ((layout.X + layout.Width) + 4.0)) && worksheet.Columns[layout.Column].CanUserResize))
                {
                    return layout;
                }
            }
            if (((columnViewportIndex >= 0) && (columnViewportIndex < GetViewportInfo().ColumnViewportCount)) && (viewportColumnLayoutModel.Count > 0))
            {
                ColumnLayout layout2 = viewportColumnLayoutModel[0];
                if (((x >= Math.Max((double)0.0, (double)(layout2.X - 4.0))) && (x < (layout2.X + 4.0))) && ((columnViewportIndex - 1) >= -1))
                {
                    ColumnLayoutModel model2 = GetViewportColumnLayoutModel(Math.Max(-1, columnViewportIndex - 1));
                    for (int j = layout2.Column - 1; j >= worksheet.FrozenColumnCount; j--)
                    {
                        if (model2.Find(j) != null)
                        {
                            break;
                        }
                        if ((worksheet.GetActualColumnWidth(j, SheetArea.Cells) == 0.0) && worksheet.Columns[j].CanUserResize)
                        {
                            return new ColumnLayout(j, layout2.X, 0.0);
                        }
                    }
                }
            }
            return null;
        }

        ColumnLayout GetViewportResizingColumnLayoutFromXForTouch(int columnViewportIndex, double x)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(columnViewportIndex);
            for (int i = viewportColumnLayoutModel.Count - 1; i >= 0; i--)
            {
                ColumnLayout layout = Enumerable.ElementAt<ColumnLayout>((IEnumerable<ColumnLayout>)viewportColumnLayoutModel, i);
                if (((layout != null) && (x >= Math.Max(layout.X, (layout.X + layout.Width) - 8.0))) && ((x < ((layout.X + layout.Width) + 8.0)) && worksheet.Columns[layout.Column].CanUserResize))
                {
                    return layout;
                }
            }
            if (((columnViewportIndex >= 0) && (columnViewportIndex < GetViewportInfo().ColumnViewportCount)) && (viewportColumnLayoutModel.Count > 0))
            {
                ColumnLayout layout2 = viewportColumnLayoutModel[0];
                if (((x >= Math.Max((double)0.0, (double)(layout2.X - 8.0))) && (x < (layout2.X + 8.0))) && ((columnViewportIndex - 1) >= -1))
                {
                    ColumnLayoutModel model2 = GetViewportColumnLayoutModel(Math.Max(-1, columnViewportIndex - 1));
                    for (int j = layout2.Column - 1; j >= worksheet.FrozenColumnCount; j--)
                    {
                        if (model2.Find(j) != null)
                        {
                            break;
                        }
                        if ((worksheet.GetActualColumnWidth(j, SheetArea.Cells) == 0.0) && worksheet.Columns[j].CanUserResize)
                        {
                            return new ColumnLayout(j, layout2.X, 0.0);
                        }
                    }
                }
            }
            return null;
        }

        RowLayout GetViewportResizingRowLayoutFromY(int rowViewportIndex, double y)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(rowViewportIndex);
            for (int i = viewportRowLayoutModel.Count - 1; i >= 0; i--)
            {
                RowLayout layout = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, i);
                if (((layout != null) && (y >= Math.Max(layout.Y, (layout.Y + layout.Height) - 4.0))) && ((y < ((layout.Y + layout.Height) + 4.0)) && worksheet.Rows[layout.Row].CanUserResize))
                {
                    return layout;
                }
            }
            if (((rowViewportIndex >= 0) && (rowViewportIndex < GetViewportInfo().RowViewportCount)) && (viewportRowLayoutModel.Count > 0))
            {
                RowLayout layout2 = viewportRowLayoutModel[0];
                if (((y >= Math.Max((double)0.0, (double)(layout2.Y - 4.0))) && (y < (layout2.Y + 4.0))) && ((rowViewportIndex - 1) >= -1))
                {
                    RowLayoutModel model2 = GetViewportRowLayoutModel(Math.Max(-1, rowViewportIndex - 1));
                    for (int j = layout2.Row - 1; j >= worksheet.FrozenRowCount; j--)
                    {
                        if (model2.Find(j) != null)
                        {
                            break;
                        }
                        if ((worksheet.GetActualRowHeight(j, SheetArea.Cells) == 0.0) && worksheet.Rows[j].CanUserResize)
                        {
                            return new RowLayout(j, layout2.Y, 0.0);
                        }
                    }
                }
            }
            return null;
        }

        RowLayout GetViewportResizingRowLayoutFromYForTouch(int rowViewportIndex, double y)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(rowViewportIndex);
            for (int i = viewportRowLayoutModel.Count - 1; i >= 0; i--)
            {
                RowLayout layout = Enumerable.ElementAt<RowLayout>((IEnumerable<RowLayout>)viewportRowLayoutModel, i);
                if (((layout != null) && (y >= Math.Max(layout.Y, (layout.Y + layout.Height) - 8.0))) && ((y < ((layout.Y + layout.Height) + 8.0)) && worksheet.Rows[layout.Row].CanUserResize))
                {
                    return layout;
                }
            }
            if (((rowViewportIndex >= 0) && (rowViewportIndex < GetViewportInfo().RowViewportCount)) && (viewportRowLayoutModel.Count > 0))
            {
                RowLayout layout2 = viewportRowLayoutModel[0];
                if (((y >= Math.Max((double)0.0, (double)(layout2.Y - 8.0))) && (y < (layout2.Y + 8.0))) && ((rowViewportIndex - 1) >= -1))
                {
                    RowLayoutModel model2 = GetViewportRowLayoutModel(Math.Max(-1, rowViewportIndex - 1));
                    for (int j = layout2.Row - 1; j >= worksheet.FrozenRowCount; j--)
                    {
                        if (model2.Find(j) != null)
                        {
                            break;
                        }
                        if ((worksheet.GetActualRowHeight(j, SheetArea.Cells) == 0.0) && worksheet.Rows[j].CanUserResize)
                        {
                            return new RowLayout(j, layout2.Y, 0.0);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the column viewport's right column index.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index.</param>
        /// <returns>The right column index in the column viewport.</returns>
        public int GetViewportRightColumn(int columnViewportIndex)
        {
            return GetViewportRightColumn(Worksheet, columnViewportIndex);
        }

        int GetViewportRightColumn(Dt.Cells.Data.Worksheet sheet, int columnViewportIndex)
        {
            if (columnViewportIndex == GetViewportInfo(sheet).ColumnViewportCount)
            {
                return (sheet.ColumnCount - 1);
            }
            int viewportLeftColumn = sheet.GetViewportLeftColumn(columnViewportIndex);
            double viewportWidth = GetViewportWidth(sheet, columnViewportIndex);
            int num3 = 0;
            double num4 = 0.0;
            float zoomFactor = ZoomFactor;
            int column = viewportLeftColumn;
            while ((column < (sheet.ColumnCount - sheet.FrozenTrailingColumnCount)) && (num4 < viewportWidth))
            {
                num4 += Math.Ceiling((double)(sheet.GetActualColumnWidth(column, SheetArea.Cells) * zoomFactor));
                column++;
                num3++;
            }
            return ((viewportLeftColumn + num3) - 1);
        }

        RowLayout GetViewportRowLayoutFromY(int rowViewportIndex, double y)
        {
            if (ResizeZeroIndicator != Dt.Cells.UI.ResizeZeroIndicator.Enhanced)
            {
                return GetViewportRowLayoutModel(rowViewportIndex).FindY(y);
            }
            RowLayoutModel rowHeaderViewportRowLayoutModel = GetRowHeaderViewportRowLayoutModel(rowViewportIndex);
            RowLayout layout = rowHeaderViewportRowLayoutModel.FindY(y);
            if ((InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch) && (layout != null))
            {
                if (Worksheet.GetActualRowHeight(layout.Row, SheetArea.Cells).IsZero())
                {
                    return layout;
                }
                if ((layout.Row <= 0) || !Worksheet.GetActualRowHeight(layout.Row - 1, SheetArea.Cells).IsZero())
                {
                    return layout;
                }
                RowLayout layout2 = rowHeaderViewportRowLayoutModel.FindRow(layout.Row - 1);
                if ((layout2 != null) && (((layout2.Y + layout2.Height) + 3.0) >= y))
                {
                    return layout2;
                }
            }
            return layout;
        }

        internal RowLayoutModel GetViewportRowLayoutModel(int rowViewportIndex)
        {
            if (_cachedViewportRowLayoutModel == null)
            {
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                _cachedViewportRowLayoutModel = new RowLayoutModel[rowViewportCount + 2];
            }
            if (_cachedViewportRowLayoutModel[rowViewportIndex + 1] == null)
            {
                _cachedViewportRowLayoutModel[rowViewportIndex + 1] = CreateViewportRowLayoutModel(rowViewportIndex);
            }
            return _cachedViewportRowLayoutModel[rowViewportIndex + 1];
        }

        internal RowLayout GetViewportRowLayoutNearY(int rowViewportIndex, double y)
        {
            SheetLayout sheetLayout = GetSheetLayout();
            RowLayout layout2 = null;
            int rowViewportCount = GetViewportInfo().RowViewportCount;
            if ((rowViewportIndex == -1) && (sheetLayout.GetViewportY(0) < y))
            {
                layout2 = GetViewportRowLayoutModel(0).FindNearY(y);
            }
            else if (((rowViewportIndex == 0) && (y < sheetLayout.GetViewportY(0))) && (GetViewportTopRow(0) == Worksheet.FrozenRowCount))
            {
                layout2 = GetViewportRowLayoutModel(-1).FindNearY(y);
            }
            else if (((rowViewportIndex == (rowViewportCount - 1)) && (y > sheetLayout.GetViewportY(rowViewportCount))) && (GetViewportBottomRow(rowViewportCount - 1) == ((Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1)))
            {
                layout2 = GetViewportRowLayoutModel(rowViewportCount).FindNearY(y);
            }
            else if ((rowViewportIndex == rowViewportCount) && (y < sheetLayout.GetViewportY(rowViewportCount)))
            {
                layout2 = GetViewportRowLayoutModel(rowViewportCount - 1).FindNearY(y);
            }
            if (layout2 == null)
            {
                layout2 = GetViewportRowLayoutModel(rowViewportIndex).FindNearY(y);
            }
            return layout2;
        }

        internal GcViewport GetViewportRowsPresenter(int rowViewportIndex, int columnViewportIndex)
        {
            if (_viewportPresenters != null)
            {
                int length = _viewportPresenters.GetLength(0);
                if ((rowViewportIndex >= -1) && (rowViewportIndex < (length - 1)))
                {
                    int num2 = _viewportPresenters.GetLength(1);
                    if ((columnViewportIndex >= -1) && (columnViewportIndex < (num2 - 1)))
                    {
                        return _viewportPresenters[rowViewportIndex + 1, columnViewportIndex + 1];
                    }
                }
            }
            return null;
        }

        Point GetViewportTopLeftCoordinates(int rowViewportIndex, int columnViewportIndex)
        {
            int viewportTopRow = GetViewportTopRow(rowViewportIndex);
            double y = 0.0;
            for (int i = 0; i < viewportTopRow; i++)
            {
                double num4 = Math.Ceiling((double)(Worksheet.GetActualRowHeight(i, SheetArea.Cells) * ZoomFactor));
                y += num4;
            }
            int viewportLeftColumn = GetViewportLeftColumn(columnViewportIndex);
            double x = 0.0;
            for (int j = 0; j < viewportLeftColumn; j++)
            {
                double num8 = Math.Ceiling((double)(Worksheet.GetActualColumnWidth(j, SheetArea.Cells) * ZoomFactor));
                x += num8;
            }
            return new Point(x, y);
        }

        /// <summary>
        /// Gets the row viewport's top row index.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index.</param>
        /// <returns>The top row index in the row viewport.</returns>
        public int GetViewportTopRow(int rowViewportIndex)
        {
            return GetViewportTopRow(Worksheet, rowViewportIndex);
        }

        int GetViewportTopRow(Dt.Cells.Data.Worksheet sheet, int rowViewportIndex)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            if ((viewportInfo.RowViewportCount > 0) && (viewportInfo.ColumnViewportCount > 0))
            {
                if (rowViewportIndex == -1)
                {
                    return 0;
                }
                if ((rowViewportIndex >= 0) && (rowViewportIndex < viewportInfo.RowViewportCount))
                {
                    return viewportInfo.TopRows[rowViewportIndex];
                }
                if (rowViewportIndex == viewportInfo.RowViewportCount)
                {
                    return Math.Max(sheet.FrozenRowCount, sheet.RowCount - sheet.FrozenTrailingRowCount);
                }
            }
            return -1;
        }

        internal double GetViewportWidth(int columnViewportIndex)
        {
            return GetViewportWidth(Worksheet, columnViewportIndex);
        }

        double GetViewportWidth(Dt.Cells.Data.Worksheet sheet, int columnViewportIndex)
        {
            return GetSheetLayout().GetViewportWidth(columnViewportIndex);
        }

        internal int GetVisibleColumnCount()
        {
            return GetVisibleColumnCount(Worksheet);
        }

        int GetVisibleColumnCount(Dt.Cells.Data.Worksheet worksheet)
        {
            if (worksheet == null)
            {
                return -1;
            }
            int num = 0;
            for (int i = 0; i < worksheet.ColumnCount; i++)
            {
                if (worksheet.GetActualColumnVisible(i, SheetArea.Cells))
                {
                    num++;
                }
            }
            return num;
        }

        internal int GetVisibleRowCount()
        {
            return GetVisibleRowCount(Worksheet);
        }

        int GetVisibleRowCount(Dt.Cells.Data.Worksheet worksheet)
        {
            if (worksheet == null)
            {
                return -1;
            }
            int num = 0;
            for (int i = 0; i < worksheet.RowCount; i++)
            {
                if (worksheet.GetActualRowVisible(i, SheetArea.Cells))
                {
                    num++;
                }
            }
            return num;
        }

        internal void HandleCellChanged(object sender, CellChangedEventArgs e)
        {
            if (sender == Worksheet)
            {
                switch (e.SheetArea)
                {
                    case SheetArea.CornerHeader:
                    case (SheetArea.Cells | SheetArea.RowHeader):
                        return;

                    case SheetArea.Cells:
                        if (e.PropertyName != "Formula")
                        {
                            if (e.PropertyName == "Axis")
                            {
                                InvalidateLayout();
                            }
                            InvalidateRange(e.Row, e.Column, e.RowCount, e.ColumnCount, e.SheetArea);
                            return;
                        }
                        InvalidateRange(-1, -1, -1, -1, SheetArea.Cells);
                        return;

                    case (SheetArea.CornerHeader | SheetArea.RowHeader):
                    case SheetArea.ColumnHeader:
                        if (e.PropertyName == "Axis")
                        {
                            InvalidateLayout();
                        }
                        InvalidateRange(e.Row, e.Column, e.RowCount, e.ColumnCount, e.SheetArea);
                        return;
                }
            }
        }

        internal void HandleChartChanged(object sender, ChartChangedBaseEventArgs e, bool autoRefresh)
        {
            if (_viewportPresenters != null)
            {
                if (e.Property == "IsSelected")
                {
                    UpdateSelectState(e);
                }
                else if (autoRefresh)
                {
                    if (e.Chart == null)
                    {
                        InvalidateFloatingObjectLayout();
                    }
                    else if (((e.ChartArea == ChartArea.AxisX) || (e.ChartArea == ChartArea.AxisY)) || (e.ChartArea == ChartArea.AxisZ))
                    {
                        GcViewport[,] viewportArray = _viewportPresenters;
                        int upperBound = viewportArray.GetUpperBound(0);
                        int num2 = viewportArray.GetUpperBound(1);
                        for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                        {
                            for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                            {
                                GcViewport viewport = viewportArray[i, j];
                                if (viewport != null)
                                {
                                    if (e.Chart == null)
                                    {
                                        viewport.RefreshFloatingObjects();
                                    }
                                    else
                                    {
                                        viewport.RefreshFloatingObject(e);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        bool displayHidden = false;
                        if (e.Chart is SpreadChart)
                        {
                            displayHidden = (e.Chart as SpreadChart).DisplayHidden;
                        }
                        if (((e.Property == "Location") || (e.Property == "Size")) || (((e.Property == "SheetRowChanged") || (e.Property == "SheetColumnChanged")) || (e.Property == "Name")))
                        {
                            GcViewport[,] viewportArray2 = _viewportPresenters;
                            int num5 = viewportArray2.GetUpperBound(0);
                            int num6 = viewportArray2.GetUpperBound(1);
                            for (int k = viewportArray2.GetLowerBound(0); k <= num5; k++)
                            {
                                for (int m = viewportArray2.GetLowerBound(1); m <= num6; m++)
                                {
                                    if (viewportArray2[k, m] != null)
                                    {
                                        InvalidateFloatingObjectLayout();
                                    }
                                }
                            }
                        }
                        else if ((((e.Property == "RowFilter") || (e.Property == "RowRangeGroup")) || ((e.Property == "ColumnRangeGroup") || (e.Property == "TableFilter"))) || (((e.Property == "AxisX") || (e.Property == "AxisY")) || (e.Property == "AxisZ")))
                        {
                            GcViewport[,] viewportArray3 = _viewportPresenters;
                            int num9 = viewportArray3.GetUpperBound(0);
                            int num10 = viewportArray3.GetUpperBound(1);
                            for (int n = viewportArray3.GetLowerBound(0); n <= num9; n++)
                            {
                                for (int num12 = viewportArray3.GetLowerBound(1); num12 <= num10; num12++)
                                {
                                    GcViewport viewport3 = viewportArray3[n, num12];
                                    if (viewport3 != null)
                                    {
                                        viewport3.InvalidateFloatingObjectMeasureState(e.Chart);
                                        if (e.Chart == null)
                                        {
                                            viewport3.InvalidateFloatingObjectsMeasureState();
                                            foreach (SpreadChart chart in Worksheet.Charts)
                                            {
                                                if (!displayHidden)
                                                {
                                                    viewport3.RefreshFloatingObject(e);
                                                }
                                            }
                                        }
                                        if ((e.Chart != null) && !displayHidden)
                                        {
                                            viewport3.RefreshFloatingObject(e);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            GcViewport[,] viewportArray4 = _viewportPresenters;
                            int num13 = viewportArray4.GetUpperBound(0);
                            int num14 = viewportArray4.GetUpperBound(1);
                            for (int num15 = viewportArray4.GetLowerBound(0); num15 <= num13; num15++)
                            {
                                for (int num16 = viewportArray4.GetLowerBound(1); num16 <= num14; num16++)
                                {
                                    GcViewport viewport4 = viewportArray4[num15, num16];
                                    if (viewport4 != null)
                                    {
                                        viewport4.InvalidateFloatingObjectsMeasureState();
                                        if (e.Chart == null)
                                        {
                                            foreach (SpreadChart chart in Worksheet.Charts)
                                            {
                                                viewport4.RefreshFloatingObject(e);
                                            }
                                        }
                                        if (e.Chart != null)
                                        {
                                            viewport4.RefreshFloatingObject(e);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void HandleFloatingObjectChanged(FloatingObject floatingObject, string property, bool autoRefresh)
        {
            if (_viewportPresenters != null)
            {
                if (floatingObject == null)
                {
                    InvalidateFloatingObjectLayout();
                }
                else if (property == "IsSelected")
                {
                    GcViewport[,] viewportArray = _viewportPresenters;
                    int upperBound = viewportArray.GetUpperBound(0);
                    int num2 = viewportArray.GetUpperBound(1);
                    for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                    {
                        for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                        {
                            GcViewport viewport = viewportArray[i, j];
                            if (viewport != null)
                            {
                                if (floatingObject == null)
                                {
                                    viewport.RefreshFloatingObjectContainerIsSelected();
                                }
                                else
                                {
                                    viewport.RefreshFloatingObjectContainerIsSelected(floatingObject);
                                }
                            }
                        }
                    }
                    ReadOnlyCollection<CellRange> selections = Worksheet.Selections;
                    if (selections.Count != 0)
                    {
                        foreach (CellRange range in selections)
                        {
                            UpdateHeaderCellsState(range.Row, range.RowCount, range.Column, range.ColumnCount);
                        }
                    }
                }
                else if (autoRefresh)
                {
                    if ((((property == "Location") || (property == "Size")) || ((property == "SheetRowChanged") || (property == "SheetColumnChanged"))) || ((((property == "AxisX") || (property == "AxisY")) || ((property == "RowFilter") || (property == "RowRangeGroup"))) || ((property == "ColumnRangeGroup") || (property == "Name"))))
                    {
                        GcViewport[,] viewportArray2 = _viewportPresenters;
                        int num5 = viewportArray2.GetUpperBound(0);
                        int num6 = viewportArray2.GetUpperBound(1);
                        for (int k = viewportArray2.GetLowerBound(0); k <= num5; k++)
                        {
                            for (int m = viewportArray2.GetLowerBound(1); m <= num6; m++)
                            {
                                GcViewport viewport1 = viewportArray2[k, m];
                                InvalidateFloatingObjectLayout();
                            }
                        }
                    }
                    else
                    {
                        GcViewport[,] viewportArray3 = _viewportPresenters;
                        int num9 = viewportArray3.GetUpperBound(0);
                        int num10 = viewportArray3.GetUpperBound(1);
                        for (int n = viewportArray3.GetLowerBound(0); n <= num9; n++)
                        {
                            for (int num12 = viewportArray3.GetLowerBound(1); num12 <= num10; num12++)
                            {
                                GcViewport viewport2 = viewportArray3[n, num12];
                                if (viewport2 != null)
                                {
                                    viewport2.InvalidateFloatingObjectMeasureState(floatingObject);
                                    viewport2.RefreshFloatingObject(new FloatingObjectChangedEventArgs(floatingObject, null));
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void HandleFloatingObjectChanged(object sender, FloatingObjectChangedEventArgs e, bool autoRefresh)
        {
            HandleFloatingObjectChanged(e.FloatingObject, e.Property, autoRefresh);
        }

        internal void HandlePictureChanged(object sender, PictureChangedEventArgs e, bool autoRefresh)
        {
            HandleFloatingObjectChanged(e.Picture, e.Property, autoRefresh);
        }

        internal void HandleSheetColumnHeaderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == Worksheet.ColumnHeader)
            {
                switch (e.PropertyName)
                {
                    case "DefaultStyle":
                    case "AutoText":
                    case "AutoTextIndex":
                    case "IsVisible":
                    case "RowCount":
                        Invalidate();
                        return;

                    case "DefaultRowHeight":
                        InvalidateRows(0, Worksheet.ColumnHeader.RowCount, SheetArea.ColumnHeader);
                        return;
                }
            }
        }

        internal void HandleSheetPropertyChanged(object sender, PropertyChangedEventArgs e, bool autoRefresh)
        {
            if (Worksheet != null)
            {
                if (e.PropertyName == "Visible")
                {
                    Dt.Cells.Data.Worksheet sheet = sender as Dt.Cells.Data.Worksheet;
                    if (sheet != null)
                    {
                        HandleVisibleChanged(sheet);
                        if (autoRefresh)
                        {
                            Invalidate();
                        }
                    }
                }
                if ((e.PropertyName == "SheetTabColor") || (e.PropertyName == "SheetTabThemeColor"))
                {
                    UpdateTabStrip();
                }
                if (sender == Worksheet)
                {
                    switch (e.PropertyName)
                    {
                        case "ActiveCell":
                        case "ActiveColumnIndex":
                        case "ActiveRowIndex":
                            Navigation.UpdateStartPosition(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
                            UpdateHeaderCellsStateInSpanArea();
                            UpdateFocusIndicator();
                            UpdateHeaderCellsStateInSpanArea();
                            PrepareCellEditing();
                            UpdateDataValidationUI(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
                            return;

                        case "FrozenRowCount":
                            SetViewportTopRow(Worksheet.FrozenRowCount);
                            if (autoRefresh)
                            {
                                InvalidateRows(0, Worksheet.FrozenRowCount, SheetArea.Cells | SheetArea.RowHeader);
                            }
                            return;

                        case "FrozenColumnCount":
                            SetViewportLeftColumn(Worksheet.FrozenColumnCount);
                            if (autoRefresh)
                            {
                                InvalidateColumns(0, Worksheet.FrozenColumnCount, SheetArea.Cells | SheetArea.ColumnHeader);
                            }
                            return;

                        case "FrozenTrailingRowCount":
                            if (autoRefresh)
                            {
                                InvalidateRows(Math.Max(0, Worksheet.RowCount - Worksheet.FrozenTrailingRowCount), Worksheet.FrozenTrailingRowCount, SheetArea.Cells | SheetArea.RowHeader);
                            }
                            return;

                        case "FrozenTrailingColumnCount":
                            if (autoRefresh)
                            {
                                InvalidateRows(Math.Max(0, Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount), Worksheet.FrozenTrailingColumnCount, SheetArea.Cells | SheetArea.ColumnHeader);
                            }
                            return;

                        case "RowFilter":
                            if (_cachedFilterButtonInfoModel != null)
                            {
                                _cachedFilterButtonInfoModel.Clear();
                                _cachedFilterButtonInfoModel = null;
                            }
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                            }
                            return;

                        case "ShowGridLine":
                        case "GridLineColor":
                        case "ZoomFactor":
                        case "DefaultColumnWidth":
                        case "DefaultRowHeight":
                        case "NamedStyles":
                        case "DefaultStyle":
                        case "[Sort]":
                        case "[MoveTo]":
                        case "[CopyTo]":
                        case "SelectionBorderColor":
                        case "SelectionBorderThemeColor":
                        case "SelectionBackground":
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                            }
                            return;

                        case "DataSource":
                            if (autoRefresh)
                            {
                                Invalidate();
                            }
                            return;

                        case "[ViewportInfo]":
                            return;

                        case "RowCount":
                        case "RowRangeGroup":
                            if (autoRefresh)
                            {
                                InvalidateRows(0, Worksheet.RowCount, SheetArea.Cells | SheetArea.RowHeader);
                            }
                            return;

                        case "ColumnCount":
                        case "ColumnRangeGroup":
                            if (autoRefresh)
                            {
                                InvalidateColumns(0, Worksheet.ColumnCount, SheetArea.Cells | SheetArea.ColumnHeader);
                            }
                            return;

                        case "StartingRowNumber":
                        case "RowHeaderColumnCount":
                            if (autoRefresh)
                            {
                                InvalidateColumns(0, Worksheet.RowHeader.ColumnCount, SheetArea.CornerHeader | SheetArea.RowHeader);
                            }
                            return;

                        case "StartingColumnNumber":
                        case "ColumnHeaderRowCount":
                            if (autoRefresh)
                            {
                                InvalidateRows(0, Worksheet.ColumnHeader.RowCount, SheetArea.ColumnHeader);
                            }
                            return;

                        case "RowHeaderDefaultStyle":
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.CornerHeader | SheetArea.RowHeader);
                            }
                            return;

                        case "ColumnHeaderDefaultStyle":
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.ColumnHeader);
                            }
                            return;

                        case "ReferenceStyle":
                        case "Names":
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.Cells);
                            }
                            return;

                        case "[ImportFile]":
                            if (autoRefresh)
                            {
                                InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                            }
                            if (_host is Excel excel)
                            {
                                excel.HideProgressRingOnOpenCSVCompleted();
                            }
                            return;

                        case "[OpenXml]":
                            InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                            return;

                        case "Charts":
                        case "SurfaceCharts":
                        case "FloatingObjects":
                        case "Pictures":
                            if (autoRefresh)
                            {
                                InvalidateFloatingObjectLayout();
                            }
                            return;
                    }
                }
            }
        }

        internal void HandleSheetRowHeaderPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == Worksheet.RowHeader)
            {
                switch (e.PropertyName)
                {
                    case "DefaultStyle":
                    case "AutoText":
                    case "AutoTextIndex":
                    case "IsVisible":
                    case "ColumnCount":
                        Invalidate();
                        return;

                    case "DefaultColumnWidth":
                        InvalidateColumns(0, Worksheet.RowHeader.ColumnCount, SheetArea.CornerHeader | SheetArea.RowHeader);
                        return;
                }
            }
        }

        internal void HandleSheetSelectionChanged(object sender, SheetSelectionChangedEventArgs e)
        {
            RefreshSelection();
            UpdateHeaderCellsState(e.Row, e.RowCount, e.Column, e.ColumnCount);
            Navigation.UpdateStartPosition(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
        }

        void HandleVisibleChanged(Dt.Cells.Data.Worksheet sheet)
        {
            if ((sheet != null) && (sheet.Workbook != null))
            {
                if (sheet.Visible)
                {
                    if (sheet.Workbook.ActiveSheetIndex < 0)
                    {
                        sheet.Workbook.ActiveSheet = sheet;
                    }
                }
                else if (sheet.Workbook.Sheets != null)
                {
                    int index = sheet.Workbook.Sheets.IndexOf(sheet);
                    if ((index != -1) && (index == sheet.Workbook.ActiveSheetIndex))
                    {
                        int count = sheet.Workbook.Sheets.Count;
                        int num3 = index + 1;
                        while ((num3 < count) && !sheet.Workbook.Sheets[num3].Visible)
                        {
                            num3++;
                        }
                        if (num3 >= count)
                        {
                            num3 = index - 1;
                            while ((num3 >= 0) && !sheet.Workbook.Sheets[num3].Visible)
                            {
                                num3--;
                            }
                        }
                        sheet.Workbook.ActiveSheetIndex = num3;
                    }
                }
            }
        }

        internal void HandleWorkbookPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Sheets":
                case "ActiveSheetIndex":
                case "ActiveSheet":
                    Invalidate();
                    return;

                case "StartSheetIndex":
                    ProcessStartSheetIndexChanged();
                    return;

                case "CurrentThemeName":
                case "CurrentTheme":
                    InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                    InvalidateFloatingObjects();
                    return;

                case "HorizontalScrollBarVisibility":
                case "VerticalScrollBarVisibility":
                case "ReferenceStyle":
                case "Names":
                case "CanCellOverflow":
                case "AutoRefresh":
                case "[OpenXml]":
                    InvalidateRange(-1, -1, -1, -1, SheetArea.Cells | SheetArea.ColumnHeader | SheetArea.RowHeader);
                    return;

                case "[OpenExcel]":
                case "[DataCalculated]":
                    if (_host is Excel)
                    {
                        (_host as Excel).HideOpeningStatusOnOpenExcelCompleted();
                    }
                    InvalidateLayout();
                    base.InvalidateMeasure();
                    base.InvalidateArrange();
                    Invalidate();
                    return;
            }
        }

        internal static bool HasArrayFormulas(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount)
        {
            object[,] objArray = GetsArrayFormulas(sheet, row, column, rowCount, columnCount);
            return ((objArray != null) && (objArray.Length > 0));
        }

        static bool HasPartArrayFormulas(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount, CellRange exceptedRange)
        {
            object[,] objArray = GetsArrayFormulas(sheet, row, column, rowCount, columnCount);
            if ((objArray != null) && (objArray.Length > 0))
            {
                int length = objArray.GetLength(0);
                for (int i = 0; i < length; i++)
                {
                    CellRange range = (CellRange)objArray[i, 0];
                    if ((exceptedRange == null) || !exceptedRange.Equals(range))
                    {
                        if ((row != -1) && ((range.Row < row) || ((range.Row + range.RowCount) > (row + rowCount))))
                        {
                            return true;
                        }
                        if ((column != -1) && ((range.Column < column) || ((range.Column + range.ColumnCount) > (column + columnCount))))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static bool HasPartSpans(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount)
        {
            if ((row >= 0) || (column >= 0))
            {
                if (row < 0)
                {
                    SheetSpanModel columnHeaderSpanModel = sheet.ColumnHeaderSpanModel;
                    if ((columnHeaderSpanModel != null) && !columnHeaderSpanModel.IsEmpty())
                    {
                        IEnumerator enumerator = columnHeaderSpanModel.GetEnumerator(-1, column, -1, columnCount);
                        CellRange current = null;
                        while (enumerator.MoveNext())
                        {
                            current = (CellRange)enumerator.Current;
                            if ((current.Column < column) || ((current.Column + current.ColumnCount) > (column + columnCount)))
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (column < 0)
                {
                    SheetSpanModel rowHeaderSpanModel = sheet.RowHeaderSpanModel;
                    if ((rowHeaderSpanModel != null) && !rowHeaderSpanModel.IsEmpty())
                    {
                        IEnumerator enumerator2 = rowHeaderSpanModel.GetEnumerator(row, -1, rowCount, -1);
                        CellRange range2 = null;
                        while (enumerator2.MoveNext())
                        {
                            range2 = (CellRange)enumerator2.Current;
                            if ((range2.Row < row) || ((range2.Row + range2.RowCount) > (row + rowCount)))
                            {
                                return true;
                            }
                        }
                    }
                }
                SheetSpanModel spanModel = sheet.SpanModel;
                if ((spanModel != null) && !spanModel.IsEmpty())
                {
                    IEnumerator enumerator3 = spanModel.GetEnumerator(row, column, rowCount, columnCount);
                    CellRange range3 = null;
                    while (enumerator3.MoveNext())
                    {
                        range3 = (CellRange)enumerator3.Current;
                        if ((row != -1) && ((range3.Row < row) || ((range3.Row + range3.RowCount) > (row + rowCount))))
                        {
                            return true;
                        }
                        if ((column != -1) && ((range3.Column < column) || ((range3.Column + range3.ColumnCount) > (column + columnCount))))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal bool HasSelectedFloatingObject()
        {
            foreach (IFloatingObject obj2 in GetAllFloatingObjects())
            {
                if (obj2.IsSelected)
                {
                    return true;
                }
            }
            return false;
        }

        bool HasSpans(int row, int column, int rowCount, int columnCount)
        {
            IEnumerable spanModel = Worksheet.SpanModel;
            if (spanModel != null)
            {
                foreach (CellRange range in spanModel)
                {
                    if (range.Intersects(row, column, rowCount, columnCount))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal static bool HasTable(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount, bool isInsert)
        {
            int num = (row < 0) ? 0 : row;
            int num2 = (column < 0) ? 0 : column;
            foreach (SheetTable table in sheet.GetTables())
            {
                if (table.Range.Intersects(row, column, rowCount, columnCount))
                {
                    if (!isInsert)
                    {
                        return true;
                    }
                    if ((num > table.Range.Row) || (num2 > table.Range.Column))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal void HideCursor()
        {
#if UWP
            Window.Current.CoreWindow.PointerCursor = null;
#endif
        }

        internal void HideFormulaSelectionTouchGrippers()
        {
            if (_formulaSelectionGripperPanel != null)
            {
                _formulaSelectionGripperPanel.Visibility = Visibility.Collapsed;
            }
        }

        void HideMouseCursor()
        {
            if (_mouseCursor != null)
            {
                _mouseCursor.Opacity = 0.0;
            }
        }

        bool HitTestFloatingObject(int rowViewportIndex, int columnViewportIndex, double mouseX, double mouseY, HitTestInformation hi)
        {
            FloatingObjectLayoutModel viewportFloatingObjectLayoutModel = GetViewportFloatingObjectLayoutModel(rowViewportIndex, columnViewportIndex);
            if ((viewportFloatingObjectLayoutModel != null) && (viewportFloatingObjectLayoutModel.Count != 0))
            {
                FloatingObject[] allFloatingObjects = GetAllFloatingObjects();
                foreach (FloatingObject obj2 in SortFloatingObjectByZIndex(allFloatingObjects))
                {
                    FloatingObjectLayout layout = viewportFloatingObjectLayoutModel.Find(obj2.Name);
                    if ((layout != null) && obj2.Visible)
                    {
                        bool isSelected = obj2.IsSelected;
                        double x = layout.X;
                        double y = layout.Y;
                        double width = layout.Width;
                        double height = layout.Height;
                        if (isSelected)
                        {
                            double num5 = 7.0;
                            x -= num5;
                            y -= num5;
                            width += 2.0 * num5;
                            height += 2.0 * num5;
                        }
                        Rect rect = new Rect(x, y, width, height);
                        if (rect.Contains(new Point(mouseX, mouseY)))
                        {
                            ViewportFloatingObjectHitTestInformation information = new ViewportFloatingObjectHitTestInformation();
                            hi.HitTestType = HitTestType.FloatingObject;
                            hi.FloatingObjectInfo = information;
                            information.FloatingObject = obj2;
                            if (!isSelected)
                            {
                                information.InMoving = true;
                                return true;
                            }
                            double num6 = 7.0;
                            double size = 10.0;
                            Rect rect2 = new Rect(x, y, num6, num6);
                            if (InflateRect(rect2, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InTopNWSEResize = true;
                                return true;
                            }
                            Rect rect3 = new Rect((x + (width / 2.0)) - num6, y, 2.0 * num6, num6);
                            if (InflateRect(rect3, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InTopNSResize = true;
                                return true;
                            }
                            Rect rect4 = new Rect((x + width) - num6, y, num6, num6);
                            if (InflateRect(rect4, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InTopNESWResize = true;
                                return true;
                            }
                            Rect rect5 = new Rect(x, (y + (height / 2.0)) - num6, num6, 2.0 * num6);
                            if (InflateRect(rect5, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InLeftWEResize = true;
                                return true;
                            }
                            Rect rect6 = new Rect((x + width) - num6, (y + (height / 2.0)) - num6, num6, 2.0 * num6);
                            if (InflateRect(rect6, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InRightWEResize = true;
                                return true;
                            }
                            Rect rect7 = new Rect(x, (y + height) - num6, num6, num6);
                            if (InflateRect(rect7, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InBottomNESWResize = true;
                                return true;
                            }
                            Rect rect8 = new Rect((x + (width / 2.0)) - num6, (y + height) - num6, 2.0 * num6, num6);
                            if (InflateRect(rect8, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InBottomNSResize = true;
                                return true;
                            }
                            Rect rect9 = new Rect((x + width) - num6, (y + height) - num6, num6, num6);
                            if (InflateRect(rect9, size).Contains(new Point(mouseX, mouseY)))
                            {
                                information.InBottomNWSEResize = true;
                                return true;
                            }
                            information.InMoving = true;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool HitTestPopup(PopupHelper popUpHelper, Point point)
        {
            if (popUpHelper == null)
            {
                return false;
            }
            Rect rect = new Rect(popUpHelper.Location.X, popUpHelper.Location.Y, popUpHelper.Size.Width, popUpHelper.Size.Height);
            return rect.Expand(10, 5).Contains(point);
        }

        string IndexToLetter(int index)
        {
            StringBuilder builder = new StringBuilder();
            while (index > 0)
            {
                builder.Append((char)(0x41 + ((index - 1) % 0x1a)));
                index = (index - 1) / 0x1a;
            }
            for (int i = 0; i < (builder.Length / 2); i++)
            {
                char ch = builder[i];
                builder[i] = builder[(builder.Length - i) - 1];
                builder[(builder.Length - i) - 1] = ch;
            }
            return builder.ToString();
        }

        Rect InflateRect(Rect rect, double size)
        {
            double x = rect.X - size;
            double y = rect.Y - size;
            double width = rect.Width + (2.0 * size);
            double height = rect.Height + (2.0 * size);
            if (width < 0.0)
            {
                width = 0.0;
            }
            if (height < 0.0)
            {
                height = 0.0;
            }
            return new Rect(x, y, width, height);
        }

#if IOS
        new
#endif
        void Init()
        {
            _allowUserFormula = true;
            _allowUndo = true;
            _freezeLineStyle = null;
            _trailingFreezeLineStyle = null;
            _showFreezeLine = true;
            _allowUserZoom = true;
            _autoClipboard = true;
            _clipBoardOptions = ClipboardPasteOptions.All;
            _allowEditOverflow = true;
            _protect = false;
            _vScrollable = true;
            _hScrollable = true;
            _sheet = null;
            _cachedColumnHeaderRowLayoutModel = null;
            _cachedViewportRowLayoutModel = null;
            _cachedRowHeaderColumnLayoutModel = null;
            _cachedViewportColumnLayoutModel = null;
            _cachedColumnHeaderViewportColumnLayoutModel = null;
            _cachedRowHeaderViewportRowLayoutModel = null;
            _cachedRowHeaderCellLayoutModel = null;
            _cachedColumnHeaderCellLayoutModel = null;
            _cachedViewportCellLayoutModel = null;
            _cachedGroupLayout = null;
            _cachedFloatingObjectLayoutModel = null;
            _cornerPresenter = null;
            _rowHeaderPresenters = null;
            _columnHeaderPresenters = null;
            _viewportPresenters = null;
            _groupCornerPresenter = null;
            _rowGroupHeaderPresenter = null;
            _columnGroupHeaderPresenter = null;
            _rowGroupPresenters = null;
            _columnGroupPresenters = null;
            _showColumnRangeGroup = true;
            _showRowRangeGroup = true;
            _shapeDrawingContainer = null;
            _trackersContainer = null;
            _columnFreezeLine = null;
            _rowFreezeLine = null;
            _columnTrailingFreezeLine = null;
            _rowTrailingFreezeLine = null;
            _resizingTracker = null;
            _currentActiveColumnIndex = 0;
            _currentActiveRowIndex = 0;
            _verticalSelectionMgr = null;
            _horizontalSelectionMgr = null;
            _keyMap = null;
            _floatingObjectsKeyMap = null;
            _availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            _isEditing = false;
            _isDoubleClick = false;
            _positionInfo = null;
            _navigation = null;
            _undoManager = null;
            _eventSuspended = 0;
            _lastClickPoint = new Point();
            _lastClickLocation = new Point(-1.0, -1.0);
            _hoverManager = new Dt.Cells.UI.HoverManager(this);
            _allowDragDrop = true;
            _dragDropIndicator = null;
            _dragDropInsertIndicator = null;
            _dragDropFromRange = null;
            _dragDropColumnOffset = 0;
            _dragDropRowOffset = 0;
            _isDragInsert = false;
            _isDragCopy = false;
            _dragStartRowViewport = -2;
            _dragStartColumnViewport = -2;
            _dragToColumnViewport = -2;
            _dragToRowViewport = -2;
            _dragToColumn = -2;
            _dragToRow = -2;
            _allowDragFill = true;
            _highlightDataValidationInvalidData = false;
            _mouseCursor = null;
            _tooltipHelper = null;
            _filterPopupHelper = null;
            _dataValidationPopUpHelper = null;
            _inputDeviceType = Dt.Cells.UI.InputDeviceType.Mouse;
            _canTouchMultiSelect = false;
            _resizeZeroIndicator = Dt.Cells.UI.ResizeZeroIndicator.Default;
            _cachedResizerGipper = new Dictionary<string, BitmapImage>();
            _cachedToolbarImageSources = new Dictionary<string, ImageSource>();
        }

        void InitDefaultKeyMap()
        {
            if (_keyMap == null)
            {
                _keyMap = new Dictionary<KeyStroke, SpreadAction>();
                _keyMap.Add(new KeyStroke(VirtualKey.Z, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.Undo));
                _keyMap.Add(new KeyStroke(VirtualKey.Y, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.Redo));
                _keyMap.Add(new KeyStroke(VirtualKey.Down, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationBottom));
                _keyMap.Add(new KeyStroke(VirtualKey.Down, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationDown));
                _keyMap.Add(new KeyStroke(VirtualKey.End, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationEnd));
                _keyMap.Add(new KeyStroke(VirtualKey.Right, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationEnd));
                _keyMap.Add(new KeyStroke(VirtualKey.Home, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationFirst));
                _keyMap.Add(new KeyStroke(VirtualKey.Home, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationHome));
                _keyMap.Add(new KeyStroke(VirtualKey.Left, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationHome));
                _keyMap.Add(new KeyStroke(VirtualKey.End, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationLast));
                _keyMap.Add(new KeyStroke(VirtualKey.Left, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationLeft));
                _keyMap.Add(new KeyStroke(VirtualKey.Tab, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.CommitInputNavigationTabNext));
                _keyMap.Add(new KeyStroke(VirtualKey.PageDown, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationPageDown));
                _keyMap.Add(new KeyStroke(VirtualKey.PageUp, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationPreviousSheet));
                _keyMap.Add(new KeyStroke(VirtualKey.PageDown, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationNextSheet));
                _keyMap.Add(new KeyStroke(VirtualKey.PageUp, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationPageUp));
                _keyMap.Add(new KeyStroke(VirtualKey.Tab, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.CommitInputNavigationTabPrevious));
                _keyMap.Add(new KeyStroke(VirtualKey.Right, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationRight));
                _keyMap.Add(new KeyStroke(VirtualKey.Up, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.NavigationTop));
                _keyMap.Add(new KeyStroke(VirtualKey.Up, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationUp));
                _keyMap.Add(new KeyStroke(VirtualKey.Delete, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.Clear));
                _keyMap.Add(new KeyStroke(VirtualKey.Back, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.ClearAndEditing));
                _keyMap.Add(new KeyStroke(VirtualKey.Enter, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.CommitInputNavigationDown));
                _keyMap.Add(new KeyStroke(VirtualKey.Enter, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.CommitInputNavigationUp));
                _keyMap.Add(new KeyStroke(VirtualKey.Escape, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.CancelInput));
                _keyMap.Add(new KeyStroke(VirtualKey.Left, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionLeft));
                _keyMap.Add(new KeyStroke(VirtualKey.Right, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionRight));
                _keyMap.Add(new KeyStroke(VirtualKey.Up, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionUp));
                _keyMap.Add(new KeyStroke(VirtualKey.Down, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionDown));
                _keyMap.Add(new KeyStroke(VirtualKey.Home, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionHome));
                _keyMap.Add(new KeyStroke(VirtualKey.Left, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionHome));
                _keyMap.Add(new KeyStroke(VirtualKey.End, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionEnd));
                _keyMap.Add(new KeyStroke(VirtualKey.Right, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionEnd));
                _keyMap.Add(new KeyStroke(VirtualKey.PageUp, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionPageUp));
                _keyMap.Add(new KeyStroke(VirtualKey.PageDown, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionPageDown));
                _keyMap.Add(new KeyStroke(VirtualKey.Up, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionTop));
                _keyMap.Add(new KeyStroke(VirtualKey.Down, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionBottom));
                _keyMap.Add(new KeyStroke(VirtualKey.Home, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionFirst));
                _keyMap.Add(new KeyStroke(VirtualKey.End, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.SelectionLast));
                _keyMap.Add(new KeyStroke(VirtualKey.C, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.Copy));
                _keyMap.Add(new KeyStroke(VirtualKey.X, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.Cut));
                _keyMap.Add(new KeyStroke(VirtualKey.V, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.Paste));
                _keyMap.Add(new KeyStroke(VirtualKey.Enter, VirtualKeyModifiers.Menu), new SpreadAction(SpreadActions.InputNewLine));
                _keyMap.Add(new KeyStroke(VirtualKey.F2, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.StartEditing));
                _keyMap.Add(new KeyStroke(VirtualKey.Enter, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.InputArrayFormula));
                _keyMap.Add(new KeyStroke(VirtualKey.A, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.SelectionAll));
            }
        }

        void InitFloatingObjectKeyMap()
        {
            if (_floatingObjectsKeyMap == null)
            {
                _floatingObjectsKeyMap = new Dictionary<KeyStroke, SpreadAction>();
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Escape, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.UnSelectAllFloatingObjects));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Delete, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.DeleteFloatingObject));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Tab, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.NavigationNextFloatingObject));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Tab, VirtualKeyModifiers.None | VirtualKeyModifiers.Shift), new SpreadAction(SpreadActions.NavigationPreviousFloatingObject));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.X, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.ClipboardCutFloatingObjects));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.C, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.ClipboardCopyFloatingObjects));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.V, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.ClipboardPasteFloatingObjects));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.A, VirtualKeyModifiers.Control), new SpreadAction(SpreadActions.SelectionAll));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Left, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.MoveFloatingObjectLeft));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Up, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.MoveFloatingObjectTop));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Right, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.MoveFloatingObjectRight));
                _floatingObjectsKeyMap.Add(new KeyStroke(VirtualKey.Down, VirtualKeyModifiers.None), new SpreadAction(SpreadActions.MoveFloatingObjectDown));
            }
        }

        bool InitFloatingObjectsMovingResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (IsTouching)
            {
                savedHitTestInformation = _touchStartHitTestInfo;
            }
            if (((savedHitTestInformation.ViewportInfo == null) || (savedHitTestInformation.RowViewportIndex == -2)) || (savedHitTestInformation.ColumnViewportIndex == 2))
            {
                return false;
            }
            _floatingObjectsMovingResizingStartRow = savedHitTestInformation.ViewportInfo.Row;
            _floatingObjectsMovingResizingStartColumn = savedHitTestInformation.ViewportInfo.Column;
            _dragStartRowViewport = savedHitTestInformation.RowViewportIndex;
            _dragStartColumnViewport = savedHitTestInformation.ColumnViewportIndex;
            _dragToRowViewport = savedHitTestInformation.RowViewportIndex;
            _dragToColumnViewport = savedHitTestInformation.ColumnViewportIndex;
            _floatingObjectsMovingResizingStartPoint = savedHitTestInformation.HitPoint;
            SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
            SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
            CachFloatingObjectsMovingResizingLayoutModels();
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragStartRowViewport, _floatingObjectsMovingResizingStartPoint.Y);
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragToColumnViewport, _floatingObjectsMovingResizingStartPoint.X);
            _floatingObjectsMovingResizingStartPointCellBounds = new Rect(viewportColumnLayoutNearX.X, viewportRowLayoutNearY.Y, viewportColumnLayoutNearX.Width, viewportRowLayoutNearY.Height);
            _floatingObjectsMovingStartLocations = new Dictionary<string, Point>();
            FloatingObject[] objArray = _movingResizingFloatingObjects;
            for (int i = 0; i < objArray.Length; i++)
            {
                IFloatingObject obj2 = objArray[i];
                _floatingObjectsMovingStartLocations.Add(obj2.Name, obj2.Location);
            }
            return true;
        }

        void InvaidateViewportHorizontalArrangementInternal(int columnViewportIndex)
        {
            int rowViewportCount = GetViewportInfo().RowViewportCount;
            for (int i = -1; i <= rowViewportCount; i++)
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(i, columnViewportIndex);
                if (viewportRowsPresenter != null)
                {
                    viewportRowsPresenter.InvalidateRowsMeasureState(true);
                    viewportRowsPresenter.InvalidateBordersMeasureState();
                    viewportRowsPresenter.InvalidateSelectionMeasureState();
                    viewportRowsPresenter.InvalidateFloatingObjectsMeasureState();
                    viewportRowsPresenter.InvalidateMeasure();
                }
            }
            GcViewport columnHeaderRowsPresenter = GetColumnHeaderRowsPresenter(columnViewportIndex);
            if (columnHeaderRowsPresenter != null)
            {
                columnHeaderRowsPresenter.InvalidateRowsMeasureState(true);
                columnHeaderRowsPresenter.InvalidateBordersMeasureState();
                columnHeaderRowsPresenter.InvalidateMeasure();
            }
        }

        /// <summary>
        /// Invalidates the measurement state (layout) and the arranged state (layout) for the control.
        /// The view layout and data is updated after the invalidation.
        /// </summary>
#if ANDROID
        new
#endif
        public void Invalidate()
        {
            if (!IsSuspendInvalidate())
            {
                if (IsEditing)
                {
                    StopCellEditing(true);
                }
                InvalidateLayout();
                Children.Clear();
                _cornerPresenter = null;
                _rowHeaderPresenters = null;
                _columnHeaderPresenters = null;
                if (_viewportPresenters != null)
                {
                    GcViewport[,] viewportArray = _viewportPresenters;
                    int upperBound = viewportArray.GetUpperBound(0);
                    int num2 = viewportArray.GetUpperBound(1);
                    for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                    {
                        for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                        {
                            GcViewport viewport = viewportArray[i, j];
                            if (viewport != null)
                            {
                                viewport.RemoveDataValidationUI();
                            }
                        }
                    }
                }
                _viewportPresenters = null;
                _groupCornerPresenter = null;
                _rowGroupHeaderPresenter = null;
                _columnGroupHeaderPresenter = null;
                _rowGroupPresenters = null;
                _columnGroupPresenters = null;
                _tooltipHelper = null;
                _currentActiveColumnIndex = (Worksheet == null) ? -1 : Worksheet.ActiveColumnIndex;
                _currentActiveRowIndex = (Worksheet == null) ? -1 : Worksheet.ActiveRowIndex;
                Navigation.UpdateStartPosition(_currentActiveRowIndex, _currentActiveColumnIndex);
            }
        }

        /// <summary>
        /// Invalidates the charts.
        /// </summary>
        public void InvalidateCharts()
        {
            if ((Worksheet != null) && (Worksheet.Charts.Count > 0))
            {
                InvalidateCharts(Worksheet.Charts.ToArray());
            }
        }

        /// <summary>
        /// Invalidates the charts.
        /// </summary>
        /// <param name="charts">The charts.</param>
        public void InvalidateCharts(params SpreadChart[] charts)
        {
            InvalidateFloatingObjectLayout();
            foreach (SpreadChart chart in charts)
            {
                RefreshViewportFloatingObjects(chart);
            }
        }

        /// <summary>
        /// Invalidates the column state in the control; the column layout and data is updated after the invalidation.
        /// </summary>
        /// <param name="column">The start column index.</param>
        /// <param name="columnCount">The column count.</param>
        /// <param name="sheetArea">The invalidated sheet area.</param>
        public void InvalidateColumns(int column, int columnCount, SheetArea sheetArea)
        {
            if (!IsSuspendInvalidate())
            {
                InvalidateRange(-1, column, -1, columnCount, sheetArea);
            }
        }

        /// <summary>
        /// Invalidates the custom floating objects.
        /// </summary>
        public void InvalidateCustomFloatingObjects()
        {
            if ((Worksheet != null) && (Worksheet.FloatingObjects.Count > 0))
            {
                List<CustomFloatingObject> list = new List<CustomFloatingObject>();
                foreach (FloatingObject obj2 in Worksheet.FloatingObjects)
                {
                    if (obj2 is CustomFloatingObject)
                    {
                        list.Add(obj2 as CustomFloatingObject);
                    }
                }
                InvalidateCustomFloatingObjects(list.ToArray());
            }
        }

        /// <summary>
        /// Invalidates the custom floating objects.
        /// </summary>
        /// <param name="floatingObjects">The floating objects.</param>
        public void InvalidateCustomFloatingObjects(params CustomFloatingObject[] floatingObjects)
        {
            InvalidateFloatingObjectLayout();
            foreach (CustomFloatingObject obj2 in floatingObjects)
            {
                RefreshViewportFloatingObjects(obj2);
            }
        }

        internal void InvalidateFloatingObjectLayout()
        {
            InvalidateFloatingObjectsLayoutModel();
            RefreshViewportFloatingObjectsLayout();
        }

        /// <summary>
        /// Invalidates the charts.
        /// </summary>
        public void InvalidateFloatingObjects()
        {
            InvalidateFloatingObjectLayout();
            RefreshViewportFloatingObjects();
        }

        /// <summary>
        /// Invalidates the floating object.
        /// </summary>
        /// <param name="floatingObjects">The floating objects.</param>
        public void InvalidateFloatingObjects(params FloatingObject[] floatingObjects)
        {
            InvalidateFloatingObjectLayout();
            foreach (FloatingObject obj2 in floatingObjects)
            {
                RefreshViewportFloatingObjects(obj2);
            }
        }

        void InvalidateFloatingObjectsLayoutModel()
        {
            _cachedFloatingObjectLayoutModel = null;
        }

        internal void InvalidateHeaderHorizontalArrangement()
        {
            if (!IsSuspendInvalidate())
            {
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                for (int i = -1; i <= rowViewportCount; i++)
                {
                    GcViewport rowHeaderRowsPresenter = GetRowHeaderRowsPresenter(i);
                    if (rowHeaderRowsPresenter != null)
                    {
                        rowHeaderRowsPresenter.InvalidateRowsMeasureState(true);
                        rowHeaderRowsPresenter.InvalidateBordersMeasureState();
                        rowHeaderRowsPresenter.InvalidateMeasure();
                    }
                }
                GcViewport cornerPresenter = GetCornerPresenter();
                if (cornerPresenter != null)
                {
                    cornerPresenter.InvalidateRowsMeasureState(true);
                    cornerPresenter.InvalidateBordersMeasureState();
                    cornerPresenter.InvalidateMeasure();
                }
            }
        }

        void InvalidateHeaderRowMeasure(int rowIndex)
        {
            RowPresenter objRow;
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            for (int i = -1; i <= columnViewportCount; i++)
            {
                Action<CellLayout> action = null;
                GcViewport columnHeaderViewport = GetColumnHeaderRowsPresenter(i);
                if (columnHeaderViewport != null)
                {
                    objRow = columnHeaderViewport.GetRow(rowIndex);
                    if (objRow != null)
                    {
                        objRow.InvalidateMeasure();
                    }
                    if (action == null)
                    {
                        action = delegate (CellLayout cellLayout)
                        {
                            if ((rowIndex >= cellLayout.Row) && (rowIndex < (cellLayout.Row + cellLayout.RowCount)))
                            {
                                objRow = columnHeaderViewport.GetRow(cellLayout.Row);
                                if (objRow != null)
                                {
                                    objRow.InvalidateMeasure();
                                }
                            }
                        };
                    }
                    Enumerable.ToList<CellLayout>(GetColumnHeaderCellLayoutModel(i)).ForEach<CellLayout>(action);
                }
            }
            GcViewport cornerPresenter = GetCornerPresenter();
            if (cornerPresenter != null)
            {
                objRow = cornerPresenter.GetRow(rowIndex);
                if (objRow != null)
                {
                    objRow.InvalidateMeasure();
                }
            }
        }

        void InvalidateHeaderRowsPresenterMeasure(bool invalidateRowMeasure)
        {
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            for (int i = -1; i <= columnViewportCount; i++)
            {
                GcViewport columnHeaderRowsPresenter = GetColumnHeaderRowsPresenter(i);
                if (columnHeaderRowsPresenter != null)
                {
                    if (invalidateRowMeasure)
                    {
                        columnHeaderRowsPresenter.InvalidateRowsMeasureState(true);
                        columnHeaderRowsPresenter.InvalidateBordersMeasureState();
                    }
                    columnHeaderRowsPresenter.InvalidateMeasure();
                }
            }
        }

        internal virtual void InvalidateLayout()
        {
            if (!IsSuspendInvalidate())
            {
                _cachedLayout = null;
                _cachedViewportRowLayoutModel = null;
                _cachedViewportColumnLayoutModel = null;
                _cachedColumnHeaderRowLayoutModel = null;
                _cachedColumnHeaderViewportColumnLayoutModel = null;
                _cachedRowHeaderViewportRowLayoutModel = null;
                _cachedRowHeaderColumnLayoutModel = null;
                _cachedViewportCellLayoutModel = null;
                _cachedColumnHeaderCellLayoutModel = null;
                _cachedRowHeaderCellLayoutModel = null;
                _cachedGroupLayout = null;
                _cachedFilterButtonInfoModel = null;
                _cachedFloatingObjectLayoutModel = null;
            }
        }

        /// <summary>
        /// Invalidates the pictures.
        /// </summary>
        public void InvalidatePictures()
        {
            if ((Worksheet != null) && (Worksheet.Pictures.Count > 0))
            {
                InvalidatePictures(Worksheet.Pictures.ToArray());
            }
        }

        /// <summary>
        /// Invalidates the pictures.
        /// </summary>
        /// <param name="pictures">The pictures.</param>
        public void InvalidatePictures(params Picture[] pictures)
        {
            InvalidateFloatingObjectLayout();
            foreach (Picture picture in pictures)
            {
                RefreshViewportFloatingObjects(picture);
            }
        }

        /// <summary>
        /// Invalidates a range state in the control; the range layout and data is updated after the invalidation.
        /// </summary>
        /// <param name="row">The start row index.</param>
        /// <param name="column">The start column index.</param>
        /// <param name="rowCount">The row count.</param>
        /// <param name="columnCount">The column count.</param>
        /// <param name="sheetArea">The invalidated sheet area.</param>
        public void InvalidateRange(int row, int column, int rowCount, int columnCount, SheetArea sheetArea)
        {
            if (!IsSuspendInvalidate())
            {
                if ((row < 0) || (column < 0))
                {
                    InvalidateLayout();
                }
                _cachedFilterButtonInfoModel = null;
                InvalidateMeasure();
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                if (((byte)(sheetArea & SheetArea.Cells)) == 1)
                {
                    if (row < 0)
                    {
                        row = 0;
                        rowCount = (worksheet == null) ? 0 : worksheet.RowCount;
                    }
                    if (column < 0)
                    {
                        column = 0;
                        columnCount = (worksheet == null) ? 0 : worksheet.ColumnCount;
                    }
                    _cachedViewportCellLayoutModel = null;
                    RefreshViewportCells(_viewportPresenters, row, column, rowCount, columnCount);
                }
                if (((byte)(sheetArea & SheetArea.ColumnHeader)) == 4)
                {
                    if (row < 0)
                    {
                        row = 0;
                        rowCount = (worksheet == null) ? 0 : worksheet.RowCount;
                    }
                    if (column < 0)
                    {
                        column = 0;
                        columnCount = (worksheet == null) ? 0 : worksheet.ColumnCount;
                    }
                    _cachedColumnHeaderCellLayoutModel = null;
                    RefreshHeaderCells(_columnHeaderPresenters, row, column, rowCount, columnCount);
                }
                if (((byte)(sheetArea & (SheetArea.CornerHeader | SheetArea.RowHeader))) == 2)
                {
                    if (row < 0)
                    {
                        row = 0;
                        rowCount = (worksheet == null) ? 0 : worksheet.RowCount;
                    }
                    if (column < 0)
                    {
                        column = 0;
                        columnCount = (worksheet == null) ? 0 : worksheet.ColumnCount;
                    }
                    _cachedRowHeaderCellLayoutModel = null;
                    RefreshHeaderCells(_rowHeaderPresenters, row, column, rowCount, columnCount);
                }
            }
        }

        /// <summary>
        /// Invalidates the row state in the control; the row layout and data is updated after the invalidation.
        /// </summary>
        /// <param name="row">The start row index.</param>
        /// <param name="rowCount">The row count.</param>
        /// <param name="sheetArea">The invalidated sheet area.</param>
        public void InvalidateRows(int row, int rowCount, SheetArea sheetArea)
        {
            if (!IsSuspendInvalidate())
            {
                InvalidateRange(row, -1, rowCount, -1, sheetArea);
            }
        }

        internal void InvalidateViewportColumnsLayout()
        {
            if (!IsSuspendInvalidate())
            {
                _cachedViewportColumnLayoutModel = null;
                _cachedColumnHeaderViewportColumnLayoutModel = null;
                _cachedViewportCellLayoutModel = null;
                _cachedColumnHeaderCellLayoutModel = null;
                _cachedFloatingObjectLayoutModel = null;
            }
        }

        internal void InvalidateViewportHorizontalArrangement(int columnViewportIndex)
        {
            if (!IsSuspendInvalidate())
            {
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                if (columnViewportIndex < -1)
                {
                    for (int i = -1; i <= columnViewportCount; i++)
                    {
                        InvaidateViewportHorizontalArrangementInternal(i);
                    }
                }
                else
                {
                    InvaidateViewportHorizontalArrangementInternal(columnViewportIndex);
                }
            }
        }

        void InvalidateViewportRowMeasure(int rowViewportIndex, int rowIndex)
        {
            if (rowViewportIndex < -1)
            {
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                for (int i = -1; i <= rowViewportCount; i++)
                {
                    InvalidateViewportRowMeasureInternal(i, rowIndex);
                }
            }
            else
            {
                InvalidateViewportRowMeasureInternal(rowViewportIndex, rowIndex);
            }
        }

        void InvalidateViewportRowMeasureInternal(int rowViewportIndex, int rowIndex)
        {
            Action<CellLayout> action2 = null;
            RowPresenter objRow;
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            for (int i = -1; i <= columnViewportCount; i++)
            {
                Action<CellLayout> action = null;
                GcViewport viewport = GetViewportRowsPresenter(rowViewportIndex, i);
                if (viewport != null)
                {
                    viewport.InvalidateMeasure();
                    objRow = viewport.GetRow(rowIndex);
                    if (objRow != null)
                    {
                        objRow.InvalidateMeasure();
                    }
                    if (action == null)
                    {
                        action = delegate (CellLayout cellLayout)
                        {
                            if ((rowIndex >= cellLayout.Row) && (rowIndex < (cellLayout.Row + cellLayout.RowCount)))
                            {
                                objRow = viewport.GetRow(cellLayout.Row);
                                if (objRow != null)
                                {
                                    objRow.InvalidateMeasure();
                                }
                            }
                        };
                    }
                    Enumerable.ToList<CellLayout>(GetViewportCellLayoutModel(rowViewportIndex, i)).ForEach<CellLayout>(action);
                }
            }
            GcViewport rowHeaderViewport = GetRowHeaderRowsPresenter(rowViewportIndex);
            if (rowHeaderViewport != null)
            {
                rowHeaderViewport.InvalidateMeasure();
                objRow = rowHeaderViewport.GetRow(rowIndex);
                if (objRow != null)
                {
                    objRow.InvalidateMeasure();
                }
                if (action2 == null)
                {
                    action2 = delegate (CellLayout cellLayout)
                    {
                        if ((rowIndex >= cellLayout.Row) && (rowIndex < (cellLayout.Row + cellLayout.RowCount)))
                        {
                            objRow = rowHeaderViewport.GetRow(cellLayout.Row);
                            if (objRow != null)
                            {
                                objRow.InvalidateMeasure();
                            }
                        }
                    };
                }
                Enumerable.ToList<CellLayout>(GetRowHeaderCellLayoutModel(rowViewportIndex)).ForEach<CellLayout>(action2);
            }
        }

        internal void InvalidateViewportRowsLayout()
        {
            if (!IsSuspendInvalidate())
            {
                _cachedViewportRowLayoutModel = null;
                _cachedRowHeaderViewportRowLayoutModel = null;
                _cachedViewportCellLayoutModel = null;
                _cachedRowHeaderCellLayoutModel = null;
                _cachedFloatingObjectLayoutModel = null;
            }
        }

        /// <summary>
        /// Invalidate the specified RowsPresenter.
        /// </summary>
        internal void InvalidateViewportRowsPresenterMeasure(int rowViewportIndex, bool invalidateRowsMeasure)
        {
            if (!IsSuspendInvalidate())
            {
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                if (rowViewportIndex < -1)
                {
                    for (int i = -1; i <= rowViewportCount; i++)
                    {
                        InvalidateViewportRowsPresenterMeasureInternal(i, invalidateRowsMeasure);
                    }
                }
                else
                {
                    InvalidateViewportRowsPresenterMeasureInternal(rowViewportIndex, invalidateRowsMeasure);
                }
            }
        }

        void InvalidateViewportRowsPresenterMeasureInternal(int rowViewportIndex, bool invalidateRowsMeasure)
        {
            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
            for (int i = -1; i <= columnViewportCount; i++)
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(rowViewportIndex, i);
                if (viewportRowsPresenter != null)
                {
                    viewportRowsPresenter.InvalidateBordersMeasureState();
                    viewportRowsPresenter.InvalidateSelectionMeasureState();
                    viewportRowsPresenter.InvalidateRowsMeasureState(false);
                    viewportRowsPresenter.InvalidateMeasure();
                }
            }
            GcViewport rowHeaderRowsPresenter = GetRowHeaderRowsPresenter(rowViewportIndex);
            if (rowHeaderRowsPresenter != null)
            {
                rowHeaderRowsPresenter.InvalidateMeasure();
                rowHeaderRowsPresenter.InvalidateRowsMeasureState(false);
            }
        }

        internal static bool IsAnyCellInRangeLocked(Dt.Cells.Data.Worksheet sheet, int row, int column, int rowCount, int columnCount)
        {
            if (sheet != null)
            {
                int num = (row < 0) ? 0 : row;
                int num2 = (column < 0) ? 0 : column;
                int num3 = (row < 0) ? sheet.RowCount : rowCount;
                int num4 = (column < 0) ? sheet.ColumnCount : columnCount;
                for (int i = 0; i < num3; i++)
                {
                    for (int j = 0; j < num4; j++)
                    {
                        if (sheet.Cells[num + i, num2 + j].ActualLocked)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IsCellEditable(int rowIndex, int columnIndex)
        {
            // hdt 在报表预览中实现单元格不可编辑且图表可拖动
            if (Worksheet != null)
            {
                if (Worksheet.LockCell)
                    return false;
                if (Worksheet.Protect)
                    return !Worksheet.Cells[rowIndex, columnIndex].ActualLocked;
            }
            return true;
        }

        bool IsColumnInViewport(int columnViewport, int column)
        {
            int viewportLeftColumn = GetViewportLeftColumn(columnViewport);
            int viewportRightColumn = GetViewportRightColumn(columnViewport);
            return ((column >= viewportLeftColumn) && (column <= viewportRightColumn));
        }

        bool IsColumnRangeGroupHitTest(Point hitPoint)
        {
            GroupLayout groupLayout = GetGroupLayout();
            if ((Worksheet != null) && (groupLayout.Height > 0.0))
            {
                SheetLayout sheetLayout = GetSheetLayout();
                double headerX = sheetLayout.HeaderX;
                double y = groupLayout.Y;
                double width = sheetLayout.HeaderWidth - 1.0;
                double height = groupLayout.Height - 1.0;
                Rect empty = Rect.Empty;
                if ((width >= 0.0) && (height >= 0.0))
                {
                    empty = new Rect(headerX, y, width, height);
                }
                if (empty.Contains(hitPoint))
                {
                    return true;
                }
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                for (int i = -1; i <= columnViewportCount; i++)
                {
                    double viewportX = sheetLayout.GetViewportX(i);
                    double num8 = groupLayout.Y;
                    double num9 = groupLayout.Height - 1.0;
                    double num10 = sheetLayout.GetViewportWidth(i) - 1.0;
                    Rect rect2 = Rect.Empty;
                    if ((num9 >= 0.0) && (num10 >= 0.0))
                    {
                        rect2 = new Rect(viewportX, num8, num10, num9);
                    }
                    if (rect2.Contains(hitPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool IsCornerRangeGroupHitTest(Point hitPoint)
        {
            GroupLayout groupLayout = GetGroupLayout();
            if ((groupLayout.Width > 0.0) && (groupLayout.Height > 0.0))
            {
                double x = groupLayout.X;
                double y = groupLayout.Y;
                double width = groupLayout.Width - 1.0;
                double height = groupLayout.Height - 1.0;
                Rect empty = Rect.Empty;
                if ((width >= 0.0) && (height >= 0.0))
                {
                    empty = new Rect(x, y, width, height);
                }
                if (empty.Contains(hitPoint))
                {
                    return true;
                }
            }
            return false;
        }

        bool IsEntrieColumnSelection()
        {
            if (Worksheet.Selections.Count != 1)
            {
                return false;
            }
            CellRange range = Worksheet.Selections[0];
            return ((range.Row == -1) && (range.RowCount == -1));
        }

        bool IsEntrieRowSelection()
        {
            if (Worksheet.Selections.Count != 1)
            {
                return false;
            }
            CellRange range = Worksheet.Selections[0];
            return ((range.Column == -1) && (range.ColumnCount == -1));
        }

        bool IsEntrieSheetSelection()
        {
            return (IsEntrieRowSelection() && IsEntrieColumnSelection());
        }

        bool IsInSelectionGripper(Point point)
        {
            if (CachedGripperLocation == null)
            {
                return false;
            }
            Rect topLeft = CachedGripperLocation.TopLeft;
            if (CachedGripperLocation.TopLeft.Expand(10, 10).Contains(point))
            {
                return true;
            }
            Rect bottomRight = CachedGripperLocation.BottomRight;
            return CachedGripperLocation.BottomRight.Expand(10, 10).Contains(point);
        }

        bool IsMouseInDragDropLocation(double mouseX, double mouseY, int rowViewportIndex, int columnViewportIndex, bool isTouching = false)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet != null)
            {
                int row;
                int column;
                int rowCount;
                int columnCount;
                CellRange spanCell = worksheet.GetSpanCell(worksheet.ActiveRowIndex, worksheet.ActiveColumnIndex);
                if (spanCell == null)
                {
                    spanCell = new CellRange(worksheet.ActiveRowIndex, worksheet.ActiveColumnIndex, 1, 1);
                }
                if (worksheet.Selections.Count > 1)
                {
                    return false;
                }
                if (worksheet.Selections.Count == 1)
                {
                    CellRange range2 = worksheet.Selections[0];
                    row = range2.Row;
                    column = range2.Column;
                    rowCount = range2.RowCount;
                    columnCount = range2.ColumnCount;
                }
                else
                {
                    row = spanCell.Row;
                    column = spanCell.Column;
                    rowCount = spanCell.RowCount;
                    columnCount = spanCell.ColumnCount;
                }
                if ((row == -1) && (column == -1))
                {
                    return false;
                }
                if (row == -1)
                {
                    row = 0;
                    rowCount = worksheet.RowCount;
                }
                if (column == -1)
                {
                    column = 0;
                    columnCount = worksheet.ColumnCount;
                }
                SheetLayout sheetLayout = GetSheetLayout();
                RowLayout layout2 = GetViewportRowLayoutModel(rowViewportIndex).Find(row);
                RowLayout layout3 = GetViewportRowLayoutModel(rowViewportIndex).Find((row + rowCount) - 1);
                ColumnLayout layout4 = GetViewportColumnLayoutModel(columnViewportIndex).Find(column);
                ColumnLayout layout5 = GetViewportColumnLayoutModel(columnViewportIndex).Find((column + columnCount) - 1);
                if (((rowCount < worksheet.RowCount) && (layout2 == null)) && (layout3 == null))
                {
                    return false;
                }
                if (((columnCount < worksheet.ColumnCount) && (layout4 == null)) && (layout5 == null))
                {
                    return false;
                }
                double num5 = Math.Ceiling((layout4 == null) ? sheetLayout.GetViewportX(columnViewportIndex) : layout4.X);
                double num6 = Math.Ceiling((layout5 == null) ? ((double)((sheetLayout.GetViewportX(columnViewportIndex) + sheetLayout.GetViewportWidth(columnViewportIndex)) - 1.0)) : ((double)((layout5.X + layout5.Width) - 1.0)));
                double num7 = Math.Ceiling((layout2 == null) ? sheetLayout.GetViewportY(rowViewportIndex) : layout2.Y);
                double num8 = Math.Ceiling((layout3 == null) ? ((double)((sheetLayout.GetViewportY(rowViewportIndex) + sheetLayout.GetViewportHeight(rowViewportIndex)) - 1.0)) : ((double)((layout3.Y + layout3.Height) - 1.0)));
                double num9 = 2.0;
                double num10 = 1.0;
                if (isTouching)
                {
                    num9 = 10.0;
                    num10 = 5.0;
                }
                if (IsEditing && spanCell.Equals(row, column, rowCount, columnCount))
                {
                    if ((mouseY >= (num7 - num9)) && (mouseY <= (num8 + num10)))
                    {
                        if (((layout4 != null) && (mouseX >= (num5 - num9))) && (mouseX <= (num5 - num10)))
                        {
                            return true;
                        }
                        if (((layout5 != null) && (mouseX >= (num6 + num10))) && (mouseX <= (num6 + num10)))
                        {
                            return true;
                        }
                    }
                    if (((mouseX >= (num5 - num9)) && (mouseX <= (num6 + num10))) && ((((layout2 != null) && (mouseY >= (num7 - num9))) && (mouseY <= (num7 - num10))) || (((layout3 != null) && (mouseY >= (num8 + num10))) && (mouseY <= (num8 + num10)))))
                    {
                        return true;
                    }
                }
                else
                {
                    if ((mouseY >= (num7 - num9)) && (mouseY <= (num8 + num10)))
                    {
                        if (((layout4 != null) && (mouseX >= (num5 - num9))) && (mouseX <= num5))
                        {
                            return true;
                        }
                        if (((layout5 != null) && (mouseX >= (num6 - num10))) && (mouseX <= (num6 + num10)))
                        {
                            return true;
                        }
                    }
                    if ((mouseX >= (num5 - num9)) && (mouseX <= (num6 + num10)))
                    {
                        if (((layout2 != null) && (mouseY >= (num7 - num9))) && (mouseY <= num7))
                        {
                            return true;
                        }
                        if (((layout3 != null) && (mouseY >= (num8 - num10))) && (mouseY <= (num8 + num10)))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool IsMouseInDragFillIndicator(double mouseX, double mouseY, int rowViewportIndex, int columnViewportIndex, bool isTouching = false)
        {
            int row;
            int column;
            CellRange spanCell;
            double num7;
            double num8;
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (worksheet == null)
            {
                return false;
            }
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(rowViewportIndex, columnViewportIndex);
            if ((viewportRowsPresenter == null) || !viewportRowsPresenter.SelectionContainer.FocusIndicator.IsFillIndicatorVisible)
            {
                return false;
            }
            FillIndicatorPosition fillIndicatorPosition = viewportRowsPresenter.SelectionContainer.FocusIndicator.FillIndicatorPosition;
            if (worksheet.Selections.Count > 1)
            {
                return false;
            }
            if (worksheet.Selections.Count == 1)
            {
                spanCell = worksheet.Selections[0];
            }
            else
            {
                spanCell = worksheet.GetSpanCell(worksheet.ActiveRowIndex, worksheet.ActiveColumnIndex);
                if (spanCell == null)
                {
                    spanCell = new CellRange(worksheet.ActiveRowIndex, worksheet.ActiveColumnIndex, 1, 1);
                }
            }
            spanCell = AdjustViewportRange(rowViewportIndex, columnViewportIndex, spanCell);
            switch (fillIndicatorPosition)
            {
                case FillIndicatorPosition.BottomRight:
                    row = (spanCell.Row + spanCell.RowCount) - 1;
                    column = (spanCell.Column + spanCell.ColumnCount) - 1;
                    break;

                case FillIndicatorPosition.BottomLeft:
                    row = (spanCell.Row + spanCell.RowCount) - 1;
                    column = spanCell.Column;
                    break;

                default:
                    row = spanCell.Row;
                    column = (spanCell.Column + spanCell.ColumnCount) - 1;
                    break;
            }
            SheetLayout sheetLayout = GetSheetLayout();
            double viewportX = sheetLayout.GetViewportX(columnViewportIndex);
            double viewportY = sheetLayout.GetViewportY(rowViewportIndex);
            double viewportWidth = sheetLayout.GetViewportWidth(columnViewportIndex);
            double viewportHeight = sheetLayout.GetViewportHeight(rowViewportIndex);
            Rect rect = new Rect(viewportX, viewportY, viewportWidth, viewportHeight);
            if (!rect.Contains(new Point(mouseX, mouseY)))
            {
                return false;
            }
            RowLayout layout2 = GetViewportRowLayoutModel(rowViewportIndex).Find(row);
            ColumnLayout layout3 = GetViewportColumnLayoutModel(columnViewportIndex).FindColumn(column);
            if ((layout2 == null) || (layout3 == null))
            {
                return false;
            }
            int num9 = 5;
            double num10 = 3.0;
            switch (fillIndicatorPosition)
            {
                case FillIndicatorPosition.BottomRight:
                    num7 = (layout3.X + layout3.Width) - num10;
                    num8 = (layout2.Y + layout2.Height) - num10;
                    break;

                case FillIndicatorPosition.BottomLeft:
                    num7 = layout3.X + 1.0;
                    num8 = (layout2.Y + layout2.Height) - num10;
                    break;

                default:
                    num7 = (layout3.X + layout3.Width) - num10;
                    num8 = layout2.Y + 1.0;
                    break;
            }
            Point point = new Point(mouseX, mouseY);
            if (IsTouching)
            {
                if (IsEditing)
                {
                    return false;
                }
                double x = Math.Max((double)0.0, (double)(num7 - 15.0));
                double y = Math.Max((double)0.0, (double)(num8 - 5.0));
                Rect rect2 = new Rect(x, y, 30.0, 25.0);
                return rect2.Contains(point);
            }
            Rect rect3 = new Rect(num7, num8, (double)num9, (double)num9);
            if (!IsEditing)
            {
                return rect3.Contains(point);
            }
            Rect empty = Rect.Empty;
            switch (fillIndicatorPosition)
            {
                case FillIndicatorPosition.BottomRight:
                    empty = new Rect(num7, num8, 2.0, 2.0);
                    break;

                case FillIndicatorPosition.TopRight:
                    empty = new Rect(num7, num8, 2.0, (double)num9);
                    break;

                case FillIndicatorPosition.BottomLeft:
                    empty = new Rect(num7, num8, (double)num9, 2.0);
                    break;
            }
            return (rect3.Contains(point) && !empty.Contains(point));
        }


        bool IsNeedRefreshFloatingObjectsMovingResizingContainer(int rowViewport, int columnViewport)
        {
            return true;
        }

        static bool IsPastedInternal(Dt.Cells.Data.Worksheet srcSheet, CellRange srcRange, Dt.Cells.Data.Worksheet destSheet, string clipboadText)
        {
            string str = null;
            if ((srcSheet != null) && (srcRange != null))
            {
                str = srcSheet.GetCsv(srcRange.Row, srcRange.Column, srcRange.RowCount, srcRange.ColumnCount, "\r\n", "\t", "\"", false);
                if (str == string.Empty)
                {
                    str = null;
                }
            }
            return ((((srcSheet != null) && (srcRange != null)) && ((destSheet.Workbook != null) && (destSheet.Workbook == srcSheet.Workbook))) && (str == clipboadText));
        }

        static bool IsRangesEqual(CellRange[] oldSelection, CellRange[] newSelection)
        {
            int num = (oldSelection == null) ? 0 : oldSelection.Length;
            int num2 = (newSelection == null) ? 0 : newSelection.Length;
            bool flag = true;
            if (num == num2)
            {
                for (int i = 0; i < num; i++)
                {
                    if (!object.Equals(oldSelection[i], newSelection[i]))
                    {
                        return false;
                    }
                }
                return flag;
            }
            return false;
        }

        bool IsRowInViewport(int rowViewport, int row)
        {
            int viewportTopRow = GetViewportTopRow(rowViewport);
            int viewportBottomRow = GetViewportBottomRow(rowViewport);
            return ((row >= viewportTopRow) && (row <= viewportBottomRow));
        }

        bool IsRowRangeGroupHitTest(Point hitPoint)
        {
            GroupLayout groupLayout = GetGroupLayout();
            if ((Worksheet != null) && (groupLayout.Width > 0.0))
            {
                SheetLayout sheetLayout = GetSheetLayout();
                double x = groupLayout.X;
                double headerY = sheetLayout.HeaderY;
                double width = groupLayout.Width - 1.0;
                double height = sheetLayout.HeaderHeight - 1.0;
                Rect empty = Rect.Empty;
                if ((width >= 0.0) && (height >= 0.0))
                {
                    empty = new Rect(x, headerY, width, height);
                }
                if (empty.Contains(hitPoint))
                {
                    return true;
                }
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                for (int i = -1; i <= rowViewportCount; i++)
                {
                    double num7 = groupLayout.X;
                    double viewportY = sheetLayout.GetViewportY(i);
                    double num9 = groupLayout.Width - 1.0;
                    double num10 = sheetLayout.GetViewportHeight(i) - 1.0;
                    Rect rect2 = Rect.Empty;
                    if ((num9 >= 0.0) && (num10 >= 0.0))
                    {
                        rect2 = new Rect(num7, viewportY, num9, num10);
                    }
                    if (rect2.Contains(hitPoint))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool IsSuspendInvalidate()
        {
            return (_suspendViewInvalidate > 0);
        }

        internal static bool IsValidRange(int row, int column, int rowCount, int columnCount, int maxRowCount, int maxColumnCount)
        {
            if (((-1 <= row) && (row < maxRowCount)) && ((-1 <= column) && (column < maxColumnCount)))
            {
                if ((row == -1) && (column == -1))
                {
                    return true;
                }
                if (row == -1)
                {
                    if ((columnCount != 0) && ((column + columnCount) <= maxColumnCount))
                    {
                        return true;
                    }
                }
                else if (column == -1)
                {
                    if ((rowCount != 0) && ((row + rowCount) <= maxRowCount))
                    {
                        return true;
                    }
                }
                else if (((columnCount != 0) && ((column + columnCount) <= maxColumnCount)) && ((rowCount != 0) && ((row + rowCount) <= maxRowCount)))
                {
                    return true;
                }
            }
            return false;
        }

        double MeasureCellText(Cell cell, int row, int column, Size maxSize, FontFamily fontFamily, object textFormattingMode, bool useLayoutRounding)
        {
            double num = 0.0;
            Size size2 = MeasureHelper.ConvertTextSizeToExcelCellSize(MeasureHelper.MeasureTextInCell(cell, maxSize, 1.0, fontFamily, textFormattingMode, useLayoutRounding), 1.0);
            num = Math.Max(num, size2.Width);
            if (!ContainsFilterButton(row, column, cell.SheetArea))
            {
                return num;
            }
            switch (cell.ToHorizontalAlignment())
            {
                case HorizontalAlignment.Right:
                    return num;

                case HorizontalAlignment.Center:
                    return Math.Max(num, size2.Width + 36.0);
            }
            return Math.Max(num, size2.Width + 16.0);
        }

        /// <summary>
        /// This method measures the layout size required for child elements and determines a size for the <see cref="T:System.Windows.FrameworkElement" />-derived class, when overridden in a derived class.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will base the size on the available content.</param>
        /// <returns>
        /// The size that this element determines it needs during layout, based on its calculations of child element sizes.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            double viewportX;
            double viewportY;
            if (AvailableSize != availableSize)
            {
                AvailableSize = availableSize;
                InvalidateLayout();
            }
            if (!IsWorking)
            {
                SaveHitInfo(null);
            }
            ViewportInfo viewportInfo = GetViewportInfo();
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            int rowViewportCount = viewportInfo.RowViewportCount;
            SheetLayout sheetLayout = GetSheetLayout();
            UpdateFreezeLines();
            if (!base.Children.Contains(TrackersContainer))
            {
                base.Children.Add(TrackersContainer);
            }
            TrackersContainer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (!base.Children.Contains(CursorsContainer))
            {
                base.Children.Add(CursorsContainer);
            }
            CursorsContainer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            if (_cornerPresenter == null)
            {
                _cornerPresenter = new GcHeaderCornerViewport(this);
            }
            _cornerPresenter.Location = new Point(sheetLayout.HeaderX, sheetLayout.HeaderY);
            if ((sheetLayout.HeaderWidth > 0.0) && (sheetLayout.HeaderHeight > 0.0))
            {
                if (!base.Children.Contains(_cornerPresenter))
                {
                    base.Children.Add(_cornerPresenter);
                }
                _cornerPresenter.InvalidateMeasure();
                _cornerPresenter.Measure(new Size(sheetLayout.HeaderWidth, sheetLayout.HeaderHeight));
            }
            else
            {
                base.Children.Remove(_cornerPresenter);
                _cornerPresenter = null;
            }
            if ((_columnHeaderPresenters != null) && ((Worksheet == null) || (_columnHeaderPresenters.Length != (columnViewportCount + 2))))
            {
                foreach (GcViewport viewport in _columnHeaderPresenters)
                {
                    base.Children.Remove(viewport);
                }
                _columnHeaderPresenters = null;
            }
            if (_columnHeaderPresenters == null)
            {
                _columnHeaderPresenters = new GcColumnHeaderViewport[columnViewportCount + 2];
            }
            if (sheetLayout.HeaderHeight > 0.0)
            {
                for (int j = -1; j <= columnViewportCount; j++)
                {
                    viewportX = sheetLayout.GetViewportX(j);
                    double viewportWidth = sheetLayout.GetViewportWidth(j);
                    if (_columnHeaderPresenters[j + 1] == null)
                    {
                        _columnHeaderPresenters[j + 1] = new GcColumnHeaderViewport(this);
                    }
                    GcViewport viewport2 = _columnHeaderPresenters[j + 1];
                    viewport2.Location = new Point(viewportX, sheetLayout.HeaderY);
                    viewport2.ColumnViewportIndex = j;
                    if (viewportWidth > 0.0)
                    {
                        if (!base.Children.Contains(viewport2))
                        {
                            base.Children.Add(viewport2);
                        }
                        viewport2.InvalidateMeasure();
                        viewport2.Measure(new Size(viewportWidth, sheetLayout.HeaderHeight));
                    }
                    else
                    {
                        base.Children.Remove(viewport2);
                        _columnHeaderPresenters[j + 1] = null;
                    }
                }
            }
            else
            {
                foreach (GcViewport viewport3 in _columnHeaderPresenters)
                {
                    base.Children.Remove(viewport3);
                }
            }
            if ((_rowHeaderPresenters != null) && ((Worksheet == null) || (_rowHeaderPresenters.Length != (rowViewportCount + 2))))
            {
                foreach (GcViewport viewport4 in _rowHeaderPresenters)
                {
                    base.Children.Remove(viewport4);
                }
                _rowHeaderPresenters = null;
            }
            if (_rowHeaderPresenters == null)
            {
                _rowHeaderPresenters = new GcRowHeaderViewport[rowViewportCount + 2];
            }
            if (sheetLayout.HeaderWidth > 0.0)
            {
                for (int k = -1; k <= rowViewportCount; k++)
                {
                    double viewportHeight = sheetLayout.GetViewportHeight(k);
                    viewportY = sheetLayout.GetViewportY(k);
                    if (_rowHeaderPresenters[k + 1] == null)
                    {
                        _rowHeaderPresenters[k + 1] = new GcRowHeaderViewport(this);
                    }
                    GcViewport viewport5 = _rowHeaderPresenters[k + 1];
                    viewport5.Location = new Point(sheetLayout.HeaderX, viewportY);
                    viewport5.RowViewportIndex = k;
                    if (viewportHeight > 0.0)
                    {
                        if (!base.Children.Contains(viewport5))
                        {
                            base.Children.Add(viewport5);
                        }
                        viewport5.InvalidateMeasure();
                        viewport5.Measure(new Size(sheetLayout.HeaderWidth, viewportHeight));
                    }
                    else
                    {
                        base.Children.Remove(viewport5);
                        _rowHeaderPresenters[k + 1] = null;
                    }
                }
            }
            else
            {
                foreach (GcViewport viewport6 in _rowHeaderPresenters)
                {
                    base.Children.Remove(viewport6);
                }
            }
            if ((_viewportPresenters != null) && (((Worksheet == null) || (_viewportPresenters.GetUpperBound(0) != (rowViewportCount + 1))) || (_viewportPresenters.GetUpperBound(1) != (columnViewportCount + 1))))
            {
                GcViewport[,] viewportArray5 = _viewportPresenters;
                int upperBound = viewportArray5.GetUpperBound(0);
                int num18 = viewportArray5.GetUpperBound(1);
                for (int m = viewportArray5.GetLowerBound(0); m <= upperBound; m++)
                {
                    for (int n = viewportArray5.GetLowerBound(1); n <= num18; n++)
                    {
                        GcViewport viewport7 = viewportArray5[m, n];
                        if (viewport7 != null)
                        {
                            base.Children.Remove(viewport7);
                        }
                    }
                }
                _viewportPresenters = null;
            }
            if (_viewportPresenters == null)
            {
                _viewportPresenters = new GcViewport[rowViewportCount + 2, columnViewportCount + 2];
            }
            for (int i = -1; i <= columnViewportCount; i++)
            {
                double width = sheetLayout.GetViewportWidth(i);
                viewportX = sheetLayout.GetViewportX(i);
                for (int num11 = -1; num11 <= rowViewportCount; num11++)
                {
                    double height = sheetLayout.GetViewportHeight(num11);
                    viewportY = sheetLayout.GetViewportY(num11);
                    if (_viewportPresenters[num11 + 1, i + 1] == null)
                    {
                        _viewportPresenters[num11 + 1, i + 1] = new GcViewport(this);
                    }
                    GcViewport viewport8 = _viewportPresenters[num11 + 1, i + 1];
                    viewport8.Location = new Point(viewportX, viewportY);
                    viewport8.ColumnViewportIndex = i;
                    viewport8.RowViewportIndex = num11;
                    if ((width > 0.0) && (height > 0.0))
                    {
                        if (!base.Children.Contains(viewport8))
                        {
                            base.Children.Add(viewport8);
                        }
                        viewport8.InvalidateMeasure();
                        viewport8.InvalidateRowsMeasureState(false);
                        viewport8.Measure(new Size(width, height));
                    }
                    else
                    {
                        base.Children.Remove(viewport8);
                        _viewportPresenters[num11 + 1, i + 1] = null;
                    }
                }
            }
            MeasureRangeGroup(rowViewportCount, columnViewportCount, sheetLayout);
            return AvailableSize;
        }

        internal void MeasureRangeGroup(int rowPaneCount, int columnPaneCount, SheetLayout layout)
        {
            GroupLayout groupLayout = GetGroupLayout();
            if ((_rowGroupPresenters != null) && ((Worksheet == null) || (_rowGroupPresenters.Length != rowPaneCount)))
            {
                foreach (GcRangeGroup group in _rowGroupPresenters)
                {
                    base.Children.Remove(group);
                }
                _rowGroupPresenters = null;
            }
            if (_rowGroupPresenters == null)
            {
                _rowGroupPresenters = new GcRangeGroup[rowPaneCount + 2];
            }
            if (groupLayout.Width > 0.0)
            {
                for (int i = -1; i <= rowPaneCount; i++)
                {
                    double viewportY = layout.GetViewportY(i);
                    double viewportHeight = layout.GetViewportHeight(i);
                    if (_rowGroupPresenters[i + 1] == null)
                    {
                        GcRangeGroup group2 = new GcRangeGroup(this);
                        _rowGroupPresenters[i + 1] = group2;
                    }
                    GcRangeGroup group3 = _rowGroupPresenters[i + 1];
                    group3.Orientation = Orientation.Horizontal;
                    group3.ViewportIndex = i;
                    group3.Location = new Point(groupLayout.X, viewportY);
                    if (viewportHeight > 0.0)
                    {
                        if (!base.Children.Contains(group3))
                        {
                            base.Children.Add(group3);
                        }
                        group3.InvalidateMeasure();
                        group3.Measure(new Size(groupLayout.Width, viewportHeight));
                    }
                    else
                    {
                        base.Children.Remove(group3);
                        _rowGroupPresenters[i + 1] = null;
                    }
                }
            }
            else
            {
                GcRangeGroup[] groupArray2 = _rowGroupPresenters;
                for (int j = 0; j < groupArray2.Length; j++)
                {
                    GcGroupBase base2 = groupArray2[j];
                    base.Children.Remove(base2);
                }
            }
            if ((_columnGroupPresenters != null) && ((Worksheet == null) || (_columnGroupPresenters.Length != columnPaneCount)))
            {
                foreach (GcRangeGroup group4 in _columnGroupPresenters)
                {
                    base.Children.Remove(group4);
                }
                _columnGroupPresenters = null;
            }
            if (_columnGroupPresenters == null)
            {
                _columnGroupPresenters = new GcRangeGroup[columnPaneCount + 2];
            }
            if (groupLayout.Height > 0.0)
            {
                for (int k = -1; k <= columnPaneCount; k++)
                {
                    double viewportX = layout.GetViewportX(k);
                    double viewportWidth = layout.GetViewportWidth(k);
                    if (_columnGroupPresenters[k + 1] == null)
                    {
                        GcRangeGroup group5 = new GcRangeGroup(this);
                        _columnGroupPresenters[k + 1] = group5;
                    }
                    GcRangeGroup group6 = _columnGroupPresenters[k + 1];
                    group6.Orientation = Orientation.Vertical;
                    group6.ViewportIndex = k;
                    group6.Location = new Point(viewportX, groupLayout.Y);
                    if (viewportWidth > 0.0)
                    {
                        if (!base.Children.Contains(group6))
                        {
                            base.Children.Add(group6);
                        }
                        group6.InvalidateMeasure();
                        group6.Measure(new Size(viewportWidth, groupLayout.Height));
                    }
                    else
                    {
                        base.Children.Remove(group6);
                        _columnGroupPresenters[k + 1] = null;
                    }
                }
            }
            else
            {
                GcRangeGroup[] groupArray4 = _columnGroupPresenters;
                for (int m = 0; m < groupArray4.Length; m++)
                {
                    GcGroupBase base3 = groupArray4[m];
                    base.Children.Remove(base3);
                }
            }
            if (_rowGroupHeaderPresenter == null)
            {
                _rowGroupHeaderPresenter = new GcRangeGroupHeader(this);
            }
            _rowGroupHeaderPresenter.Orientation = Orientation.Horizontal;
            _rowGroupHeaderPresenter.Location = new Point(groupLayout.X, groupLayout.Y + groupLayout.Height);
            if (groupLayout.Width > 0.0)
            {
                if (!base.Children.Contains(_rowGroupHeaderPresenter))
                {
                    base.Children.Add(_rowGroupHeaderPresenter);
                }
                _rowGroupHeaderPresenter.InvalidateMeasure();
                _rowGroupHeaderPresenter.Measure(new Size(groupLayout.Width, layout.HeaderHeight));
            }
            else
            {
                base.Children.Remove(_rowGroupHeaderPresenter);
                _rowGroupHeaderPresenter = null;
            }
            if (_columnGroupHeaderPresenter == null)
            {
                _columnGroupHeaderPresenter = new GcRangeGroupHeader(this);
            }
            _columnGroupHeaderPresenter.Orientation = Orientation.Vertical;
            _columnGroupHeaderPresenter.Location = new Point(groupLayout.X + groupLayout.Width, groupLayout.Y);
            if (groupLayout.Height > 0.0)
            {
                if (!base.Children.Contains(_columnGroupHeaderPresenter))
                {
                    base.Children.Add(_columnGroupHeaderPresenter);
                }
                _columnGroupHeaderPresenter.InvalidateMeasure();
                _columnGroupHeaderPresenter.Measure(new Size(layout.HeaderWidth, groupLayout.Height));
            }
            else
            {
                base.Children.Remove(_columnGroupHeaderPresenter);
                _columnGroupHeaderPresenter = null;
            }
            if (_groupCornerPresenter == null)
            {
                _groupCornerPresenter = new GcRangeGroupCorner(this);
            }
            _groupCornerPresenter.Location = new Point(groupLayout.X, groupLayout.Y);
            if ((groupLayout.Width > 0.0) && (groupLayout.Height > 0.0))
            {
                if (!base.Children.Contains(_groupCornerPresenter))
                {
                    base.Children.Add(_groupCornerPresenter);
                }
                _groupCornerPresenter.InvalidateMeasure();
                _groupCornerPresenter.Measure(new Size(groupLayout.Width, groupLayout.Height));
            }
            else
            {
                base.Children.Remove(_groupCornerPresenter);
                _groupCornerPresenter = null;
            }
        }

        void MoveActiveCellToBottom()
        {
            CellRange activeSelection = GetActiveSelection();
            if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
            {
                activeSelection = Worksheet.Selections[0];
            }
            if ((Worksheet.ActiveRowIndex != ((activeSelection.Row + activeSelection.RowCount) - 1)) || (Worksheet.ActiveColumnIndex != ((activeSelection.Column + activeSelection.ColumnCount) - 1)))
            {
                Worksheet.Workbook.SuspendEvent();
                Worksheet.SetActiveCell((activeSelection.Row + activeSelection.RowCount) - 1, (activeSelection.Column + activeSelection.ColumnCount) - 1, false);
                Worksheet.Workbook.ResumeEvent();
            }
        }

        bool NeedRefresh(int rowViewport, int columnViewport)
        {
            bool flag = false;
            bool flag2 = false;
            ViewportInfo viewportInfo = GetViewportInfo();
            if (IsDragFillWholeColumns)
            {
                if (Worksheet.FrozenRowCount == 0)
                {
                    flag = (rowViewport == _dragToRowViewport) || (rowViewport == viewportInfo.RowViewportCount);
                }
                else if (_dragToRowViewport >= 1)
                {
                    flag = ((rowViewport == -1) || (rowViewport == viewportInfo.RowViewportCount)) || (rowViewport == _dragToRowViewport);
                }
                else
                {
                    flag = ((rowViewport == -1) || (rowViewport == viewportInfo.RowViewportCount)) || (rowViewport == 0);
                }
                flag2 = ((columnViewport == _dragFillStartLeftColumnViewport) || (columnViewport == _dragFillStartRightColumnViewport)) || (columnViewport == _dragToColumnViewport);
            }
            else if (IsDragFillWholeRows)
            {
                if (Worksheet.FrozenColumnCount == 0)
                {
                    flag2 = (columnViewport == _dragToColumnViewport) || (columnViewport == viewportInfo.ColumnViewportCount);
                }
                else if (_dragToColumnViewport >= 1)
                {
                    flag2 = ((columnViewport == -1) || (columnViewport == viewportInfo.ColumnViewportCount)) || (columnViewport == _dragToColumnViewport);
                }
                else
                {
                    flag2 = ((columnViewport == -1) || (columnViewport == viewportInfo.ColumnViewportCount)) || (columnViewport == 0);
                }
                flag = ((rowViewport == _dragFillStartTopRowViewport) || (rowViewport == _dragFillStartBottomRowViewport)) || (rowViewport == _dragToRowViewport);
            }
            else
            {
                flag = ((rowViewport >= _dragFillStartTopRowViewport) && (rowViewport <= _dragFillStartBottomRowViewport)) || (rowViewport == _dragToRowViewport);
                flag2 = ((columnViewport >= _dragFillStartLeftColumnViewport) && (columnViewport <= _dragFillStartRightColumnViewport)) || (columnViewport == _dragToColumnViewport);
            }
            return (flag && flag2);
        }

        internal void OnActiveSheetChanged()
        {
            if (EditorConnector.IsFormulaSelectionBegined)
            {
                EditorConnector.UpdateSelectionItemsForCurrentSheet();
                EditorConnector.ActivateEditor = true;
            }
            Invalidate();
        }

        void OnEditedCellChanged(object sender, CellChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, "Value"))
            {
                RaiseValueChanged(e.Row, e.Column);
            }
        }

        void OnHorizontalSelectionTick(bool needIncrease)
        {
            if (_hScrollable)
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                int viewportLeftColumn = GetViewportLeftColumn(savedHitTestInformation.ColumnViewportIndex);
                int viewportRightColumn = GetViewportRightColumn(savedHitTestInformation.ColumnViewportIndex);
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                if (needIncrease)
                {
                    if (viewportRightColumn < ((worksheet.ColumnCount - worksheet.FrozenTrailingColumnCount) - 1))
                    {
                        SetViewportLeftColumn(savedHitTestInformation.ColumnViewportIndex, viewportLeftColumn + 1);
                        base.InvalidateMeasure();
                    }
                    else
                    {
                        ColumnLayout layout = Enumerable.Last<ColumnLayout>((IEnumerable<ColumnLayout>)GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex));
                        if (layout != null)
                        {
                            SheetLayout sheetLayout = GetSheetLayout();
                            double num3 = sheetLayout.GetViewportX(savedHitTestInformation.ColumnViewportIndex) + sheetLayout.GetViewportWidth(savedHitTestInformation.ColumnViewportIndex);
                            if ((layout.X + layout.Width) >= num3)
                            {
                                SetViewportLeftColumn(savedHitTestInformation.ColumnViewportIndex, viewportLeftColumn + 1);
                                base.InvalidateMeasure();
                            }
                        }
                    }
                }
                else if ((viewportLeftColumn - 1) >= 0)
                {
                    SetViewportLeftColumn(savedHitTestInformation.ColumnViewportIndex, viewportLeftColumn - 1);
                    base.InvalidateMeasure();
                }
                if (_formulaSelectionFeature.IsDragging)
                {
                    _formulaSelectionFeature.ContinueDragging();
                }
                if (IsSelectingCells)
                {
                    ContinueCellSelecting();
                }
                if (IsSelectingColumns)
                {
                    ContinueColumnSelecting();
                }
                if (IsTouchSelectingCells)
                {
                    ContinueTouchSelectingCells(MousePosition);
                }
                if (IsTouchSelectingColumns)
                {
                    ContinueColumnSelecting();
                }
                if (IsTouchDragFilling)
                {
                    ContinueTouchDragFill();
                }
                if (IsTouchDrapDropping)
                {
                    ContinueTouchDragDropping();
                }
                if (IsDragDropping)
                {
                    ContinueDragDropping();
                }
                if (IsDraggingFill)
                {
                    ContinueDragFill();
                }
                if (IsMovingFloatingOjects)
                {
                    ContinueFloatingObjectsMoving();
                }
                if (IsResizingFloatingObjects)
                {
                    ContinueFloatingObjectsResizing();
                }
            }
        }

        void OnVerticalSelectionTick(bool needIncrease)
        {
            if (_vScrollable)
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                int viewportTopRow = GetViewportTopRow(savedHitTestInformation.RowViewportIndex);
                int viewportBottomRow = GetViewportBottomRow(savedHitTestInformation.RowViewportIndex);
                Dt.Cells.Data.Worksheet worksheet = Worksheet;
                if (needIncrease)
                {
                    if (viewportBottomRow < ((Worksheet.RowCount - worksheet.FrozenTrailingRowCount) - 1))
                    {
                        SetViewportTopRow(savedHitTestInformation.RowViewportIndex, viewportTopRow + 1);
                        base.InvalidateMeasure();
                    }
                    else
                    {
                        RowLayout layout = Enumerable.Last<RowLayout>((IEnumerable<RowLayout>)GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex));
                        if (layout != null)
                        {
                            SheetLayout sheetLayout = GetSheetLayout();
                            double num3 = sheetLayout.GetViewportY(savedHitTestInformation.RowViewportIndex) + sheetLayout.GetViewportHeight(savedHitTestInformation.RowViewportIndex);
                            if ((layout.Y + layout.Height) >= num3)
                            {
                                SetViewportTopRow(savedHitTestInformation.RowViewportIndex, viewportTopRow + 1);
                                base.InvalidateMeasure();
                            }
                        }
                    }
                }
                else if ((viewportTopRow - 1) >= 0)
                {
                    SetViewportTopRow(savedHitTestInformation.RowViewportIndex, viewportTopRow - 1);
                    base.InvalidateMeasure();
                }
                if (_formulaSelectionFeature.IsDragging)
                {
                    _formulaSelectionFeature.ContinueDragging();
                }
                if (IsSelectingCells)
                {
                    ContinueCellSelecting();
                }
                if (IsSelectingRows)
                {
                    ContinueRowSelecting();
                }
                if (IsTouchSelectingCells)
                {
                    ContinueTouchSelectingCells(MousePosition);
                }
                if (IsTouchSelectingRows)
                {
                    ContinueRowSelecting();
                }
                if (IsTouchDragFilling)
                {
                    ContinueTouchDragFill();
                }
                if (IsDragDropping)
                {
                    ContinueDragDropping();
                }
                if (IsTouchDrapDropping)
                {
                    ContinueTouchDragDropping();
                }
                if (IsDraggingFill)
                {
                    ContinueDragFill();
                }
                if (IsMovingFloatingOjects)
                {
                    ContinueFloatingObjectsMoving();
                }
                if (IsResizingFloatingObjects)
                {
                    ContinueFloatingObjectsResizing();
                }
            }
        }

        void PrepareCellEditing()
        {
            if (IsCellEditable(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex))
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
                if (viewportRowsPresenter != null)
                {
                    viewportRowsPresenter.PrepareCellEditing(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
                }
            }
        }

        bool PreviewLeaveCell(int row, int column)
        {
            return (((row != Worksheet.ActiveRowIndex) || (column != Worksheet.ActiveColumnIndex)) && RaiseLeaveCell(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, row, column));
        }

        internal void ProcessIMEInputInWinRT(KeyRoutedEventArgs e)
        {
            if (AllowEnterEditing(e) && (!IsEditing && IsCellEditable(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex)))
            {
                if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                {
                    RefreshSelection();
                }
                InputDeviceType = Dt.Cells.UI.InputDeviceType.Keyboard;
                UpdateCursorType();
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
                if (viewportRowsPresenter != null)
                {
                    CoreWindow.GetForCurrentThread().ReleasePointerCapture();
                    EditingViewport = viewportRowsPresenter;
                    bool flag2 = false;
                    flag2 = viewportRowsPresenter.StartTextInputForWinRT(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, EditorStatus.Enter);
                    IsEditing = flag2;
                    _host.IsTabStop = !flag2;
                    if (!flag2)
                    {
                        EditingViewport = null;
                    }
                }
            }
        }

        internal virtual void ProcessKeyDown(KeyRoutedEventArgs e)
        {
            bool flag2;
            bool flag3;
            bool flag4;
            bool flag = AllowEnterEditing(e);
            if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
            {
                RefreshSelection();
            }
            InputDeviceType = Dt.Cells.UI.InputDeviceType.Keyboard;
            UpdateCursorType();
            VirtualKey keyCode = e.Key;
            if (keyCode == VirtualKey.Enter)
            {
                Dt.Cells.Data.Worksheet activeSheet = (_host as Excel).ActiveSheet;
                if (EditorInfo.Sheet != activeSheet)
                {
                    int index = activeSheet.Workbook.Sheets.IndexOf(EditorInfo.Sheet);
                    activeSheet.Workbook.ActiveSheetIndex = index;
                    StopCellEditing(false);
                    EditorConnector.ActivateEditor = false;
                    e.Handled = true;
                    return;
                }
            }
            KeyboardHelper.GetMetaKeyState(out flag3, out flag2, out flag4);
            VirtualKeyModifiers none = VirtualKeyModifiers.None;
            if (flag2)
            {
                none |= VirtualKeyModifiers.Control;
            }
            if (flag3)
            {
                none |= VirtualKeyModifiers.None | VirtualKeyModifiers.Shift;
            }
            if (flag4)
            {
                none |= VirtualKeyModifiers.Menu;
            }
            KeyStroke ks = new KeyStroke(keyCode, none, false);
            if (ProcessKeyDownOnFloatingObjectSelected(ks))
            {
                e.Handled = true;
            }
            else
            {
                if (KeyMap.ContainsKey(ks))
                {
                    SpreadAction action = KeyMap[ks];
                    if (action != null)
                    {
                        CloseDragFillPopup();
                        ActionEventArgs args = new ActionEventArgs();
                        action(this, args);
                        if (args.Handled)
                        {
                            e.Handled = true;
                        }
                    }
                }
                if (IsDragDropping)
                {
                    SwitchDragDropIndicator();
                }
                else
                {
                    if (!IsEditing && flag)
                    {
                        StartCellEditing(true, null, EditorStatus.Enter);
                    }
                    if (!IsEditing)
                    {
                        FocusInternal();
                    }
                }
            }
        }

        bool ProcessKeyDownOnFloatingObjectSelected(KeyStroke ks)
        {
            if ((((ks.KeyCode != VirtualKey.Z) || (ks.Modifiers != VirtualKeyModifiers.Control)) && ((ks.KeyCode != VirtualKey.Y) || (ks.Modifiers != VirtualKeyModifiers.Control))) && (HasSelectedFloatingObject() || FloatingObjectKeyMap.ContainsKey(ks)))
            {
                SpreadAction action = null;
                if (FloatingObjectKeyMap.TryGetValue(ks, out action) && (action != null))
                {
                    ActionEventArgs e = new ActionEventArgs();
                    action(this, e);
                    return e.Handled;
                }
            }
            return false;
        }

        internal void ProcessKeyUp(KeyRoutedEventArgs e)
        {
            UpdateCursorType();
            if (!IsEditing)
            {
                bool flag;
                bool flag2;
                VirtualKey keyCode = e.Key;
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                VirtualKeyModifiers none = VirtualKeyModifiers.None;
                if (flag2)
                {
                    none |= VirtualKeyModifiers.Control;
                }
                if (flag)
                {
                    none |= VirtualKeyModifiers.None | VirtualKeyModifiers.Shift;
                }
                KeyStroke stroke = new KeyStroke(keyCode, none, true);
                if (KeyMap.ContainsKey(stroke))
                {
                    SpreadAction action = KeyMap[stroke];
                    if (action != null)
                    {
                        ActionEventArgs args = new ActionEventArgs();
                        action(this, args);
                        if (args.Handled)
                        {
                            e.Handled = true;
                        }
                    }
                }
            }
            if (IsDragDropping)
            {
                SwitchDragDropIndicator();
            }
        }

        void ProcessMouseDownDataValidationListButton(DataValidationListButtonInfo dataBtnInfo)
        {
            if (!RaiseDataValidationListPopupOpening(dataBtnInfo.Row, dataBtnInfo.Column) && (dataBtnInfo != null))
            {
                _dataValidationPopUpHelper = new PopupHelper(DataValidationListPopUp);
                DataValidationListBox dvListBox = new DataValidationListBox();
                dvListBox.Background = new SolidColorBrush(Colors.White);
                object[] array = dataBtnInfo.Validator.GetValidList(Worksheet, dataBtnInfo.Row, dataBtnInfo.Column);
                if ((dataBtnInfo.Validator.Type == CriteriaType.List) && (dataBtnInfo.Validator.Value1 != null))
                {
                    string str = (string)(dataBtnInfo.Validator.Value1 as string);
                    if (!str.StartsWith("="))
                    {
                        string listSeparator = CultureInfo.InvariantCulture.TextInfo.ListSeparator;
                        string[] strArray = new string[] { listSeparator, @"\0" };
                        string[] strArray2 = str.Split(strArray, (StringSplitOptions)StringSplitOptions.None);
                        if (strArray2 != null)
                        {
                            List<string> list = new List<string>();
                            foreach (string str3 in strArray2)
                            {
                                list.Add(str3.Trim(new char[] { ' ' }));
                            }
                            array = list.ToArray();
                        }
                    }
                }
                object obj2 = Worksheet.GetValue(dataBtnInfo.Row, dataBtnInfo.Column);
                if (array != null)
                {
                    int index = -1;
                    index = Array.IndexOf<object>(array, obj2);
                    float zoomFactor = Worksheet.ZoomFactor;
                    for (int i = 0; i < array.Length; i++)
                    {
                        object obj3 = array[i];
                        DataValidationListItem item = new DataValidationListItem
                        {
                            Value = obj3,
                            TextSize = 14f * zoomFactor
                        };
                        dvListBox.Items.Add(item);
                    }
                    dvListBox.SelectedIndex = index;
                    dvListBox.Command = new SetValueCommand(this, dataBtnInfo);
                }
                dvListBox.Popup = _dataValidationPopUpHelper;
                int row = dataBtnInfo.Row;
                int column = dataBtnInfo.Column;
                CellRange range = Worksheet.GetSpanCell(row, column, dataBtnInfo.SheetArea);
                if (range != null)
                {
                    row = (range.Row + range.RowCount) - 1;
                    column = (range.Column + range.ColumnCount) - 1;
                }
                RowLayout columnHeaderRowLayout = null;
                ColumnLayout layout2 = GetViewportColumnLayoutModel(dataBtnInfo.ColumnViewportIndex).Find(column);
                if (dataBtnInfo.SheetArea == SheetArea.ColumnHeader)
                {
                    columnHeaderRowLayout = GetColumnHeaderRowLayout(row);
                }
                else if (dataBtnInfo.SheetArea == SheetArea.Cells)
                {
                    columnHeaderRowLayout = GetViewportRowLayoutModel(dataBtnInfo.RowViewportIndex).Find(row);
                }
                if ((columnHeaderRowLayout != null) && (layout2 != null))
                {
                    double num6 = Math.Min(16.0, layout2.Width);
                    if (dataBtnInfo.Column == dataBtnInfo.DisplayColumn)
                    {
                        dvListBox.Width = Math.Max((double)60.0, (double)(GetDataValidationListDropdownWidth(row, dataBtnInfo.Column, dataBtnInfo.ColumnViewportIndex) + 5.0));
                        dvListBox.MaxHeight = 200.0;
                        _dataValidationPopUpHelper.ShowAsModal(this, dvListBox, new Point(layout2.X + layout2.Width, columnHeaderRowLayout.Y + columnHeaderRowLayout.Height));
                    }
                    else
                    {
                        dvListBox.Width = Math.Max((double)60.0, (double)((GetDataValidationListDropdownWidth(row, dataBtnInfo.Column, dataBtnInfo.ColumnViewportIndex) + 5.0) + 16.0));
                        dvListBox.MaxHeight = 200.0;
                        _dataValidationPopUpHelper.ShowAsModal(this, dvListBox, new Point((layout2.X + layout2.Width) + num6, columnHeaderRowLayout.Y + columnHeaderRowLayout.Height));
                    }
                }
            }
        }

        void ProcessMouseDownFilterButton(FilterButtonInfo filterBtnInfo)
        {
            if (!RaiseFilterPopupOpening(filterBtnInfo.Row, filterBtnInfo.Column) && (filterBtnInfo != null))
            {
                _filterPopupHelper = new PopupHelper(FilterPopup);
                ColumnDropDownList dropdown = new ColumnDropDownList();
                AddSortItems(dropdown, filterBtnInfo);
                dropdown.Items.Add(new SeparatorDropDownItemControl());
                AutoFilterDropDownItemControl control = CreateAutoFilter(filterBtnInfo);
                dropdown.Items.Add(control);
                dropdown.Popup = _filterPopupHelper;
                int row = filterBtnInfo.Row;
                int column = filterBtnInfo.Column;
                CellRange range = Worksheet.GetSpanCell(row, column, filterBtnInfo.SheetArea);
                if (range != null)
                {
                    row = (range.Row + range.RowCount) - 1;
                    column = (range.Column + range.ColumnCount) - 1;
                }
                RowLayout columnHeaderRowLayout = null;
                ColumnLayout layout2 = GetViewportColumnLayoutModel(filterBtnInfo.ColumnViewportIndex).Find(column);
                if (filterBtnInfo.SheetArea == SheetArea.ColumnHeader)
                {
                    columnHeaderRowLayout = GetColumnHeaderRowLayout(row);
                }
                else if (filterBtnInfo.SheetArea == SheetArea.Cells)
                {
                    columnHeaderRowLayout = GetViewportRowLayoutModel(filterBtnInfo.RowViewportIndex).Find(row);
                }
                if ((columnHeaderRowLayout != null) && (layout2 != null))
                {
                    _filterPopupHelper.ShowAsModal(this, dropdown, new Point(layout2.X + layout2.Width, columnHeaderRowLayout.Y + columnHeaderRowLayout.Height));
                }
            }
        }

        void ProcessScrollTimer()
        {
            if (IsWorking)
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                SheetLayout sheetLayout = GetSheetLayout();
                ViewportInfo viewportInfo = GetViewportInfo();
                double viewportX = sheetLayout.GetViewportX(savedHitTestInformation.ColumnViewportIndex);
                double viewportY = sheetLayout.GetViewportY(savedHitTestInformation.RowViewportIndex);
                if (_verticalSelectionMgr != null)
                {
                    int rowViewportCount = viewportInfo.RowViewportCount;
                    if (savedHitTestInformation.RowViewportIndex == -1)
                    {
                        RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(0);
                        if ((viewportRowLayoutModel != null) && (viewportRowLayoutModel.Count > 0))
                        {
                            RowLayout layout2 = viewportRowLayoutModel[0];
                            if (((MousePosition.Y >= sheetLayout.GetViewportY(0)) && ((_verticalSelectionMgr.MousePosition + viewportY) < sheetLayout.GetViewportY(0))) && (layout2.Row > Worksheet.FrozenRowCount))
                            {
                                SetViewportTopRow(0, Worksheet.FrozenRowCount);
                            }
                        }
                    }
                    else if (savedHitTestInformation.RowViewportIndex == rowViewportCount)
                    {
                        RowLayoutModel model2 = GetViewportRowLayoutModel(rowViewportCount - 1);
                        if ((model2 != null) && (model2.Count > 0))
                        {
                            RowLayout layout3 = model2[model2.Count - 1];
                            if (((MousePosition.Y < sheetLayout.GetViewportY(rowViewportCount)) && ((_verticalSelectionMgr.MousePosition + viewportY) >= sheetLayout.GetViewportY(rowViewportCount))) && ((layout3.Y + layout3.Height) > sheetLayout.GetViewportY(rowViewportCount)))
                            {
                                double viewportHeight = sheetLayout.GetViewportHeight(rowViewportCount - 1);
                                double num5 = 0.0;
                                int num6 = (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1;
                                for (int i = num6; i >= Worksheet.FrozenRowCount; i--)
                                {
                                    num5 += Worksheet.GetActualRowHeight(i, SheetArea.Cells) * Worksheet.ZoomFactor;
                                    if (num5 > viewportHeight)
                                    {
                                        num6 = Math.Min(i + 1, num6);
                                        break;
                                    }
                                }
                                SetViewportTopRow(rowViewportCount - 1, num6);
                            }
                        }
                    }
                    _verticalSelectionMgr.MousePosition = MousePosition.Y - viewportY;
                }
                if (_horizontalSelectionMgr != null)
                {
                    int columnViewportCount = viewportInfo.ColumnViewportCount;
                    if (savedHitTestInformation.ColumnViewportIndex == -1)
                    {
                        ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(0);
                        if ((viewportColumnLayoutModel != null) && (viewportColumnLayoutModel.Count > 0))
                        {
                            ColumnLayout layout4 = viewportColumnLayoutModel[0];
                            if (((MousePosition.X >= sheetLayout.GetViewportX(0)) && ((_horizontalSelectionMgr.MousePosition + viewportX) < sheetLayout.GetViewportX(0))) && (layout4.Column > Worksheet.FrozenColumnCount))
                            {
                                SetViewportLeftColumn(0, Worksheet.FrozenColumnCount);
                            }
                        }
                    }
                    else if (savedHitTestInformation.ColumnViewportIndex == columnViewportCount)
                    {
                        ColumnLayoutModel model4 = GetViewportColumnLayoutModel(columnViewportCount - 1);
                        if ((model4 != null) && (model4.Count > 0))
                        {
                            ColumnLayout layout5 = model4[model4.Count - 1];
                            if (((MousePosition.X < sheetLayout.GetViewportX(columnViewportCount)) && ((_horizontalSelectionMgr.MousePosition + viewportX) >= sheetLayout.GetViewportX(columnViewportCount))) && ((layout5.X + layout5.Width) > sheetLayout.GetViewportX(columnViewportCount)))
                            {
                                double viewportWidth = sheetLayout.GetViewportWidth(columnViewportCount - 1);
                                double num10 = 0.0;
                                int num11 = (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1;
                                for (int j = num11; j >= Worksheet.FrozenColumnCount; j--)
                                {
                                    num10 += Worksheet.GetActualColumnWidth(j, SheetArea.Cells) * Worksheet.ZoomFactor;
                                    if (num10 > viewportWidth)
                                    {
                                        num11 = Math.Min(j + 1, num11);
                                        break;
                                    }
                                }
                                SetViewportLeftColumn(columnViewportCount - 1, num11);
                            }
                        }
                    }
                    _horizontalSelectionMgr.MousePosition = MousePosition.X - viewportX;
                }
            }
        }

        internal virtual void ProcessStartSheetIndexChanged()
        {
        }

        internal void ProcessTextInput(string c, bool replace, bool justInputText = false)
        {
            if (!justInputText)
            {
                bool flag;
                bool flag2;
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                if (flag2)
                {
                    return;
                }
            }
            if (IsEditing)
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
                if (viewportRowsPresenter != null)
                {
                    viewportRowsPresenter.SendFirstKey(c, replace);
                }
            }
        }

        internal string RaiseCellTextRendering(int row, int column, string text)
        {
            if (CellTextRendering != null)
            {
                CellTextRenderingEventArgs args = new CellTextRenderingEventArgs(row, column, text);
                CellTextRendering(this, args);
                return args.CellText;
            }
            return text;
        }

        internal object RaiseCellValueApplying(int row, int column, object value)
        {
            if (CellValueApplying != null)
            {
                CellValueApplyingEventArgs args = new CellValueApplyingEventArgs(row, column, value);
                CellValueApplying(this, args);
                return args.CellValue;
            }
            return value;
        }

        internal void RaiseClipboardChanged()
        {
            if ((ClipboardChanged != null) && (_eventSuspended == 0))
            {
                ClipboardChanged(this, EventArgs.Empty);
            }
        }

        internal void RaiseClipboardChanging()
        {
            if ((ClipboardChanging != null) && (_eventSuspended == 0))
            {
                ClipboardChanging(this, EventArgs.Empty);
            }
        }

        internal void RaiseClipboardPasted(Dt.Cells.Data.Worksheet sourceSheet, CellRange sourceRange, Dt.Cells.Data.Worksheet worksheet, CellRange cellRange, ClipboardPasteOptions pastOption)
        {
            if ((ClipboardPasted != null) && (_eventSuspended == 0))
            {
                ClipboardPasted(this, new ClipboardPastedEventArgs(sourceSheet, sourceRange, worksheet, cellRange, pastOption));
            }
        }

        internal bool RaiseClipboardPasting(Dt.Cells.Data.Worksheet sourceSheet, CellRange sourceRange, Dt.Cells.Data.Worksheet worksheet, CellRange cellRange, ClipboardPasteOptions pastOption, bool isCutting, out ClipboardPasteOptions newPastOption)
        {
            newPastOption = pastOption;
            if ((ClipboardPasting != null) && (_eventSuspended == 0))
            {
                ClipboardPastingEventArgs args = new ClipboardPastingEventArgs(sourceSheet, sourceRange, worksheet, cellRange, pastOption, isCutting);
                ClipboardPasting(this, args);
                newPastOption = args.PasteOption;
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseColumnWidthChanged(int[] columnList, bool header)
        {
            if ((ColumnWidthChanged != null) && (_eventSuspended == 0))
            {
                ColumnWidthChanged(this, new ColumnWidthChangedEventArgs(columnList, header));
            }
        }

        internal bool RaiseColumnWidthChanging(int[] columnList, bool header)
        {
            if ((ColumnWidthChanging != null) && (_eventSuspended == 0))
            {
                ColumnWidthChangingEventArgs args = new ColumnWidthChangingEventArgs(columnList, header);
                ColumnWidthChanging(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool RaiseDataValidationListPopupOpening(int row, int column)
        {
            if (DataValidationListPopupOpening != null)
            {
                CellCancelEventArgs args = new CellCancelEventArgs(row, column);
                DataValidationListPopupOpening(this, args);
                return args.Cancel;
            }
            return false;
        }

        internal bool RaiseDragDropBlock(int fromRow, int fromColumn, int toRow, int toColumn, int rowCount, int columnCount, bool copy, bool insert, CopyToOption copyOption, out CopyToOption newCopyOption)
        {
            newCopyOption = copyOption;
            if ((DragDropBlock != null) && (_eventSuspended == 0))
            {
                DragDropBlockEventArgs args = new DragDropBlockEventArgs(fromRow, fromColumn, toRow, toColumn, rowCount, columnCount, copy, insert, copyOption);
                DragDropBlock(this, args);
                newCopyOption = args.CopyOption;
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseDragDropBlockCompleted(int fromRow, int fromColumn, int toRow, int toColumn, int rowCount, int columnCount, bool copy, bool insert, CopyToOption copyOption)
        {
            if ((DragDropBlockCompleted != null) && (_eventSuspended == 0))
            {
                DragDropBlockCompleted(this, new DragDropBlockCompletedEventArgs(fromRow, fromColumn, toRow, toColumn, rowCount, columnCount, copy, insert, copyOption));
            }
        }

        internal bool RaiseDragFillBlock(CellRange fillRange, FillDirection fillDirection, AutoFillType fillType)
        {
            if ((DragFillBlock != null) && (_eventSuspended == 0))
            {
                DragFillBlockEventArgs args = new DragFillBlockEventArgs(fillRange, fillDirection, fillType);
                DragFillBlock(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseDragFillBlockCompleted(CellRange fillRange, FillDirection fillDirection, AutoFillType fillType)
        {
            if ((DragFillBlockCompleted != null) && (_eventSuspended == 0))
            {
                DragFillBlockCompleted(this, new DragFillBlockCompletedEventArgs(fillRange, fillDirection, fillType));
            }
        }

        internal void RaiseEditChange(int row, int column)
        {
            if ((EditChange != null) && (_eventSuspended == 0))
            {
                EditChange(this, new EditCellEventArgs(row, column));
            }
        }

        internal void RaiseEditEnd(int row, int column)
        {
            if ((EditEnd != null) && (_eventSuspended == 0))
            {
                EditEnd(this, new EditCellEventArgs(row, column));
            }
        }

        internal bool RaiseEditStarting(int row, int column)
        {
            if ((EditStarting != null) && (_eventSuspended == 0))
            {
                EditCellStartingEventArgs args = new EditCellStartingEventArgs(row, column);
                EditStarting(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseEnterCell(int row, int column)
        {
            if ((EnterCell != null) && (_eventSuspended == 0))
            {
                EnterCellEventArgs args = new EnterCellEventArgs(row, column);
                EnterCell(this, args);
            }
        }

        /// <summary>
        /// Raises the error.
        /// </summary>
        /// <param name="row">The row</param>
        /// <param name="column">The column</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="exception">The exception</param>
        /// <returns>Return if ignore the error</returns>
        internal bool RaiseError(int row, int column, string errorMessage, Exception exception)
        {
            if ((Error != null) && (_eventSuspended == 0))
            {
                UserErrorEventArgs args = new UserErrorEventArgs(this, row, column, errorMessage, exception);
                Error(this, args);
                return args.Cancel;
            }
            return false;
        }

        internal bool RaiseFilterPopupOpening(int row, int column)
        {
            if (FilterPopupOpening != null)
            {
                CellCancelEventArgs args = new CellCancelEventArgs(row, column);
                FilterPopupOpening(this, args);
                return args.Cancel;
            }
            return false;
        }

        internal void RaiseFloatingObjectPasted(Dt.Cells.Data.Worksheet worksheet, FloatingObject pastedObject)
        {
            if ((FloatingObjectPasted != null) && (_eventSuspended == 0))
            {
                FloatingObjectPasted(this, new FloatingObjectPastedEventArgs(worksheet, pastedObject));
            }
        }

        internal void RaiseInvalidOperation(string message, string operation = null, object context = null)
        {
            if ((InvalidOperation != null) && (_eventSuspended == 0))
            {
                InvalidOperationEventArgs args = new InvalidOperationEventArgs(message, operation, context);
                InvalidOperation(this, args);
            }
        }

        internal bool RaiseLeaveCell(int row, int column, int toRow, int toColumn)
        {
            if ((LeaveCell != null) && (_eventSuspended == 0))
            {
                LeaveCellEventArgs args = new LeaveCellEventArgs(row, column, toRow, toColumn);
                LeaveCell(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        void RaiseLeftChanged(int oldIndex, int newIndex, int viewportIndex)
        {
            if ((LeftColumnChanged != null) && (_eventSuspended == 0))
            {
                LeftColumnChanged(this, new ViewportEventArgs(oldIndex, newIndex, viewportIndex));
            }
        }

        internal void RaiseRangeFiltered(int column, object[] filterValues)
        {
            if ((RangeFiltered != null) && (_eventSuspended == 0))
            {
                RangeFiltered(this, new RangeFilteredEventArgs(column, filterValues));
            }
        }

        internal bool RaiseRangeFiltering(int column, object[] filterValues)
        {
            if ((RangeFiltering != null) && (_eventSuspended == 0))
            {
                RangeFilteringEventArgs args = new RangeFilteringEventArgs(column, filterValues);
                RangeFiltering(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseRangeGroupStateChanged(bool isRowGroup, int index, int level)
        {
            if ((RangeGroupStateChanged != null) && (_eventSuspended == 0))
            {
                RangeGroupStateChanged(this, new RangeGroupStateChangedEventArgs(isRowGroup, index, level));
            }
        }

        internal bool RaiseRangeGroupStateChanging(bool isRowGroup, int index, int level)
        {
            if ((RangeGroupStateChanging != null) && (_eventSuspended == 0))
            {
                RangeGroupStateChangingEventArgs args = new RangeGroupStateChangingEventArgs(isRowGroup, index, level);
                RangeGroupStateChanging(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseRangeSorted(int column, bool isAscending)
        {
            if ((RangeSorted != null) && (_eventSuspended == 0))
            {
                RangeSorted(this, new RangeSortedEventArgs(column, isAscending));
            }
        }

        internal bool RaiseRangeSorting(int column, bool isAscending)
        {
            if ((RangeSorting != null) && (_eventSuspended == 0))
            {
                RangeSortingEventArgs args = new RangeSortingEventArgs(column, isAscending);
                RangeSorting(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseRowHeightChanged(int[] rowList, bool header)
        {
            if ((RowHeightChanged != null) && (_eventSuspended == 0))
            {
                RowHeightChanged(this, new RowHeightChangedEventArgs(rowList, header));
            }
        }

        internal bool RaiseRowHeightChanging(int[] rowList, bool header)
        {
            if ((RowHeightChanging != null) && (_eventSuspended == 0))
            {
                RowHeightChangingEventArgs args = new RowHeightChangingEventArgs(rowList, header);
                RowHeightChanging(this, args);
                if (args.Cancel)
                {
                    return true;
                }
            }
            return false;
        }

        internal void RaiseSelectionChanged()
        {
            if ((SelectionChanged != null) && (_eventSuspended == 0))
            {
                SelectionChanged(this, EventArgs.Empty);
            }
        }

        internal bool RaiseSelectionChanging(CellRange[] oldSelection, CellRange[] newSelection)
        {
            if (((SelectionChanging != null) && (_eventSuspended == 0)) && !IsRangesEqual(oldSelection, newSelection))
            {
                SelectionChanging(this, new SelectionChangingEventArgs(oldSelection, newSelection));
                return true;
            }
            return false;
        }

        internal void RaiseSheetTabClick(int sheetTabIndex)
        {
            if ((SheetTabClick != null) && (_eventSuspended == 0))
            {
                SheetTabClick(this, new SheetTabClickEventArgs(sheetTabIndex));
            }
        }

        internal void RaiseSheetTabDoubleClick(int sheetTabIndex)
        {
            if ((SheetTabDoubleClick != null) && (_eventSuspended == 0))
            {
                SheetTabDoubleClick(this, new SheetTabDoubleClickEventArgs(sheetTabIndex));
            }
        }

        void RaiseTopChanged(int oldIndex, int newIndex, int viewportIndex)
        {
            if ((TopRowChanged != null) && (_eventSuspended == 0))
            {
                TopRowChanged(this, new ViewportEventArgs(oldIndex, newIndex, viewportIndex));
            }
        }

        internal void RaiseTouchCellClick(HitTestInformation hi)
        {
            if ((CellClick != null) && (_eventSuspended == 0))
            {
                CellClickEventArgs args = null;
                Point point = new Point(-1.0, -1.0);
                if (hi.HitTestType == HitTestType.Viewport)
                {
                    args = CreateCellClickEventArgs(hi.ViewportInfo.Row, hi.ViewportInfo.Column, Worksheet.SpanModel, SheetArea.Cells, MouseButtonType.Left);
                    point = new Point((double)hi.ViewportInfo.Row, (double)hi.ViewportInfo.Column);
                }
                else if (hi.HitTestType == HitTestType.RowHeader)
                {
                    args = CreateCellClickEventArgs(hi.ViewportInfo.Row, hi.ViewportInfo.Column, Worksheet.SpanModel, SheetArea.CornerHeader | SheetArea.RowHeader, MouseButtonType.Left);
                    point = new Point((double)hi.HeaderInfo.Row, (double)hi.HeaderInfo.Column);
                }
                else if (hi.HitTestType == HitTestType.ColumnHeader)
                {
                    args = CreateCellClickEventArgs(hi.ViewportInfo.Row, hi.ViewportInfo.Column, Worksheet.SpanModel, SheetArea.ColumnHeader, MouseButtonType.Left);
                    point = new Point((double)hi.HeaderInfo.Row, (double)hi.HeaderInfo.Column);
                }
                if (((args != null) && (point.X != -1.0)) && (point.Y != -1.0))
                {
                    CellClick(this, args);
                }
            }
        }

        internal bool RaiseTouchToolbarOpeningEvent(Point touchPoint, TouchToolbarShowingArea area)
        {
            if ((TouchToolbarOpening != null) && (_eventSuspended == 0))
            {
                TouchToolbarOpeningEventArgs args = new TouchToolbarOpeningEventArgs((int)touchPoint.X, (int)touchPoint.Y, area);
                TouchToolbarOpening(this, args);
                return false;
            }
            return true;
        }

        internal void RaiseUserFormulaEntered(int row, int column, string formula)
        {
            if ((UserFormulaEntered != null) && (_eventSuspended == 0))
            {
                if (formula != null)
                {
                    formula = formula.ToUpperInvariant();
                }
                else
                {
                    formula = "";
                }
                UserFormulaEntered(this, new UserFormulaEnteredEventArgs(row, column, formula));
            }
        }

        internal void RaiseUserZooming(float oldZoomFactor, float newZoomFactor)
        {
            if ((UserZooming != null) && (_eventSuspended == 0))
            {
                UserZooming(this, new ZoomEventArgs(oldZoomFactor, newZoomFactor));
            }
        }

        internal bool RaiseValidationDragDropBlock(int fromRow, int fromColumn, int toRow, int toColumn, int rowCount, int columnCount, bool copy, bool insert, out bool isInValid, out string invalidMessage)
        {
            isInValid = false;
            invalidMessage = "";
            if ((ValidationDragDropBlock != null) && (_eventSuspended == 0))
            {
                ValidationDragDropBlockEventArgs args = new ValidationDragDropBlockEventArgs(fromRow, fromColumn, toRow, toColumn, rowCount, columnCount, copy, insert);
                ValidationDragDropBlock(this, args);
                if (args.Handle)
                {
                    isInValid = args.IsInvalid;
                    invalidMessage = args.InvalidMessage;
                    return true;
                }
            }
            return false;
        }

        internal void RaiseValidationError(int row, int column, ValidationErrorEventArgs eventArgs)
        {
            if ((ValidationError != null) && (_eventSuspended == 0))
            {
                ValidationError(this, eventArgs);
            }
        }

        internal bool RaiseValidationPasting(Dt.Cells.Data.Worksheet sourceSheet, CellRange sourceRange, Dt.Cells.Data.Worksheet worksheet, CellRange cellRange, CellRange pastingRange, bool isCutting, out bool isInvalid, out string invalidMessage)
        {
            isInvalid = false;
            invalidMessage = "";
            if ((ValidationPasting == null) || (_eventSuspended != 0))
            {
                return false;
            }
            ValidationPastingEventArgs args = new ValidationPastingEventArgs(sourceSheet, sourceRange, worksheet, cellRange, pastingRange, isCutting);
            ValidationPasting(this, args);
            if (args.Handle)
            {
                isInvalid = args.IsInvalid;
                invalidMessage = args.InvalidMessage;
            }
            return args.Handle;
        }

        internal void RaiseValueChanged(int row, int column)
        {
            if ((ValueChanged != null) && (_eventSuspended == 0))
            {
                ValueChanged(this, new CellEventArgs(row, column));
            }
        }

        internal virtual void ReadXmlInternal(XmlReader reader)
        {
            switch (reader.Name)
            {
                case "AllowUserFormula":
                    _allowUserFormula = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "AllowUndo":
                    _allowUndo = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "FreezeLineStyle":
                    _freezeLineStyle = Serializer.DeserializeObj(typeof(Style), reader) as Style;
                    return;

                case "TrailingFreezeLineStyle":
                    _trailingFreezeLineStyle = Serializer.DeserializeObj(typeof(Style), reader) as Style;
                    return;

                case "ShowFreezeLine":
                    _showFreezeLine = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "AllowUserZoom":
                    _allowUserZoom = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "AutoClipboard":
                    _autoClipboard = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "ClipBoardOptions":
                    _clipBoardOptions = (ClipboardPasteOptions)Serializer.DeserializeObj(typeof(ClipboardPasteOptions), reader);
                    return;

                case "AllowEditOverflow":
                    _allowEditOverflow = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "Protect":
                    _protect = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "AllowDragDrop":
                    _allowDragDrop = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "ShowRowRangeGroup":
                    _showRowRangeGroup = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "ShowColumnRangeGroup":
                    _showColumnRangeGroup = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "AllowDragFill":
                    _allowDragFill = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "CanTouchMultiSelect":
                    _canTouchMultiSelect = (bool)((bool)Serializer.DeserializeObj(typeof(bool), reader));
                    return;

                case "ResizeZeroIndicator":
                    _resizeZeroIndicator = (Dt.Cells.UI.ResizeZeroIndicator)Serializer.DeserializeObj(typeof(Dt.Cells.UI.ResizeZeroIndicator), reader);
                    return;

                case "RangeGroupBackground":
                    _rangeGroupBackground = (Brush)Serializer.DeserializeObj(typeof(Brush), reader);
                    return;

                case "RangeGroupBorderBrush":
                    _rangeGroupBorderBrush = (Brush)Serializer.DeserializeObj(typeof(Brush), reader);
                    return;

                case "RangeGroupLineStroke":
                    _rangeGroupLineStroke = (Brush)Serializer.DeserializeObj(typeof(Brush), reader);
                    return;
            }
        }

        internal void RefreshCellAreaViewport(int row, int column, int rowCount, int columnCount)
        {
            RefreshViewportCells(_viewportPresenters, 0, 0, rowCount, columnCount);
        }

        internal void RefreshCells(GcViewport viewport, int row, int column, int rowCount, int columnCount)
        {
            foreach (RowLayout layout in viewport.GetRowLayoutModel())
            {
                if ((row <= layout.Row) && (layout.Row < (row + rowCount)))
                {
                    RowPresenter presenter = viewport.GetRow(layout.Row);
                    if (presenter != null)
                    {
                        foreach (CellPresenterBase base2 in presenter.Children)
                        {
                            if ((column <= base2.Column) && (base2.Column < (column + columnCount)))
                            {
                                base2.Invalidate();
                            }
                        }
                    }
                }
            }
        }

        internal void RefreshDataValidationInvalidCircles()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if ((viewport != null) && (viewport.SheetArea == SheetArea.Cells))
                        {
                            viewport.RefreshDataValidationInvalidCircles();
                        }
                    }
                }
            }
        }

        void RefreshDragDropIndicator(int dragToRowViewportIndex, int dragToColumnViewportIndex, int dragToRow, int dragToColumn)
        {
            RowLayout layout = GetViewportRowLayoutModel(dragToRowViewportIndex).FindRow(dragToRow);
            ColumnLayout layout2 = GetViewportColumnLayoutModel(dragToColumnViewportIndex).FindColumn(dragToColumn);
            if ((layout != null) && (layout2 != null))
            {
                _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                int row = _dragDropFromRange.Row;
                int column = _dragDropFromRange.Column;
                int rowCount = _dragDropFromRange.RowCount;
                int columnCount = _dragDropFromRange.ColumnCount;
                int num5 = (row < 0) ? -1 : Math.Max(0, Math.Min((int)(Worksheet.RowCount - rowCount), (int)(dragToRow - _dragDropRowOffset)));
                int num6 = (column < 0) ? -1 : Math.Max(0, Math.Min((int)(Worksheet.ColumnCount - columnCount), (int)(dragToColumn - _dragDropColumnOffset)));
                int index = (num6 < 0) ? 0 : num6;
                int num8 = (num6 < 0) ? (Worksheet.ColumnCount - 1) : ((index + columnCount) - 1);
                int num9 = (num5 < 0) ? 0 : num5;
                int num10 = (num5 < 0) ? (Worksheet.RowCount - 1) : ((num9 + rowCount) - 1);
                int columnViewportIndex = dragToColumnViewportIndex;
                int num12 = dragToColumnViewportIndex;
                int rowViewportIndex = dragToRowViewportIndex;
                int num14 = dragToRowViewportIndex;
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                if ((Worksheet.FrozenColumnCount > 0) && ((dragToColumnViewportIndex == -1) || (dragToColumnViewportIndex == 0)))
                {
                    if (index < Worksheet.FrozenColumnCount)
                    {
                        columnViewportIndex = -1;
                    }
                    if (num8 < Worksheet.FrozenColumnCount)
                    {
                        num12 = -1;
                    }
                    else if (((columnViewportCount == 1) && (Worksheet.FrozenTrailingColumnCount > 0)) && (num8 >= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                    {
                        num12 = 1;
                    }
                    else
                    {
                        num12 = 0;
                    }
                }
                else if ((Worksheet.FrozenTrailingColumnCount > 0) && ((dragToColumnViewportIndex == (columnViewportCount - 1)) || (dragToColumnViewportIndex == columnViewportCount)))
                {
                    if (index < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount))
                    {
                        if (((columnViewportCount == 1) && (Worksheet.FrozenColumnCount > 0)) && (index < Worksheet.FrozenColumnCount))
                        {
                            columnViewportIndex = -1;
                        }
                        else
                        {
                            columnViewportIndex = columnViewportCount - 1;
                        }
                        if (num8 < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount))
                        {
                            num12 = columnViewportCount - 1;
                        }
                        else
                        {
                            num12 = columnViewportCount;
                        }
                    }
                    else
                    {
                        columnViewportIndex = columnViewportCount;
                        num12 = columnViewportCount;
                    }
                }
                if ((Worksheet.FrozenRowCount > 0) && ((dragToRowViewportIndex == -1) || (dragToRowViewportIndex == 0)))
                {
                    if (num5 < Worksheet.FrozenRowCount)
                    {
                        rowViewportIndex = -1;
                    }
                    if (num10 < Worksheet.FrozenRowCount)
                    {
                        num14 = -1;
                    }
                    else if (((rowViewportCount == 1) && (Worksheet.FrozenTrailingRowCount > 0)) && (num10 >= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                    {
                        num14 = 1;
                    }
                    else
                    {
                        num14 = 0;
                    }
                }
                else if ((Worksheet.FrozenTrailingRowCount > 0) && ((dragToRowViewportIndex == (rowViewportCount - 1)) || (dragToRowViewportIndex == rowViewportCount)))
                {
                    if (num9 < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount))
                    {
                        if (((rowViewportCount == 1) && (Worksheet.FrozenRowCount > 0)) && (num9 < Worksheet.FrozenRowCount))
                        {
                            rowViewportIndex = -1;
                        }
                        else
                        {
                            rowViewportIndex = rowViewportCount - 1;
                        }
                        if (num10 < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount))
                        {
                            num14 = rowViewportCount - 1;
                        }
                        else
                        {
                            num14 = rowViewportCount;
                        }
                    }
                    else
                    {
                        rowViewportIndex = rowViewportCount;
                        num14 = rowViewportCount;
                    }
                }
                ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(columnViewportIndex);
                ColumnLayoutModel model2 = viewportColumnLayoutModel;
                if (num12 != columnViewportIndex)
                {
                    model2 = GetViewportColumnLayoutModel(num12);
                }
                RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(rowViewportIndex);
                RowLayoutModel model4 = viewportRowLayoutModel;
                if (num14 != rowViewportIndex)
                {
                    model4 = GetViewportRowLayoutModel(num14);
                }
                if ((((viewportRowLayoutModel != null) && (viewportRowLayoutModel.Count > 0)) && ((model4 != null) && (model4.Count > 0))) && (((viewportColumnLayoutModel != null) && (viewportColumnLayoutModel.Count > 0)) && ((model2 != null) && (model2.Count > 0))))
                {
                    double d = -1.0;
                    double num18 = -1.0;
                    double num19 = -1.0;
                    double num20 = -1.0;
                    ColumnLayout layout3 = viewportColumnLayoutModel.Find(index);
                    ColumnLayout layout4 = model2.Find(num8);
                    if (layout3 != null)
                    {
                        d = layout3.X;
                    }
                    else
                    {
                        d = viewportColumnLayoutModel[0].X;
                    }
                    if (layout4 != null)
                    {
                        num19 = layout4.X + layout4.Width;
                    }
                    else
                    {
                        num19 = model2[model2.Count - 1].X + model2[model2.Count - 1].Width;
                    }
                    RowLayout layout5 = viewportRowLayoutModel.Find(num9);
                    RowLayout layout6 = model4.Find(num10);
                    if (layout5 != null)
                    {
                        num18 = layout5.Y;
                    }
                    else
                    {
                        num18 = viewportRowLayoutModel[0].Y;
                    }
                    if (layout6 != null)
                    {
                        num20 = layout6.Y + layout6.Height;
                    }
                    else
                    {
                        num20 = model4[model4.Count - 1].Y + model4[model4.Count - 1].Height;
                    }
                    SheetLayout sheetLayout = GetSheetLayout();
                    bool flag = ((index >= viewportColumnLayoutModel[0].Column) && (index <= viewportColumnLayoutModel[viewportColumnLayoutModel.Count - 1].Column)) && Worksheet.GetActualColumnVisible(index, SheetArea.Cells);
                    bool flag2 = ((num8 >= model2[0].Column) && (num8 <= model2[model2.Count - 1].Column)) && Worksheet.GetActualColumnVisible(num8, SheetArea.Cells);
                    bool flag3 = ((num9 >= viewportRowLayoutModel[0].Row) && (num9 <= viewportRowLayoutModel[viewportRowLayoutModel.Count - 1].Row)) && Worksheet.GetActualRowVisible(num9, SheetArea.Cells);
                    bool flag4 = ((num10 >= model4[0].Row) && (num10 <= model4[model4.Count - 1].Row)) && Worksheet.GetActualRowVisible(num10, SheetArea.Cells);
                    double num21 = sheetLayout.GetViewportX(num12) + sheetLayout.GetViewportWidth(num12);
                    double num22 = sheetLayout.GetViewportY(num14) + sheetLayout.GetViewportHeight(num14);
                    if (flag2 && (num21 < num19))
                    {
                        flag2 = false;
                    }
                    if (flag4 && (num22 < num20))
                    {
                        flag4 = false;
                    }
                    double num23 = Math.Floor((double)((Math.Min(num21, num19) - d) + 3.0));
                    double num24 = Math.Floor((double)((Math.Min(num22, num20) - num18) + 3.0));
                    d -= 2.0;
                    num18 -= 2.0;
                    Canvas.SetLeft(_dragDropIndicator, Math.Floor(d));
                    Canvas.SetTop(_dragDropIndicator, Math.Floor(num18));
                    _dragDropIndicator.Visibility = Visibility.Visible;
                    _dragDropIndicator.Height = num24;
                    _dragDropIndicator.Width = num23;
                    double x = (index <= viewportColumnLayoutModel[0].Column) ? 2.0 : 0.0;
                    double y = (num9 <= viewportRowLayoutModel[0].Row) ? 2.0 : 0.0;
                    double width = 3.0;
                    Rect empty = Rect.Empty;
                    Rect rect2 = Rect.Empty;
                    Rect rect3 = Rect.Empty;
                    Rect rect4 = Rect.Empty;
                    if (flag)
                    {
                        empty = new Rect(x, y, width - x, num24 - y);
                    }
                    if (flag3)
                    {
                        rect2 = new Rect(x, y, num23 - x, width - y);
                    }
                    if (flag2)
                    {
                        rect3 = new Rect(num23 - width, y, width, num24 - y);
                    }
                    if (flag4)
                    {
                        rect4 = new Rect(x, num24 - width, num23 - x, width);
                    }
                    if (_dragDropIndicator.Children.Count >= 8)
                    {
                        if (flag)
                        {
                            RectangleGeometry geometry = new RectangleGeometry();
                            geometry.Rect = empty;
                            ((UIElement)_dragDropIndicator.Children[0]).Clip = geometry;
                            RectangleGeometry geometry2 = new RectangleGeometry();
                            geometry2.Rect = empty;
                            ((UIElement)_dragDropIndicator.Children[4]).Clip = geometry2;
                        }
                        if (flag3)
                        {
                            RectangleGeometry geometry3 = new RectangleGeometry();
                            geometry3.Rect = rect2;
                            ((UIElement)_dragDropIndicator.Children[1]).Clip = geometry3;
                            RectangleGeometry geometry4 = new RectangleGeometry();
                            geometry4.Rect = rect2;
                            ((UIElement)_dragDropIndicator.Children[5]).Clip = geometry4;
                        }
                        if (flag2)
                        {
                            RectangleGeometry geometry5 = new RectangleGeometry();
                            geometry5.Rect = rect3;
                            ((UIElement)_dragDropIndicator.Children[2]).Clip = geometry5;
                            RectangleGeometry geometry6 = new RectangleGeometry();
                            geometry6.Rect = rect3;
                            ((UIElement)_dragDropIndicator.Children[6]).Clip = geometry6;
                        }
                        if (flag4)
                        {
                            RectangleGeometry geometry7 = new RectangleGeometry();
                            geometry7.Rect = rect4;
                            ((UIElement)_dragDropIndicator.Children[3]).Clip = geometry7;
                            RectangleGeometry geometry8 = new RectangleGeometry();
                            geometry8.Rect = rect4;
                            ((UIElement)_dragDropIndicator.Children[7]).Clip = geometry8;
                        }
                    }
                    if (ShowDragDropTip)
                    {
                        TooltipHelper.ShowTooltip(GetRangeString(new CellRange(num5, num6, rowCount, columnCount)), num19 + 2.0, num20 + 5.0);
                    }
                }
            }
        }

        void RefreshDragDropInsertIndicator(int dragToRowViewportIndex, int dragToColumnViewportIndex, int dragToRow, int dragToColumn)
        {
            RowLayout layout = GetViewportRowLayoutModel(dragToRowViewportIndex).FindRow(dragToRow);
            ColumnLayout layout2 = GetViewportColumnLayoutModel(dragToColumnViewportIndex).FindColumn(dragToColumn);
            if ((layout != null) && (layout2 != null))
            {
                _dragDropIndicator.Visibility = Visibility.Collapsed;
                SheetLayout sheetLayout = GetSheetLayout();
                int row = _dragDropFromRange.Row;
                int column = _dragDropFromRange.Column;
                int rowCount = _dragDropFromRange.RowCount;
                int columnCount = _dragDropFromRange.ColumnCount;
                double width = 3.0;
                if ((row < 0) || (column < 0))
                {
                    if (column >= 0)
                    {
                        int num6 = (column < 0) ? 0 : column;
                        int num7 = (column < 0) ? Worksheet.ColumnCount : columnCount;
                        double d = layout2.X - (width / 2.0);
                        if (MousePosition.X > (layout2.X + (layout2.Width / 2.0)))
                        {
                            d = (layout2.X + layout2.Width) - (width / 2.0);
                            dragToColumn++;
                        }
                        if (d > (sheetLayout.GetViewportX(dragToColumnViewportIndex) + sheetLayout.GetViewportWidth(dragToColumnViewportIndex)))
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                        }
                        else if (((_isDragCopy && (dragToColumn > num6)) && (dragToColumn < (num6 + num7))) || ((!_isDragCopy && (dragToColumn >= num6)) && (dragToColumn < (num6 + num7))))
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Visible;
                            int rowViewportCount = GetViewportInfo().RowViewportCount;
                            double viewportY = 0.0;
                            double num11 = 0.0;
                            if ((Worksheet.FrozenRowCount > 0) && ((dragToRowViewportIndex == -1) || (dragToRowViewportIndex == 0)))
                            {
                                viewportY = sheetLayout.GetViewportY(-1);
                                if ((rowViewportCount == 1) && (Worksheet.FrozenTrailingRowCount > 0))
                                {
                                    num11 = sheetLayout.GetViewportY(1) + sheetLayout.GetViewportHeight(1);
                                }
                                else
                                {
                                    RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(0);
                                    if ((viewportRowLayoutModel != null) && (viewportRowLayoutModel.Count > 0))
                                    {
                                        RowLayout layout4 = viewportRowLayoutModel[viewportRowLayoutModel.Count - 1];
                                        num11 = Math.Min((double)(layout4.Y + layout4.Height), (double)(sheetLayout.GetViewportY(0) + sheetLayout.GetViewportHeight(0)));
                                    }
                                    else
                                    {
                                        num11 = sheetLayout.GetViewportY(-1) + sheetLayout.GetViewportHeight(-1);
                                    }
                                }
                            }
                            else if ((Worksheet.FrozenTrailingRowCount > 0) && ((dragToRowViewportIndex == rowViewportCount) || (dragToRowViewportIndex == (rowViewportCount - 1))))
                            {
                                if ((rowViewportCount == 1) && (Worksheet.FrozenRowCount > 0))
                                {
                                    viewportY = sheetLayout.GetViewportY(-1);
                                }
                                else
                                {
                                    viewportY = sheetLayout.GetViewportY(rowViewportCount - 1);
                                }
                                num11 = sheetLayout.GetViewportY(rowViewportCount) + sheetLayout.GetViewportHeight(rowViewportCount);
                            }
                            else
                            {
                                viewportY = sheetLayout.GetViewportY(dragToRowViewportIndex);
                                RowLayoutModel model2 = GetViewportRowLayoutModel(dragToRowViewportIndex);
                                if ((model2 != null) && (model2.Count > 0))
                                {
                                    RowLayout layout5 = model2[model2.Count - 1];
                                    num11 = Math.Min((double)(layout5.Y + layout5.Height), (double)(sheetLayout.GetViewportY(dragToRowViewportIndex) + sheetLayout.GetViewportHeight(dragToRowViewportIndex)));
                                }
                                else
                                {
                                    num11 = sheetLayout.GetViewportY(dragToRowViewportIndex) + sheetLayout.GetViewportHeight(dragToRowViewportIndex);
                                }
                            }
                            Canvas.SetLeft(_dragDropInsertIndicator, Math.Floor(d));
                            Canvas.SetTop(_dragDropInsertIndicator, Math.Floor(viewportY));
                            double num12 = width * 2.0;
                            double num13 = Math.Floor((double)(num11 - viewportY));
                            _dragDropInsertIndicator.Width = num12;
                            _dragDropInsertIndicator.Height = num13;
                            RectangleGeometry geometry = new RectangleGeometry();
                            geometry.Rect = new Rect(0.0, 0.0, width, num13);
                            _dragDropInsertIndicator.Clip = geometry;
                            if (ShowDragDropTip)
                            {
                                TooltipHelper.ShowTooltip(GetRangeString(new CellRange(-1, dragToColumn, -1, num7)), MousePosition.X + 10.0, _mouseDownPosition.Y + 10.0);
                            }
                        }
                    }
                    else if (row >= 0)
                    {
                        int num14 = (row < 0) ? 0 : row;
                        int num15 = (row < 0) ? Worksheet.RowCount : rowCount;
                        double num16 = layout.Y - (width / 2.0);
                        if (MousePosition.Y > (layout.Y + (layout.Height / 2.0)))
                        {
                            num16 = (layout.Y + layout.Height) - (width / 2.0);
                            dragToRow++;
                        }
                        if (num16 > (sheetLayout.GetViewportY(dragToRowViewportIndex) + sheetLayout.GetViewportHeight(dragToRowViewportIndex)))
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                        }
                        else if (((_isDragCopy && (dragToRow > num14)) && (dragToRow < (num14 + num15))) || ((!_isDragCopy && (dragToRow >= num14)) && (dragToRow < (num14 + num15))))
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            _dragDropInsertIndicator.Visibility = Visibility.Visible;
                            int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                            double viewportX = 0.0;
                            double num19 = 0.0;
                            if ((Worksheet.FrozenColumnCount > 0) && ((dragToColumnViewportIndex == -1) || (dragToColumnViewportIndex == 0)))
                            {
                                viewportX = sheetLayout.GetViewportX(-1);
                                if ((columnViewportCount == 1) && (Worksheet.FrozenTrailingColumnCount > 0))
                                {
                                    num19 = sheetLayout.GetViewportX(1) + sheetLayout.GetViewportWidth(1);
                                }
                                else
                                {
                                    ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(0);
                                    if ((viewportColumnLayoutModel != null) && (viewportColumnLayoutModel.Count > 0))
                                    {
                                        ColumnLayout layout6 = viewportColumnLayoutModel[viewportColumnLayoutModel.Count - 1];
                                        num19 = Math.Min((double)(layout6.X + layout6.Width), (double)(sheetLayout.GetViewportX(0) + sheetLayout.GetViewportWidth(0)));
                                    }
                                    else
                                    {
                                        num19 = sheetLayout.GetViewportX(-1) + sheetLayout.GetViewportWidth(-1);
                                    }
                                }
                            }
                            else if ((Worksheet.FrozenTrailingColumnCount > 0) && ((dragToColumnViewportIndex == columnViewportCount) || (dragToColumnViewportIndex == (columnViewportCount - 1))))
                            {
                                if ((columnViewportCount == 1) && (Worksheet.FrozenColumnCount > 0))
                                {
                                    viewportX = sheetLayout.GetViewportX(-1);
                                }
                                else
                                {
                                    viewportX = sheetLayout.GetViewportX(columnViewportCount - 1);
                                }
                                num19 = sheetLayout.GetViewportX(columnViewportCount) + sheetLayout.GetViewportWidth(columnViewportCount);
                            }
                            else
                            {
                                viewportX = sheetLayout.GetViewportX(dragToColumnViewportIndex);
                                ColumnLayoutModel model4 = GetViewportColumnLayoutModel(dragToColumnViewportIndex);
                                if ((model4 != null) && (model4.Count > 0))
                                {
                                    ColumnLayout layout7 = model4[model4.Count - 1];
                                    num19 = Math.Min((double)(layout7.X + layout7.Width), (double)(sheetLayout.GetViewportX(dragToColumnViewportIndex) + sheetLayout.GetViewportWidth(dragToColumnViewportIndex)));
                                }
                                else
                                {
                                    num19 = sheetLayout.GetViewportX(dragToColumnViewportIndex) + sheetLayout.GetViewportWidth(dragToColumnViewportIndex);
                                }
                            }
                            Canvas.SetLeft(_dragDropInsertIndicator, Math.Floor(viewportX));
                            Canvas.SetTop(_dragDropInsertIndicator, Math.Floor(num16));
                            double num20 = Math.Floor((double)(num19 - viewportX));
                            double num21 = width * 2.0;
                            _dragDropInsertIndicator.Width = num20;
                            _dragDropInsertIndicator.Height = num21;
                            RectangleGeometry geometry2 = new RectangleGeometry();
                            geometry2.Rect = new Rect(0.0, 0.0, num20, width);
                            _dragDropInsertIndicator.Clip = geometry2;
                            if (ShowDragDropTip)
                            {
                                TooltipHelper.ShowTooltip(GetRangeString(new CellRange(dragToRow, -1, num15, -1)), _mouseDownPosition.X + 10.0, MousePosition.Y + 10.0);
                            }
                        }
                    }
                }
            }
        }

        void RefreshDragFill()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            if (NeedRefresh(viewport.RowViewportIndex, viewport.ColumnViewportIndex))
                            {
                                viewport.RefreshDragFill();
                            }
                            else
                            {
                                viewport.ResetDragFill();
                            }
                        }
                    }
                }
            }
        }

        internal void RefreshFormulaSelectionGrippers()
        {
            if (_formulaSelectionGripperPanel != null)
            {
                _formulaSelectionGripperPanel.Refresh();
            }
        }

        internal void RefreshHeaderCells(GcViewport[] viewportPresenters, int row, int column, int rowCount, int columnCount)
        {
            if (!IsSuspendInvalidate() && (viewportPresenters != null))
            {
                foreach (GcViewport viewport in viewportPresenters)
                {
                    if (viewport != null)
                    {
                        RefreshCells(viewport, row, column, rowCount, columnCount);
                        viewport.InvalidateBordersMeasureState();
                        viewport.InvalidateRowsMeasureState(true);
                    }
                }
            }
        }

        internal void RefreshSelection()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.RefreshSelection();
                        }
                    }
                }
            }
        }

        void RefreshSelectionBorder()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num4 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num4; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            int rowViewportIndex = viewport.RowViewportIndex;
                            int columnViewportIndex = viewport.ColumnViewportIndex;
                            if (NeedRefresh(rowViewportIndex, columnViewportIndex))
                            {
                                viewport.SelectionContainer.FocusIndicator.IsTopVisible = false;
                                viewport.SelectionContainer.FocusIndicator.IsLeftVisible = false;
                                viewport.SelectionContainer.FocusIndicator.IsRightVisible = false;
                                viewport.SelectionContainer.FocusIndicator.IsBottomVisible = false;
                                if (IsVerticalDragFill)
                                {
                                    if (_currentFillDirection == DragFillDirection.Down)
                                    {
                                        if (rowViewportIndex == _dragFillStartBottomRowViewport)
                                        {
                                            viewport.SelectionContainer.FocusIndicator.IsBottomVisible = true;
                                        }
                                    }
                                    else if (_currentFillDirection == DragFillDirection.Up)
                                    {
                                        if (rowViewportIndex == _dragFillStartTopRowViewport)
                                        {
                                            viewport.SelectionContainer.FocusIndicator.IsTopVisible = true;
                                        }
                                    }
                                    else if (_currentFillDirection == DragFillDirection.UpClear)
                                    {
                                    }
                                }
                                else if (_currentFillDirection == DragFillDirection.Right)
                                {
                                    if (columnViewportIndex == _dragFillStartRightColumnViewport)
                                    {
                                        viewport.SelectionContainer.FocusIndicator.IsRightVisible = true;
                                    }
                                }
                                else if (_currentFillDirection == DragFillDirection.Left)
                                {
                                    if (columnViewportIndex == _dragFillStartLeftColumnViewport)
                                    {
                                        viewport.SelectionContainer.FocusIndicator.IsLeftVisible = true;
                                    }
                                }
                                else
                                {
                                    DragFillDirection direction1 = _currentFillDirection;
                                }
                                viewport.SelectionContainer.FocusIndicator.InvalidateMeasure();
                                viewport.SelectionContainer.FocusIndicator.InvalidateArrange();
                            }
                        }
                    }
                }
            }
        }

        internal void RefreshViewportCells(GcViewport[,] viewportPresenters, int row, int column, int rowCount, int columnCount)
        {
            if (viewportPresenters != null)
            {
                GcViewport[,] viewportArray = viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            if (((IsEditing && (viewport.EditingContainer != null)) && viewport.IsActived) && ((viewport.EditingContainer.EditingRowIndex != Worksheet.ActiveRowIndex) || (Worksheet.ActiveColumnIndex != viewport.EditingContainer.EditingColumnIndex)))
                            {
                                StopCellEditing(true);
                            }
                            RefreshCells(viewport, row, column, rowCount, columnCount);
                            viewport.InvalidateBordersMeasureState();
                            viewport.InvalidateSelectionMeasureState();
                            viewport.InvalidateRowsMeasureState(true);
                        }
                    }
                }
            }
        }

        void RefreshViewportFloatingObjects()
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.RefreshFloatingObjects();
                        }
                    }
                }
            }
        }

        void RefreshViewportFloatingObjects(FloatingObject floatingObject)
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            if (floatingObject is SpreadChart)
                            {
                                viewport.RefreshFloatingObject(new ChartChangedEventArgs(floatingObject as SpreadChart, ChartArea.All, null));
                            }
                            else if (floatingObject is Picture)
                            {
                                viewport.RefreshFloatingObject(new PictureChangedEventArgs(floatingObject as Picture, null));
                            }
                            else if (floatingObject != null)
                            {
                                viewport.RefreshFloatingObject(new FloatingObjectChangedEventArgs(floatingObject, null));
                            }
                        }
                    }
                }
            }
        }

        void RefreshViewportFloatingObjectsContainerMoving()
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            if (IsNeedRefreshFloatingObjectsMovingResizingContainer(viewport.RowViewportIndex, viewport.ColumnViewportIndex))
                            {
                                viewport.RefreshFloatingObjectMovingFrames();
                            }
                            else
                            {
                                viewport.ResetFloatingObjectovingFrames();
                            }
                        }
                    }
                }
            }
        }

        void RefreshViewportFloatingObjectsContainerResizing()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            if (IsNeedRefreshFloatingObjectsMovingResizingContainer(viewport.RowViewportIndex, viewport.ColumnViewportIndex))
                            {
                                viewport.RefreshFlaotingObjectResizingFrames();
                            }
                            else
                            {
                                viewport.ResetFloatingObjectResizingFrames();
                            }
                        }
                    }
                }
            }
        }

        void RefreshViewportFloatingObjectsLayout()
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.InvalidateFloatingObjectsMeasureState();
                        }
                    }
                }
            }
        }

        internal void RemoveDataValidationUI()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.RemoveDataValidationUI();
                        }
                    }
                }
            }
        }

        internal virtual void Reset()
        {
            Init();
        }

        internal void ResetCursor()
        {
            SetBuiltInCursor(CoreCursorType.Arrow);
        }

        void ResetDragFill()
        {
            ResetMouseCursor();
            IsWorking = false;
            IsDraggingFill = false;
            ResetDragFillViewportInfo();
            StopScrollTimer();
            TooltipHelper.CloseTooltip();
        }

        void ResetDragFillViewportInfo()
        {
            _dragStartRowViewport = -2;
            _dragStartColumnViewport = -2;
            _dragToRowViewport = -2;
            _dragToColumnViewport = -2;
            _dragToRow = -2;
            _dragToColumn = -2;
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.ResetDragFill();
                        }
                    }
                }
            }
        }

        void ResetFlagasAfterDragDropping()
        {
            IsWorking = false;
            _dragDropIndicator.Clip = null;
            _dragDropIndicator.Visibility = Visibility.Collapsed;
            _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
            _dragDropFromRange = null;
            _dragDropRowOffset = 0;
            _dragDropColumnOffset = 0;
            _isDragInsert = false;
            _isDragCopy = false;
            _dragStartRowViewport = -2;
            _dragStartColumnViewport = -2;
            _dragToRowViewport = -2;
            _dragToColumnViewport = -2;
            _dragToRow = -2;
            _dragToColumn = -2;
        }

        void ResetFloatingObjectsMovingResizing()
        {
            IsWorking = false;
            IsMovingFloatingOjects = false;
            IsResizingFloatingObjects = false;
            IsTouchingMovingFloatingObjects = false;
            IsTouchingResizingFloatingObjects = false;
            _movingResizingFloatingObjects = null;
            _dragStartRowViewport = -2;
            _dragStartColumnViewport = -2;
            _dragToRowViewport = -2;
            _dragToColumnViewport = -2;
            _dragToRow = -2;
            _dragToColumn = -2;
            _floatingObjectsMovingResizingStartRow = -2;
            _floatingObjectsMovingResizingStartColumn = -2;
            _floatingObjectsMovingResizingOffset = new Point(0.0, 0.0);
            _floatingObjectsMovingStartLocations = null;
            _floatingObjectsMovingResizingStartPointCellBounds = new Rect(0.0, 0.0, 0.0, 0.0);
            _cachedFloatingObjectMovingResizingLayoutModel = null;
            ResetViewportFloatingObjectsContainerMoving();
            ResetViewportFloatingObjectsContainerReSizing();
        }

        void ResetMouseCursor()
        {
            if (_mouseCursor != null)
            {
                _mouseCursor.Opacity = 0.0;
            }
            ResetCursor();
        }

        void ResetSelectionFrameStroke()
        {
            if (_resetSelectionFrameStroke)
            {
                ViewportInfo viewportInfo = GetViewportInfo();
                int rowViewportCount = viewportInfo.RowViewportCount;
                int columnViewportCount = viewportInfo.ColumnViewportCount;
                for (int i = -1; i <= rowViewportCount; i++)
                {
                    for (int j = -1; j <= columnViewportCount; j++)
                    {
                        GcViewport viewportRowsPresenter = GetViewportRowsPresenter(i, j);
                        if (viewportRowsPresenter != null)
                        {
                            viewportRowsPresenter.SelectionContainer.ResetSelectionFrameStroke();
                        }
                    }
                }
            }
            _resetSelectionFrameStroke = false;
        }

        void ResetTouchDragFill()
        {
            ResetMouseCursor();
            IsWorking = false;
            ResetDragFillViewportInfo();
            StopScrollTimer();
            TooltipHelper.CloseTooltip();
        }

        void ResetViewportFloatingObjectsContainerMoving()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.ResetFloatingObjectovingFrames();
                        }
                    }
                }
            }
        }

        void ResetViewportFloatingObjectsContainerReSizing()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.ResetFloatingObjectResizingFrames();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resumes the events.
        /// </summary>
        public void ResumeEvent()
        {
            _eventSuspended--;
            if (_eventSuspended < 0)
            {
                _eventSuspended = 0;
            }
        }

        internal void ResumeFloatingObjectsInvalidate()
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.ResumeFloatingObjectsInvalidate(false);
                        }
                    }
                }
            }
        }

        internal void ResumeInvalidate()
        {
            _suspendViewInvalidate--;
            if (_suspendViewInvalidate < 0)
            {
                _suspendViewInvalidate = 0;
            }
            ResumeFloatingObjectsInvalidate();
        }

        double RoundToPoint(double value)
        {
            return Math.Floor(value);
        }

        /// <summary>
        /// Sets the active cell of the sheet.
        /// </summary>
        /// <param name="row">The active row index.</param>
        /// <param name="column">The active column index.</param>
        /// <param name="clearSelection"> if set to <c>true</c> clears the old selection.</param>
        public void SetActiveCell(int row, int column, bool clearSelection)
        {
            if (Worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable)
            {
                SetActiveCellInternal(row, column, clearSelection);
            }
        }

        internal void SetActiveCellInternal(int row, int column, bool clearSelection)
        {
            if ((row != Worksheet.ActiveRowIndex) || (column != Worksheet.ActiveColumnIndex))
            {
                Worksheet.SetActiveCell(row, column, clearSelection);
                RaiseEnterCell(row, column);
            }
        }

        internal void SetActiveColumnViewportIndex(int value)
        {
            ViewportInfo viewportInfo = Worksheet.GetViewportInfo();
            if (viewportInfo.ActiveColumnViewport != value)
            {
                viewportInfo.ActiveColumnViewport = value;
                Worksheet.SetViewportInfo(viewportInfo);
                UpdateFocusIndicator();
                UpdateDataValidationUI(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
            }
        }

        void SetActiveportIndexAfterDragDrop()
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            if ((_dragToRowViewport != -2) && (_dragToColumnViewport != -2))
            {
                int num = _dragToRowViewport;
                int num2 = _dragToColumnViewport;
                int activeRowIndex = Worksheet.ActiveRowIndex;
                int activeColumnIndex = Worksheet.ActiveColumnIndex;
                if ((num == 0) && (activeRowIndex < Worksheet.FrozenRowCount))
                {
                    num = -1;
                }
                else if ((num == viewportInfo.RowViewportCount) && (activeRowIndex < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                {
                    num = viewportInfo.RowViewportCount - 1;
                }
                if ((num2 == 0) && (activeColumnIndex < Worksheet.FrozenColumnCount))
                {
                    num2 = -1;
                }
                else if ((num2 == viewportInfo.ColumnViewportCount) && (activeColumnIndex < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                {
                    num2 = viewportInfo.ColumnViewportCount - 1;
                }
                if (num != GetActiveRowViewportIndex())
                {
                    SetActiveRowViewportIndex(num);
                }
                if (num2 != GetActiveColumnViewportIndex())
                {
                    SetActiveColumnViewportIndex(num2);
                }
            }
        }

        internal void SetActiveRowViewportIndex(int value)
        {
            ViewportInfo viewportInfo = Worksheet.GetViewportInfo();
            if (viewportInfo.ActiveRowViewport != value)
            {
                viewportInfo.ActiveRowViewport = value;
                Worksheet.SetViewportInfo(viewportInfo);
                UpdateFocusIndicator();
                UpdateDataValidationUI(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
            }
        }

        internal void SetBuiltInCursor(CoreCursorType cursorType)
        {
#if UWP
            CoreWindow win = Windows.UI.Xaml.Window.Current.CoreWindow;
            win.PointerCursor = new CoreCursor(cursorType, 0);
#endif
        }

        void SetCursor(HitTestInformation hi)
        {
            if (_allowDragFill && hi.ViewportInfo.InDragFillIndicator)
            {
                bool flag;
                bool flag2;
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                CursorType cursorType = flag2 ? CursorType.DragFill_CtrlDragCursor : CursorType.DragFill_DragCursor;
                SetMouseCursor(cursorType);
            }
            else if (_allowDragDrop && hi.ViewportInfo.InSelectionDrag)
            {
                bool flag3;
                bool flag4;
                KeyboardHelper.GetMetaKeyState(out flag3, out flag4);
                CursorType type2 = flag4 ? CursorType.DragCell_CtrlDragCursor : CursorType.DragCell_DragCursor;
                SetMouseCursor(type2);
            }
            else
            {
                if (_mouseCursor != null)
                {
                    _mouseCursor.Opacity = 0.0;
                }
                ResetCursor();
            }
        }

        void SetCursorForFloatingObject(ViewportFloatingObjectHitTestInformation chartInfo)
        {
            // hdt 图表锁定时显示默认光标
            if (chartInfo.FloatingObject != null && chartInfo.FloatingObject.Locked)
            {
                ResetCursor();
            }
            else if (chartInfo.InMoving)
            {
                SetMouseCursor(CursorType.DragCell_DragCursor);
            }
            else if (chartInfo.InTopNWSEResize || chartInfo.InBottomNWSEResize)
            {
                SetBuiltInCursor(CoreCursorType.SizeNorthwestSoutheast);
            }
            else if (chartInfo.InLeftWEResize || chartInfo.InRightWEResize)
            {
                SetBuiltInCursor(CoreCursorType.SizeWestEast);
            }
            else if (chartInfo.InTopNSResize || chartInfo.InBottomNSResize)
            {
                SetBuiltInCursor(CoreCursorType.SizeNorthSouth);
            }
            else if (chartInfo.InTopNESWResize || chartInfo.InBottomNESWResize)
            {
                SetBuiltInCursor(CoreCursorType.SizeNortheastSouthwest);
            }
            else
            {
                ResetCursor();
            }
        }

        /// <summary>
        /// Sets the index of the floating object Z.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="zIndex">Index of the z.</param>
        public void SetFloatingObjectZIndex(string name, int zIndex)
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.SetFlotingObjectZIndex(name, zIndex);
                        }
                    }
                }
            }
        }

        internal void SetMouseCursor(CursorType cursorType)
        {
            if (_mouseCursor == null)
            {
                _mouseCursor = new Image();
                _mouseCursor.IsHitTestVisible = false;
                CursorsContainer.Children.Add(_mouseCursor);
            }
            _mouseCursor.Opacity = 1.0;
            HideCursor();
            _mouseCursor.Source = CursorGenerator.GetCursor(cursorType);
            _mouseCursor.SetValue(Canvas.LeftProperty, (double)(MousePosition.X - 32.0));
            _mouseCursor.SetValue(Canvas.TopProperty, (double)(MousePosition.Y - 32.0));
        }

        /// <summary>
        /// Selects the specified cells.
        /// </summary>
        /// <param name="row">The row index of the first cell.</param>
        /// <param name="column">The column index of the first cell.</param>
        /// <param name="rowCount">The number of rows in the selection.</param>
        /// <param name="columnCount">The number of columns in the selection.</param>
        public void SetSelection(int row, int column, int rowCount, int columnCount)
        {
            if (Worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable)
            {
                Worksheet.SetSelection(row, column, rowCount, columnCount);
            }
        }

        void SetSelectionFrame(int rowViewportIndex, int columnViewportIndex)
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            int rowViewportCount = viewportInfo.RowViewportCount;
            int columnViewportCount = viewportInfo.ColumnViewportCount;
            List<Tuple<int, int>> list = new List<Tuple<int, int>> {
                new Tuple<int, int>(rowViewportIndex, columnViewportIndex)
            };
            if ((columnViewportIndex == -1) || (columnViewportIndex == 0))
            {
                list.Add(new Tuple<int, int>(rowViewportIndex, 0));
                list.Add(new Tuple<int, int>(rowViewportIndex, -1));
            }
            if ((columnViewportIndex == columnViewportCount) || (columnViewportIndex == (columnViewportCount - 1)))
            {
                list.Add(new Tuple<int, int>(rowViewportIndex, columnViewportCount - 1));
                list.Add(new Tuple<int, int>(rowViewportIndex, columnViewportCount));
            }
            if ((rowViewportIndex == -1) || (rowViewportIndex == 0))
            {
                list.Add(new Tuple<int, int>(0, columnViewportIndex));
                list.Add(new Tuple<int, int>(-1, columnViewportIndex));
            }
            if ((rowViewportIndex == rowViewportCount) || (rowViewportIndex == (rowViewportCount - 1)))
            {
                list.Add(new Tuple<int, int>(rowViewportCount, columnViewportIndex));
                list.Add(new Tuple<int, int>(rowViewportCount - 1, columnViewportIndex));
            }
            foreach (Tuple<int, int> tuple in Enumerable.Distinct<Tuple<int, int>>((IEnumerable<Tuple<int, int>>)list))
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(tuple.Item1, tuple.Item2);
                if (viewportRowsPresenter != null)
                {
                    SolidColorBrush brush = new SolidColorBrush(Windows.UI.Color.FromArgb(0xff, 0x21, 0x73, 70));
                    viewportRowsPresenter.SelectionContainer.SetSelectionFrameStroke(brush);
                    _resetSelectionFrameStroke = true;
                }
            }
        }

        internal virtual void SetViewportInfo(Dt.Cells.Data.Worksheet sheet, ViewportInfo value)
        {
            sheet.SetViewportInfo(value);
        }

        /// <summary>
        /// Sets the active column viewport's left column.
        /// </summary>
        /// <param name="value">The column index.</param>
        public void SetViewportLeftColumn(int value)
        {
            SetViewportLeftColumn(0, value);
        }

        /// <summary>
        /// Sets the column viewport's left column.
        /// </summary>
        /// <param name="columnViewportIndex">The column viewport index.</param>
        /// <param name="value">The column index.</param>
        public virtual void SetViewportLeftColumn(int columnViewportIndex, int value)
        {
            if ((Worksheet != null) && (_hScrollable || _isTouchScrolling))
            {
                ViewportInfo viewportInfo = GetViewportInfo();
                value = Math.Max(Worksheet.FrozenColumnCount, value);
                value = Math.Min((Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1, value);
                value = TryGetNextScrollableColumn(value);
                if (((columnViewportIndex >= 0) && (columnViewportIndex < viewportInfo.ColumnViewportCount)) && (viewportInfo.LeftColumns[columnViewportIndex] != value))
                {
                    int oldIndex = viewportInfo.LeftColumns[columnViewportIndex];
                    viewportInfo.LeftColumns[columnViewportIndex] = value;
                    InvalidateViewportColumnsLayout();
                    InvalidateViewportHorizontalArrangement(columnViewportIndex);
                    if (_columnGroupPresenters != null)
                    {
                        GcRangeGroup group = _columnGroupPresenters[columnViewportIndex + 1];
                        if (group != null)
                        {
                            group.InvalidateMeasure();
                        }
                    }
                    RaiseLeftChanged(oldIndex, value, columnViewportIndex);
                }
                if (!IsWorking)
                {
                    SaveHitInfo(null);
                }
            }
        }

        /// <summary>
        /// Sets the active row viewport's top row.
        /// </summary>
        /// <param name="value">The row index.</param>
        public void SetViewportTopRow(int value)
        {
            SetViewportTopRow(0, value);
        }

        /// <summary>
        /// Sets the row viewport's top row.
        /// </summary>
        /// <param name="rowViewportIndex">The row viewport index.</param>
        /// <param name="value">The row index.</param>
        public virtual void SetViewportTopRow(int rowViewportIndex, int value)
        {
            if ((Worksheet != null) && (_vScrollable || _isTouchScrolling))
            {
                ViewportInfo viewportInfo = GetViewportInfo();
                value = Math.Max(Worksheet.FrozenRowCount, value);
                value = Math.Min((Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1, value);
                value = TryGetNextScrollableRow(value);
                if (((rowViewportIndex >= 0) && (rowViewportIndex < viewportInfo.RowViewportCount)) && (viewportInfo.TopRows[rowViewportIndex] != value))
                {
                    int oldIndex = viewportInfo.TopRows[rowViewportIndex];
                    viewportInfo.TopRows[rowViewportIndex] = value;
                    InvalidateViewportRowsLayout();
                    InvalidateViewportRowsPresenterMeasure(rowViewportIndex, false);
                    for (int i = -1; i < viewportInfo.ColumnViewportCount; i++)
                    {
                        GcViewport viewportRowsPresenter = GetViewportRowsPresenter(rowViewportIndex, i);
                        if (viewportRowsPresenter != null)
                        {
                            if ((viewportRowsPresenter.RowViewportIndex == GetActiveRowViewportIndex()) && (viewportRowsPresenter.ColumnViewportIndex == GetActiveColumnViewportIndex()))
                            {
                                viewportRowsPresenter.UpdateDataValidationUI(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
                            }
                            viewportRowsPresenter.InvalidateMeasure();
                            viewportRowsPresenter.InvalidateBordersMeasureState();
                            viewportRowsPresenter.InvalidateSelectionMeasureState();
                            viewportRowsPresenter.InvalidateFloatingObjectsMeasureState();
                        }
                    }
                    GcViewport rowHeaderRowsPresenter = GetRowHeaderRowsPresenter(rowViewportIndex);
                    if (rowHeaderRowsPresenter != null)
                    {
                        rowHeaderRowsPresenter.InvalidateBordersMeasureState();
                        rowHeaderRowsPresenter.InvalidateMeasure();
                    }
                    if (_rowGroupPresenters != null)
                    {
                        GcRangeGroup group = _rowGroupPresenters[rowViewportIndex + 1];
                        if (group != null)
                        {
                            group.InvalidateMeasure();
                        }
                    }
                    RaiseTopChanged(oldIndex, value, rowViewportIndex);
                }
                if (!IsWorking)
                {
                    SaveHitInfo(null);
                }
            }
        }

        /// <summary>
        /// Displays the automatic fill indicator.
        /// </summary>
        public void ShowAutoFillIndicator()
        {
            if (CanUserDragFill)
            {
                GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
                if (viewportRowsPresenter != null)
                {
                    CellRange activeSelection = GetActiveSelection();
                    if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
                    {
                        activeSelection = Worksheet.Selections[0];
                    }
                    if (activeSelection != null)
                    {
                        _autoFillIndicatorContainer.Width = 16.0;
                        _autoFillIndicatorContainer.Height = 16.0;
                        AutoFillIndicatorRec = new Rect?(GetAutoFillIndicatorRect(viewportRowsPresenter, activeSelection));
                        base.InvalidateArrange();
                        CachedGripperLocation = null;
                    }
                }
            }
        }

        internal void ShowCell(int rowViewportIndex, int columnViewportIndex, int row, int column, VerticalPosition verticalPosition, HorizontalPosition horizontalPosition)
        {
            Dt.Cells.Data.Worksheet worksheet = Worksheet;
            if (((worksheet != null) && (row <= worksheet.RowCount)) && (column <= worksheet.ColumnCount))
            {
                int viewportTopRow = GetViewportTopRow(rowViewportIndex);
                int viewportLeftColumn = GetViewportLeftColumn(columnViewportIndex);
                switch (horizontalPosition)
                {
                    case HorizontalPosition.Center:
                        {
                            double num3 = RoundToPoint((GetViewportWidth(columnViewportIndex) - RoundToPoint(worksheet.Columns[column].ActualWidth * ZoomFactor)) / 2.0);
                            while (0 < column)
                            {
                                num3 -= RoundToPoint(worksheet.Columns[column - 1].ActualWidth * ZoomFactor);
                                if (num3 < 0.0)
                                {
                                    break;
                                }
                                column--;
                            }
                            break;
                        }
                    case HorizontalPosition.Right:
                        {
                            double num4 = GetViewportWidth(columnViewportIndex) - RoundToPoint(worksheet.Columns[column].ActualWidth * ZoomFactor);
                            while (0 < column)
                            {
                                num4 -= RoundToPoint(worksheet.Columns[column - 1].ActualWidth * ZoomFactor);
                                if (num4 < 0.0)
                                {
                                    break;
                                }
                                column--;
                            }
                            break;
                        }
                    case HorizontalPosition.Nearest:
                        if (column >= viewportLeftColumn)
                        {
                            double num5 = GetViewportWidth(columnViewportIndex) - RoundToPoint(worksheet.Columns[column].Width * ZoomFactor);
                            while (viewportLeftColumn < column)
                            {
                                num5 -= RoundToPoint(worksheet.Columns[column - 1].ActualWidth * ZoomFactor);
                                if (num5 < 0.0)
                                {
                                    break;
                                }
                                column--;
                            }
                        }
                        break;
                }
                switch (verticalPosition)
                {
                    case VerticalPosition.Center:
                        {
                            double num6 = RoundToPoint((GetViewportHeight(rowViewportIndex) - RoundToPoint(worksheet.Rows[row].ActualHeight * ZoomFactor)) / 2.0);
                            while (0 < row)
                            {
                                num6 -= RoundToPoint(worksheet.Rows[row - 1].ActualHeight * ZoomFactor);
                                if (num6 < 0.0)
                                {
                                    break;
                                }
                                row--;
                            }
                            break;
                        }
                    case VerticalPosition.Bottom:
                        {
                            double num7 = GetViewportHeight(rowViewportIndex) - RoundToPoint(worksheet.Rows[row].ActualHeight * ZoomFactor);
                            while (0 < row)
                            {
                                num7 -= RoundToPoint(worksheet.Rows[row - 1].ActualHeight * ZoomFactor);
                                if (num7 < 0.0)
                                {
                                    break;
                                }
                                row--;
                            }
                            break;
                        }
                    case VerticalPosition.Nearest:
                        if ((row >= viewportTopRow) && (viewportTopRow != -1))
                        {
                            double num8 = GetViewportHeight(rowViewportIndex) - RoundToPoint(worksheet.Rows[row].ActualHeight * ZoomFactor);
                            while (viewportTopRow < row)
                            {
                                num8 -= RoundToPoint(worksheet.Rows[row - 1].ActualHeight * ZoomFactor);
                                if (num8 < 0.0)
                                {
                                    break;
                                }
                                row--;
                            }
                        }
                        break;
                }
                if (row != viewportTopRow)
                {
                    SetViewportTopRow(rowViewportIndex, row);
                }
                if (column != viewportLeftColumn)
                {
                    SetViewportLeftColumn(columnViewportIndex, column);
                }
            }
        }

        internal void ShowColumn(int columnViewportIndex, int column, HorizontalPosition horizontalPosition)
        {
            int viewportTopRow = GetViewportTopRow(0);
            ShowCell(0, columnViewportIndex, viewportTopRow, column, VerticalPosition.Top, horizontalPosition);
        }

        void ShowDragFillSmartTag(CellRange fillRange, AutoFillType initFillType)
        {
            double x = 0.0;
            double y = 0.0;
            if (!IsDragFillWholeColumns && !IsDragFillWholeRows)
            {
                int index = (fillRange.Row + fillRange.RowCount) - 1;
                int num4 = (fillRange.Column + fillRange.ColumnCount) - 1;
                if (IsVerticalDragFill)
                {
                    ColumnLayout layout = GetViewportColumnLayoutModel(_dragFillStartRightColumnViewport).Find(num4);
                    if (layout == null)
                    {
                        int viewportRightColumn = GetViewportRightColumn(_dragFillStartLeftColumnViewport);
                        layout = GetViewportColumnLayoutModel(_dragFillStartLeftColumnViewport).FindColumn(viewportRightColumn);
                    }
                    x = layout.X + layout.Width;
                    RowLayout validVerDragToRowLayout = GetValidVerDragToRowLayout();
                    y = validVerDragToRowLayout.Y + validVerDragToRowLayout.Height;
                }
                else
                {
                    RowLayout layout3 = GetViewportRowLayoutModel(_dragFillStartBottomRowViewport).Find(index);
                    if (layout3 == null)
                    {
                        int viewportBottomRow = GetViewportBottomRow(_dragFillStartTopRowViewport);
                        layout3 = GetViewportRowLayoutModel(_dragFillStartTopRowViewport).FindRow(viewportBottomRow);
                    }
                    y = layout3.Y + layout3.Height;
                    ColumnLayout validHorDragToColumnLayout = GetValidHorDragToColumnLayout();
                    x = validHorDragToColumnLayout.X + validHorDragToColumnLayout.Width;
                }
            }
            else if (IsDragFillWholeColumns && !IsDragFillWholeRows)
            {
                int column = fillRange.Column;
                int columnCount = fillRange.ColumnCount;
                ColumnLayout layout5 = GetValidHorDragToColumnLayout();
                x = layout5.X + layout5.Width;
                y = DragFillStartViewportTopRowLayout.Y;
            }
            else if (IsDragFillWholeRows && !IsDragFillWholeColumns)
            {
                int row = fillRange.Row;
                int rowCount = fillRange.RowCount;
                RowLayout layout6 = GetValidVerDragToRowLayout();
                y = layout6.Y + layout6.Height;
                x = DragFillStartViewportLeftColumnLayout.X;
                y = layout6.Y + layout6.Height;
            }
            if ((x != 0.0) && (y != 0.0))
            {
                x -= 4.0;
                y++;
                Windows.UI.Xaml.Controls.Primitives.Popup popup = new Windows.UI.Xaml.Controls.Primitives.Popup();
                _dragFillPopup = new PopupHelper(popup);
                base.Children.Add(popup);
                popup.Closed += DragFillSmartTagPopup_Closed;
                _dragFillSmartTag = new DragFillSmartTag(this);
                _dragFillSmartTag.AutoFilterType = initFillType;
                _dragFillSmartTag.AutoFilterTypeChanged += new EventHandler(DragFillSmartTag_AutoFilterTypeChanged);
                if (InputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                {
                    x += 4.0;
                    y += 4.0;
                }
                _dragFillPopup.ShowAsModal(this, _dragFillSmartTag, new Point(x, y), PopupDirection.BottomRight, false, false);
            }
        }

        internal void ShowFormulaSelectionTouchGrippers()
        {
            if (_formulaSelectionGripperPanel != null)
            {
                _formulaSelectionGripperPanel.Visibility = Visibility.Visible;
            }
        }

        internal void ShowRow(int rowViewportIndex, int row, VerticalPosition verticalPosition)
        {
            int viewportLeftColumn = GetViewportLeftColumn(0);
            ShowCell(rowViewportIndex, 0, row, viewportLeftColumn, verticalPosition, HorizontalPosition.Left);
        }

        IEnumerable<FloatingObject> SortFloatingObjectByZIndex(FloatingObject[] floatingObjects)
        {
            Dictionary<int, List<FloatingObject>> dictionary = new Dictionary<int, List<FloatingObject>>();
            foreach (FloatingObject obj2 in floatingObjects)
            {
                int floatingObjectZIndex = GetFloatingObjectZIndex(obj2.Name);
                List<FloatingObject> list = null;
                dictionary.TryGetValue(floatingObjectZIndex, out list);
                if (list == null)
                {
                    list = new List<FloatingObject> {
                        obj2
                    };
                    dictionary.Add(floatingObjectZIndex, list);
                }
                else
                {
                    list.Add(obj2);
                }
            }
            IOrderedEnumerable<KeyValuePair<int, List<FloatingObject>>> enumerable = Enumerable.OrderBy<KeyValuePair<int, List<FloatingObject>>, int>((IEnumerable<KeyValuePair<int, List<FloatingObject>>>)dictionary, delegate (KeyValuePair<int, List<FloatingObject>> p)
            {
                return p.Key;
            });
            List<FloatingObject> list2 = new List<FloatingObject>();
            foreach (KeyValuePair<int, List<FloatingObject>> pair in enumerable)
            {
                list2.AddRange(pair.Value);
            }
            list2.Reverse();
            return (IEnumerable<FloatingObject>)list2;
        }

        /// <summary>
        /// Starts to edit the active cell.
        /// </summary>
        /// <param name="selectAll">if set to <c>true</c> selects all the text when the text is changed during editing.</param>
        /// <param name="defaultText">if set to <c>true</c> [default text].</param>
        public void StartCellEditing(bool selectAll = false, string defaultText = null)
        {
            StartCellEditing(selectAll, defaultText, EditorStatus.Edit);
        }

        /// <summary>
        /// Starts to edit the active cell.
        /// </summary>
        /// <param name="selectAll">if set to <c>true</c> will select all the text when text changed during editing.</param>
        /// <param name="defaultText">The default text of editor.</param>
        /// <param name="status">The status of the editor</param>
        internal void StartCellEditing(bool selectAll = false, string defaultText = null, EditorStatus status = EditorStatus.Edit)
        {
            StartTextInputInternal(selectAll, defaultText, status, false);
        }

        void StartCellSelecting()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if (savedHitTestInformation.ViewportInfo != null)
            {
                int row = savedHitTestInformation.ViewportInfo.Row;
                int column = savedHitTestInformation.ViewportInfo.Column;
                int rowCount = 1;
                int columnCount = 1;
                if ((savedHitTestInformation.ViewportInfo.Row > -1) && (savedHitTestInformation.ViewportInfo.Column > -1))
                {
                    bool flag;
                    bool flag2;
                    CellLayout layout = GetViewportCellLayoutModel(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.ColumnViewportIndex).FindCell(savedHitTestInformation.ViewportInfo.Row, savedHitTestInformation.ViewportInfo.Column);
                    KeyboardHelper.GetMetaKeyState(out flag2, out flag);
                    if (layout != null)
                    {
                        row = layout.Row;
                        column = layout.Column;
                        rowCount = layout.RowCount;
                        columnCount = layout.ColumnCount;
                    }
                    if (Worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable)
                    {
                        IsWorking = true;
                        if (PreviewLeaveCell(row, column))
                        {
                            IsWorking = false;
                        }
                        else
                        {
                            IsSelectingCells = true;
                            IsTouchSelectingCells = false;
                            SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                            SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                            CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                            SavedOldSelections = oldSelection;
                            if (!flag2)
                            {
                                SetActiveCellInternal(row, column, false);
                            }
                            if (flag)
                            {
                                AddSelection(row, column, 1, 1);
                            }
                            else if (flag2)
                            {
                                ExtendSelection((row + rowCount) - 1, (column + columnCount) - 1);
                            }
                            else
                            {
                                Worksheet.SetSelection(row, column, 1, 1);
                            }
                            RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                            if (!IsWorking || !IsSelectingCells)
                            {
                                EndCellSelecting();
                            }
                            StartScrollTimer();
                        }
                    }
                }
            }
        }

        void StartColumnResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            SheetLayout sheetLayout = GetSheetLayout();
            ColumnLayout viewportResizingColumnLayoutFromX = null;
            IsWorking = true;
            IsResizingColumns = true;
            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            if (_resizingTracker == null)
            {
                Line line = new Line();
                line.Stroke = brush;
                line.StrokeThickness = 1.0;
                line.StrokeDashArray = new DoubleCollection { 1.0 };
                _resizingTracker = line;
                TrackersContainer.Children.Add(_resizingTracker);
            }
            _resizingTracker.Visibility = Visibility.Visible;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingColumnLayoutFromX = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                    break;

                case HitTestType.ColumnHeader:
                    viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                    if (viewportResizingColumnLayoutFromX == null)
                    {
                        viewportResizingColumnLayoutFromX = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if (viewportResizingColumnLayoutFromX == null)
                        {
                            if (savedHitTestInformation.ColumnViewportIndex == 0)
                            {
                                viewportResizingColumnLayoutFromX = GetViewportResizingColumnLayoutFromX(-1, savedHitTestInformation.HitPoint.X);
                            }
                            if ((viewportResizingColumnLayoutFromX == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                            {
                                viewportResizingColumnLayoutFromX = GetRowHeaderResizingColumnLayoutFromX(savedHitTestInformation.HitPoint.X);
                            }
                        }
                    }
                    break;
            }
            if (viewportResizingColumnLayoutFromX != null)
            {
                _resizingTracker.X1 = (viewportResizingColumnLayoutFromX.X + viewportResizingColumnLayoutFromX.Width) - 0.5;
                _resizingTracker.Y1 = sheetLayout.HeaderY;
                _resizingTracker.X2 = _resizingTracker.X1;
                _resizingTracker.Y2 = _resizingTracker.Y1 + AvailableSize.Height;
                if (((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Column))) && ((savedHitTestInformation.ColumnViewportIndex > -2) && (_columnHeaderPresenters[savedHitTestInformation.ColumnViewportIndex + 1].GetViewportCell(savedHitTestInformation.HeaderInfo.Row, savedHitTestInformation.HeaderInfo.Column, true) != null)))
                {
                    UpdateResizeToolTip(GetHorizontalResizeTip(viewportResizingColumnLayoutFromX.Width), true);
                }
            }
        }

        void StartColumnSelecting()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if ((savedHitTestInformation.HitTestType == HitTestType.Empty) || (savedHitTestInformation.HeaderInfo == null))
            {
                savedHitTestInformation = HitTest(_touchStartPoint.X, _touchStartPoint.Y);
            }
            if (savedHitTestInformation.HeaderInfo != null)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                int viewportTopRow = GetViewportTopRow((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                int column = savedHitTestInformation.HeaderInfo.Column;
                Cell canSelectedCellInColumn = GetCanSelectedCellInColumn(viewportTopRow, column);
                if (canSelectedCellInColumn != null)
                {
                    viewportTopRow = canSelectedCellInColumn.Row.Index;
                    IsWorking = true;
                    if (PreviewLeaveCell(viewportTopRow, column))
                    {
                        IsWorking = false;
                    }
                    else
                    {
                        if (IsTouching)
                        {
                            IsTouchSelectingColumns = true;
                        }
                        else
                        {
                            IsSelectingColumns = true;
                        }
                        SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                        SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                        if (savedHitTestInformation.HeaderInfo.Column > -1)
                        {
                            CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                            if (InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch)
                            {
                                bool flag2;
                                bool flag3;
                                KeyboardHelper.GetMetaKeyState(out flag3, out flag2);
                                SavedOldSelections = oldSelection;
                                if (!flag3)
                                {
                                    SetActiveCellInternal(viewportTopRow, column, false);
                                }
                                if (flag2)
                                {
                                    AddSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1);
                                }
                                else if (flag3)
                                {
                                    ExtendSelection(-1, savedHitTestInformation.HeaderInfo.Column);
                                }
                                else
                                {
                                    Worksheet.SetSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1);
                                }
                                if (!flag3)
                                {
                                    Worksheet.SetActiveCell(viewportTopRow, column, false);
                                }
                            }
                            else
                            {
                                if ((Worksheet.SelectionPolicy == SelectionPolicy.MultiRange) && CanTouchMultiSelect)
                                {
                                    Worksheet.AddSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1);
                                }
                                else
                                {
                                    Worksheet.SetSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1);
                                }
                                Worksheet.SetActiveCell(viewportTopRow, column, false);
                            }
                            RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                            if (!IsWorking || (!IsSelectingColumns && !IsTouchSelectingColumns))
                            {
                                EndColumnSelecting();
                            }
                            StartScrollTimer();
                        }
                    }
                }
            }
        }

        void StartDragDropping()
        {
            if (!IsDragDropping)
            {
                CellRange fromRange = GetFromRange();
                if (fromRange != null)
                {
                    IsDragDropping = true;
                    IsWorking = true;
                    UpdateDragIndicatorAndStartTimer(fromRange);
                }
            }
        }

        void StartDragFill()
        {
            if (!IsDraggingFill)
            {
                UpdateDragFillStartRange();
                if (_dragFillStartRange != null)
                {
                    IsDraggingFill = true;
                    IsWorking = true;
                    UpdateDragFillViewportInfoAndStartTimer();
                }
            }
        }

        void StartFloatingObjectsMoving()
        {
            _movingResizingFloatingObjects = GetAllSelectedFloatingObjects();
            if (((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length != 0)) && InitFloatingObjectsMovingResizing())
            {
                if ((_touchToolbarPopup != null) && _touchToolbarPopup.IsOpen)
                {
                    _touchToolbarPopup.IsOpen = false;
                }
                IsWorking = true;
                if (IsTouching)
                {
                    IsTouchingMovingFloatingObjects = true;
                }
                else
                {
                    IsMovingFloatingOjects = true;
                }
                StartScrollTimer();
            }
        }

        void StartFloatingObjectsResizing()
        {
            _movingResizingFloatingObjects = GetAllSelectedFloatingObjects();
            if (((_movingResizingFloatingObjects != null) && (_movingResizingFloatingObjects.Length != 0)) && InitFloatingObjectsMovingResizing())
            {
                if ((_touchToolbarPopup != null) && _touchToolbarPopup.IsOpen)
                {
                    _touchToolbarPopup.IsOpen = false;
                }
                IsWorking = true;
                if (IsTouching)
                {
                    IsTouchingResizingFloatingObjects = true;
                }
                else
                {
                    IsResizingFloatingObjects = true;
                }
                StartScrollTimer();
            }
        }

        void StartRowResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            SheetLayout sheetLayout = GetSheetLayout();
            RowLayout viewportResizingRowLayoutFromY = null;
            IsResizingRows = true;
            IsWorking = true;
            if (_resizingTracker == null)
            {
                _resizingTracker = new Line();
                _resizingTracker.Stroke = new SolidColorBrush(Colors.Black);
                _resizingTracker.StrokeThickness = 1.0;
                _resizingTracker.StrokeDashArray = new DoubleCollection { 1.0 };
                TrackersContainer.Children.Add(_resizingTracker);
            }
            _resizingTracker.Visibility = Visibility.Visible;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingRowLayoutFromY = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    break;

                case HitTestType.RowHeader:
                    viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                    if (((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                    {
                        viewportResizingRowLayoutFromY = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    }
                    if ((viewportResizingRowLayoutFromY == null) && (savedHitTestInformation.RowViewportIndex == 0))
                    {
                        viewportResizingRowLayoutFromY = GetViewportResizingRowLayoutFromY(-1, savedHitTestInformation.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromY == null) && ((savedHitTestInformation.RowViewportIndex == 0) || (savedHitTestInformation.RowViewportIndex == -1)))
                    {
                        viewportResizingRowLayoutFromY = GetColumnHeaderResizingRowLayoutFromY(savedHitTestInformation.HitPoint.Y);
                    }
                    break;
            }
            if (viewportResizingRowLayoutFromY != null)
            {
                _resizingTracker.X1 = sheetLayout.HeaderX;
                _resizingTracker.X2 = sheetLayout.HeaderX + AvailableSize.Width;
                _resizingTracker.Y1 = (viewportResizingRowLayoutFromY.Y + viewportResizingRowLayoutFromY.Height) - 0.5;
                _resizingTracker.Y2 = _resizingTracker.Y1;
                if (((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Row))) && ((savedHitTestInformation.RowViewportIndex > -2) && (_rowHeaderPresenters[savedHitTestInformation.RowViewportIndex + 1].GetViewportCell(savedHitTestInformation.HeaderInfo.Row, savedHitTestInformation.HeaderInfo.Column, true) != null)))
                {
                    UpdateResizeToolTip(GetVerticalResizeTip(viewportResizingRowLayoutFromY.Height), false);
                }
            }
        }

        void StartRowsSelecting()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if ((savedHitTestInformation.HitTestType == HitTestType.Empty) || (savedHitTestInformation.HeaderInfo == null))
            {
                savedHitTestInformation = HitTest(_touchStartPoint.X, _touchStartPoint.Y);
            }
            if (savedHitTestInformation.HeaderInfo != null)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                int row = savedHitTestInformation.HeaderInfo.Row;
                int viewportLeftColumn = GetViewportLeftColumn((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                Cell canSelectedCellInRow = GetCanSelectedCellInRow(row, viewportLeftColumn);
                if (canSelectedCellInRow != null)
                {
                    viewportLeftColumn = canSelectedCellInRow.Column.Index;
                    IsWorking = true;
                    if (PreviewLeaveCell(row, viewportLeftColumn))
                    {
                        IsWorking = false;
                    }
                    else
                    {
                        if (!IsTouching)
                        {
                            IsSelectingRows = true;
                        }
                        else
                        {
                            IsTouchSelectingRows = true;
                        }
                        SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                        SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                        if (savedHitTestInformation.HeaderInfo.Row > -1)
                        {
                            CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                            SavedOldSelections = oldSelection;
                            if (InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch)
                            {
                                bool flag2;
                                bool flag3;
                                KeyboardHelper.GetMetaKeyState(out flag2, out flag3);
                                if (!flag2)
                                {
                                    SetActiveCellInternal(row, viewportLeftColumn, false);
                                }
                                if (flag3)
                                {
                                    AddSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1);
                                }
                                else if (flag2)
                                {
                                    ExtendSelection(savedHitTestInformation.HeaderInfo.Row, -1);
                                }
                                else
                                {
                                    Worksheet.SetSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1);
                                }
                                if (!flag2)
                                {
                                    Worksheet.SetActiveCell(row, viewportLeftColumn, false);
                                }
                            }
                            else
                            {
                                if ((Worksheet.SelectionPolicy == SelectionPolicy.MultiRange) && CanTouchMultiSelect)
                                {
                                    Worksheet.AddSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1);
                                }
                                else
                                {
                                    Worksheet.SetSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1);
                                }
                                Worksheet.SetActiveCell(savedHitTestInformation.HeaderInfo.Row, viewportLeftColumn, false);
                            }
                            RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                            if (!IsWorking || (!IsSelectingRows && !IsTouchSelectingRows))
                            {
                                EndRowSelecting();
                            }
                            StartScrollTimer();
                        }
                    }
                }
            }
        }

        void StartScrollTimer()
        {
            if (IsWorking)
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if (((savedHitTestInformation.HitTestType == HitTestType.Viewport) || (savedHitTestInformation.HitTestType == HitTestType.RowHeader)) || ((savedHitTestInformation.HitTestType == HitTestType.FloatingObject) || (savedHitTestInformation.HitTestType == HitTestType.FormulaSelection)))
                {
                    double viewportHeight = GetViewportHeight(savedHitTestInformation.RowViewportIndex);
                    _verticalSelectionMgr = new ScrollSelectionManager(0.0, viewportHeight, new Action<bool>(OnVerticalSelectionTick));
                }
                if (((savedHitTestInformation.HitTestType == HitTestType.Viewport) || (savedHitTestInformation.HitTestType == HitTestType.ColumnHeader)) || ((savedHitTestInformation.HitTestType == HitTestType.FloatingObject) || (savedHitTestInformation.HitTestType == HitTestType.FormulaSelection)))
                {
                    double viewportWidth = GetViewportWidth(savedHitTestInformation.ColumnViewportIndex);
                    _horizontalSelectionMgr = new ScrollSelectionManager(0.0, viewportWidth, new Action<bool>(OnHorizontalSelectionTick));
                }
            }
        }

        void StartSheetSelecting()
        {
            SheetLayout sheetLayout = GetSheetLayout();
            int viewportTopRow = GetViewportTopRow((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
            int viewportLeftColumn = GetViewportLeftColumn((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
            Cell cell = GetCanSelectedCell(viewportTopRow, viewportLeftColumn, (viewportTopRow < 0) ? -1 : (Worksheet.RowCount - viewportTopRow), (viewportLeftColumn < 0) ? -1 : (Worksheet.ColumnCount - viewportLeftColumn));
            if (cell != null)
            {
                viewportTopRow = cell.Row.Index;
                viewportLeftColumn = cell.Column.Index;
                if (((Worksheet.ColumnCount <= 0) || (Worksheet.RowCount <= 0)) || !PreviewLeaveCell(viewportTopRow, viewportLeftColumn))
                {
                    SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                    SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                    CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                    if ((Worksheet.ColumnCount > 0) && (Worksheet.RowCount > 0))
                    {
                        SetActiveCellInternal(viewportTopRow, viewportLeftColumn, true);
                    }
                    Worksheet.ClearSelections();
                    Worksheet.AddSelection(-1, -1, -1, -1, false);
                    if (RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections)))
                    {
                        RaiseSelectionChanged();
                    }
                }
            }
        }

        void StartTapSelectCells()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            int row = savedHitTestInformation.ViewportInfo.Row;
            int column = savedHitTestInformation.ViewportInfo.Column;
            CloseTouchToolbar();
            if ((savedHitTestInformation.ViewportInfo.Row > -1) && (savedHitTestInformation.ViewportInfo.Column > -1))
            {
                CellLayout layout = GetViewportCellLayoutModel(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.ColumnViewportIndex).FindCell(savedHitTestInformation.ViewportInfo.Row, savedHitTestInformation.ViewportInfo.Column);
                if (layout != null)
                {
                    row = layout.Row;
                    column = layout.Column;
                    int rowCount = layout.RowCount;
                    int columnCount = layout.ColumnCount;
                }
                if (Worksheet.GetActualStyleInfo(row, column, SheetArea.Cells).Focusable)
                {
                    IsWorking = true;
                    if (PreviewLeaveCell(row, column))
                    {
                        IsWorking = false;
                    }
                    else
                    {
                        IsSelectingCells = false;
                        IsTouchSelectingCells = true;
                        SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                        SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                        SetActiveCellInternal(row, column, false);
                        CellRange[] oldSelection = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
                        SavedOldSelections = oldSelection;
                        if ((Worksheet.SelectionPolicy == SelectionPolicy.MultiRange) && CanTouchMultiSelect)
                        {
                            Worksheet.AddSelection(row, column, 1, 1);
                        }
                        else
                        {
                            Worksheet.SetSelection(row, column, 1, 1);
                            RefreshSelection();
                        }
                        RaiseSelectionChanging(oldSelection, Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections));
                        if (!IsWorking)
                        {
                            EndCellSelecting();
                        }
                    }
                }
            }
        }

        internal void StartTextInput(EditorStatus status = EditorStatus.Edit)
        {
            StartTextInputInternal(false, null, status, true);
        }

        void StartTextInputInternal(bool selectAll = false, string defaultText = null, EditorStatus status = (EditorStatus)2, bool fromTextInputService = false)
        {
            if ((!IsEditing || StopCellEditing(false)) && (Worksheet != null))
            {
                EditingViewport = null;
                if (!IsEditing && IsCellEditable(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex))
                {
                    GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
                    if (viewportRowsPresenter != null)
                    {
                        CoreWindow.GetForCurrentThread().ReleasePointerCapture();
                        EditingViewport = viewportRowsPresenter;
                        bool flag = false;
                        if (fromTextInputService)
                        {
                            flag = viewportRowsPresenter.StartTextInput(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, status);
                        }
                        else
                        {
                            flag = viewportRowsPresenter.StartCellEditing(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, selectAll, defaultText, status);
                        }
                        IsEditing = flag;
                        _host.IsTabStop = !flag;
                        if (!flag)
                        {
                            EditingViewport = null;
                        }
                    }
                }
            }
        }

        void StartTouchColumnResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            SheetLayout sheetLayout = GetSheetLayout();
            ColumnLayout viewportResizingColumnLayoutFromXForTouch = null;
            IsWorking = true;
            IsTouchResizingColumns = true;
            _DoTouchResizing = false;
            CloseTouchToolbar();
            if (_resizingTracker == null)
            {
                SolidColorBrush brush = new SolidColorBrush(Colors.Black);
                Line line = new Line();
                line.Stroke = brush;
                line.StrokeThickness = 1.0;
                line.StrokeDashArray = new DoubleCollection { 1.0 };
                _resizingTracker = line;
                TrackersContainer.Children.Add(_resizingTracker);
            }
            _resizingTracker.Visibility = Visibility.Visible;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingColumnLayoutFromXForTouch = GetRowHeaderColumnLayoutModel().FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                    break;

                case HitTestType.ColumnHeader:
                    viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(savedHitTestInformation.ColumnViewportIndex, savedHitTestInformation.HitPoint.X);
                    if (viewportResizingColumnLayoutFromXForTouch == null)
                    {
                        viewportResizingColumnLayoutFromXForTouch = GetViewportColumnLayoutModel(savedHitTestInformation.ColumnViewportIndex).FindColumn(savedHitTestInformation.HeaderInfo.ResizingColumn);
                        if ((viewportResizingColumnLayoutFromXForTouch == null) && (savedHitTestInformation.ColumnViewportIndex == 0))
                        {
                            viewportResizingColumnLayoutFromXForTouch = GetViewportResizingColumnLayoutFromXForTouch(-1, savedHitTestInformation.HitPoint.X);
                        }
                        if ((viewportResizingColumnLayoutFromXForTouch == null) && ((savedHitTestInformation.ColumnViewportIndex == 0) || (savedHitTestInformation.ColumnViewportIndex == -1)))
                        {
                            viewportResizingColumnLayoutFromXForTouch = GetRowHeaderResizingColumnLayoutFromXForTouch(savedHitTestInformation.HitPoint.X);
                        }
                    }
                    break;
            }
            if (viewportResizingColumnLayoutFromXForTouch != null)
            {
                _resizingTracker.X1 = (viewportResizingColumnLayoutFromXForTouch.X + viewportResizingColumnLayoutFromXForTouch.Width) - 0.5;
                _resizingTracker.Y1 = sheetLayout.HeaderY;
                _resizingTracker.X2 = _resizingTracker.X1;
                _resizingTracker.Y2 = _resizingTracker.Y1 + AvailableSize.Height;
                if (((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Column))) && ((savedHitTestInformation.ColumnViewportIndex > -2) && (_columnHeaderPresenters[savedHitTestInformation.ColumnViewportIndex + 1].GetViewportCell(savedHitTestInformation.HeaderInfo.Row, savedHitTestInformation.HeaderInfo.Column, true) != null)))
                {
                    UpdateResizeToolTip(GetHorizontalResizeTip(viewportResizingColumnLayoutFromXForTouch.Width), true);
                }
            }
        }

        void StartTouchDragDopping()
        {
            if (!IsTouchDrapDropping)
            {
                CellRange fromRange = GetFromRange();
                if (fromRange != null)
                {
                    IsTouchDrapDropping = true;
                    IsWorking = true;
                    UpdateDragIndicatorAndStartTimer(fromRange);
                }
            }
        }

        void StartTouchDragFill()
        {
            if (!IsTouchDragFilling)
            {
                UpdateDragFillStartRange();
                if (_dragFillStartRange != null)
                {
                    IsTouchDragFilling = true;
                    IsWorking = true;
                    UpdateDragFillViewportInfoAndStartTimer();
                }
            }
        }

        void StartTouchingSelecting()
        {
            if (!IsEntrieSheetSelection() && IsEntrieRowSelection())
            {
                IsTouchSelectingRows = true;
            }
            else if (!IsEntrieSheetSelection() && IsEntrieColumnSelection())
            {
                IsTouchSelectingColumns = true;
            }
            else
            {
                IsTouchSelectingCells = true;
            }
            CloseTouchToolbar();
            CellRange[] rangeArray = Enumerable.ToArray<CellRange>((IEnumerable<CellRange>)Worksheet.Selections);
            SavedOldSelections = rangeArray;
            IsWorking = true;
            StartScrollTimer();
        }

        void StartTouchRowResizing()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            SheetLayout sheetLayout = GetSheetLayout();
            RowLayout viewportResizingRowLayoutFromYForTouch = null;
            _DoTouchResizing = false;
            IsTouchResizingRows = true;
            IsWorking = true;
            CloseTouchToolbar();
            if (_resizingTracker == null)
            {
                _resizingTracker = new Line();
                _resizingTracker.Stroke = new SolidColorBrush(Colors.Black);
                _resizingTracker.StrokeThickness = 1.0;
                _resizingTracker.StrokeDashArray = new DoubleCollection { 1.0 };
                TrackersContainer.Children.Add(_resizingTracker);
            }
            _resizingTracker.Visibility = Visibility.Visible;
            switch (savedHitTestInformation.HitTestType)
            {
                case HitTestType.Corner:
                    viewportResizingRowLayoutFromYForTouch = GetColumnHeaderRowLayoutModel().FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    break;

                case HitTestType.RowHeader:
                    viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.HitPoint.Y);
                    if (((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.HeaderInfo != null)) && (savedHitTestInformation.HeaderInfo.ResizingRow >= 0))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetViewportRowLayoutModel(savedHitTestInformation.RowViewportIndex).FindRow(savedHitTestInformation.HeaderInfo.ResizingRow);
                    }
                    if ((viewportResizingRowLayoutFromYForTouch == null) && (savedHitTestInformation.RowViewportIndex == 0))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetViewportResizingRowLayoutFromYForTouch(-1, savedHitTestInformation.HitPoint.Y);
                    }
                    if ((viewportResizingRowLayoutFromYForTouch == null) && ((savedHitTestInformation.RowViewportIndex == 0) || (savedHitTestInformation.RowViewportIndex == -1)))
                    {
                        viewportResizingRowLayoutFromYForTouch = GetColumnHeaderResizingRowLayoutFromYForTouch(savedHitTestInformation.HitPoint.Y);
                    }
                    break;
            }
            if (viewportResizingRowLayoutFromYForTouch != null)
            {
                _resizingTracker.X1 = sheetLayout.HeaderX;
                _resizingTracker.X2 = sheetLayout.HeaderX + AvailableSize.Width;
                _resizingTracker.Y1 = (viewportResizingRowLayoutFromYForTouch.Y + viewportResizingRowLayoutFromYForTouch.Height) - 0.5;
                _resizingTracker.Y2 = _resizingTracker.Y1;
                if (((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Row))) && ((savedHitTestInformation.RowViewportIndex > -2) && (_rowHeaderPresenters[savedHitTestInformation.RowViewportIndex + 1].GetViewportCell(savedHitTestInformation.HeaderInfo.Row, savedHitTestInformation.HeaderInfo.Column, true) != null)))
                {
                    UpdateResizeToolTip(GetVerticalResizeTip(viewportResizingRowLayoutFromYForTouch.Height), false);
                }
            }
        }

        /// <summary>
        /// Stops editing the active cell.
        /// </summary>
        /// <param name="cancel">if set to <c>true</c> does not apply the edited text to the cell.</param>
        /// <returns><c>true</c> when able to stop cell editing successfully; otherwise, <c>false</c>.</returns>
        public bool StopCellEditing(bool cancel = false)
        {
            if (IsEditing && (Worksheet != null))
            {
                GcViewport editingViewport = EditingViewport;
                if (editingViewport != null)
                {
                    if (!cancel && (ApplyEditingValue(cancel) == DataValidationResult.Retry))
                    {
                        editingViewport.RetryEditing();
                    }
                    else
                    {
                        bool editorDirty = editingViewport.EditorDirty;
                        editingViewport.StopCellEditing(cancel);
                        if (editorDirty && !cancel)
                        {
                            RefreshViewportCells(_viewportPresenters, 0, 0, Worksheet.RowCount, Worksheet.ColumnCount);
                        }
                    }
                    if (editingViewport.IsEditing())
                    {
                        return false;
                    }
                    EditingViewport = null;
                }
            }
            IsEditing = false;
            return true;
        }

        void StopScrollTimer()
        {
            if (_verticalSelectionMgr != null)
            {
                _verticalSelectionMgr.Dispose();
                _verticalSelectionMgr = null;
            }
            if (_horizontalSelectionMgr != null)
            {
                _horizontalSelectionMgr.Dispose();
                _horizontalSelectionMgr = null;
            }
        }

        /// <summary>
        /// Suspends all events.
        /// </summary>
        public void SuspendEvent()
        {
            _eventSuspended++;
        }

        internal void SuspendFloatingObjectsInvalidate()
        {
            if ((_viewportPresenters != null) && (_viewportPresenters != null))
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.SuspendFloatingObjectsInvalidate();
                        }
                    }
                }
            }
        }

        internal void SuspendInvalidate()
        {
            _suspendViewInvalidate++;
            SuspendFloatingObjectsInvalidate();
        }

        void SwitchDragDropIndicator()
        {
            bool flag;
            bool flag2;
            KeyboardHelper.GetMetaKeyState(out flag, out flag2);
            if (((_dragToRowViewport != -2) && (_dragToColumnViewport != -2)) && ((_dragToRow != -2) && (_dragToColumn != -2)))
            {
                bool flag3 = _isDragInsert;
                if (flag)
                {
                    if (!flag3 && ((_dragDropFromRange.Row == -1) || (_dragDropFromRange.Column == -1)))
                    {
                        RefreshDragDropInsertIndicator(_dragToRowViewport, _dragToColumnViewport, _dragToRow, _dragToColumn);
                    }
                }
                else if (flag3)
                {
                    RefreshDragDropIndicator(_dragToRowViewport, _dragToColumnViewport, _dragToRow, _dragToColumn);
                }
            }
            _isDragInsert = flag;
            _isDragCopy = flag2;
        }

        void SynViewportChartShapeThemes()
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if (viewport != null)
                        {
                            viewport.SynChartShapeThemes();
                        }
                    }
                }
            }
        }

        XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            Serializer.InitReader(reader);
            Reset();
            while (reader.Read())
            {
                if (reader.NodeType == ((XmlNodeType)((int)XmlNodeType.Element)))
                {
                    ReadXmlInternal(reader);
                }
            }
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            Serializer.InitWriter(writer);
            Serializer.WriteStartObj("SpreadUI", writer);
            WriteXmlInternal(writer);
            Serializer.WriteEndObj(writer);
        }

        Point TapInColumnHeaderSelection(Point point, HitTestInformation hi)
        {
            UnSelectedAllFloatingObjects();
            StartColumnSelecting();
            EndColumnSelecting();
            RaiseTouchCellClick(hi);
            return point;
        }

        Point TapInRowHeaderSelection(Point point, HitTestInformation hi)
        {
            UnSelectedAllFloatingObjects();
            StartRowsSelecting();
            EndRowSelecting();
            RaiseTouchCellClick(hi);
            return point;
        }

        bool TapInSelection(Point point)
        {
            if (_formulaSelectionFeature.IsSelectionBegined)
            {
                return false;
            }
            return GetActiveSelectionBounds().Contains(point);
        }

        bool TapInSelectionColumn(int column)
        {
            ReadOnlyCollection<CellRange> selections = Worksheet.Selections;
            if (selections != null)
            {
                foreach (CellRange range in selections)
                {
                    if (((range.Row == -1) && (range.RowCount == -1)) && range.IntersectColumn(column))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool TapInSelectionRow(int row)
        {
            ReadOnlyCollection<CellRange> selections = Worksheet.Selections;
            if (selections != null)
            {
                foreach (CellRange range in selections)
                {
                    if (((range.Column == -1) && (range.ColumnCount == -1)) && range.IntersectRow(row))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        internal int TryGetNextScrollableColumn(int startColumn)
        {
            int frozenColumnCount = Worksheet.FrozenColumnCount;
            int num2 = (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1;
            if (startColumn < frozenColumnCount)
            {
                return frozenColumnCount;
            }
            if (startColumn > num2)
            {
                return num2;
            }
            for (int i = startColumn; i <= num2; i++)
            {
                if (Worksheet.GetActualColumnWidth(i, SheetArea.Cells) > 0.0)
                {
                    return i;
                }
            }
            return -1;
        }

        internal int TryGetNextScrollableRow(int startRow)
        {
            int frozenRowCount = Worksheet.FrozenRowCount;
            int num2 = (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1;
            if (startRow < frozenRowCount)
            {
                return frozenRowCount;
            }
            if (startRow > num2)
            {
                return num2;
            }
            for (int i = startRow; i <= num2; i++)
            {
                if (Worksheet.GetActualRowHeight(i, SheetArea.Cells) > 0.0)
                {
                    return i;
                }
            }
            return -1;
        }

        internal int TryGetPreviousScrollableColumn(int startColumn)
        {
            int frozenColumnCount = Worksheet.FrozenColumnCount;
            int num2 = (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1;
            if (startColumn < frozenColumnCount)
            {
                return frozenColumnCount;
            }
            if (startColumn > num2)
            {
                return num2;
            }
            for (int i = startColumn; i >= frozenColumnCount; i--)
            {
                if (Worksheet.GetActualColumnWidth(i, SheetArea.Cells) > 0.0)
                {
                    return i;
                }
            }
            return -1;
        }

        internal int TryGetPreviousScrollableRow(int startRow)
        {
            int frozenRowCount = Worksheet.FrozenRowCount;
            int num2 = (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1;
            if (startRow < frozenRowCount)
            {
                return frozenRowCount;
            }
            if (startRow > num2)
            {
                return num2;
            }
            for (int i = startRow; i >= frozenRowCount; i--)
            {
                if (Worksheet.GetActualRowHeight(i, SheetArea.Cells) > 0.0)
                {
                    return i;
                }
            }
            return -1;
        }

        internal void UnSelectedAllFloatingObjects()
        {
            FloatingObject[] allFloatingObjects = GetAllFloatingObjects();
            if (allFloatingObjects.Length > 0)
            {
                FloatingObject[] objArray2 = allFloatingObjects;
                for (int i = 0; i < objArray2.Length; i++)
                {
                    IFloatingObject obj2 = objArray2[i];
                    obj2.IsSelected = false;
                }
            }
        }

        void UnSelectFloatingObject(FloatingObject floatingObject)
        {
            try
            {
                if (!_isMouseDownFloatingObject)
                {
                    bool flag;
                    bool flag2;
                    KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                    if (((flag2 || flag) && !(floatingObject.Locked && Worksheet.Protect)) && floatingObject.IsSelected)
                    {
                        floatingObject.IsSelected = false;
                    }
                }
            }
            finally
            {
                _isMouseDownFloatingObject = false;
            }
        }

        internal void UpdateColumnHeaderCellsState(int row, int column, int rowCount, int columnCount)
        {
            if (_columnHeaderPresenters != null)
            {
                rowCount = ((rowCount < 0) || (row < 0)) ? Worksheet.ColumnHeader.RowCount : rowCount;
                columnCount = ((columnCount < 0) || (column < 0)) ? Worksheet.ColumnCount : columnCount;
                row = (row < 0) ? 0 : row;
                column = (column < 0) ? 0 : column;
                new CellRange(row, column, rowCount, columnCount);
                foreach (GcViewport viewport in _columnHeaderPresenters)
                {
                    if (viewport != null)
                    {
                        ColumnLayoutModel viewportColumnLayoutModel = GetViewportColumnLayoutModel(viewport.ColumnViewportIndex);
                        GetColumnHeaderRowLayoutModel();
                        if ((viewportColumnLayoutModel != null) && (viewportColumnLayoutModel.Count > 0))
                        {
                            for (int i = row; i < (row + rowCount); i++)
                            {
                                RowPresenter presenter = viewport.GetRow(i);
                                if (presenter != null)
                                {
                                    for (int j = Math.Max(column, viewportColumnLayoutModel[0].Column); j < (column + columnCount); j++)
                                    {
                                        if (j > viewportColumnLayoutModel[viewportColumnLayoutModel.Count - 1].Column)
                                        {
                                            break;
                                        }
                                        CellPresenterBase cell = presenter.GetCell(j);
                                        if (cell != null)
                                        {
                                            cell.ApplyState();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void UpdateCornerHeaderCellState()
        {
            if (_cornerPresenter != null)
            {
                _cornerPresenter.GetRow(0).GetCell(0).ApplyState();
            }
        }

        void UpdateCurrentFillRange()
        {
            _currentFillRange = GetCurrentFillRange();
        }

        void UpdateCurrentFillSettings()
        {
            if (!IsDragFillWholeRows && !IsDragFillWholeColumns)
            {
                if ((_dragToRow < DragFillStartTopRow) || (_dragToRow > DragFillStartBottomRow))
                {
                    if (_dragToRow >= DragFillStartTopRow)
                    {
                        if (_dragToRow > DragFillStartBottomRow)
                        {
                            if ((_dragToColumn >= DragFillStartLeftColumn) && (_dragToColumn <= DragFillStartRightColumn))
                            {
                                _currentFillDirection = DragFillDirection.Down;
                            }
                            else if (_dragToColumn < DragFillStartLeftColumn)
                            {
                                int num9 = Math.Abs((int)(_dragToColumn - DragFillStartLeftColumn));
                                if (Math.Abs((int)(_dragToRow - DragFillStartBottomRow)) >= num9)
                                {
                                    _currentFillDirection = DragFillDirection.Down;
                                }
                                else
                                {
                                    _currentFillDirection = DragFillDirection.Left;
                                }
                            }
                            else if (_dragToColumn > DragFillStartRightColumn)
                            {
                                int num11 = Math.Abs((int)(_dragToColumn - DragFillStartRightColumn));
                                if (Math.Abs((int)(_dragToRow - DragFillStartBottomRow)) >= num11)
                                {
                                    _currentFillDirection = DragFillDirection.Down;
                                }
                                else
                                {
                                    _currentFillDirection = DragFillDirection.Right;
                                }
                            }
                        }
                    }
                    else if ((_dragToColumn >= DragFillStartLeftColumn) && (_dragToColumn <= DragFillStartRightColumn))
                    {
                        _currentFillDirection = DragFillDirection.Up;
                    }
                    else if (_dragToColumn < DragFillStartLeftColumn)
                    {
                        int num5 = Math.Abs((int)(_dragToColumn - DragFillStartLeftColumn));
                        if (Math.Abs((int)(_dragToRow - DragFillStartTopRow)) >= num5)
                        {
                            _currentFillDirection = DragFillDirection.Up;
                        }
                        else
                        {
                            _currentFillDirection = DragFillDirection.Left;
                        }
                    }
                    else if (_dragToColumn > DragFillStartRightColumn)
                    {
                        int num7 = Math.Abs((int)(_dragToColumn - DragFillStartRightColumn));
                        if (Math.Abs((int)(_dragToRow - DragFillStartTopRow)) >= num7)
                        {
                            _currentFillDirection = DragFillDirection.Up;
                        }
                        else
                        {
                            _currentFillDirection = DragFillDirection.Right;
                        }
                    }
                }
                else if ((_dragToColumn >= DragFillStartLeftColumn) && (_dragToColumn <= DragFillStartRightColumn))
                {
                    int num = Math.Abs((int)(_dragToColumn - DragFillStartRightColumn));
                    int num2 = Math.Abs((int)(_dragToRow - DragFillStartBottomRow));
                    if (num2 > num)
                    {
                        _currentFillDirection = DragFillDirection.UpClear;
                    }
                    else if (num2 < num)
                    {
                        _currentFillDirection = DragFillDirection.LeftClear;
                    }
                    else
                    {
                        RowLayout dragFillStartBottomRowLayout = DragFillStartBottomRowLayout;
                        if (dragFillStartBottomRowLayout == null)
                        {
                            dragFillStartBottomRowLayout = DragFillToViewportBottomRowLayout;
                        }
                        if (MousePosition.Y > (dragFillStartBottomRowLayout.Y + dragFillStartBottomRowLayout.Height))
                        {
                            _currentFillDirection = DragFillDirection.Down;
                        }
                        else
                        {
                            ColumnLayout dragFillStartRightColumnLayout = DragFillStartRightColumnLayout;
                            if (dragFillStartRightColumnLayout == null)
                            {
                                dragFillStartRightColumnLayout = DragFillToViewportRightColumnLayout;
                            }
                            double num3 = (dragFillStartRightColumnLayout.X + dragFillStartRightColumnLayout.Width) - MousePosition.X;
                            double num4 = (dragFillStartBottomRowLayout.Y + dragFillStartBottomRowLayout.Height) - MousePosition.Y;
                            if (num3 >= num4)
                            {
                                _currentFillDirection = DragFillDirection.LeftClear;
                            }
                            else
                            {
                                _currentFillDirection = DragFillDirection.UpClear;
                            }
                        }
                    }
                }
                else if (_dragToColumn < DragFillStartLeftColumn)
                {
                    _currentFillDirection = DragFillDirection.Left;
                }
                else if (_dragToColumn > DragFillStartRightColumn)
                {
                    _currentFillDirection = DragFillDirection.Right;
                }
            }
            else if (IsDragFillWholeColumns)
            {
                if ((_dragToColumn >= DragFillStartLeftColumn) && (_dragToColumn <= DragFillStartRightColumn))
                {
                    _currentFillDirection = DragFillDirection.LeftClear;
                }
                else if (_dragToColumn < DragFillStartLeftColumn)
                {
                    _currentFillDirection = DragFillDirection.Left;
                }
                else if (_dragToColumn > DragFillStartRightColumn)
                {
                    _currentFillDirection = DragFillDirection.Right;
                }
            }
            else if (IsDragFillWholeRows)
            {
                if ((_dragToRow >= DragFillStartTopRow) && (_dragToRow <= DragFillStartBottomRow))
                {
                    _currentFillDirection = DragFillDirection.UpClear;
                }
                else if (_dragToRow < DragFillStartTopRow)
                {
                    _currentFillDirection = DragFillDirection.Up;
                }
                else if (_dragToRow > DragFillStartBottomRow)
                {
                    _currentFillDirection = DragFillDirection.Down;
                }
            }
        }

        void UpdateCursorType()
        {
            if ((_mouseCursor != null) && (_mouseCursor.Opacity != 0.0))
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if ((((savedHitTestInformation != null) && (savedHitTestInformation.ViewportInfo != null)) && savedHitTestInformation.ViewportInfo.InDragFillIndicator) || IsDraggingFill)
                {
                    bool flag;
                    bool flag2;
                    KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                    CursorType cursorType = flag2 ? CursorType.DragFill_CtrlDragCursor : CursorType.DragFill_DragCursor;
                    UpdateMouseCursorType(cursorType);
                }
                else if ((((savedHitTestInformation != null) && (savedHitTestInformation.ViewportInfo != null)) && savedHitTestInformation.ViewportInfo.InSelectionDrag) || IsDragDropping)
                {
                    bool flag3;
                    bool flag4;
                    KeyboardHelper.GetMetaKeyState(out flag3, out flag4);
                    CursorType type2 = flag4 ? CursorType.DragCell_CtrlDragCursor : CursorType.DragCell_DragCursor;
                    UpdateMouseCursorType(type2);
                }
            }
        }

        internal void UpdateDataValidationUI(int row, int column)
        {
            if (_viewportPresenters != null)
            {
                GcViewport[,] viewportArray = _viewportPresenters;
                int upperBound = viewportArray.GetUpperBound(0);
                int num2 = viewportArray.GetUpperBound(1);
                for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
                {
                    for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                    {
                        GcViewport viewport = viewportArray[i, j];
                        if ((viewport != null) && (viewport.SheetArea == SheetArea.Cells))
                        {
                            viewport.UpdateDataValidationUI(row, column);
                        }
                    }
                }
            }
        }

        void UpdateDragFillStartRange()
        {
            if (Worksheet.Selections.Count == 1)
            {
                _dragFillStartRange = Worksheet.Selections[0];
            }
            else if (Worksheet.ActiveCell != null)
            {
                _dragFillStartRange = new CellRange(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex, 1, 1);
            }
        }

        void UpdateDragFillViewportInfoAndStartTimer()
        {
            HitTestInformation savedHitTestInformation = GetHitInfo();
            _dragStartRowViewport = savedHitTestInformation.RowViewportIndex;
            _dragStartColumnViewport = savedHitTestInformation.ColumnViewportIndex;
            _dragToRowViewport = savedHitTestInformation.RowViewportIndex;
            _dragToColumnViewport = savedHitTestInformation.ColumnViewportIndex;
            UpdateDragStartRangeViewports();
            StartScrollTimer();
        }

        void UpdateDragIndicatorAndStartTimer(CellRange fromRange)
        {
            SolidColorBrush brush = new SolidColorBrush(Colors.Black);
            if (_dragDropInsertIndicator == null)
            {
                _dragDropInsertIndicator = new Grid();
                _dragDropInsertIndicator.Visibility = Visibility.Collapsed;
                Rectangle rectangle = new Rectangle();
                rectangle.Stroke = brush;
                rectangle.StrokeThickness = 1.0;
                rectangle.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle.StrokeDashOffset = 0.5;
                _dragDropInsertIndicator.Children.Add(rectangle);
                Rectangle rectangle2 = new Rectangle();
                rectangle2.Stroke = brush;
                rectangle2.StrokeThickness = 1.0;
                rectangle2.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle2.StrokeDashOffset = 0.5;
                rectangle2.Margin = new Windows.UI.Xaml.Thickness(1.0);
                _dragDropInsertIndicator.Children.Add(rectangle2);
                Rectangle rectangle3 = new Rectangle();
                rectangle3.Stroke = brush;
                rectangle3.StrokeThickness = 1.0;
                rectangle3.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle3.StrokeDashOffset = 0.5;
                rectangle3.Margin = new Windows.UI.Xaml.Thickness(2.0);
                _dragDropInsertIndicator.Children.Add(rectangle3);
                TrackersContainer.Children.Add(_dragDropInsertIndicator);
            }
            if (_dragDropIndicator == null)
            {
                _dragDropIndicator = new Grid();
                _dragDropIndicator.Visibility = Visibility.Collapsed;
                Rectangle rectangle4 = new Rectangle();
                rectangle4.Stroke = brush;
                rectangle4.StrokeThickness = 1.0;
                rectangle4.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle4.StrokeDashOffset = 0.5;
                _dragDropIndicator.Children.Add(rectangle4);
                Rectangle rectangle5 = new Rectangle();
                rectangle5.Stroke = brush;
                rectangle5.StrokeThickness = 1.0;
                rectangle5.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle5.StrokeDashOffset = 0.5;
                _dragDropIndicator.Children.Add(rectangle5);
                Rectangle rectangle6 = new Rectangle();
                rectangle6.Stroke = brush;
                rectangle6.StrokeThickness = 1.0;
                rectangle6.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle6.StrokeDashOffset = 0.5;
                _dragDropIndicator.Children.Add(rectangle6);
                Rectangle rectangle7 = new Rectangle();
                rectangle7.Stroke = brush;
                rectangle7.StrokeThickness = 1.0;
                rectangle7.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle7.StrokeDashOffset = 0.5;
                _dragDropIndicator.Children.Add(rectangle7);
                Rectangle rectangle8 = new Rectangle();
                rectangle8.Stroke = brush;
                rectangle8.StrokeThickness = 1.0;
                rectangle8.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle8.StrokeDashOffset = 0.5;
                rectangle8.Margin = new Windows.UI.Xaml.Thickness(1.0);
                _dragDropIndicator.Children.Add(rectangle8);
                Rectangle rectangle9 = new Rectangle();
                rectangle9.Stroke = brush;
                rectangle9.StrokeThickness = 1.0;
                rectangle9.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle9.StrokeDashOffset = 0.5;
                rectangle9.Margin = new Windows.UI.Xaml.Thickness(1.0);
                _dragDropIndicator.Children.Add(rectangle9);
                Rectangle rectangle10 = new Rectangle();
                rectangle10.Stroke = brush;
                rectangle10.StrokeThickness = 1.0;
                rectangle10.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle10.StrokeDashOffset = 0.5;
                rectangle10.Margin = new Windows.UI.Xaml.Thickness(1.0);
                _dragDropIndicator.Children.Add(rectangle10);
                Rectangle rectangle11 = new Rectangle();
                rectangle11.Stroke = brush;
                rectangle11.StrokeThickness = 1.0;
                rectangle11.StrokeDashArray = new DoubleCollection { 1.0, 1.0 };
                rectangle11.StrokeDashOffset = 0.5;
                rectangle11.Margin = new Windows.UI.Xaml.Thickness(1.0);
                _dragDropIndicator.Children.Add(rectangle11);
                TrackersContainer.Children.Add(_dragDropIndicator);
            }
            _dragDropFromRange = fromRange;
            HitTestInformation savedHitTestInformation = GetHitInfo();
            if ((savedHitTestInformation != null) && (savedHitTestInformation.ViewportInfo != null))
            {
                int row = savedHitTestInformation.ViewportInfo.Row;
                int column = savedHitTestInformation.ViewportInfo.Column;
                int num3 = (fromRange.Row < 0) ? 0 : fromRange.Row;
                int num4 = (fromRange.Column < 0) ? 0 : fromRange.Column;
                int num5 = (fromRange.Row < 0) ? (Worksheet.RowCount - 1) : ((fromRange.Row + fromRange.RowCount) - 1);
                int num6 = (fromRange.Column < 0) ? (Worksheet.ColumnCount - 1) : ((fromRange.Column + fromRange.ColumnCount) - 1);
                if (row < num3)
                {
                    row = num3;
                }
                if (row > num5)
                {
                    row = num5;
                }
                if (column < num4)
                {
                    column = num4;
                }
                if (column > num6)
                {
                    column = num6;
                }
                _dragDropRowOffset = row - num3;
                _dragDropColumnOffset = column - num4;
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                _dragStartColumnViewport = savedHitTestInformation.ColumnViewportIndex;
                if ((savedHitTestInformation.ColumnViewportIndex == -1) && (column == Worksheet.FrozenColumnCount))
                {
                    _dragStartColumnViewport = 0;
                }
                else if ((savedHitTestInformation.ColumnViewportIndex == 0) && (column == (Worksheet.FrozenColumnCount - 1)))
                {
                    _dragStartColumnViewport = -1;
                }
                else if ((savedHitTestInformation.ColumnViewportIndex == columnViewportCount) && (column == ((Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount) - 1)))
                {
                    _dragStartColumnViewport = columnViewportCount - 1;
                }
                else if ((savedHitTestInformation.ColumnViewportIndex == (columnViewportCount - 1)) && (column == (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                {
                    _dragStartColumnViewport = columnViewportCount;
                }
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                _dragStartRowViewport = savedHitTestInformation.RowViewportIndex;
                if ((savedHitTestInformation.RowViewportIndex == -1) && (row == Worksheet.FrozenRowCount))
                {
                    _dragStartRowViewport = 0;
                }
                else if ((savedHitTestInformation.RowViewportIndex == 0) && (row == (Worksheet.FrozenRowCount - 1)))
                {
                    _dragStartRowViewport = -1;
                }
                else if ((savedHitTestInformation.RowViewportIndex == rowViewportCount) && (row == ((Worksheet.RowCount - Worksheet.FrozenTrailingRowCount) - 1)))
                {
                    _dragStartRowViewport = rowViewportCount - 1;
                }
                else if ((savedHitTestInformation.RowViewportIndex == (rowViewportCount - 1)) && (row == (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                {
                    _dragStartRowViewport = rowViewportCount;
                }
                StartScrollTimer();
            }
        }

        void UpdateDragStartRangeViewports()
        {
            ViewportInfo viewportInfo = GetViewportInfo();
            int dragFillStartTopRow = DragFillStartTopRow;
            if ((dragFillStartTopRow >= 0) && (dragFillStartTopRow < Worksheet.FrozenRowCount))
            {
                _dragFillStartTopRowViewport = -1;
            }
            else if ((dragFillStartTopRow >= Worksheet.FrozenRowCount) && (dragFillStartTopRow < (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
            {
                if (DragFillStartBottomRow >= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount))
                {
                    _dragFillStartTopRowViewport = viewportInfo.RowViewportCount - 1;
                }
                else
                {
                    _dragFillStartTopRowViewport = _dragStartRowViewport;
                }
            }
            else if (dragFillStartTopRow >= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount))
            {
                _dragFillStartTopRowViewport = viewportInfo.RowViewportCount;
            }
            if (IsDragFillWholeColumns)
            {
                if (Worksheet.FrozenTrailingColumnCount == 0)
                {
                    _dragFillStartBottomRowViewport = viewportInfo.RowViewportCount - 1;
                }
                else
                {
                    _dragFillStartBottomRowViewport = viewportInfo.RowViewportCount;
                }
            }
            else
            {
                _dragFillStartBottomRowViewport = _dragStartRowViewport;
            }
            int dragFillStartLeftColumn = DragFillStartLeftColumn;
            if ((dragFillStartLeftColumn >= 0) && (dragFillStartLeftColumn < Worksheet.FrozenColumnCount))
            {
                _dragFillStartLeftColumnViewport = -1;
            }
            else if ((dragFillStartLeftColumn >= Worksheet.FrozenColumnCount) && (dragFillStartLeftColumn < (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
            {
                if (DragFillStartRightColumn >= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount))
                {
                    _dragFillStartLeftColumnViewport = viewportInfo.ColumnViewportCount - 1;
                }
                else
                {
                    _dragFillStartLeftColumnViewport = _dragStartColumnViewport;
                }
            }
            else if (dragFillStartLeftColumn >= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount))
            {
                _dragFillStartLeftColumnViewport = viewportInfo.ColumnViewportCount;
            }
            if (IsDragFillWholeRows)
            {
                if (Worksheet.FrozenTrailingRowCount == 0)
                {
                    _dragFillStartRightColumnViewport = viewportInfo.ColumnViewportCount - 1;
                }
                else
                {
                    _dragFillStartRightColumnViewport = viewportInfo.ColumnViewportCount;
                }
            }
            else
            {
                _dragFillStartRightColumnViewport = _dragStartColumnViewport;
            }
        }

        void UpdateDragToColumn()
        {
            double maxValue;
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragToColumnViewport, MousePosition.X);
            if (viewportColumnLayoutNearX != null)
            {
                _dragToColumn = viewportColumnLayoutNearX.Column;
                maxValue = (viewportColumnLayoutNearX.X + viewportColumnLayoutNearX.Width) - 1.0;
            }
            else
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if (MousePosition.X > savedHitTestInformation.HitPoint.X)
                {
                    _dragToColumn = DragFillStartViewportRightColumn;
                    maxValue = (DragFillStartViewportRightColumnLayout.X + DragFillStartViewportRightColumnLayout.Width) - 1.0;
                }
                else
                {
                    _dragToColumn = DragFillStartViewportLeftColumn;
                    maxValue = double.MaxValue;
                }
            }
            if (_dragToColumn == DragFillToViewportRightColumn)
            {
                double width = 0.0;
                Rect rowHeaderRectangle = GetRowHeaderRectangle(_dragStartRowViewport);
                if (!rowHeaderRectangle.IsEmpty)
                {
                    width = rowHeaderRectangle.Width;
                }
                for (int i = -1; i <= _dragToColumnViewport; i++)
                {
                    width += GetViewportWidth(i);
                }
                if (maxValue > width)
                {
                    _dragToColumn = DragFillToViewportRightColumn - 1;
                    if (_dragToColumn < 0)
                    {
                        _dragToColumn = 0;
                    }
                }
            }
        }

        void UpdateDragToColumnViewport()
        {
            _dragToColumnViewport = _dragStartColumnViewport;
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragToColumnViewport, MousePosition.X);
            if ((viewportColumnLayoutNearX == null) || (GetViewportColumnLayoutModel(_dragToColumnViewport).FindColumn(viewportColumnLayoutNearX.Column) == null))
            {
                double x = GetHitInfo().HitPoint.X;
                int columnViewportCount = GetViewportInfo().ColumnViewportCount;
                if (MousePosition.X < x)
                {
                    if ((_dragStartColumnViewport == 0) && (_dragToColumn <= Worksheet.FrozenColumnCount))
                    {
                        _dragToColumnViewport = -1;
                    }
                    else if ((_dragStartColumnViewport == columnViewportCount) && (_dragToColumn <= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                    {
                        _dragToColumnViewport = columnViewportCount - 1;
                    }
                }
                else if ((_dragStartColumnViewport == -1) && (_dragToColumn >= Worksheet.FrozenColumnCount))
                {
                    _dragToColumnViewport = 0;
                }
                else if ((_dragStartColumnViewport == (columnViewportCount - 1)) && (_dragToColumn >= (Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount)))
                {
                    _dragToColumnViewport = columnViewportCount;
                }
            }
        }

        void UpdateDragToCoordicates()
        {
            UpdateDragToRow();
            UpdateDragToColumn();
        }

        void UpdateDragToRow()
        {
            double maxValue;
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragToRowViewport, MousePosition.Y);
            if (viewportRowLayoutNearY != null)
            {
                _dragToRow = viewportRowLayoutNearY.Row;
                maxValue = (viewportRowLayoutNearY.Y + viewportRowLayoutNearY.Height) - 1.0;
            }
            else
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if (MousePosition.Y > savedHitTestInformation.HitPoint.Y)
                {
                    _dragToRow = DragFillStartViewportBottomRow;
                    maxValue = (DragFillStartViewportBottomRowLayout.Y + DragFillStartViewportBottomRowLayout.Height) - 1.0;
                }
                else
                {
                    _dragToRow = DragFillStartViewportTopRow;
                    maxValue = double.MaxValue;
                }
            }
            if (_dragToRow == DragFillToViewportBottomRow)
            {
                double height = 0.0;
                Rect columnHeaderRectangle = GetColumnHeaderRectangle(_dragStartColumnViewport);
                if (!columnHeaderRectangle.IsEmpty)
                {
                    height = columnHeaderRectangle.Height;
                }
                for (int i = -1; i <= _dragToRowViewport; i++)
                {
                    height += GetViewportHeight(i);
                }
                if (maxValue > height)
                {
                    _dragToRow = DragFillToViewportBottomRow - 1;
                    if (_dragToRow < 0)
                    {
                        _dragToRow = 0;
                    }
                }
            }
        }

        void UpdateDragToRowViewport()
        {
            _dragToRowViewport = _dragStartRowViewport;
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragToRowViewport, MousePosition.Y);
            if ((viewportRowLayoutNearY == null) || (GetViewportRowLayoutModel(_dragToRowViewport).FindRow(viewportRowLayoutNearY.Row) == null))
            {
                double y = GetHitInfo().HitPoint.Y;
                int rowViewportCount = GetViewportInfo().RowViewportCount;
                if (MousePosition.Y < y)
                {
                    if ((_dragStartRowViewport == 0) && (_dragToRow <= Worksheet.FrozenRowCount))
                    {
                        _dragToRowViewport = -1;
                    }
                    else if ((_dragStartRowViewport == rowViewportCount) && (_dragToRow <= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                    {
                        _dragToRowViewport = rowViewportCount - 1;
                    }
                }
                else if ((_dragStartRowViewport == -1) && (_dragToRow >= Worksheet.FrozenRowCount))
                {
                    _dragToRowViewport = 0;
                }
                else if ((_dragStartRowViewport == (rowViewportCount - 1)) && (_dragToRow >= (Worksheet.RowCount - Worksheet.FrozenTrailingRowCount)))
                {
                    _dragToRowViewport = rowViewportCount;
                }
            }
        }

        void UpdateDragToViewports()
        {
            UpdateDragToRowViewport();
            UpdateDragToColumnViewport();
        }

        void UpdateFloatingObjectsMovingResizingToColumn()
        {
            double maxValue;
            ColumnLayout viewportColumnLayoutNearX = GetViewportColumnLayoutNearX(_dragToColumnViewport, MousePosition.X);
            if (viewportColumnLayoutNearX != null)
            {
                _dragToColumn = viewportColumnLayoutNearX.Column;
                maxValue = (viewportColumnLayoutNearX.X + viewportColumnLayoutNearX.Width) - 1.0;
            }
            else
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if (MousePosition.X > savedHitTestInformation.HitPoint.X)
                {
                    _dragToColumn = GetViewportRightColumn(_dragStartColumnViewport);
                    ColumnLayout layout2 = GetViewportColumnLayoutModel(_dragStartColumnViewport).FindColumn(_dragToColumn);
                    maxValue = (layout2.X + layout2.Width) - 1.0;
                }
                else
                {
                    _dragToColumn = GetViewportLeftColumn(_dragToColumnViewport);
                    maxValue = double.MaxValue;
                }
            }
            int viewportRightColumn = GetViewportRightColumn(_dragToColumnViewport);
            if (_dragToColumn == viewportRightColumn)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                double num3 = sheetLayout.GetViewportX(_dragToColumnViewport) + sheetLayout.GetViewportWidth(_dragToColumnViewport);
                if (maxValue > num3)
                {
                    _dragToColumn = GetViewportRightColumn(_dragToColumnViewport) - 1;
                    if (_dragToColumn < 0)
                    {
                        _dragToColumn = 0;
                    }
                }
            }
        }

        void UpdateFloatingObjectsMovingResizingToCoordicates()
        {
            UpdateFloatingObjectsMovingResizingToRow();
            UpdateFloatingObjectsMovingResizingToColumn();
        }

        void UpdateFloatingObjectsMovingResizingToRow()
        {
            double maxValue;
            RowLayout viewportRowLayoutNearY = GetViewportRowLayoutNearY(_dragToRowViewport, MousePosition.Y);
            if (viewportRowLayoutNearY != null)
            {
                _dragToRow = viewportRowLayoutNearY.Row;
                maxValue = (viewportRowLayoutNearY.Y + viewportRowLayoutNearY.Height) - 1.0;
            }
            else
            {
                HitTestInformation savedHitTestInformation = GetHitInfo();
                if (MousePosition.Y > savedHitTestInformation.HitPoint.Y)
                {
                    _dragToRow = GetViewportBottomRow(_dragStartRowViewport);
                    RowLayout layout2 = GetViewportRowLayoutModel(_dragStartRowViewport).FindRow(_dragToRow);
                    maxValue = (layout2.Y + layout2.Height) - 1.0;
                }
                else
                {
                    _dragToRow = GetViewportTopRow(_dragStartRowViewport);
                    maxValue = double.MaxValue;
                }
            }
            int viewportBottomRow = GetViewportBottomRow(_dragToRowViewport);
            if (_dragToRow == viewportBottomRow)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                double num3 = sheetLayout.GetViewportY(_dragToRowViewport) + sheetLayout.GetViewportHeight(_dragToRowViewport);
                if (maxValue > num3)
                {
                    _dragToRow = GetViewportBottomRow(_dragToRowViewport) - 1;
                    if (_dragToRow < 0)
                    {
                        _dragToRow = 0;
                    }
                }
            }
        }

        void UpdateFloatingObjectsMovingResizingToViewports()
        {
            UpdateDragToRowViewport();
            UpdateDragToColumnViewport();
        }

        void UpdateFocusIndicator()
        {
            UpdateColumnHeaderCellsState(-1, _currentActiveColumnIndex, -1, 1);
            UpdateRowHeaderCellsState(_currentActiveRowIndex, -1, 1, -1);
            RefreshSelection();
            _currentActiveRowIndex = Worksheet.ActiveRowIndex;
            _currentActiveColumnIndex = Worksheet.ActiveColumnIndex;
            UpdateColumnHeaderCellsState(-1, _currentActiveColumnIndex, -1, 1);
            UpdateRowHeaderCellsState(_currentActiveRowIndex, -1, 1, -1);
        }

        internal void UpdateFreezeLines()
        {
            if (!IsTouchZooming)
            {
                SheetLayout sheetLayout = GetSheetLayout();
                ViewportInfo viewportInfo = GetViewportInfo();
                int columnViewportCount = viewportInfo.ColumnViewportCount;
                int rowViewportCount = viewportInfo.RowViewportCount;
                if (_columnFreezeLine == null)
                {
                    _columnFreezeLine = CreateFreezeLine();
                }
                if ((sheetLayout.FrozenWidth > 0.0) && ShowFreezeLine)
                {
                    if (!TrackersContainer.Children.Contains(_columnFreezeLine))
                    {
                        TrackersContainer.Children.Add(_columnFreezeLine);
                    }
                    int frozenColumnCount = Worksheet.FrozenColumnCount;
                    if (frozenColumnCount > Worksheet.ColumnCount)
                    {
                        frozenColumnCount = Worksheet.ColumnCount;
                    }
                    ColumnLayout layout2 = GetViewportColumnLayoutModel(-1).FindColumn(frozenColumnCount - 1);
                    if (layout2 != null)
                    {
                        _columnFreezeLine.X1 = layout2.X + layout2.Width;
                        _columnFreezeLine.X2 = _columnFreezeLine.X1;
                        _columnFreezeLine.Y1 = 0.0;
                        _columnFreezeLine.Y2 = sheetLayout.FrozenTrailingY + sheetLayout.FrozenTrailingHeight;
                    }
                    else
                    {
                        TrackersContainer.Children.Remove(_columnFreezeLine);
                    }
                }
                else
                {
                    TrackersContainer.Children.Remove(_columnFreezeLine);
                }
                if (_columnTrailingFreezeLine == null)
                {
                    _columnTrailingFreezeLine = CreateFreezeLine();
                }
                if ((sheetLayout.FrozenTrailingWidth > 0.0) && ShowFreezeLine)
                {
                    if (!TrackersContainer.Children.Contains(_columnTrailingFreezeLine))
                    {
                        TrackersContainer.Children.Add(_columnTrailingFreezeLine);
                    }
                    ColumnLayout layout3 = GetViewportColumnLayoutModel(columnViewportCount).FindColumn(Math.Max(Worksheet.FrozenColumnCount, Worksheet.ColumnCount - Worksheet.FrozenTrailingColumnCount));
                    if (layout3 != null)
                    {
                        _columnTrailingFreezeLine.X1 = layout3.X;
                        _columnTrailingFreezeLine.X2 = _columnTrailingFreezeLine.X1;
                        _columnTrailingFreezeLine.Y1 = 0.0;
                        _columnTrailingFreezeLine.Y2 = sheetLayout.FrozenTrailingY + sheetLayout.FrozenTrailingHeight;
                    }
                    else
                    {
                        TrackersContainer.Children.Remove(_columnTrailingFreezeLine);
                    }
                }
                else
                {
                    TrackersContainer.Children.Remove(_columnTrailingFreezeLine);
                }
                if (_rowFreezeLine == null)
                {
                    _rowFreezeLine = CreateFreezeLine();
                }
                if ((sheetLayout.FrozenHeight > 0.0) && ShowFreezeLine)
                {
                    if (!TrackersContainer.Children.Contains(_rowFreezeLine))
                    {
                        TrackersContainer.Children.Add(_rowFreezeLine);
                    }
                    int frozenRowCount = Worksheet.FrozenRowCount;
                    if (Worksheet.RowCount < frozenRowCount)
                    {
                        frozenRowCount = Worksheet.RowCount;
                    }
                    RowLayout layout4 = GetViewportRowLayoutModel(-1).FindRow(frozenRowCount - 1);
                    if (layout4 != null)
                    {
                        _rowFreezeLine.X1 = 0.0;
                        if (_translateOffsetX >= 0.0)
                        {
                            _rowFreezeLine.X2 = sheetLayout.FrozenTrailingX + sheetLayout.FrozenTrailingWidth;
                        }
                        else
                        {
                            _rowFreezeLine.X2 = (sheetLayout.FrozenTrailingX + _translateOffsetX) + sheetLayout.FrozenTrailingWidth;
                        }
                        _rowFreezeLine.Y1 = layout4.Y + layout4.Height;
                        _rowFreezeLine.Y2 = _rowFreezeLine.Y1;
                    }
                    else
                    {
                        TrackersContainer.Children.Remove(_rowFreezeLine);
                    }
                }
                else
                {
                    TrackersContainer.Children.Remove(_rowFreezeLine);
                }
                if (_rowTrailingFreezeLine == null)
                {
                    _rowTrailingFreezeLine = CreateFreezeLine();
                }
                if ((sheetLayout.FrozenTrailingHeight > 0.0) && ShowFreezeLine)
                {
                    if (!TrackersContainer.Children.Contains(_rowTrailingFreezeLine))
                    {
                        TrackersContainer.Children.Add(_rowTrailingFreezeLine);
                    }
                    RowLayout layout5 = GetViewportRowLayoutModel(rowViewportCount).FindRow(Math.Max(Worksheet.FrozenRowCount, Worksheet.RowCount - Worksheet.FrozenTrailingRowCount));
                    if (layout5 != null)
                    {
                        _rowTrailingFreezeLine.X1 = 0.0;
                        _rowTrailingFreezeLine.X2 = sheetLayout.FrozenTrailingX + sheetLayout.FrozenTrailingWidth;
                        _rowTrailingFreezeLine.Y1 = layout5.Y + ((_translateOffsetY < 0.0) ? _translateOffsetY : 0.0);
                        _rowTrailingFreezeLine.Y2 = _rowTrailingFreezeLine.Y1;
                    }
                    else
                    {
                        TrackersContainer.Children.Remove(_rowTrailingFreezeLine);
                    }
                }
                else
                {
                    TrackersContainer.Children.Remove(_rowTrailingFreezeLine);
                }
            }
        }

        internal void UpdateHeaderCellsState(int row, int rowCount, int column, int columnCount)
        {
            UpdateColumnHeaderCellsState(-1, column, -1, columnCount);
            UpdateRowHeaderCellsState(row, -1, rowCount, -1);
            UpdateHeaderCellsStateInSpanArea();
            UpdateFocusIndicator();
            UpdateHeaderCellsStateInSpanArea();
            UpdateCornerHeaderCellState();
        }

        void UpdateHeaderCellsStateInSpanArea()
        {
            Enumerable.ToList<CellLayout>((IEnumerable<CellLayout>)(from cellLayout in GetViewportCellLayoutModel(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex()) select cellLayout)).ForEach<CellLayout>(delegate (CellLayout cellLayout)
            {
                UpdateRowHeaderCellsState(cellLayout.Row, -1, cellLayout.RowCount, -1);
                UpdateColumnHeaderCellsState(-1, cellLayout.Column, -1, cellLayout.ColumnCount);
            });
        }

        void UpdateHitFilterCellState()
        {
            if (_hitFilterInfo != null)
            {
                if (_hitFilterInfo.SheetArea == SheetArea.ColumnHeader)
                {
                    GcViewport columnHeaderRowsPresenter = GetColumnHeaderRowsPresenter(_hitFilterInfo.ColumnViewportIndex);
                    if (columnHeaderRowsPresenter != null)
                    {
                        RowPresenter row = columnHeaderRowsPresenter.GetRow(_hitFilterInfo.Row);
                        if (row != null)
                        {
                            CellPresenterBase cell = row.GetCell(_hitFilterInfo.Column);
                            if (cell != null)
                            {
                                cell.ApplyState();
                            }
                        }
                    }
                }
                else if (_hitFilterInfo.SheetArea == SheetArea.Cells)
                {
                    GcViewport viewportRowsPresenter = GetViewportRowsPresenter(_hitFilterInfo.RowViewportIndex, _hitFilterInfo.ColumnViewportIndex);
                    if (viewportRowsPresenter != null)
                    {
                        RowPresenter presenter2 = viewportRowsPresenter.GetRow(_hitFilterInfo.Row);
                        if (presenter2 != null)
                        {
                            CellPresenterBase base3 = presenter2.GetCell(_hitFilterInfo.Column);
                            if (base3 != null)
                            {
                                base3.ApplyState();
                            }
                        }
                    }
                }
            }
        }

        void UpdateLastClickLocation(HitTestInformation hi)
        {
            if ((hi.HitTestType == HitTestType.Viewport) && (hi.ViewportInfo != null))
            {
                _lastClickLocation = new Point((double)hi.ViewportInfo.Row, (double)hi.ViewportInfo.Column);
            }
            else if ((hi.HitTestType == HitTestType.ColumnHeader) && (hi.HeaderInfo != null))
            {
                _lastClickLocation = new Point((double)hi.HeaderInfo.Row, (double)hi.HeaderInfo.Column);
            }
            else if ((hi.HitTestType == HitTestType.RowHeader) && (hi.HeaderInfo != null))
            {
                _lastClickLocation = new Point((double)hi.HeaderInfo.Row, (double)hi.HeaderInfo.Column);
            }
            else
            {
                _lastClickLocation = new Point(-1.0, -1.0);
            }
        }

        internal void UpdateMouseCursorLocation()
        {
            if (_mouseCursor != null)
            {
                _mouseCursor.SetValue(Canvas.LeftProperty, (double)(MousePosition.X - 32.0));
                _mouseCursor.SetValue(Canvas.TopProperty, (double)(MousePosition.Y - 32.0));
            }
        }

        void UpdateMouseCursorType(CursorType cursorType)
        {
            _mouseCursor.Source = CursorGenerator.GetCursor(cursorType);
        }

        void UpdateResizeToolTip(string text, bool resizeColumn)
        {
            if (resizeColumn && ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Column) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both)))
            {
                double x = _mouseDownPosition.X;
                double offsetY = _mouseDownPosition.Y - 40.0;
                TooltipHelper.ShowTooltip(text, x, offsetY);
            }
            else if ((ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Row) || (ShowResizeTip == Dt.Cells.Data.ShowResizeTip.Both))
            {
                double offsetX = _mouseDownPosition.X;
                double num4 = _mouseDownPosition.Y - 38.0;
                TooltipHelper.ShowTooltip(text, offsetX, num4);
            }
        }

        internal void UpdateRowHeaderCellsState(int row, int column, int rowCount, int columnCount)
        {
            if (_rowHeaderPresenters != null)
            {
                rowCount = ((rowCount < 0) || (row < 0)) ? Worksheet.RowCount : rowCount;
                columnCount = ((columnCount < 0) || (column < 0)) ? Worksheet.RowHeader.ColumnCount : columnCount;
                row = (row < 0) ? 0 : row;
                column = (column < 0) ? 0 : column;
                new CellRange(row, column, rowCount, columnCount);
                foreach (GcViewport viewport in _rowHeaderPresenters)
                {
                    if (viewport != null)
                    {
                        RowLayoutModel viewportRowLayoutModel = GetViewportRowLayoutModel(viewport.RowViewportIndex);
                        if ((viewportRowLayoutModel != null) && (viewportRowLayoutModel.Count > 0))
                        {
                            for (int i = Math.Max(row, viewportRowLayoutModel[0].Row); i < (row + rowCount); i++)
                            {
                                if (i > viewportRowLayoutModel[viewportRowLayoutModel.Count - 1].Row)
                                {
                                    break;
                                }
                                RowPresenter presenter = viewport.GetRow(i);
                                if (presenter != null)
                                {
                                    for (int j = column; j < (column + columnCount); j++)
                                    {
                                        CellPresenterBase cell = presenter.GetCell(j);
                                        if (cell != null)
                                        {
                                            cell.ApplyState();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void UpdateScrollToolTip(bool verticalScroll, int scrollTo = -1)
        {
            if (verticalScroll && ((ShowScrollTip == Dt.Cells.Data.ShowScrollTip.Vertical) || (ShowScrollTip == Dt.Cells.Data.ShowScrollTip.Both)))
            {
                double offsetX = _mouseDownPosition.X - 100.0;
                double offsetY = _mouseDownPosition.Y - 10.0;
                if (scrollTo == -1)
                {
                    scrollTo = GetViewportTopRow(GetHitInfo().RowViewportIndex) + 1;
                }
                TooltipHelper.ShowTooltip(GetVericalScrollTip(scrollTo), offsetX, offsetY);
            }
            else if ((ShowScrollTip == Dt.Cells.Data.ShowScrollTip.Horizontal) || (ShowScrollTip == Dt.Cells.Data.ShowScrollTip.Both))
            {
                double num3 = _mouseDownPosition.X - 20.0;
                double num4 = _mouseDownPosition.Y - 40.0;
                if (scrollTo == -1)
                {
                    scrollTo = GetViewportLeftColumn(GetHitInfo().ColumnViewportIndex) + 1;
                }
                TooltipHelper.ShowTooltip(GetHorizentalScrollTip(scrollTo), num3, num4);
            }
        }

        void UpdateSelectState(ChartChangedBaseEventArgs e)
        {
            GcViewport[,] viewportArray = _viewportPresenters;
            int upperBound = viewportArray.GetUpperBound(0);
            int num2 = viewportArray.GetUpperBound(1);
            for (int i = viewportArray.GetLowerBound(0); i <= upperBound; i++)
            {
                for (int j = viewportArray.GetLowerBound(1); j <= num2; j++)
                {
                    GcViewport viewport = viewportArray[i, j];
                    if (viewport != null)
                    {
                        if (e.Chart == null)
                        {
                            viewport.RefreshFloatingObjectContainerIsSelected();
                        }
                        else
                        {
                            viewport.RefreshFloatingObjectContainerIsSelected(e.Chart);
                        }
                    }
                }
            }
            ReadOnlyCollection<CellRange> selections = Worksheet.Selections;
            if (selections.Count != 0)
            {
                foreach (CellRange range in selections)
                {
                    UpdateHeaderCellsState(range.Row, range.RowCount, range.Column, range.ColumnCount);
                }
            }
        }

        internal virtual void UpdateTabStrip()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        protected void UpdateTouchHitTestInfoForHold(Point point)
        {
            GetHitInfo();
            Point point2 = point;
            SaveHitInfo(TouchHitTest(point2.X, point2.Y));
            _lastClickPoint = new Point(point2.X, point2.Y);
        }

        internal void UpdateTouchSelectionGripper()
        {
            Rect? autoFillIndicatorRec;
            if (((InputDeviceType != Dt.Cells.UI.InputDeviceType.Touch) || IsTouchPromotedMouseMessage) || _formulaSelectionFeature.IsSelectionBegined)
            {
                Rect rect16 = new Rect(0.0, 0.0, 0.0, 0.0);
                GripperLocations = null;
                ResizerGripperRect = null;
                _topLeftGripper.Arrange(rect16);
                _bottomRightGripper.Arrange(rect16);
                _resizerGripperContainer.Arrange(rect16);
                autoFillIndicatorRec = AutoFillIndicatorRec;
                if (autoFillIndicatorRec.HasValue)
                {
                    _autoFillIndicatorContainer.Arrange(rect16);
                    AutoFillIndicatorRec = null;
                }
                if ((_touchToolbarPopup != null) && _touchToolbarPopup.IsOpen)
                {
                    _touchToolbarPopup.IsOpen = false;
                }
                return;
            }
            Rect rect = new Rect(0.0, 0.0, 0.0, 0.0);
            GcViewport viewportRowsPresenter = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex());
            if (viewportRowsPresenter == null)
            {
                return;
            }
            if ((IsContinueTouchOperation || IsEditing) || (Worksheet.SelectionPolicy == SelectionPolicy.Single))
            {
                if (GripperLocations != null)
                {
                    CachedGripperLocation = GripperLocations;
                }
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                return;
            }
            FloatingObject[] allSelectedFloatingObjects = GetAllSelectedFloatingObjects();
            if ((allSelectedFloatingObjects != null) && (allSelectedFloatingObjects.Length > 0))
            {
                if (GripperLocations != null)
                {
                    CachedGripperLocation = GripperLocations;
                }
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                return;
            }
            CellRange activeSelection = GetActiveSelection();
            if ((activeSelection == null) && (Worksheet.Selections.Count > 0))
            {
                activeSelection = Worksheet.Selections[0];
            }
            if (activeSelection == null)
            {
                if (GripperLocations != null)
                {
                    CachedGripperLocation = GripperLocations;
                }
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                return;
            }
            autoFillIndicatorRec = AutoFillIndicatorRec;
            if (autoFillIndicatorRec.HasValue)
            {
                if (GripperLocations != null)
                {
                    CachedGripperLocation = GripperLocations;
                }
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                Rect autoFillIndicatorRect = GetAutoFillIndicatorRect(viewportRowsPresenter, activeSelection);
                _autoFillIndicatorContainer.Arrange(autoFillIndicatorRect);
                AutoFillIndicatorRec = new Rect?(autoFillIndicatorRect);
                return;
            }
            if (viewportRowsPresenter.Sheet.Worksheet.Selections.Count <= 0)
            {
                return;
            }
            SheetLayout sheetLayout = GetSheetLayout();
            Rect rangeBounds = viewportRowsPresenter._cachedSelectionFrameLayout;
            if (!viewportRowsPresenter.SelectionContainer.IsAnchorCellInSelection)
            {
                rangeBounds = viewportRowsPresenter._cachedFocusCellLayout;
            }
            if (viewportRowsPresenter.Sheet.Worksheet.Selections.Count > 0)
            {
                rangeBounds = viewportRowsPresenter.GetRangeBounds(activeSelection);
            }
            List<Tuple<Point, double>> list = new List<Tuple<Point, double>>();
            if (IsEntrieSheetSelection())
            {
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                autoFillIndicatorRec = null;
                ResizerGripperRect = autoFillIndicatorRec;
            }
            else
            {
                double viewportY;
                bool flag2;
                if (!IsEntrieColumnSelection())
                {
                    if (!IsEntrieRowSelection())
                    {
                        double num27 = sheetLayout.GetViewportX(viewportRowsPresenter.ColumnViewportIndex);
                        double num28 = sheetLayout.GetViewportY(viewportRowsPresenter.RowViewportIndex);
                        int num29 = GetActiveRowViewportIndex();
                        int activeColumnViewportIndex = GetActiveColumnViewportIndex();
                        int viewportLeftColumn = GetViewportLeftColumn(activeColumnViewportIndex);
                        int num32 = GetViewportTopRow(num29);
                        int num33 = GetViewportBottomRow(num29);
                        int viewportRightColumn = GetViewportRightColumn(activeColumnViewportIndex);
                        int num35 = -7;
                        int num36 = -7;
                        if ((activeSelection.Column < viewportLeftColumn) || (activeSelection.Row < num32))
                        {
                            list.Add(Tuple.Create<Point, double>(new Point(-2147483648.0, -2147483648.0), 0.0));
                        }
                        else
                        {
                            list.Add(Tuple.Create<Point, double>(new Point((num27 + rangeBounds.X) + num35, (num28 + rangeBounds.Y) + num36), 16.0));
                        }
                        num35 = (int)(rangeBounds.Width - 9.0);
                        num36 = (int)(rangeBounds.Height - 9.0);
                        int num37 = (activeSelection.Row + activeSelection.RowCount) - 1;
                        int num38 = (activeSelection.Column + activeSelection.ColumnCount) - 1;
                        if (num37 > num33)
                        {
                            num36 = 0x7fffffff;
                        }
                        if (num38 > viewportRightColumn)
                        {
                            num35 = 0x7fffffff;
                        }
                        int num39 = GetActiveRowViewportIndex();
                        int num40 = GetActiveColumnViewportIndex();
                        Worksheet.GetViewportInfo();
                        if ((num35 == 0x7fffffff) || (num36 == 0x7fffffff))
                        {
                            for (int i = num39; i <= GetViewportInfo(Worksheet).RowViewportCount; i++)
                            {
                                for (int j = num40; j <= GetViewportInfo(Worksheet).ColumnViewportCount; j++)
                                {
                                    num33 = GetViewportBottomRow(i);
                                    viewportRightColumn = GetViewportRightColumn(j);
                                    if ((num33 >= num37) && (viewportRightColumn >= num38))
                                    {
                                        GcViewport viewport8 = _viewportPresenters[i + 1, j + 1];
                                        if (viewport8 != null)
                                        {
                                            Rect rect13 = viewport8._cachedSelectionFrameLayout;
                                            if (!viewport8.SelectionContainer.IsAnchorCellInSelection)
                                            {
                                                rect13 = viewport8._cachedFocusCellLayout;
                                            }
                                            num35 = (int)(((sheetLayout.GetViewportX(j) + rect13.X) + rect13.Width) - 9.0);
                                            num36 = (int)(((sheetLayout.GetViewportY(i) + rect13.Y) + rect13.Height) - 9.0);
                                            if (list.Count == 1)
                                            {
                                                if ((num35 > (sheetLayout.GetViewportX(j) + sheetLayout.GetViewportWidth(j))) || (num36 > (sheetLayout.GetViewportY(i) + sheetLayout.GetViewportHeight(i))))
                                                {
                                                    list.Add(Tuple.Create<Point, double>(new Point(2147483647.0, 2147483647.0), 0.0));
                                                }
                                                else
                                                {
                                                    list.Add(Tuple.Create<Point, double>(new Point((double)num35, (double)num36), 16.0));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (list.Count == 1)
                            {
                                list.Add(Tuple.Create<Point, double>(new Point(2147483647.0, 2147483647.0), 0.0));
                            }
                        }
                        else
                        {
                            num35 = (int)((num35 + num27) + rangeBounds.X);
                            num36 = (int)((num36 + num28) + rangeBounds.Y);
                            if ((num35 > (sheetLayout.GetViewportX(activeColumnViewportIndex) + sheetLayout.GetViewportWidth(activeColumnViewportIndex))) || (num36 > (sheetLayout.GetViewportY(num29) + sheetLayout.GetViewportHeight(num29))))
                            {
                                list.Add(Tuple.Create<Point, double>(new Point(2147483647.0, 2147483647.0), 0.0));
                            }
                            else
                            {
                                list.Add(Tuple.Create<Point, double>(new Point((double)num35, (double)num36), 16.0));
                            }
                        }
                        goto Label_10BF;
                    }
                    double viewportX = sheetLayout.GetViewportX(viewportRowsPresenter.ColumnViewportIndex);
                    viewportY = sheetLayout.GetViewportY(viewportRowsPresenter.RowViewportIndex);
                    int viewportTopRow = GetViewportTopRow(viewportRowsPresenter.RowViewportIndex);
                    int viewportBottomRow = GetViewportBottomRow(viewportRowsPresenter.RowViewportIndex);
                    if (Worksheet.FrozenColumnCount > 0)
                    {
                        GcViewport viewport5 = GetViewportRowsPresenter(GetActiveRowViewportIndex(), GetActiveColumnViewportIndex() + 1);
                        Rect rect9 = viewport5._cachedSelectionFrameLayout;
                        if (!viewport5.SelectionContainer.IsAnchorCellInSelection)
                        {
                            rect9 = viewportRowsPresenter._cachedFocusCellLayout;
                        }
                        rangeBounds = new Rect(rangeBounds.X, rangeBounds.Y, rangeBounds.Width + rect9.Width, rangeBounds.Height);
                    }
                    if (activeSelection.Row >= viewportTopRow)
                    {
                        list.Add(Tuple.Create<Point, double>(new Point(((viewportX + rangeBounds.X) + (rangeBounds.Width / 2.0)) - 7.0, (viewportY + rangeBounds.Y) - 16.0), 16.0));
                    }
                    else
                    {
                        list.Add(Tuple.Create<Point, double>(new Point(((viewportX + rangeBounds.X) + (rangeBounds.Width / 2.0)) - 7.0, -2147483648.0), 0.0));
                    }
                    int num18 = (int)(rangeBounds.Height - 9.0);
                    int num19 = (activeSelection.Row + activeSelection.RowCount) - 1;
                    if (num19 > viewportBottomRow)
                    {
                        num18 = 0x7fffffff;
                    }
                    int activeRowViewportIndex = GetActiveRowViewportIndex();
                    Worksheet.GetViewportInfo();
                    flag2 = true;
                    int rowViewportIndex = activeRowViewportIndex;
                    if (num18 == 0x7fffffff)
                    {
                        while (rowViewportIndex <= GetViewportInfo(Worksheet).RowViewportCount)
                        {
                            if (GetViewportBottomRow(rowViewportIndex) >= num19)
                            {
                                GcViewport viewport6 = _viewportPresenters[rowViewportIndex + 1, viewportRowsPresenter.ColumnViewportIndex + 1];
                                if (viewport6 != null)
                                {
                                    Rect rect10 = viewport6._cachedSelectionFrameLayout;
                                    if (!viewport6.SelectionContainer.IsAnchorCellInSelection)
                                    {
                                        rect10 = viewport6._cachedFocusCellLayout;
                                    }
                                    num18 = (int)((sheetLayout.GetViewportY(rowViewportIndex) + rect10.Y) + rect10.Height);
                                    if (list.Count == 1)
                                    {
                                        if (num18 <= (sheetLayout.GetViewportY(rowViewportIndex) + sheetLayout.GetViewportHeight(rowViewportIndex)))
                                        {
                                            list.Add(Tuple.Create<Point, double>(new Point(((viewportX + rangeBounds.X) + (rangeBounds.Width / 2.0)) - 7.0, (double)num18), 16.0));
                                        }
                                        else
                                        {
                                            list.Add(Tuple.Create<Point, double>(new Point(((viewportX + rangeBounds.X) + (rangeBounds.Width / 2.0)) - 7.0, 2147483647.0), 0.0));
                                            flag2 = false;
                                        }
                                        break;
                                    }
                                }
                            }
                            rowViewportIndex++;
                        }
                    }
                    else
                    {
                        double viewportHeight = sheetLayout.GetViewportHeight(viewportRowsPresenter.RowViewportIndex);
                        double y = (viewportY + rangeBounds.Y) + rangeBounds.Height;
                        if (y <= (viewportY + viewportHeight))
                        {
                            list.Add(Tuple.Create<Point, double>(new Point(((viewportX + rangeBounds.X) + (rangeBounds.Width / 2.0)) - 7.0, y), 16.0));
                        }
                        else
                        {
                            list.Add(Tuple.Create<Point, double>(new Point(viewportX, 2147483647.0), 0.0));
                            flag2 = false;
                        }
                    }
                }
                else
                {
                    double num = sheetLayout.GetViewportX(viewportRowsPresenter.ColumnViewportIndex);
                    double num2 = sheetLayout.GetViewportY(viewportRowsPresenter.RowViewportIndex);
                    int num3 = GetViewportLeftColumn(viewportRowsPresenter.ColumnViewportIndex);
                    int num4 = GetViewportRightColumn(viewportRowsPresenter.ColumnViewportIndex);
                    if (Worksheet.FrozenRowCount > 0)
                    {
                        GcViewport viewport2 = GetViewportRowsPresenter(GetActiveRowViewportIndex() + 1, GetActiveColumnViewportIndex());
                        Rect rect5 = viewport2._cachedSelectionFrameLayout;
                        if (!viewport2.SelectionContainer.IsAnchorCellInSelection)
                        {
                            rect5 = viewport2._cachedFocusCellLayout;
                        }
                        rangeBounds = new Rect(rangeBounds.X, rangeBounds.Y, rangeBounds.Width, rangeBounds.Height + rect5.Height);
                    }
                    if (activeSelection.Column >= num3)
                    {
                        list.Add(Tuple.Create<Point, double>(new Point((num + rangeBounds.X) - 16.0, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 16.0));
                    }
                    else
                    {
                        list.Add(Tuple.Create<Point, double>(new Point(-2147483648.0, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 0.0));
                    }
                    int num5 = (int)(rangeBounds.Width - 9.0);
                    int num6 = (activeSelection.Column + activeSelection.ColumnCount) - 1;
                    if (num6 > num4)
                    {
                        num5 = 0x7fffffff;
                    }
                    int num7 = GetActiveColumnViewportIndex();
                    Worksheet.GetViewportInfo();
                    bool flag = true;
                    int columnViewportIndex = num7;
                    if (num5 == 0x7fffffff)
                    {
                        while (columnViewportIndex <= GetViewportInfo(Worksheet).ColumnViewportCount)
                        {
                            if (GetViewportRightColumn(columnViewportIndex) >= num6)
                            {
                                GcViewport viewport3 = _viewportPresenters[viewportRowsPresenter.RowViewportIndex + 1, columnViewportIndex + 1];
                                if (viewport3 != null)
                                {
                                    Rect rect6 = viewport3._cachedSelectionFrameLayout;
                                    if (!viewport3.SelectionContainer.IsAnchorCellInSelection)
                                    {
                                        rect6 = viewport3._cachedFocusCellLayout;
                                    }
                                    num5 = (int)((sheetLayout.GetViewportX(columnViewportIndex) + rect6.X) + rect6.Width);
                                    if (list.Count == 1)
                                    {
                                        if (num5 <= (sheetLayout.GetViewportX(columnViewportIndex) + sheetLayout.GetViewportWidth(columnViewportIndex)))
                                        {
                                            list.Add(Tuple.Create<Point, double>(new Point((double)num5, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 16.0));
                                        }
                                        else
                                        {
                                            list.Add(Tuple.Create<Point, double>(new Point(-2147483648.0, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 0.0));
                                            flag = false;
                                        }
                                        break;
                                    }
                                }
                            }
                            columnViewportIndex++;
                        }
                    }
                    else
                    {
                        double viewportWidth = sheetLayout.GetViewportWidth(viewportRowsPresenter.ColumnViewportIndex);
                        double x = (num + rangeBounds.X) + rangeBounds.Width;
                        if (x <= (num + viewportWidth))
                        {
                            list.Add(Tuple.Create<Point, double>(new Point(x, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 16.0));
                        }
                        else
                        {
                            list.Add(Tuple.Create<Point, double>(new Point(2147483647.0, ((num2 + rangeBounds.Y) + (rangeBounds.Height / 2.0)) - 9.0), 0.0));
                            flag = false;
                        }
                    }
                    GcViewport viewport4 = _columnHeaderPresenters[viewportRowsPresenter.ColumnViewportIndex + 1];
                    CellRange range2 = new CellRange(Worksheet.ColumnHeader.RowCount - 1, (activeSelection.Column + activeSelection.ColumnCount) - 1, 1, 1);
                    Rect rect7 = viewport4.GetRangeBounds(range2, SheetArea.ColumnHeader);
                    int column = (activeSelection.Column + activeSelection.ColumnCount) - 1;
                    if ((Worksheet.GetColumnResizable(column) && !rect7.IsEmpty) && flag)
                    {
                        double num12 = 0.0;
                        for (int k = 0; k < Worksheet.ColumnHeader.RowCount; k++)
                        {
                            num12 += Worksheet.GetActualRowHeight(k, SheetArea.ColumnHeader) * Worksheet.ZoomFactor;
                        }
                        Rect rect8 = new Rect(((num + rect7.X) + rect7.Width) - 8.0, (viewport4.Location.Y + num12) - 16.0, 16.0, 16.0);
                        _resizerGripperContainer.Child = _cachedColumnResizerGripperImage;
                        _resizerGripperContainer.Arrange(rect8);
                        ResizerGripperRect = new Rect?(rect8);
                    }
                    else
                    {
                        _resizerGripperContainer.Arrange(rect);
                        autoFillIndicatorRec = null;
                        ResizerGripperRect = autoFillIndicatorRec;
                    }
                    goto Label_10BF;
                }
                GcViewport viewport7 = _rowHeaderPresenters[viewportRowsPresenter.RowViewportIndex + 1];
                CellRange range = new CellRange((activeSelection.Row + activeSelection.RowCount) - 1, Worksheet.RowHeader.ColumnCount - 1, 1, 1);
                Rect rect11 = viewport7.GetRangeBounds(range, SheetArea.CornerHeader | SheetArea.RowHeader);
                int row = (activeSelection.Row + activeSelection.RowCount) - 1;
                if ((Worksheet.GetRowResizable(row) && !rect11.IsEmpty) && flag2)
                {
                    double num25 = 0.0;
                    for (int m = 0; m < Worksheet.RowHeader.ColumnCount; m++)
                    {
                        num25 += Worksheet.GetActualColumnWidth(m, SheetArea.CornerHeader | SheetArea.RowHeader) * Worksheet.ZoomFactor;
                    }
                    Rect rect12 = new Rect((viewport7.Location.X + num25) - 16.0, ((viewportY + rect11.Y) + rect11.Height) - 8.0, 16.0, 16.0);
                    _resizerGripperContainer.Child = _cachedRowResizerGripperImage;
                    _resizerGripperContainer.Arrange(rect12);
                    ResizerGripperRect = new Rect?(rect12);
                }
                else
                {
                    _resizerGripperContainer.Arrange(rect);
                    autoFillIndicatorRec = null;
                    ResizerGripperRect = autoFillIndicatorRec;
                }
            }
        Label_10BF:
            if (list.Count == 2)
            {
                Point point = list[0].Item1;
                double width = list[0].Item2;
                Rect rect14 = new Rect((double)((int)point.X), (double)((int)point.Y), width, width);
                _topLeftGripper.Arrange(rect14);
                point = list[1].Item1;
                width = list[1].Item2;
                Rect rect15 = new Rect((double)((int)point.X), (double)((int)point.Y), width, width);
                _bottomRightGripper.Arrange(rect15);
                GripperLocationsStruct struct2 = new GripperLocationsStruct
                {
                    TopLeft = rect14,
                    BottomRight = rect15
                };
                GripperLocations = struct2;
                CachedGripperLocation = GripperLocations;
                if (IsEntrieSheetSelection() || (!IsEntrieRowSelection() && !IsEntrieColumnSelection()))
                {
                    _resizerGripperContainer.Arrange(rect);
                    ResizerGripperRect = null;
                    return;
                }
            }
            else
            {
                GripperLocations = null;
                _topLeftGripper.Arrange(rect);
                _bottomRightGripper.Arrange(rect);
                _resizerGripperContainer.Arrange(rect);
                ResizerGripperRect = null;
            }
        }

        bool ValidateFillRange(CellRange fillRange)
        {
            bool flag = true;
            string message = string.Empty;
            if (HasSpans(fillRange.Row, fillRange.Column, fillRange.RowCount, fillRange.ColumnCount))
            {
                flag = false;
                message = ResourceStrings.SheetViewDragFillChangePartOfMergeCell;
            }
            if ((flag && Worksheet.Protect) && IsAnyCellInRangeLocked(Worksheet, fillRange.Row, fillRange.Column, fillRange.RowCount, fillRange.ColumnCount))
            {
                flag = false;
                message = ResourceStrings.SheetViewDragFillChangeProtectCell;
            }
            if (!flag)
            {
                RaiseInvalidOperation(message, null, null);
            }
            return flag;
        }

        internal virtual void WriteXmlInternal(XmlWriter writer)
        {
            if (!_allowUserFormula)
            {
                Serializer.SerializeObj((bool)_allowUserFormula, "AllowUserFormula", writer);
            }
            if (!_allowUndo)
            {
                Serializer.SerializeObj((bool)_allowUndo, "AllowUndo", writer);
            }
            if (_freezeLineStyle != null)
            {
                Serializer.SerializeObj(_freezeLineStyle, "FreezeLineStyle", writer);
            }
            if (_trailingFreezeLineStyle != null)
            {
                Serializer.SerializeObj(_trailingFreezeLineStyle, "TrailingFreezeLineStyle", writer);
            }
            if (!_showFreezeLine)
            {
                Serializer.SerializeObj((bool)_showFreezeLine, "ShowFreezeLine", writer);
            }
            if (!_allowUserZoom)
            {
                Serializer.SerializeObj((bool)_allowUserZoom, "AllowUserZoom", writer);
            }
            if (!_autoClipboard)
            {
                Serializer.SerializeObj((bool)_autoClipboard, "AutoClipboard", writer);
            }
            if (_clipBoardOptions != ClipboardPasteOptions.All)
            {
                Serializer.SerializeObj(_clipBoardOptions, "ClipBoardOptions", writer);
            }
            if (!_allowEditOverflow)
            {
                Serializer.SerializeObj((bool)_allowEditOverflow, "AllowEditOverflow", writer);
            }
            if (_protect)
            {
                Serializer.SerializeObj((bool)_protect, "Protect", writer);
            }
            if (!_allowDragDrop)
            {
                Serializer.SerializeObj((bool)_allowDragDrop, "AllowDragDrop", writer);
            }
            if (!_showRowRangeGroup)
            {
                Serializer.SerializeObj((bool)_showRowRangeGroup, "ShowRowRangeGroup", writer);
            }
            if (!_showColumnRangeGroup)
            {
                Serializer.SerializeObj((bool)_showColumnRangeGroup, "ShowColumnRangeGroup", writer);
            }
            if (!_allowDragFill)
            {
                Serializer.SerializeObj((bool)_allowDragFill, "AllowDragFill", writer);
            }
            if (_canTouchMultiSelect)
            {
                Serializer.SerializeObj((bool)_canTouchMultiSelect, "CanTouchMultiSelect", writer);
            }
            if (_resizeZeroIndicator != Dt.Cells.UI.ResizeZeroIndicator.Default)
            {
                Serializer.SerializeObj(_resizeZeroIndicator, "ResizeZeroIndicator", writer);
            }
            if (DefaultAutoFillType.HasValue)
            {
                Serializer.SerializeObj(DefaultAutoFillType, "DefaultAutoFillType", writer);
            }
            if (_rangeGroupBackground != null)
            {
                Serializer.SerializeObj(_rangeGroupBackground, "RangeGroupBackground", writer);
            }
            if (_rangeGroupBorderBrush != null)
            {
                Serializer.SerializeObj(_rangeGroupBorderBrush, "RangeGroupBorderBrush", writer);
            }
            if (_rangeGroupLineStroke != null)
            {
                Serializer.SerializeObj(_rangeGroupLineStroke, "RangeGroupLineStroke", writer);
            }
        }

        internal CellRange ActiveCell
        {
            get { return GetActiveCell(); }
        }

        internal CellRange ActiveSelection
        {
            get { return GetActiveSelection(); }
        }

        /// <summary>
        /// Gets or sets whether the component handles the shortcut keys for Clipboard actions. 
        /// </summary>
        [DefaultValue(true)]
        public bool AutoClipboard
        {
            get { return _autoClipboard; }
            set { _autoClipboard = value; }
        }

        internal Rect? AutoFillIndicatorRec { get; set; }

        internal Size AvailableSize
        {
            get
            {
                double width = _availableSize.Width;
                double height = _availableSize.Height;
                bool flag = false;
                bool designModeEnabled = DesignMode.DesignModeEnabled;
                flag = true;
                if (designModeEnabled || flag)
                {
                    if (double.IsInfinity(width) || double.IsNaN(width))
                    {
                        width = GCSPREAD_DefaultSize.Width;
                    }
                    if (double.IsInfinity(height) || double.IsNaN(height))
                    {
                        height = GCSPREAD_DefaultSize.Height;
                    }
                }
                return new Size(width, height);
            }
            set
            {
                if ((value.Width != _availableSize.Width) || (value.Height != _availableSize.Height))
                {
                    _availableSize = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether data can overflow into adjacent empty cells in the component.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can overflow; otherwise, <c>false</c>.
        /// </value>
        [DefaultValue(false)]
        public bool CanCellOverflow
        {
            get
            {
                Excel sheet = _host as Excel;
                return ((sheet != null) && sheet.CanCellOverflow);
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.Workbook.CanCellOverflow = value;
                }
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    sheet.CanCellOverflow = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether data can overflow into adjacent empty cells in the component while the cell is in edit mode. 
        /// </summary>
        [DefaultValue(true)]
        public bool CanEditOverflow
        {
            get { return _allowEditOverflow; }
            set { _allowEditOverflow = value; }
        }

        /// <summary>
        /// Gets a value that indicates whether the user is editing a formula.
        /// </summary>
        public bool CanSelectFormula
        {
            get { return (_formulaSelectionFeature.IsSelectionBegined && _formulaSelectionFeature.CanSelectFormula); }
        }

        /// <summary>
        /// Indicates whether the user can select multiple ranges by touch.
        /// </summary>
        [DefaultValue(false)]
        public bool CanTouchMultiSelect
        {
            get { return _canTouchMultiSelect; }
            set { _canTouchMultiSelect = value; }
        }

        /// <summary>
        /// Gets or sets whether to allow users to drag and drop a range.
        /// </summary>
        [DefaultValue(true)]
        public bool CanUserDragDrop
        {
            get { return _allowDragDrop; }
            set { _allowDragDrop = value; }
        }

        /// <summary>
        /// Gets or sets whether to allow users to drag and fill a range.
        /// </summary>
        [DefaultValue(true)]
        public bool CanUserDragFill
        {
            get { return _allowDragFill; }
            set
            {
                if (_allowDragFill != value)
                {
                    _allowDragFill = value;
                    InvalidateRange(-1, -1, -1, -1, SheetArea.Cells);
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to allow the user to enter formulas in a cell in the component.
        /// </summary>
        [DefaultValue(true)]
        public bool CanUserEditFormula
        {
            get { return _allowUserFormula; }
            set { _allowUserFormula = value; }
        }

        /// <summary>
        /// Gets or sets whether to allow the user to undo edit operations.
        /// </summary>
        [DefaultValue(true)]
        public bool CanUserUndo
        {
            get { return _allowUndo; }
            set
            {
                _allowUndo = value;
                if (_undoManager != null)
                {
                    _undoManager.AllowUndo = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the user can scale the display of the component using the Ctrl key and the mouse wheel. 
        /// </summary>
        [DefaultValue(true)]
        public bool CanUserZoom
        {
            get { return _allowUserZoom; }
            set { _allowUserZoom = value; }
        }

        /// <summary>
        /// Gets the cell editor control on the editing viewport.
        /// </summary>
        public Control CellEditor
        {
            get
            {
                if (((EditingViewport != null) && (EditingViewport.EditingContainer != null)) && ((EditingViewport.EditingContainer.EditingRowIndex == Worksheet.ActiveRowIndex) && (EditingViewport.EditingContainer.EditingColumnIndex == Worksheet.ActiveColumnIndex)))
                {
                    return (EditingViewport.EditingContainer.Editor as Control);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the cell editor status.
        /// </summary>
        public EditorStatus CellEditorStatus
        {
            get
            {
                if ((CellEditor != null) && (CellEditor is EditingElement))
                {
                    return (CellEditor as EditingElement).Status;
                }
                return EditorStatus.Ready;
            }
        }

        /// <summary>
        /// Gets or sets whether the component handles the shortcut keys for Clipboard actions. 
        /// </summary>
        [DefaultValue(0xff)]
        public ClipboardPasteOptions ClipBoardOptions
        {
            get { return _clipBoardOptions; }
            set { _clipBoardOptions = value; }
        }

        internal Canvas CursorsContainer
        {
            get
            {
                if (_cursorsContainer == null)
                {
                    _cursorsContainer = new Canvas();
                    Canvas.SetZIndex(_cursorsContainer, 100);
                }
                return _cursorsContainer;
            }
        }

        Windows.UI.Xaml.Controls.Primitives.Popup DataValidationListPopUp
        {
            get
            {
                if (_dataValidationListPopUp == null)
                {
                    _dataValidationListPopUp = new Windows.UI.Xaml.Controls.Primitives.Popup();
                    _dataValidationListPopUp.Opened += _dataValidationListPopUp_Opened;
                    _dataValidationListPopUp.Closed += _dataValidationListPopUp_Closed;
                }
                return _dataValidationListPopUp;
            }
        }

        /// <summary>
        /// Gets or sets the default type of the automatic fill.
        /// </summary>
        /// <value>
        /// The default type of the automatic fill.
        /// </value>
        [DefaultValue((string)null)]
        public AutoFillType? DefaultAutoFillType { get; set; }

        int DragFillStartBottomRow
        {
            get
            {
                if (_dragFillStartRange == null)
                {
                    return -1;
                }
                if (_dragFillStartRange.Row == -1)
                {
                    return (Worksheet.RowCount - 1);
                }
                return ((_dragFillStartRange.Row + _dragFillStartRange.RowCount) - 1);
            }
        }

        RowLayout DragFillStartBottomRowLayout
        {
            get
            {
                int dragFillStartBottomRow = DragFillStartBottomRow;
                if (dragFillStartBottomRow != -1)
                {
                    return GetViewportRowLayoutModel(_dragFillStartBottomRowViewport).FindRow(dragFillStartBottomRow);
                }
                return null;
            }
        }

        int DragFillStartLeftColumn
        {
            get
            {
                if (_dragFillStartRange == null)
                {
                    return -1;
                }
                if (_dragFillStartRange.Column == -1)
                {
                    return 0;
                }
                return _dragFillStartRange.Column;
            }
        }

        ColumnLayout DragFillStartLeftColumnLayout
        {
            get
            {
                int dragFillStartLeftColumn = DragFillStartLeftColumn;
                if (dragFillStartLeftColumn != -1)
                {
                    return GetViewportColumnLayoutModel(_dragFillStartLeftColumnViewport).FindColumn(dragFillStartLeftColumn);
                }
                return null;
            }
        }

        int DragFillStartRightColumn
        {
            get
            {
                if (_dragFillStartRange == null)
                {
                    return -1;
                }
                if (_dragFillStartRange.Column == -1)
                {
                    return (Worksheet.ColumnCount - 1);
                }
                return ((_dragFillStartRange.Column + _dragFillStartRange.ColumnCount) - 1);
            }
        }

        ColumnLayout DragFillStartRightColumnLayout
        {
            get
            {
                int dragFillStartRightColumn = DragFillStartRightColumn;
                if (dragFillStartRightColumn != -1)
                {
                    return GetViewportColumnLayoutModel(_dragFillStartRightColumnViewport).FindColumn(dragFillStartRightColumn);
                }
                return null;
            }
        }

        int DragFillStartTopRow
        {
            get
            {
                if (_dragFillStartRange == null)
                {
                    return -1;
                }
                if (_dragFillStartRange.Row == -1)
                {
                    return 0;
                }
                return _dragFillStartRange.Row;
            }
        }

        RowLayout DragFillStartTopRowLayout
        {
            get
            {
                int dragFillStartTopRow = DragFillStartTopRow;
                if (dragFillStartTopRow != -1)
                {
                    return GetViewportRowLayoutModel(_dragFillStartTopRowViewport).FindRow(dragFillStartTopRow);
                }
                return null;
            }
        }

        int DragFillStartViewportBottomRow
        {
            get { return GetViewportBottomRow(_dragStartRowViewport); }
        }

        RowLayout DragFillStartViewportBottomRowLayout
        {
            get { return GetViewportRowLayoutModel(_dragStartRowViewport).FindRow(DragFillStartViewportBottomRow); }
        }

        int DragFillStartViewportLeftColumn
        {
            get { return GetViewportLeftColumn(_dragStartColumnViewport); }
        }

        ColumnLayout DragFillStartViewportLeftColumnLayout
        {
            get { return GetViewportColumnLayoutModel(_dragStartColumnViewport).FindColumn(DragFillStartViewportLeftColumn); }
        }

        int DragFillStartViewportRightColumn
        {
            get { return GetViewportRightColumn(_dragStartColumnViewport); }
        }

        ColumnLayout DragFillStartViewportRightColumnLayout
        {
            get { return GetViewportColumnLayoutModel(_dragStartColumnViewport).FindColumn(DragFillStartViewportRightColumn); }
        }

        int DragFillStartViewportTopRow
        {
            get { return GetViewportTopRow(_dragStartRowViewport); }
        }

        RowLayout DragFillStartViewportTopRowLayout
        {
            get { return GetViewportRowLayoutModel(_dragStartRowViewport).FindRow(DragFillStartViewportTopRow); }
        }

        int DragFillToViewportBottomRow
        {
            get { return GetViewportBottomRow(_dragToRowViewport); }
        }

        RowLayout DragFillToViewportBottomRowLayout
        {
            get { return GetViewportRowLayoutModel(_dragToRowViewport).FindRow(DragFillToViewportBottomRow); }
        }

        int DragFillToViewportLeftColumn
        {
            get { return GetViewportLeftColumn(_dragToColumnViewport); }
        }

        ColumnLayout DragFillToViewportLeftColumnLayout
        {
            get { return GetViewportColumnLayoutModel(_dragToColumnViewport).FindColumn(DragFillToViewportLeftColumn); }
        }

        int DragFillToViewportRightColumn
        {
            get { return GetViewportRightColumn(_dragToColumnViewport); }
        }

        ColumnLayout DragFillToViewportRightColumnLayout
        {
            get { return GetViewportColumnLayoutModel(_dragToColumnViewport).FindColumn(DragFillToViewportRightColumn); }
        }

        int DragFillToViewportTopRow
        {
            get { return GetViewportTopRow(_dragToRowViewport); }
        }

        RowLayout DragFillToViewportTopRowLayout
        {
            get { return GetViewportRowLayoutModel(_dragToRowViewport).FindRow(DragFillToViewportTopRow); }
        }

        internal GcViewport EditingViewport
        {
            get { return _editinViewport; }
            set { _editinViewport = value; }
        }

        internal FormulaEditorConnector EditorConnector
        {
            get { return _formulaSelectionFeature.FormulaEditorConnector; }
        }

        internal bool EditorDirty
        {
            get { return ((_editinViewport != null) && _editinViewport.EditorDirty); }
        }

        /// <summary>
        /// Gets the information of the editor when the sheetview enters the formula selection mode.
        /// </summary>
        public Dt.Cells.UI.EditorInfo EditorInfo
        {
            get
            {
                if (_editorInfo == null)
                {
                    _editorInfo = new Dt.Cells.UI.EditorInfo(this);
                }
                return _editorInfo;
            }
        }

        Windows.UI.Xaml.Controls.Primitives.Popup FilterPopup
        {
            get
            {
                if (_filterPopup == null)
                {
                    _filterPopup = new Windows.UI.Xaml.Controls.Primitives.Popup();
                    _filterPopup.Opened += FilterPopup_Opened;
                    _filterPopup.Closed += FilterPopup_Closed;
                }
                return _filterPopup;
            }
        }

        Dictionary<KeyStroke, SpreadAction> FloatingObjectKeyMap
        {
            get
            {
                InitFloatingObjectKeyMap();
                return _floatingObjectsKeyMap;
            }
        }

        internal SpreadXFormulaNavigation FormulaNavigation
        {
            get { return _formulaSelectionFeature.Navigation; }
        }

        internal SpreadXFormulaSelection FormulaSelection
        {
            get { return _formulaSelectionFeature.Selection; }
        }

        internal IList<FormulaSelectionItem> FormulaSelections
        {
            get { return _formulaSelectionFeature.Items; }
        }

        /// <summary>
        /// Gets or sets a value that indicates the freeze line style.
        /// </summary>
        [DefaultValue((string)null)]
        internal Style FreezeLineStyle
        {
            get { return _freezeLineStyle; }
            set
            {
                _freezeLineStyle = value;
                _columnFreezeLine.TypeSafeSetStyle(value);
                _rowFreezeLine.TypeSafeSetStyle(value);
                Invalidate();
            }
        }

        internal FormulaSelectionFeature FSelectionFeature
        {
            get { return _formulaSelectionFeature; }
        }

        internal GripperLocationsStruct GripperLocations { get; set; }

        internal bool HideSelectionWhenPrinting
        {
            get { return _hideSelectionWhenPrinting; }
            set { _hideSelectionWhenPrinting = value; }
        }

        /// <summary>
        /// Gets or sets whether to highlight invalid data.
        /// </summary>
        [DefaultValue(false)]
        public bool HighlightInvalidData
        {
            get { return _highlightDataValidationInvalidData; }
            set
            {
                if (_highlightDataValidationInvalidData != value)
                {
                    _highlightDataValidationInvalidData = value;
                    if (!HighlightInvalidData)
                    {
                        RefreshDataValidationInvalidCircles();
                    }
                    else
                    {
                        Invalidate();
                    }
                }
            }
        }

        internal FilterButtonInfo HitFilterInfo
        {
            get { return _hitFilterInfo; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the horizontal scroll bar is scrollable.
        /// </summary>
        /// <value>
        /// <c>true</c> if the horizontal scroll bar is scrollable; otherwise, <c>false</c>.
        /// </value>
        [DefaultValue(true)]
        public bool HorizontalScrollable
        {
            get { return _hScrollable; }
            set { _hScrollable = value; }
        }

        internal Dt.Cells.UI.HoverManager HoverManager
        {
            get { return _hoverManager; }
        }

        internal FontFamily InheritedControlFontFamily
        {
            get
            {
                if (_inheritedControlFontFamily == null)
                {
                    if (_host != null)
                    {
                        _inheritedControlFontFamily = _host.FontFamily;
                    }
                    else
                    {
                        TextBlock block = new TextBlock();
                        _inheritedControlFontFamily = block.FontFamily;
                    }
                }
                return _inheritedControlFontFamily;
            }
        }

        /// <summary>
        /// Returns the last input device type.
        /// </summary>
        [DefaultValue(0)]
        public Dt.Cells.UI.InputDeviceType InputDeviceType
        {
            get { return _inputDeviceType; }
            internal set
            {
                _inputDeviceType = value;
                if (_inputDeviceType == Dt.Cells.UI.InputDeviceType.Touch)
                {
                    FormulaSelectionFeature.IsTouching = true;
                }
                else if (_inputDeviceType == Dt.Cells.UI.InputDeviceType.Mouse)
                {
                    FormulaSelectionFeature.IsTouching = false;
                }
            }
        }

        /// <summary>
        /// Gets a value that indicates whether there is a cell in edit mode.
        /// </summary>
        public bool IsCellEditing
        {
            get { return IsEditing; }
        }

        bool IsDecreaseFill
        {
            get
            {
                if (_currentFillDirection != DragFillDirection.Left)
                {
                    return (_currentFillDirection == DragFillDirection.Up);
                }
                return true;
            }
        }

        bool IsDragClear
        {
            get
            {
                if (_currentFillDirection != DragFillDirection.LeftClear)
                {
                    return (_currentFillDirection == DragFillDirection.UpClear);
                }
                return true;
            }
        }

        bool IsDragDropping { get; set; }

        bool IsDragFill
        {
            get
            {
                if (!IsIncreaseFill)
                {
                    return IsDecreaseFill;
                }
                return true;
            }
        }

        bool IsDragFillStartBottomRowInView
        {
            get { return IsRowInViewport(_dragFillStartBottomRowViewport, DragFillStartBottomRow); }
        }

        bool IsDragFillStartLeftColumnInView
        {
            get { return IsColumnInViewport(_dragFillStartLeftColumnViewport, DragFillStartLeftColumn); }
        }

        bool IsDragFillStartRightColumnInView
        {
            get { return IsColumnInViewport(_dragFillStartRightColumnViewport, DragFillStartRightColumn); }
        }

        bool IsDragFillStartTopRowInView
        {
            get { return IsRowInViewport(_dragFillStartTopRowViewport, DragFillStartTopRow); }
        }

        bool IsDragFillWholeColumns
        {
            get { return ((_dragFillStartRange.Row == -1) && (_dragFillStartRange.Column != -1)); }
        }

        bool IsDragFillWholeRows
        {
            get { return ((_dragFillStartRange.Column == -1) && (_dragFillStartRange.Row != -1)); }
        }

        internal bool IsDraggingFill { get; set; }

        bool IsDragToColumnInView
        {
            get { return IsColumnInViewport(_dragToColumnViewport, _dragToColumn); }
        }

        bool IsDragToRowInView
        {
            get { return IsRowInViewport(_dragToRowViewport, _dragToRow); }
        }

        internal virtual bool IsEditing
        {
            get { return _isEditing; }
            set { _isEditing = value; }
        }

        internal bool IsFilterDropDownOpen
        {
            get { return ((_filterPopup != null) && _filterPopup.IsOpen); }
        }

        bool IsIncreaseFill
        {
            get
            {
                if (_currentFillDirection != DragFillDirection.Down)
                {
                    return (_currentFillDirection == DragFillDirection.Right);
                }
                return true;
            }
        }

        internal bool IsMouseLeftButtonPressed { get; set; }

        internal bool IsMouseRightButtonPressed { get; set; }

        bool IsMovingFloatingOjects { get; set; }

        bool IsResizingColumns { get; set; }

        bool IsResizingFloatingObjects { get; set; }

        bool IsResizingRows { get; set; }

        bool IsSelectingCells { get; set; }

        bool IsSelectingColumns { get; set; }

        bool IsSelectingRows { get; set; }

        internal bool IsSelectionBegined
        {
            get { return _formulaSelectionFeature.IsSelectionBegined; }
        }

        internal bool IsTouchingMovingFloatingObjects { get; set; }

        internal bool IsTouchingResizingFloatingObjects { get; set; }

        bool IsVerticalDragFill
        {
            get
            {
                if ((_currentFillDirection != DragFillDirection.Up) && (_currentFillDirection != DragFillDirection.Down))
                {
                    return (_currentFillDirection == DragFillDirection.UpClear);
                }
                return true;
            }
        }

        internal bool IsWorking { get; set; }

        /// <summary>
        /// Gets the key map collection that contains associated keys and actions.
        /// </summary>
        public Dictionary<KeyStroke, SpreadAction> KeyMap
        {
            get
            {
                InitDefaultKeyMap();
                return _keyMap;
            }
        }

        internal int MaxCellOverflowDistance
        {
            get { return 100; }
            set
            {
            }
        }

        internal int MouseOverColumnIndex { get; set; }

        internal int MouseOverRowIndex { get; set; }

        internal Point MousePosition { get; set; }

        internal SpreadXNavigation Navigation
        {
            get
            {
                if (_navigation == null)
                {
                    _navigation = new SpreadXNavigation(this);
                    if (Worksheet != null)
                    {
                        _navigation.UpdateStartPosition(Worksheet.ActiveRowIndex, Worksheet.ActiveColumnIndex);
                    }
                }
                return _navigation;
            }
        }

        /// <summary>
        /// Gets or sets the backgroud of the range group
        /// </summary>
        public Brush RangeGroupBackground
        {
            get { return _rangeGroupBackground; }
            set
            {
                _rangeGroupBackground = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the brush of the border of the range group
        /// </summary>
        public Brush RangeGroupBorderBrush
        {
            get { return _rangeGroupBorderBrush; }
            set
            {
                _rangeGroupBorderBrush = value;
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the stroke of the group line
        /// </summary>
        public Brush RangeGroupLineStroke
        {
            get { return _rangeGroupLineStroke; }
            set
            {
                _rangeGroupLineStroke = value;
                Invalidate();
            }
        }

        internal Rect? ResizerGripperRect { get; set; }

        /// <summary>
        /// Specifies the drawing policy when the row or column is resized to zero.
        /// </summary>
        [DefaultValue(0)]
        public Dt.Cells.UI.ResizeZeroIndicator ResizeZeroIndicator
        {
            get { return _resizeZeroIndicator; }
            set { _resizeZeroIndicator = value; }
        }

        CellRange[] SavedOldSelections { get; set; }

        internal SpreadXSelection Selection
        {
            get
            {
                if (_selection == null)
                {
                    _selection = new SpreadXSelection(this);
                }
                return _selection;
            }
        }

        internal Canvas ShapeDrawingContainer
        {
            get
            {
                if (_shapeDrawingContainer == null)
                {
                    _shapeDrawingContainer = new Canvas();
                }
                return _shapeDrawingContainer;
            }
        }

        /// <summary>
        /// Gets or sets whether the column range group is visible.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowColumnRangeGroup
        {
            get { return _showColumnRangeGroup; }
            set
            {
                if (value != _showColumnRangeGroup)
                {
                    _showColumnRangeGroup = value;
                    InvalidateLayout();
                    base.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to show the drag drop tip.
        /// </summary>
        /// <value>
        /// <c>true</c> if show drag drop tip; otherwise, <c>false</c>.
        /// </value>
        public bool ShowDragDropTip
        {
            get
            {
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    return sheet.ShowDragDropTip;
                }
                return true;
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.Workbook.ShowDragDropTip = value;
                }
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    sheet.ShowDragDropTip = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to show a drag fill tip.
        /// </summary>
        /// <value>
        /// <c>true</c> if show drag fill tip; otherwise, <c>false</c>.
        /// </value>
        public bool ShowDragFillTip
        {
            get
            {
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    return sheet.ShowDragFillTip;
                }
                return true;
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.Workbook.ShowDragFillTip = value;
                }
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    sheet.ShowDragFillTip = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to show the freeze line.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowFreezeLine
        {
            get { return _showFreezeLine; }
            set
            {
                _showFreezeLine = value;
                UpdateFreezeLines();
            }
        }

        /// <summary>
        /// Gets or sets how to display the resize tip.
        /// </summary>
        [DefaultValue(0)]
        public Dt.Cells.Data.ShowResizeTip ShowResizeTip
        {
            get
            {
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    return sheet.ShowResizeTip;
                }
                return Dt.Cells.Data.ShowResizeTip.None;
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.Workbook.ShowResizeTip = value;
                }
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    sheet.ShowResizeTip = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets whether the row range group is visible.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowRowRangeGroup
        {
            get { return _showRowRangeGroup; }
            set
            {
                if (value != _showRowRangeGroup)
                {
                    _showRowRangeGroup = value;
                    InvalidateLayout();
                    base.InvalidateMeasure();
                }
            }
        }

        /// <summary>
        /// Gets or sets how to display the scroll tip.
        /// </summary>
        [DefaultValue(0)]
        public Dt.Cells.Data.ShowScrollTip ShowScrollTip
        {
            get
            {
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    return sheet.ShowScrollTip;
                }
                return Dt.Cells.Data.ShowScrollTip.None;
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.Workbook.ShowScrollTip = value;
                }
                Excel sheet = _host as Excel;
                if (sheet != null)
                {
                    sheet.ShowScrollTip = value;
                }
            }
        }

        TooltipPopupHelper TooltipHelper
        {
            get
            {
                if (_tooltipHelper == null)
                {
                    _tooltipHelper = new TooltipPopupHelper(this, -1.0);
                }
                return _tooltipHelper;
            }
            set { _tooltipHelper = value; }
        }

        internal Windows.UI.Xaml.Controls.Primitives.Popup ToolTipPopup
        {
            get
            {
                if (_tooltipPopup == null)
                {
                    _tooltipPopup = new Windows.UI.Xaml.Controls.Primitives.Popup();
                    _tooltipPopup.IsHitTestVisible = false;
                    base.Children.Add(_tooltipPopup);
                }
                return _tooltipPopup;
            }
        }

        internal Canvas TrackersContainer
        {
            get
            {
                if (_trackersContainer == null)
                {
                    _trackersContainer = new Canvas();
                    Canvas.SetZIndex(_trackersContainer, 2);
                }
                return _trackersContainer;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates the trailing freeze line style.
        /// </summary>
        [DefaultValue((string)null)]
        internal Style TrailingFreezeLineStyle
        {
            get { return _trailingFreezeLineStyle; }
            set
            {
                _trailingFreezeLineStyle = value;
                _columnTrailingFreezeLine.TypeSafeSetStyle(value);
                _rowTrailingFreezeLine.TypeSafeSetStyle(value);
                Invalidate();
            }
        }

        /// <summary>
        /// Gets the undo manager for the control.
        /// </summary>
#if IOS
        new
#endif
        public Dt.Cells.UI.UndoManager UndoManager
        {
            get
            {
                if (_undoManager == null)
                {
                    _undoManager = new Dt.Cells.UI.UndoManager(this, -1, CanUserUndo);
                }
                return _undoManager;
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the vertical scroll bar is scrollable.
        /// </summary>
        /// <value>
        /// <c>true</c> if the vertical scroll bar is scrollable; otherwise, <c>false</c>.
        /// </value>
        [DefaultValue(true)]
        public bool VerticalScrollable
        {
            get { return _vScrollable; }
            set { _vScrollable = value; }
        }

        /// <summary>
        /// Gets the worksheet associated with the view.
        /// </summary>
        public virtual Dt.Cells.Data.Worksheet Worksheet
        {
            get
            {
                if (_sheet == null)
                {
                    _sheet = new Dt.Cells.Data.Worksheet();
                    _sheet.SelectionChanged += new EventHandler<SheetSelectionChangedEventArgs>(HandleSheetSelectionChanged);
                }
                return _sheet;
            }
        }

        /// <summary>
        /// Gets or sets the scaling factor for displaying this sheet.
        /// </summary>
        /// <value>The scaling factor for displaying this sheet.</value>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// Specified scaling amount is out of range; must be between 0.5 (50%) and 4.0 (400%).
        /// </exception>
        [DefaultValue((float)1f)]
        public float ZoomFactor
        {
            get
            {
                if (Worksheet != null)
                {
                    return Worksheet.ZoomFactor;
                }
                return 1f;
            }
            set
            {
                if (Worksheet != null)
                {
                    Worksheet.ZoomFactor = value;
                    InvalidateRange(-1, -1, -1, -1, SheetArea.Cells);
                    InvalidateRange(-1, -1, -1, -1, SheetArea.ColumnHeader);
                    InvalidateRange(-1, -1, -1, -1, SheetArea.CornerHeader | SheetArea.RowHeader);
                    base.InvalidateMeasure();
                }
            }
        }

        class ActiveCellChangingEventArgs : CancelEventArgs
        {
            public ActiveCellChangingEventArgs(int row, int column)
            {
                Row = row;
                Column = column;
            }

            public int Column { get; private set; }

            public int Row { get; private set; }
        }

        internal class ColoredText
        {
            public ColoredText(string text, Windows.UI.Color color)
            {
                Text = text;
                Color = color;
            }

            public Windows.UI.Color Color { get; set; }

            public string Text { get; set; }
        }

        internal enum DragFillDirection
        {
            Left,
            Right,
            Up,
            Down,
            LeftClear,
            UpClear
        }

        internal class EditorManager : IFormulaEditor
        {
            TextBox _editorTextBox;
            string _footer;
            Dt.Cells.UI.SheetView.FormulaSelectionFeature _formulaSelectionFeature;
            string _header;
            bool _isMouseLeftButtonDown;
            string _oldText;
            bool _selectionChanged;
            bool _textChanged;
            DispatcherTimer _timer;

            public EditorManager(Dt.Cells.UI.SheetView.FormulaSelectionFeature formulaSelectionFeature)
            {
                _formulaSelectionFeature = formulaSelectionFeature;
                _formulaSelectionFeature.SheetView.EditStarting += new EventHandler<EditCellStartingEventArgs>(OnSheetViewEditStarting);
                _formulaSelectionFeature.SheetView.EditEnd += new EventHandler<EditCellEventArgs>(OnSheetViewEditEnd);
                _formulaSelectionFeature.FormulaEditorConnector.FormulaChangedByUI += new EventHandler(OnEditorConnectorFormulaChangedByUI);
            }

            void OnEditorConnectorFormulaChangedByUI(object sender, EventArgs e)
            {
                if ((_editorTextBox != null) && (_formulaSelectionFeature.FormulaEditorConnector.Editor == this))
                {
                    _isMouseLeftButtonDown = false;
                    UpdateBlocks();
                }
            }

            void OnEditorTextBoxGotFocus(object sender, RoutedEventArgs e)
            {
                _formulaSelectionFeature.FormulaEditorConnector.Editor = this;
                _textChanged = true;
                _selectionChanged = true;
                OnTimerTick(this, EventArgs.Empty);
            }

            void OnEditorTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
            {
                if (((e.Key == VirtualKey.F4) && (SheetView != null)) && (SheetView.EditorConnector.Editor == this))
                {
                    SheetView.EditorConnector.ChangeRelative();
                }
            }

            void OnEditorTextBoxSelectionChanged(object sender, RoutedEventArgs e)
            {
                if ((_editorTextBox != null) && _isMouseLeftButtonDown)
                {
                    _formulaSelectionFeature.FormulaEditorConnector.Editor = this;
                    _selectionChanged = true;
                    StartTimer();
                }
            }

            void OnEditorTextBoxTextChanged(object sender, TextChangedEventArgs e)
            {
                if ((_editorTextBox != null) && (_editorTextBox.Text != _oldText))
                {
                    _oldText = _editorTextBox.Text;
                    _textChanged = true;
                    _selectionChanged = true;
                    StartTimer();
                }
            }

            void OnPointerPressed(object sender, PointerRoutedEventArgs e)
            {
                bool isLeftButtonPressed = true;
                if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
                {
                    isLeftButtonPressed = e.GetCurrentPoint(_editorTextBox).Properties.IsLeftButtonPressed;
                }
                if (isLeftButtonPressed)
                {
                    ProcessEditorLeftMouseDown();
                }
            }

            void OnSheetViewEditEnd(object sender, EditCellEventArgs e)
            {
                if (_editorTextBox != null)
                {
                    _editorTextBox.KeyDown -= OnEditorTextBoxKeyDown;
                    _editorTextBox.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed));
                    _editorTextBox.LostFocus -= OnEditorTextBoxGotFocus;
                    UnWireEvents();
                    _editorTextBox = null;
                    if (_timer != null)
                    {
                        _timer.Stop();
                        _timer = null;
                    }
                    if (!_formulaSelectionFeature.FormulaEditorConnector.IsInOtherSheet)
                    {
                        _formulaSelectionFeature.EndFormulaSelection();
                    }
                    _formulaSelectionFeature.Items.Clear();
                }
            }

            void OnSheetViewEditStarting(object sender, EditCellStartingEventArgs e)
            {
                if (SheetView.CanUserEditFormula)
                {
                    _editorTextBox = _formulaSelectionFeature.SheetView.CellEditor as TextBox;
                    if (_editorTextBox != null)
                    {
                        _editorTextBox.KeyDown += OnEditorTextBoxKeyDown;
                        _editorTextBox.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnPointerPressed), true);
                        _editorTextBox.LostFocus += OnEditorTextBoxGotFocus;
                        _formulaSelectionFeature.FormulaEditorConnector.Editor = this;
                        WireEvents();
                        if (_formulaSelectionFeature.FormulaEditorConnector.IsInOtherSheet)
                        {
                            OnEditorConnectorFormulaChangedByUI(this, EventArgs.Empty);
                            _formulaSelectionFeature.FormulaEditorConnector.IsInOtherSheet = false;
                        }
                        else
                        {
                            _isMouseLeftButtonDown = true;
                        }
                        _textChanged = true;
                        _selectionChanged = true;
                        OnTimerTick(this, EventArgs.Empty);
                    }
                }
            }

            void OnTimerTick(object sender, object e)
            {
                if (((SheetView != null) && (SheetView.EditorConnector != null)) && ((SheetView.EditorConnector.Editor == this) && (_editorTextBox != null)))
                {
                    string text = _editorTextBox.Text;
                    Match match = new Regex(@"(^\s*=\s*)(.*?)(\s*$)").Match(text);
                    if (!match.Success)
                    {
                        _textChanged = false;
                        _selectionChanged = false;
                        if (_timer != null)
                        {
                            _timer.Stop();
                        }
                        SheetView.EndFormulaSelection();
                        return;
                    }
                    if (_textChanged)
                    {
                        SheetView.BeginFormulaSelection(null);
                        _header = match.Groups[1].Value;
                        _footer = match.Groups[3].Value;
                        SheetView.EditorConnector.OnFormulaTextChanged(match.Groups[2].Value);
                    }
                    if (_selectionChanged)
                    {
                        _selectionChanged = false;
                        int selectionStart = _editorTextBox.SelectionStart;
                        int end = selectionStart + _editorTextBox.SelectionLength;
                        if (_header != null)
                        {
                            selectionStart -= _header.Length;
                            end -= _header.Length;
                        }
                        SheetView.EditorConnector.OnCursorPositionChanged(selectionStart, end);
                    }
                    if (_textChanged)
                    {
                        _textChanged = false;
                        UpdateBlocks();
                    }
                }
                if (_timer != null)
                {
                    _timer.Stop();
                }
            }

            void ProcessEditorLeftMouseDown()
            {
                _isMouseLeftButtonDown = true;
                _selectionChanged = true;
                _formulaSelectionFeature.FormulaEditorConnector.Editor = this;
                StartTimer();
            }

            void StartTimer()
            {
                if (_timer == null)
                {
                    _timer = new DispatcherTimer();
                    _timer.Interval = new TimeSpan(0, 0, 0, 0, 200);
                    _timer.Tick += OnTimerTick;
                }
                _timer.Stop();
                _timer.Start();
            }

            void UnWireEvents()
            {
                if (_editorTextBox != null)
                {
                    _editorTextBox.TextChanged -= OnEditorTextBoxTextChanged;
                    _editorTextBox.SelectionChanged -= OnEditorTextBoxSelectionChanged;
                }
            }

            void UpdateBlocks()
            {
                UnWireEvents();
                try
                {
                    IList<Dt.Cells.UI.SheetView.ColoredText> coloredText = SheetView.EditorConnector.GetColoredText(false);
                    StringBuilder builder = new StringBuilder();
                    if (string.IsNullOrEmpty(_header) && (coloredText.Count > 0))
                    {
                        _header = "=";
                    }
                    if (!string.IsNullOrEmpty(_header))
                    {
                        builder.Append(_header);
                    }
                    foreach (Dt.Cells.UI.SheetView.ColoredText text in coloredText)
                    {
                        builder.Append(text.Text);
                    }
                    if (!string.IsNullOrEmpty(_footer))
                    {
                        builder.Append(_footer);
                    }
                    _editorTextBox.Text = builder.ToString();
                    _oldText = _editorTextBox.Text;
                    int cursorPositionStart = SheetView.EditorConnector.GetCursorPositionStart();
                    if (_header != null)
                    {
                        cursorPositionStart += _header.Length;
                    }
                    _editorTextBox.Select(cursorPositionStart, 0);
                }
                finally
                {
                    WireEvents();
                }
            }

            void WireEvents()
            {
                if (_editorTextBox != null)
                {
                    _editorTextBox.TextChanged -= OnEditorTextBoxTextChanged;
                    _editorTextBox.SelectionChanged -= OnEditorTextBoxSelectionChanged;
                    _editorTextBox.TextChanged += OnEditorTextBoxTextChanged;
                    _editorTextBox.SelectionChanged += OnEditorTextBoxSelectionChanged;
                }
            }

            public bool IsAbsolute { get; set; }

            Dt.Cells.UI.SheetView SheetView
            {
                get { return _formulaSelectionFeature.SheetView; }
            }
        }

        internal class FormulaEditorConnector
        {
            bool _activateEditor = true;
            int _colorIndex = -1;
            Windows.UI.Color[] _colors = new Windows.UI.Color[] { Windows.UI.Color.FromArgb(0xff, 0, 0, 0xff), Windows.UI.Color.FromArgb(0xff, 0, 0x80, 0), Windows.UI.Color.FromArgb(0xff, 0x99, 0, 0xcc), Windows.UI.Color.FromArgb(0xff, 0x80, 0, 0), Windows.UI.Color.FromArgb(0xff, 0, 0xcc, 0x33), Windows.UI.Color.FromArgb(0xff, 0xff, 0x66, 0), Windows.UI.Color.FromArgb(0xff, 0xcc, 0, 0x99) };
            int _columnIndex;
            int _cursorPositionEnd;
            int _cursorPositionStart;
            IFormulaEditor _editor;
            IList<SheetView.FormulaExpression> _footer;
            IList<SheetView.FormulaExpression> _formulaExpressions;
            SheetView.FormulaSelectionFeature _formulaSelectionFeature;
            IList<SheetView.FormulaExpression> _header;
            IList<SheetView.FormulaExpression> _middle;
            int _rowIndex;
            int _sheetIndex;
            bool _splited;

            public event EventHandler FormulaChangedByUI;

            public FormulaEditorConnector(SheetView.FormulaSelectionFeature formulaSelectionFeature)
            {
                _formulaSelectionFeature = formulaSelectionFeature;
                _formulaSelectionFeature.ItemAdded += new EventHandler<FormulaSelectionItemEventArgs>(OnFormulaSelectionFeatureItemAdded);
                _formulaSelectionFeature.ItemRemoved += new EventHandler<FormulaSelectionItemEventArgs>(OnFormulaSelectionFeatureItemRemoved);
            }

            bool CanSelectFormulaByUI(IList<SheetView.FormulaExpression> expressionList, int cursorStart, int cursorEnd)
            {
                if (((cursorStart < 0) || (cursorEnd < 0)) || (expressionList == null))
                {
                    return false;
                }
                int num = 0;
                for (int i = 0; i < expressionList.Count; i++)
                {
                    SheetView.FormulaExpression expression = expressionList[i];
                    int num3 = num + expression.Text.Length;
                    if (((cursorStart > num) && (cursorStart <= num3)) && ((expression.Range != null) || (expression.Text.EndsWith(")") && (cursorStart >= num3))))
                    {
                        return false;
                    }
                    num = num3;
                }
                return true;
            }

            public void ChangeRelative()
            {
                if (_splited)
                {
                    if ((_middle != null) && (_middle.Count > 0))
                    {
                        if (_middle[0].StartColumnRelative && _middle[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression in _middle)
                            {
                                expression.StartColumnRelative = expression.EndColumnRelative = false;
                                expression.StartRowRelative = expression.EndRowRelative = false;
                                expression.UpdateText();
                            }
                        }
                        else if (!_middle[0].StartColumnRelative && !_middle[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression2 in _middle)
                            {
                                expression2.StartColumnRelative = expression2.EndColumnRelative = true;
                                expression2.StartRowRelative = expression2.EndRowRelative = false;
                                expression2.UpdateText();
                            }
                        }
                        else if (_middle[0].StartColumnRelative && !_middle[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression3 in _middle)
                            {
                                expression3.StartColumnRelative = expression3.EndColumnRelative = false;
                                expression3.StartRowRelative = expression3.EndRowRelative = true;
                                expression3.UpdateText();
                            }
                        }
                        else if (!_middle[0].StartColumnRelative && _middle[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression4 in _middle)
                            {
                                expression4.StartColumnRelative = expression4.EndColumnRelative = true;
                                expression4.StartRowRelative = expression4.EndRowRelative = true;
                                expression4.UpdateText();
                            }
                        }
                        OnFormulaChangedByUI();
                    }
                }
                else if ((_formulaExpressions != null) && (_formulaExpressions.Count > 0))
                {
                    List<SheetView.FormulaExpression> list = new List<SheetView.FormulaExpression>();
                    int num = _cursorPositionStart;
                    int num2 = _cursorPositionEnd;
                    int num3 = 0;
                    foreach (SheetView.FormulaExpression expression5 in _formulaExpressions)
                    {
                        int num4 = num3;
                        num3 += expression5.Text.Length;
                        if ((num4 <= _cursorPositionEnd) && (((num3 >= _cursorPositionStart) && (_cursorPositionStart == _cursorPositionEnd)) || ((num3 > _cursorPositionStart) && (_cursorPositionStart != _cursorPositionEnd))))
                        {
                            if (list.Count == 0)
                            {
                                num = num4;
                            }
                            num2 = num3;
                            list.Add(expression5);
                        }
                    }
                    if (list.Count > 0)
                    {
                        if (list[0].StartColumnRelative && list[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression6 in list)
                            {
                                expression6.StartColumnRelative = expression6.EndColumnRelative = false;
                                expression6.StartRowRelative = expression6.EndRowRelative = false;
                                expression6.UpdateText();
                            }
                        }
                        else if (!list[0].StartColumnRelative && !list[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression7 in list)
                            {
                                expression7.StartColumnRelative = expression7.EndColumnRelative = true;
                                expression7.StartRowRelative = expression7.EndRowRelative = false;
                                expression7.UpdateText();
                            }
                        }
                        else if (list[0].StartColumnRelative && !list[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression8 in list)
                            {
                                expression8.StartColumnRelative = expression8.EndColumnRelative = false;
                                expression8.StartRowRelative = expression8.EndRowRelative = true;
                                expression8.UpdateText();
                            }
                        }
                        else if (!list[0].StartColumnRelative && list[0].StartRowRelative)
                        {
                            foreach (SheetView.FormulaExpression expression9 in list)
                            {
                                expression9.StartColumnRelative = expression9.EndColumnRelative = true;
                                expression9.StartRowRelative = expression9.EndRowRelative = true;
                                expression9.UpdateText();
                            }
                        }
                        num2 = num;
                        foreach (SheetView.FormulaExpression expression10 in list)
                        {
                            num2 += expression10.Text.Length;
                        }
                        num2--;
                        _cursorPositionStart = num;
                        _cursorPositionEnd = num2;
                        OnFormulaChangedByUI();
                    }
                }
            }

            public void ClearFlickingItems()
            {
                _formulaSelectionFeature.ClearFlickingSelection();
            }

            SheetView.FormulaExpression CreateFormulaExpression(CalcExpression expression, string expressionText, int baseRow, int baseColumn)
            {
                CalcRangeExpression expression2 = expression as CalcRangeExpression;
                CalcCellExpression expression3 = expression as CalcCellExpression;
                CalcExternalCellExpression expression4 = expression as CalcExternalCellExpression;
                CalcExternalRangeExpression expression5 = expression as CalcExternalRangeExpression;
                CalcNameExpression expression6 = expression as CalcNameExpression;
                CalcExternalNameExpression expression7 = expression as CalcExternalNameExpression;
                if (expression2 != null)
                {
                    CalcRangeIdentity id = expression2.GetId(baseRow, baseColumn) as CalcRangeIdentity;
                    return new SheetView.FormulaExpression(this, new CellRange(id.RowIndex, id.ColumnIndex, id.RowCount, id.ColumnCount), expressionText, false, null) { StartRowRelative = expression2.StartRowRelative, StartColumnRelative = expression2.StartColumnRelative, EndRowRelative = expression2.EndRowRelative, EndColumnRelative = expression2.EndColumnRelative };
                }
                if (expression3 != null)
                {
                    CalcCellIdentity identity2 = expression3.GetId(baseRow, baseColumn) as CalcCellIdentity;
                    int rowCount = 1;
                    int columnCount = 1;
                    CellRange range2 = _formulaSelectionFeature.SheetView.Worksheet.SpanModel.Find(identity2.RowIndex, identity2.ColumnIndex);
                    if (((range2 != null) && (range2.Row == identity2.RowIndex)) && (range2.Column == identity2.ColumnIndex))
                    {
                        rowCount = range2.RowCount;
                        columnCount = range2.ColumnCount;
                    }
                    return new SheetView.FormulaExpression(this, new CellRange(identity2.RowIndex, identity2.ColumnIndex, rowCount, columnCount), expressionText, false, null) { StartRowRelative = expression3.RowRelative, StartColumnRelative = expression3.ColumnRelative, EndRowRelative = expression3.RowRelative, EndColumnRelative = expression3.ColumnRelative };
                }
                if (expression5 != null)
                {
                    CalcExternalRangeIdentity identity3 = expression5.GetId(baseRow, baseColumn) as CalcExternalRangeIdentity;
                    return new SheetView.FormulaExpression(this, new CellRange(identity3.RowIndex, identity3.ColumnIndex, identity3.RowCount, identity3.ColumnCount), expressionText, false, expression5.Source as Worksheet) { StartRowRelative = expression5.StartRowRelative, StartColumnRelative = expression5.StartColumnRelative, EndRowRelative = expression5.EndRowRelative, EndColumnRelative = expression5.EndColumnRelative };
                }
                if (expression4 != null)
                {
                    CalcExternalCellIdentity identity4 = expression4.GetId(baseRow, baseColumn) as CalcExternalCellIdentity;
                    int num3 = 1;
                    int num4 = 1;
                    Worksheet source = expression4.Source as Worksheet;
                    CellRange range5 = source.SpanModel.Find(identity4.RowIndex, identity4.ColumnIndex);
                    if (((range5 != null) && (range5.Row == identity4.RowIndex)) && (range5.Column == identity4.ColumnIndex))
                    {
                        num3 = range5.RowCount;
                        num4 = range5.ColumnCount;
                    }
                    return new SheetView.FormulaExpression(this, new CellRange(identity4.RowIndex, identity4.ColumnIndex, num3, num4), expressionText, false, source) { StartRowRelative = expression4.RowRelative, StartColumnRelative = expression4.ColumnRelative, EndRowRelative = expression4.RowRelative, EndColumnRelative = expression4.ColumnRelative };
                }
                if (expression6 != null)
                {
                    NameInfo customName = _formulaSelectionFeature.SheetView.Worksheet.GetCustomName(expression6.Name);
                    if (customName == null)
                    {
                        customName = _formulaSelectionFeature.SheetView.Worksheet.Workbook.GetCustomName(expression6.Name);
                    }
                    if (customName != null)
                    {
                        CalcReferenceExpression reference = customName.Expression as CalcReferenceExpression;
                        if (reference != null)
                        {
                            CellRange rangeFromExpression = Dt.Cells.Data.CellRangUtility.GetRangeFromExpression(reference);
                            Worksheet sheet = null;
                            if (reference is CalcExternalExpression)
                            {
                                sheet = (reference as CalcExternalExpression).Source as Worksheet;
                            }
                            return new SheetView.FormulaExpression(this, rangeFromExpression, expressionText, true, sheet);
                        }
                    }
                    return new SheetView.FormulaExpression(this, expressionText);
                }
                if (expression7 != null)
                {
                    Worksheet worksheet3 = expression7.Source as Worksheet;
                    if (worksheet3 != null)
                    {
                        NameInfo info2 = worksheet3.GetCustomName(expression7.Name);
                        if (info2 != null)
                        {
                            CalcReferenceExpression expression14 = info2.Expression as CalcReferenceExpression;
                            if (expression14 != null)
                            {
                                CellRange range = Dt.Cells.Data.CellRangUtility.GetRangeFromExpression(expression14);
                                Worksheet worksheet4 = null;
                                if (expression14 is CalcExternalExpression)
                                {
                                    worksheet4 = (expression14 as CalcExternalExpression).Source as Worksheet;
                                }
                                return new SheetView.FormulaExpression(this, range, expressionText, true, worksheet4);
                            }
                        }
                    }
                }
                return new SheetView.FormulaExpression(this, expressionText);
            }

            static CalcRangeExpression CreateRangeExpressionByCount(int row, int column, int rowCount, int columnCount, bool startRowRelative = false, bool startColumnRelative = false, bool endRowRelative = false, bool endColumnRelative = false)
            {
                if ((rowCount == -1) && (columnCount == -1))
                {
                    return new CalcRangeExpression();
                }
                if (columnCount == -1)
                {
                    return new CalcRangeExpression(row, (row + rowCount) - 1, startRowRelative, endRowRelative, true);
                }
                if (rowCount == -1)
                {
                    return new CalcRangeExpression(column, (column + columnCount) - 1, startColumnRelative, endColumnRelative, false);
                }
                return new CalcRangeExpression(row, column, (row + rowCount) - 1, (column + columnCount) - 1, startRowRelative, startColumnRelative, endRowRelative, endColumnRelative);
            }

            internal string FindNameRange(CellRange range)
            {
                foreach (string str in _formulaSelectionFeature.SheetView.Worksheet.CustomNames)
                {
                    CalcReferenceExpression reference = _formulaSelectionFeature.SheetView.Worksheet.GetCustomName(str).Expression as CalcReferenceExpression;
                    if (reference != null)
                    {
                        CellRange rangeFromExpression = Dt.Cells.Data.CellRangUtility.GetRangeFromExpression(reference);
                        if ((rangeFromExpression != null) && rangeFromExpression.Equals(range))
                        {
                            return str;
                        }
                    }
                }
                foreach (string str2 in _formulaSelectionFeature.SheetView.Worksheet.Workbook.CustomNames)
                {
                    CalcReferenceExpression expression = _formulaSelectionFeature.SheetView.Worksheet.Workbook.GetCustomName(str2).Expression as CalcReferenceExpression;
                    if (expression != null)
                    {
                        CalcExternalExpression expression3 = expression as CalcExternalExpression;
                        if ((expression3 == null) || (expression3.Source == _formulaSelectionFeature.SheetView.Worksheet))
                        {
                            CellRange range3 = Dt.Cells.Data.CellRangUtility.GetRangeFromExpression(expression);
                            if ((range3 != null) && range3.Equals(range))
                            {
                                return str2;
                            }
                        }
                    }
                }
                return null;
            }

            public IList<SheetView.ColoredText> GetColoredText(bool includeSheetName = false)
            {
                List<SheetView.ColoredText> list = new List<SheetView.ColoredText>();
                foreach (SheetView.FormulaExpression expression in GetMergedExpressionList())
                {
                    if (includeSheetName && (expression.Sheet == null))
                    {
                        if (_formulaSelectionFeature.IsInOtherSheet)
                        {
                            expression.Sheet = _formulaSelectionFeature.SheetView.EditorInfo.Sheet;
                        }
                        else
                        {
                            expression.Sheet = _formulaSelectionFeature.SheetView.Worksheet;
                        }
                    }
                    list.Add(new SheetView.ColoredText(expression.Text, expression.Color));
                }
                return (IList<SheetView.ColoredText>)list;
            }

            public int GetCursorPositionEnd()
            {
                if (!_splited)
                {
                    return _cursorPositionEnd;
                }
                int num = 0;
                if (_header != null)
                {
                    foreach (SheetView.FormulaExpression expression in _header)
                    {
                        num += expression.Text.Length;
                    }
                }
                if (_middle != null)
                {
                    foreach (SheetView.FormulaExpression expression2 in _middle)
                    {
                        num += expression2.Text.Length;
                    }
                }
                return num;
            }

            public int GetCursorPositionStart()
            {
                if (!_splited)
                {
                    return _cursorPositionStart;
                }
                int num = 0;
                if (_header != null)
                {
                    foreach (SheetView.FormulaExpression expression in _header)
                    {
                        num += expression.Text.Length;
                    }
                }
                if (_middle != null)
                {
                    foreach (SheetView.FormulaExpression expression2 in _middle)
                    {
                        num += expression2.Text.Length;
                    }
                }
                return num;
            }

            IList<SheetView.FormulaExpression> GetMergedExpressionList()
            {
                List<SheetView.FormulaExpression> list = new List<SheetView.FormulaExpression>();
                if (!_splited)
                {
                    if (_formulaExpressions != null)
                    {
                        list.AddRange((IEnumerable<SheetView.FormulaExpression>)_formulaExpressions);
                    }
                }
                else
                {
                    if (_header != null)
                    {
                        list.AddRange((IEnumerable<SheetView.FormulaExpression>)_header);
                    }
                    if ((((_header != null) && (_header.Count > 0)) && ((_middle != null) && (_middle.Count > 0))) && ((_header[_header.Count - 1].Range != null) && (_middle[0].Range != null)))
                    {
                        char ch = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                        list.Add(new SheetView.FormulaExpression(this, ((char)ch).ToString()));
                    }
                    if (_middle != null)
                    {
                        list.AddRange((IEnumerable<SheetView.FormulaExpression>)_middle);
                    }
                    if ((((_middle != null) && (_middle.Count > 0)) && ((_footer != null) && (_footer.Count > 0))) && ((_middle[_middle.Count - 1].Range != null) && (_footer[0].Range != null)))
                    {
                        char ch2 = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                        list.Add(new SheetView.FormulaExpression(this, ((char)ch2).ToString()));
                    }
                    if (_footer != null)
                    {
                        list.AddRange((IEnumerable<SheetView.FormulaExpression>)_footer);
                    }
                }
                return (IList<SheetView.FormulaExpression>)list;
            }

            public string GetText()
            {
                StringBuilder builder = new StringBuilder();
                foreach (SheetView.ColoredText text in GetColoredText(false))
                {
                    builder.Append(text.Text);
                }
                return builder.ToString();
            }

            Windows.UI.Color NewColor()
            {
                _colorIndex = (_colorIndex + 1) % 7;
                return _colors[_colorIndex];
            }

            public void OnCursorPositionChanged(int start, int end)
            {
                _cursorPositionStart = start;
                _cursorPositionEnd = end;
                if (_splited)
                {
                    _formulaExpressions = GetMergedExpressionList();
                    _splited = false;
                    using (IEnumerator<FormulaSelectionItem> enumerator = _formulaSelectionFeature.Items.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            enumerator.Current.IsFlickering = false;
                        }
                    }
                }
                if (CanSelectFormulaByUI(_formulaExpressions, start, end))
                {
                    _formulaSelectionFeature.CanSelectFormula = true;
                }
                else
                {
                    _formulaSelectionFeature.CanSelectFormula = false;
                }
            }

            internal void OnFormulaChangedByUI()
            {
                EventHandler formulaChangedByUI = FormulaChangedByUI;
                if (formulaChangedByUI != null)
                {
                    formulaChangedByUI(this, EventArgs.Empty);
                }
            }

            void OnFormulaSelectionFeatureItemAdded(object sender, FormulaSelectionItemEventArgs e)
            {
                if (!_splited)
                {
                    _splited = true;
                    _header = (IList<SheetView.FormulaExpression>)new List<SheetView.FormulaExpression>();
                    _middle = (IList<SheetView.FormulaExpression>)new List<SheetView.FormulaExpression>();
                    _footer = (IList<SheetView.FormulaExpression>)new List<SheetView.FormulaExpression>();
                    if (_formulaExpressions != null)
                    {
                        int num = 0;
                        foreach (SheetView.FormulaExpression expression in _formulaExpressions)
                        {
                            if ((num + expression.Text.Length) <= _cursorPositionStart)
                            {
                                _header.Add(expression);
                            }
                            else if (num >= _cursorPositionEnd)
                            {
                                _footer.Add(expression);
                            }
                            else if (_cursorPositionStart == _cursorPositionEnd)
                            {
                                _middle.Add(expression);
                            }
                            else
                            {
                                _formulaSelectionFeature.Items.Remove(expression.SelectionItem);
                            }
                            num += expression.Text.Length;
                        }
                    }
                }
                SheetView.FormulaExpression expression2 = new SheetView.FormulaExpression(this, e.Item.Range, string.Empty, false, null)
                {
                    SelectionItem = e.Item
                };
                if (_formulaSelectionFeature.SheetView.Worksheet != _formulaSelectionFeature.SheetView.EditorInfo.Sheet)
                {
                    expression2.Sheet = _formulaSelectionFeature.SheetView.Worksheet;
                }
                if (_middle.Count > 0)
                {
                    char ch = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                    if (_middle[_middle.Count - 1].Text != ((char)ch).ToString())
                    {
                        char ch2 = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                        _middle.Add(new SheetView.FormulaExpression(this, ((char)ch2).ToString()));
                    }
                    expression2.StartRowRelative = _middle[0].StartRowRelative;
                    expression2.StartColumnRelative = _middle[0].StartColumnRelative;
                    expression2.EndRowRelative = _middle[0].EndRowRelative;
                    expression2.EndColumnRelative = _middle[0].EndColumnRelative;
                }
                else if ((Editor != null) && Editor.IsAbsolute)
                {
                    expression2.StartRowRelative = false;
                    expression2.StartColumnRelative = false;
                    expression2.EndRowRelative = false;
                    expression2.EndColumnRelative = false;
                }
                expression2.UpdateText();
                _middle.Add(expression2);
                UpdateColors();
                OnFormulaChangedByUI();
            }

            void OnFormulaSelectionFeatureItemRemoved(object sender, FormulaSelectionItemEventArgs e)
            {
                List<SheetView.FormulaExpression> list = new List<SheetView.FormulaExpression>();
                for (int i = 0; i < _middle.Count; i++)
                {
                    SheetView.FormulaExpression expression = _middle[i];
                    if (expression.SelectionItem == e.Item)
                    {
                        list.Add(expression);
                        if (_middle.Count > (i + 1))
                        {
                            char ch = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                            if (_middle[i + 1].Text == ((char)ch).ToString())
                            {
                                list.Add(_middle[i + 1]);
                            }
                        }
                        break;
                    }
                }
                foreach (SheetView.FormulaExpression expression2 in list)
                {
                    _middle.Remove(expression2);
                }
                UpdateColors();
                OnFormulaChangedByUI();
            }

            public void OnFormulaTextChanged(string formulaText)
            {
                if (formulaText == null)
                {
                    formulaText = string.Empty;
                }
                _formulaExpressions = Parse(formulaText);
                _cursorPositionStart = _cursorPositionEnd = formulaText.Length;
                _splited = false;
                UpdateSelectionItemsForCurrentSheet();
                UpdateColors();
            }

            IList<SheetView.FormulaExpression> Parse(string text)
            {
                List<SheetView.FormulaExpression> list = new List<SheetView.FormulaExpression>();
                if (!string.IsNullOrEmpty(text))
                {
                    bool flag = _formulaSelectionFeature.SheetView.EditorInfo.Sheet.ReferenceStyle == ReferenceStyle.R1C1;
                    int activeRowIndex = _formulaSelectionFeature.SheetView.Worksheet.ActiveRowIndex;
                    int activeColumnIndex = _formulaSelectionFeature.SheetView.Worksheet.ActiveColumnIndex;
                    WorkbookParserContext context = new WorkbookParserContext(_formulaSelectionFeature.SheetView.Worksheet.Workbook, flag, activeRowIndex, activeColumnIndex, CultureInfo.CurrentCulture);
                    CalcParser parser = new CalcParser();
                    List<ExpressionInfo> list2 = new List<ExpressionInfo>();
                    try
                    {
                        list2 = parser.ParseReferenceExpressionInfos(text, context);
                    }
                    catch
                    {
                    }
                    if (list2.Count == 0)
                    {
                        Match match = new Regex(@"^(.*?\()(.*)(\))(\s*)$").Match(text);
                        if (match.Success)
                        {
                            for (int i = 1; i < 5; i++)
                            {
                                if (!string.IsNullOrEmpty(match.Groups[i].Value))
                                {
                                    list.Add(new SheetView.FormulaExpression(this, match.Groups[i].Value));
                                }
                            }
                        }
                        else
                        {
                            list.Add(new SheetView.FormulaExpression(this, text));
                        }
                    }
                    else
                    {
                        int startIndex = 0;
                        foreach (ExpressionInfo info in list2)
                        {
                            if (info.StartIndex > startIndex)
                            {
                                string str = text.Substring(startIndex, info.StartIndex - startIndex);
                                foreach (string str2 in Split(str))
                                {
                                    list.Add(new SheetView.FormulaExpression(this, str2));
                                }
                            }
                            startIndex = info.EndIndex + 1;
                            string expression = text.Substring(info.StartIndex, (info.EndIndex - info.StartIndex) + 1);
                            foreach (string str4 in Split(expression))
                            {
                                if (string.IsNullOrEmpty(str4))
                                {
                                    char ch = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                                    if (str4 != ((char)ch).ToString())
                                    {
                                        list.Add(new SheetView.FormulaExpression(this, str4));
                                        continue;
                                    }
                                }
                                list.Add(CreateFormulaExpression(info.Expression, str4, activeRowIndex, activeColumnIndex));
                            }
                        }
                        if (startIndex < text.Length)
                        {
                            string str5 = text.Substring(startIndex, text.Length - startIndex);
                            foreach (string str6 in Split(str5))
                            {
                                list.Add(new SheetView.FormulaExpression(this, str6));
                            }
                        }
                    }
                }
                return (IList<SheetView.FormulaExpression>)list;
            }

            internal string RangeToFormula(Worksheet worksheet, CellRange range, bool startRowRelative = true, bool startColumnRelative = true, bool endRowRelative = true, bool endColumnRelative = true)
            {
                if (worksheet == null)
                {
                    worksheet = _formulaSelectionFeature.SheetView.EditorInfo.Sheet;
                }
                bool flag = false;
                if ((range.RowCount == 1) && (range.ColumnCount == 1))
                {
                    flag = true;
                }
                else
                {
                    foreach (object obj2 in worksheet.SpanModel)
                    {
                        if (range.Equals(obj2))
                        {
                            flag = true;
                            break;
                        }
                    }
                }
                int baseRow = 0;
                int baseColumn = 0;
                if (startRowRelative || endRowRelative)
                {
                    baseRow = worksheet.ActiveRowIndex;
                }
                if (startColumnRelative || endColumnRelative)
                {
                    baseColumn = worksheet.ActiveColumnIndex;
                }
                if (flag)
                {
                    CalcCellExpression expression = new CalcCellExpression(range.Row - baseRow, range.Column - baseColumn, startRowRelative, startColumnRelative);
                    return ((ICalcEvaluator)_formulaSelectionFeature.SheetView.EditorInfo.Sheet).Expression2Formula(expression, baseRow, baseColumn);
                }
                CalcRangeExpression expression2 = CreateRangeExpressionByCount(range.Row - baseRow, range.Column - baseColumn, range.RowCount, range.ColumnCount, startRowRelative, startColumnRelative, endRowRelative, endColumnRelative);
                return ((ICalcEvaluator)_formulaSelectionFeature.SheetView.EditorInfo.Sheet).Expression2Formula(expression2, baseRow, baseColumn);
            }

            void ResetColor()
            {
                _colorIndex = -1;
            }

            IList<string> Split(string expression)
            {
                if (string.IsNullOrEmpty(expression))
                {
                    return (IList<string>)new List<string>();
                }
                List<string> list = new List<string>();
                int length = -1;
                int num2 = -1;
                for (int i = 0; i < expression.Length; i++)
                {
                    if (expression[i] != ' ')
                    {
                        length = i;
                        break;
                    }
                }
                for (int j = expression.Length - 1; j >= 0; j--)
                {
                    if (expression[j] != ' ')
                    {
                        num2 = j;
                        break;
                    }
                }
                if (length == -1)
                {
                    list.Add(expression);
                    return (IList<string>)list;
                }
                if (length != 0)
                {
                    list.Add(expression.Substring(0, length));
                }
                char ch = CultureInfo.CurrentCulture.TextInfo.ListSeparator[0];
                if (expression.Substring(0, 1) == ((char)ch).ToString())
                {
                    list.Add(expression.Substring(0, 1));
                    length++;
                }
                if (num2 >= length)
                {
                    list.Add(expression.Substring(length, (num2 - length) + 1));
                }
                if (num2 != (expression.Length - 1))
                {
                    list.Add(expression.Substring(num2 + 1, (expression.Length - num2) - 1));
                }
                return (IList<string>)list;
            }

            internal void UpdateColors()
            {
                ResetColor();
                Dictionary<CellRange, Windows.UI.Color> dictionary = new Dictionary<CellRange, Windows.UI.Color>();
                foreach (SheetView.FormulaExpression expression in GetMergedExpressionList())
                {
                    if (expression.Range != null)
                    {
                        Windows.UI.Color color;
                        if (!dictionary.TryGetValue(expression.Range, out color))
                        {
                            color = NewColor();
                            dictionary.Add(expression.Range, color);
                        }
                        expression.Color = color;
                    }
                }
            }

            internal void UpdateCursorPosition(SheetView.FormulaExpression expression)
            {
                int num = 0;
                foreach (SheetView.FormulaExpression expression2 in GetMergedExpressionList())
                {
                    num += expression2.Text.Length;
                    if (expression2 == expression)
                    {
                        _cursorPositionStart = _cursorPositionEnd = num;
                        break;
                    }
                }
            }

            internal void UpdateSelectionItemsForCurrentSheet()
            {
                _formulaSelectionFeature.Items.Clear();
                foreach (SheetView.FormulaExpression expression in GetMergedExpressionList())
                {
                    if ((expression.Range != null) && (((expression.Sheet == null) && !IsInOtherSheet) || (expression.Sheet == _formulaSelectionFeature.SheetView.Worksheet.Workbook.ActiveSheet)))
                    {
                        if (expression.SelectionItem == null)
                        {
                            expression.SelectionItem = new FormulaSelectionItem(expression.Range, false);
                        }
                        _formulaSelectionFeature.Items.Add(expression.SelectionItem);
                    }
                }
            }

            internal bool ActivateEditor
            {
                get { return _activateEditor; }
                set { _activateEditor = value; }
            }

            internal int ColumnIndex
            {
                get { return _columnIndex; }
                set { _columnIndex = value; }
            }

            internal IFormulaEditor Editor
            {
                get { return _editor; }
                set { _editor = value; }
            }

            public bool IsFormulaSelectionBegined
            {
                get { return _formulaSelectionFeature.IsSelectionBegined; }
            }

            internal bool IsInOtherSheet
            {
                get { return _formulaSelectionFeature.IsInOtherSheet; }
                set { _formulaSelectionFeature.IsInOtherSheet = value; }
            }

            public bool IsRelative { get; set; }

            internal int RowIndex
            {
                get { return _rowIndex; }
                set { _rowIndex = value; }
            }

            internal int SheetIndex
            {
                get { return _sheetIndex; }
                set { _sheetIndex = value; }
            }

            class WorkbookParserContext : CalcParserContext
            {
                Workbook _context;

                public WorkbookParserContext(Workbook context, bool useR1C1 = false, int baseRowIndex = 0, int baseColumnIndex = 0, CultureInfo culture = null)
                    : base(useR1C1, baseRowIndex, baseColumnIndex, culture)
                {
                    _context = context;
                }

                public override ICalcSource GetExternalSource(string workbookName, string worksheetName)
                {
                    if (_context != null)
                    {
                        return _context.Sheets[worksheetName];
                    }
                    return base.GetExternalSource(workbookName, worksheetName);
                }

                public override string GetExternalSourceToken(ICalcSource source)
                {
                    Worksheet worksheet = source as Worksheet;
                    if (worksheet != null)
                    {
                        return worksheet.Name;
                    }
                    return base.GetExternalSourceToken(source);
                }
            }
        }

        internal class FormulaExpression
        {
            Windows.UI.Color _color;
            bool _endColumnRelative;
            bool _endRowRelative;
            SheetView.FormulaEditorConnector _formulaEditorConnector;
            FormulaSelectionItem _formulaSelectionItem;
            bool _isNameExpression;
            CellRange _range;
            Worksheet _sheet;
            bool _startColumnRelative;
            bool _startRowRelative;
            string _text;

            public FormulaExpression(SheetView.FormulaEditorConnector connector, string text)
            {
                _color = Colors.Black;
                _startRowRelative = true;
                _startColumnRelative = true;
                _endRowRelative = true;
                _endColumnRelative = true;
                _formulaEditorConnector = connector;
                _text = text;
            }

            public FormulaExpression(SheetView.FormulaEditorConnector connector, CellRange range, string oldText, bool isNameExpression = false, Worksheet sheet = null)
            {
                _color = Colors.Black;
                _startRowRelative = true;
                _startColumnRelative = true;
                _endRowRelative = true;
                _endColumnRelative = true;
                _formulaEditorConnector = connector;
                _range = range;
                _text = oldText;
                _isNameExpression = isNameExpression;
                _sheet = sheet;
            }

            void OnFormulaSelectionItemPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "Range")
                {
                    _range = _formulaSelectionItem.Range;
                    _text = _formulaEditorConnector.FindNameRange(_range);
                    IsNameExpression = !string.IsNullOrEmpty(_text);
                    UpdateText();
                    _formulaEditorConnector.UpdateColors();
                    _formulaEditorConnector.OnFormulaChangedByUI();
                    _formulaEditorConnector.UpdateCursorPosition(this);
                }
            }

            public void UpdateText()
            {
                if ((_range != null) && !_isNameExpression)
                {
                    _text = _formulaEditorConnector.RangeToFormula(_sheet, _range, StartRowRelative, StartColumnRelative, EndRowRelative, EndColumnRelative);
                    if ((_sheet != null) && !string.IsNullOrEmpty(_sheet.Name))
                    {
                        bool flag = false;
                        for (int i = 0; i < _sheet.Name.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(_sheet.Name.Substring(i, 1)))
                            {
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            _text = "'" + _sheet.Name + "'!" + _text;
                        }
                        else
                        {
                            _text = _sheet.Name + "!" + _text;
                        }
                    }
                }
            }

            public Windows.UI.Color Color
            {
                get { return _color; }
                set
                {
                    if (_color != value)
                    {
                        _color = value;
                        if (_formulaSelectionItem != null)
                        {
                            _formulaSelectionItem.Color = _color;
                        }
                    }
                }
            }

            public bool EndColumnRelative
            {
                get { return _endColumnRelative; }
                set { _endColumnRelative = value; }
            }

            public bool EndRowRelative
            {
                get { return _endRowRelative; }
                set { _endRowRelative = value; }
            }

            public bool IsNameExpression
            {
                get { return _isNameExpression; }
                set
                {
                    if (_isNameExpression != value)
                    {
                        _isNameExpression = value;
                        SelectionItem.CanChangeBoundsByUI = !value;
                    }
                }
            }

            public CellRange Range
            {
                get { return _range; }
            }

            public FormulaSelectionItem SelectionItem
            {
                get { return _formulaSelectionItem; }
                set
                {
                    if (_formulaSelectionItem != value)
                    {
                        if (_formulaSelectionItem != null)
                        {
                            _formulaSelectionItem.Expression = null;
                            _formulaSelectionItem.PropertyChanged -= new PropertyChangedEventHandler(OnFormulaSelectionItemPropertyChanged);
                        }
                        _formulaSelectionItem = value;
                        if (_formulaSelectionItem != null)
                        {
                            _formulaSelectionItem.CanChangeBoundsByUI = !_isNameExpression;
                            _formulaSelectionItem.Range = Range;
                            _formulaSelectionItem.Color = Color;
                            _formulaSelectionItem.Expression = this;
                            _formulaSelectionItem.PropertyChanged += new PropertyChangedEventHandler(OnFormulaSelectionItemPropertyChanged);
                        }
                    }
                }
            }

            public Worksheet Sheet
            {
                get { return _sheet; }
                set
                {
                    if (_sheet != value)
                    {
                        _sheet = value;
                        UpdateText();
                    }
                }
            }

            public bool StartColumnRelative
            {
                get { return _startColumnRelative; }
                set { _startColumnRelative = value; }
            }

            public bool StartRowRelative
            {
                get { return _startRowRelative; }
                set { _startRowRelative = value; }
            }

            public string Text
            {
                get { return _text; }
            }
        }

        internal class FormulaSelectionFeature
        {
            int _activeColumnViewportIndex;
            int _activeRowViewportIndex;
            int _anchorColumn = -1;
            int _anchorRow = -1;
            bool _canSelectFormula;
            Dt.Cells.UI.SheetView.EditorManager _editorManager;
            bool _forceSelection;
            Dt.Cells.UI.SheetView.FormulaEditorConnector _formulaEditorConnector;
            bool _isDragDropping;
            bool _isDragResizing;
            bool _isInOtherSheet;
            bool _isSelectingCells;
            bool _isSelectingColumns;
            bool _isSelectingRows;
            bool _isSelectionBegined;
            ObservableCollection<FormulaSelectionItem> _items;
            FormulaSelectionItem _lastHitItem;
            Dt.Cells.UI.SheetView.SpreadXFormulaNavigation _navigation;
            int _resizingAnchorColumn;
            int _resizingAnchorRow;
            Dt.Cells.UI.SheetView.SpreadXFormulaSelection _selection;
            Dt.Cells.UI.SheetView _sheetView;

            public event EventHandler<FormulaSelectionItemEventArgs> ItemAdded;

            public event EventHandler<FormulaSelectionItemEventArgs> ItemRemoved;

            public FormulaSelectionFeature(Dt.Cells.UI.SheetView sheetView)
            {
                _sheetView = sheetView;
                _items = new ObservableCollection<FormulaSelectionItem>();
                _items.CollectionChanged += OnItemsCollectionChanged;
                _editorManager = new Dt.Cells.UI.SheetView.EditorManager(this);
            }

            public void AddSelection(int row, int column, int rowCount, int columnCount, bool clearFlickingItems = false)
            {
                if (clearFlickingItems)
                {
                    ClearFlickingSelection();
                }
                CellRange cellRange = new CellRange(row, column, rowCount, columnCount);
                cellRange = InflateRange(cellRange);
                _anchorColumn = cellRange.Column;
                if (_anchorColumn < 0)
                {
                    _anchorColumn = 0;
                }
                _anchorRow = cellRange.Row;
                if (_anchorRow < 0)
                {
                    _anchorRow = 0;
                }
                FormulaSelectionItem item = new FormulaSelectionItem(cellRange.Row, cellRange.Column, cellRange.RowCount, cellRange.ColumnCount, true);
                Items.Add(item);
                EventHandler<FormulaSelectionItemEventArgs> itemAdded = ItemAdded;
                if (itemAdded != null)
                {
                    itemAdded(this, new FormulaSelectionItemEventArgs(item));
                }
            }

            internal void BeginFormulaSelection(object editor)
            {
                IsSelectionBegined = true;
                IFormulaEditor editor2 = editor as IFormulaEditor;
                if (editor2 != null)
                {
                    FormulaEditorConnector.Editor = editor2;
                }
            }

            public void ChangeLastSelection(CellRange cellRange, bool changeAnchor = true)
            {
                if (Items.Count == 0)
                {
                    AddSelection(cellRange.Row, cellRange.Column, cellRange.RowCount, cellRange.ColumnCount, false);
                }
                else
                {
                    FormulaSelectionItem item = Enumerable.LastOrDefault<FormulaSelectionItem>((IEnumerable<FormulaSelectionItem>)Items);
                    if (item != null)
                    {
                        item.Range = cellRange;
                        if (changeAnchor)
                        {
                            _anchorColumn = cellRange.Column;
                            if (_anchorColumn < 0)
                            {
                                _anchorColumn = 0;
                            }
                            _anchorRow = cellRange.Row;
                            if (_anchorRow < 0)
                            {
                                _anchorRow = 0;
                            }
                        }
                    }
                }
            }

            public void ClearFlickingSelection()
            {
                List<FormulaSelectionItem> list = new List<FormulaSelectionItem>();
                foreach (FormulaSelectionItem item in Items)
                {
                    if (item.IsFlickering)
                    {
                        list.Add(item);
                    }
                }
                foreach (FormulaSelectionItem item2 in list)
                {
                    Items.Remove(item2);
                    EventHandler<FormulaSelectionItemEventArgs> itemRemoved = ItemRemoved;
                    if (itemRemoved != null)
                    {
                        itemRemoved(this, new FormulaSelectionItemEventArgs(item2));
                    }
                }
            }

            void ContinueCellSelecting()
            {
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                ColumnLayout viewportColumnLayoutNearX = _sheetView.GetViewportColumnLayoutNearX(activeColumnViewportIndex, _sheetView.MousePosition.X);
                RowLayout viewportRowLayoutNearY = _sheetView.GetViewportRowLayoutNearY(activeRowViewportIndex, _sheetView.MousePosition.Y);
                CellLayout layout3 = _sheetView.GetViewportCellLayoutModel(activeRowViewportIndex, activeColumnViewportIndex).FindPoint(_sheetView.MousePosition.X, _sheetView.MousePosition.Y);
                if (layout3 != null)
                {
                    ExtendSelection(layout3.Row, layout3.Column);
                }
                else if ((viewportColumnLayoutNearX != null) && (viewportRowLayoutNearY != null))
                {
                    ExtendSelection(viewportRowLayoutNearY.Row, viewportColumnLayoutNearX.Column);
                }
                _sheetView.ProcessScrollTimer();
            }

            void ContinueColumnSelecting()
            {
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                ColumnLayout viewportColumnLayoutNearX = _sheetView.GetViewportColumnLayoutNearX(activeColumnViewportIndex, _sheetView.MousePosition.X);
                if (viewportColumnLayoutNearX != null)
                {
                    ExtendSelection(-1, viewportColumnLayoutNearX.Column);
                    _sheetView.ProcessScrollTimer();
                }
            }

            void ContinueDragDropping()
            {
                _sheetView.UpdateDragToViewports();
                _sheetView.UpdateDragToCoordicates();
                if ((_sheetView._dragToRow >= 0) || (_sheetView._dragToColumn >= 0))
                {
                    _sheetView.UpdateMouseCursorLocation();
                    UpdateSelection();
                    _sheetView.ProcessScrollTimer();
                }
            }

            internal void ContinueDragging()
            {
                if (IsSelecting)
                {
                    ContinueSelecting();
                }
                else if (_isDragDropping)
                {
                    ContinueDragDropping();
                }
                else if (_isDragResizing)
                {
                    ContinueDragResizing();
                }
            }

            void ContinueDragResizing()
            {
                _sheetView.UpdateDragToViewports();
                _sheetView.UpdateDragToCoordicates();
                if ((_sheetView._dragToRow >= 0) || (_sheetView._dragToColumn >= 0))
                {
                    _sheetView.UpdateMouseCursorLocation();
                    UpdateSelectionForResize();
                    _sheetView.ProcessScrollTimer();
                }
            }

            void ContinueRowSelecting()
            {
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                RowLayout viewportRowLayoutNearY = _sheetView.GetViewportRowLayoutNearY(activeRowViewportIndex, _sheetView.MousePosition.Y);
                if (viewportRowLayoutNearY != null)
                {
                    ExtendSelection(viewportRowLayoutNearY.Row, -1);
                    _sheetView.ProcessScrollTimer();
                }
            }

            void ContinueSelecting()
            {
                if ((_sheetView.IsWorking && IsSelecting) && (_sheetView.MousePosition != _sheetView._lastClickPoint))
                {
                    if (_isSelectingCells)
                    {
                        ContinueCellSelecting();
                    }
                    else if (_isSelectingRows)
                    {
                        ContinueRowSelecting();
                    }
                    else if (_isSelectingColumns)
                    {
                        ContinueColumnSelecting();
                    }
                }
            }

            void EndDragDropping()
            {
                _sheetView.HideMouseCursor();
                _isDragDropping = false;
                _sheetView.StopScrollTimer();
            }

            internal void EndDragging()
            {
                if (IsSelecting)
                {
                    EndSelecting();
                }
                else if (_isDragDropping)
                {
                    EndDragDropping();
                }
                else if (_isDragResizing)
                {
                    EndDragResizing();
                }
            }

            void EndDragResizing()
            {
                _sheetView.HideMouseCursor();
                _isDragResizing = false;
                _sheetView.StopScrollTimer();
                using (IEnumerator<FormulaSelectionItem> enumerator = Items.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        enumerator.Current.IsResizing = false;
                    }
                }
            }

            internal void EndFormulaSelection()
            {
                if (!IsInOtherSheet)
                {
                    _anchorRow = -1;
                    _anchorColumn = -1;
                    IsSelectionBegined = false;
                    _canSelectFormula = false;
                    _items.Clear();
                }
            }

            void EndSelecting()
            {
                _sheetView.IsWorking = false;
                _isSelectingCells = _isSelectingRows = _isSelectingColumns = false;
                _sheetView.StopScrollTimer();
            }

            void ExtendSelection(int row, int column)
            {
                FormulaSelectionItem item = Enumerable.LastOrDefault<FormulaSelectionItem>((IEnumerable<FormulaSelectionItem>)Items);
                if (item != null)
                {
                    int num = Math.Min(_anchorRow, row);
                    int num2 = Math.Min(_anchorColumn, column);
                    int num3 = Math.Max(_anchorRow, row);
                    int num4 = Math.Max(_anchorColumn, column);
                    CellRange cellRange = new CellRange(num, num2, (num3 - num) + 1, (num4 - num2) + 1);
                    cellRange = InflateRange(cellRange);
                    item.Range = cellRange;
                }
            }

            internal bool HitTest(int rowViewportIndex, int columnViewportIndex, double mouseX, double mouseY, HitTestInformation hi)
            {
                Worksheet worksheet = _sheetView.Worksheet;
                if (worksheet == null)
                {
                    return false;
                }
                if (Items.Count == 0)
                {
                    return false;
                }
                FormulaSelectionItem item = null;
                for (int i = 0; i < Items.Count; i++)
                {
                    FormulaSelectionItem item2 = Items[i];
                    if (!item2.CanChangeBoundsByUI)
                    {
                        continue;
                    }
                    int row = item2.Range.Row;
                    int column = item2.Range.Column;
                    int rowCount = item2.Range.RowCount;
                    int columnCount = item2.Range.ColumnCount;
                    if ((row == -1) && (column == -1))
                    {
                        continue;
                    }
                    if (row == -1)
                    {
                        row = 0;
                        rowCount = worksheet.RowCount;
                    }
                    if (column == -1)
                    {
                        column = 0;
                        columnCount = worksheet.ColumnCount;
                    }
                    SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                    RowLayout layout2 = _sheetView.GetViewportRowLayoutModel(rowViewportIndex).Find(row);
                    RowLayout layout3 = _sheetView.GetViewportRowLayoutModel(rowViewportIndex).Find((row + rowCount) - 1);
                    ColumnLayout layout4 = _sheetView.GetViewportColumnLayoutModel(columnViewportIndex).Find(column);
                    ColumnLayout layout5 = _sheetView.GetViewportColumnLayoutModel(columnViewportIndex).Find((column + columnCount) - 1);
                    if ((((rowCount < worksheet.RowCount) && (layout2 == null)) && (layout3 == null)) || (((columnCount < worksheet.ColumnCount) && (layout4 == null)) && (layout5 == null)))
                    {
                        continue;
                    }
                    double num6 = Math.Ceiling((layout4 == null) ? sheetLayout.GetViewportX(columnViewportIndex) : layout4.X);
                    double num7 = Math.Ceiling((layout5 == null) ? ((double)((sheetLayout.GetViewportX(columnViewportIndex) + sheetLayout.GetViewportWidth(columnViewportIndex)) - 1.0)) : ((double)((layout5.X + layout5.Width) - 1.0)));
                    double num8 = Math.Ceiling((layout2 == null) ? sheetLayout.GetViewportY(rowViewportIndex) : layout2.Y);
                    double num9 = Math.Ceiling((layout3 == null) ? ((double)((sheetLayout.GetViewportY(rowViewportIndex) + sheetLayout.GetViewportHeight(rowViewportIndex)) - 1.0)) : ((double)((layout3.Y + layout3.Height) - 1.0)));
                    double num10 = 3.0;
                    double num11 = 3.0;
                    if ((mouseY >= (num8 - 3.0)) && (mouseY <= (num8 + 3.0)))
                    {
                        if ((mouseX >= (num6 - 3.0)) && (mouseX <= (num6 + 3.0)))
                        {
                            ViewportFormulaSelectionHitTestInformation information = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.LeftTop
                            };
                            hi.FormulaSelectionInfo = information;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                        }
                        else
                        {
                            if ((mouseX < (num7 - 3.0)) || (mouseX > (num7 + 3.0)))
                            {
                                goto Label_0391;
                            }
                            ViewportFormulaSelectionHitTestInformation information2 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.RightTop
                            };
                            hi.FormulaSelectionInfo = information2;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                        }
                        break;
                    }
                    if ((mouseY >= (num9 - 3.0)) && (mouseY <= (num9 + 3.0)))
                    {
                        if ((mouseX >= (num6 - 3.0)) && (mouseX <= (num6 + 3.0)))
                        {
                            ViewportFormulaSelectionHitTestInformation information3 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.LeftBottom
                            };
                            hi.FormulaSelectionInfo = information3;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                        if ((mouseX >= (num7 - 3.0)) && (mouseX <= (num7 + 3.0)))
                        {
                            ViewportFormulaSelectionHitTestInformation information4 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.RightBottom
                            };
                            hi.FormulaSelectionInfo = information4;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                    }
                Label_0391:
                    if ((mouseY >= (num8 - num10)) && (mouseY <= (num9 + num11)))
                    {
                        if (((layout4 != null) && (mouseX >= (num6 - num10))) && (mouseX <= (num6 + num11)))
                        {
                            ViewportFormulaSelectionHitTestInformation information5 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.Left
                            };
                            hi.FormulaSelectionInfo = information5;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                        if (((layout5 != null) && (mouseX >= (num7 - num10))) && (mouseX <= (num7 + num11)))
                        {
                            ViewportFormulaSelectionHitTestInformation information6 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.Right
                            };
                            hi.FormulaSelectionInfo = information6;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                    }
                    if ((mouseX >= (num6 - num10)) && (mouseX <= (num7 + num11)))
                    {
                        if (((layout2 != null) && (mouseY >= (num8 - num10))) && (mouseY <= (num8 + num11)))
                        {
                            ViewportFormulaSelectionHitTestInformation information7 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.Top
                            };
                            hi.FormulaSelectionInfo = information7;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                        if (((layout3 != null) && (mouseY >= (num9 - num10))) && (mouseY <= (num9 + num11)))
                        {
                            ViewportFormulaSelectionHitTestInformation information8 = new ViewportFormulaSelectionHitTestInformation
                            {
                                SelectionIndex = i,
                                Position = PositionInFormulaSelection.Bottom
                            };
                            hi.FormulaSelectionInfo = information8;
                            hi.HitTestType = HitTestType.FormulaSelection;
                            item = item2;
                            break;
                        }
                    }
                }
                if (_lastHitItem != item)
                {
                    if (_lastHitItem != null)
                    {
                        _lastHitItem.IsMouseOver = false;
                    }
                    _lastHitItem = item;
                    if ((_lastHitItem != null) && !_sheetView.IsWorking)
                    {
                        _lastHitItem.IsMouseOver = true;
                    }
                }
                return (_lastHitItem != null);
            }

            CellRange InflateRange(CellRange cellRange)
            {
                List<CellRange> list = new List<CellRange>();
                foreach (CellRange range in _sheetView.Worksheet.SpanModel)
                {
                    list.Add(range);
                }
                if (list.Count != 0)
                {
                    bool flag = false;
                    while (!flag)
                    {
                        flag = true;
                        for (int i = 0; i < list.Count; i++)
                        {
                            CellRange range2 = list[i];
                            if (cellRange.Intersects(range2.Row, range2.Column, range2.RowCount, range2.ColumnCount))
                            {
                                list.RemoveAt(i--);
                                cellRange = UnionCellRange(cellRange, range2);
                                flag = false;
                                continue;
                            }
                        }
                    }
                }
                return cellRange;
            }

            void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (_sheetView._viewportPresenters != null)
                {
                    GcViewport[,] viewportArray = _sheetView._viewportPresenters;
                    int upperBound = viewportArray.GetUpperBound(0);
                    int num2 = viewportArray.GetUpperBound(1);
                    for (int k = viewportArray.GetLowerBound(0); k <= upperBound; k++)
                    {
                        for (int i = viewportArray.GetLowerBound(1); i <= num2; i++)
                        {
                            GcViewport viewport = viewportArray[k, i];
                            if (viewport != null)
                            {
                                viewport.RefreshFormulaSelection();
                            }
                        }
                    }
                    _sheetView.RefreshFormulaSelectionGrippers();
                }
            }

            internal void SetCursor(ViewportFormulaSelectionHitTestInformation info)
            {
                if ((info.Position == PositionInFormulaSelection.LeftTop) || (info.Position == PositionInFormulaSelection.RightBottom))
                {
                    _sheetView.SetBuiltInCursor(CoreCursorType.SizeNorthwestSoutheast);
                }
                else if ((info.Position == PositionInFormulaSelection.LeftBottom) || (info.Position == PositionInFormulaSelection.RightTop))
                {
                    _sheetView.SetBuiltInCursor(CoreCursorType.SizeNortheastSouthwest);
                }
                else
                {
                    _sheetView.SetMouseCursor(CursorType.DragCell_DragCursor);
                }
            }

            void StartCellSelecting()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                int row = savedHitTestInformation.ViewportInfo.Row;
                int column = savedHitTestInformation.ViewportInfo.Column;
                int rowCount = 1;
                int columnCount = 1;
                if ((savedHitTestInformation.ViewportInfo.Row > -1) && (savedHitTestInformation.ViewportInfo.Column > -1))
                {
                    bool flag;
                    bool flag2;
                    CellLayout layout = _sheetView.GetViewportCellLayoutModel(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.ColumnViewportIndex).FindCell(savedHitTestInformation.ViewportInfo.Row, savedHitTestInformation.ViewportInfo.Column);
                    KeyboardHelper.GetMetaKeyState(out flag2, out flag);
                    if (layout != null)
                    {
                        row = layout.Row;
                        column = layout.Column;
                        rowCount = layout.RowCount;
                        columnCount = layout.ColumnCount;
                    }
                    _sheetView.IsWorking = true;
                    _isSelectingCells = true;
                    _sheetView.SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                    _sheetView.SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                    if (flag)
                    {
                        AddSelection(row, column, rowCount, columnCount, false);
                    }
                    else if (flag2)
                    {
                        ExtendSelection(row, column);
                    }
                    else
                    {
                        AddSelection(row, column, rowCount, columnCount, true);
                    }
                    if (!_sheetView.IsWorking)
                    {
                        EndSelecting();
                    }
                    _sheetView.StartScrollTimer();
                }
            }

            void StartColumnSelecting()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                if ((savedHitTestInformation.HitTestType == HitTestType.Empty) || (savedHitTestInformation.HeaderInfo == null))
                {
                    savedHitTestInformation = _sheetView.HitTest(_sheetView._touchStartPoint.X, _sheetView._touchStartPoint.Y);
                }
                if (savedHitTestInformation.HeaderInfo != null)
                {
                    SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                    _sheetView.GetViewportTopRow((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                    int column = savedHitTestInformation.HeaderInfo.Column;
                    _sheetView.IsWorking = true;
                    _isSelectingColumns = true;
                    _sheetView.SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                    _sheetView.SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                    if (savedHitTestInformation.HeaderInfo.Column > -1)
                    {
                        bool flag;
                        bool flag2;
                        KeyboardHelper.GetMetaKeyState(out flag2, out flag);
                        if (flag)
                        {
                            AddSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1, false);
                        }
                        else if (flag2)
                        {
                            ExtendSelection(-1, savedHitTestInformation.HeaderInfo.Column);
                        }
                        else
                        {
                            AddSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1, true);
                        }
                        if (!_sheetView.IsWorking)
                        {
                            EndSelecting();
                        }
                        _sheetView.StartScrollTimer();
                    }
                }
            }

            internal void StartDragDropping()
            {
                if (!_isDragDropping && (Items.Count != 0))
                {
                    _sheetView.IsWorking = true;
                    _isDragDropping = true;
                    HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                    FormulaSelectionItem item = Items[savedHitTestInformation.FormulaSelectionInfo.SelectionIndex];
                    _sheetView._rowOffset = Math.Max(0, Math.Min((int)(savedHitTestInformation.ViewportInfo.Row - item.Range.Row), (int)(item.Range.RowCount - 1)));
                    _sheetView._columnOffset = Math.Max(0, Math.Min((int)(savedHitTestInformation.ViewportInfo.Column - item.Range.Column), (int)(item.Range.ColumnCount - 1)));
                    _sheetView._dragStartRowViewport = savedHitTestInformation.RowViewportIndex;
                    _sheetView._dragStartColumnViewport = savedHitTestInformation.ColumnViewportIndex;
                    _sheetView._dragToRowViewport = savedHitTestInformation.RowViewportIndex;
                    _sheetView._dragToColumnViewport = savedHitTestInformation.ColumnViewportIndex;
                    using (IEnumerator<FormulaSelectionItem> enumerator = Items.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            enumerator.Current.IsFlickering = false;
                        }
                    }
                    CanSelectFormula = false;
                    _sheetView.StartScrollTimer();
                }
            }

            internal void StartDragResizing()
            {
                if (!_isDragResizing && (Items.Count != 0))
                {
                    _sheetView.IsWorking = true;
                    _isDragResizing = true;
                    HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                    _sheetView._dragStartRowViewport = savedHitTestInformation.RowViewportIndex;
                    _sheetView._dragStartColumnViewport = savedHitTestInformation.ColumnViewportIndex;
                    _sheetView._dragToRowViewport = savedHitTestInformation.RowViewportIndex;
                    _sheetView._dragToColumnViewport = savedHitTestInformation.ColumnViewportIndex;
                    FormulaSelectionItem item = Items[savedHitTestInformation.FormulaSelectionInfo.SelectionIndex];
                    item.IsResizing = true;
                    CellRange range = item.Range;
                    if (range.Row < 0)
                    {
                        range = new CellRange(0, range.Column, _sheetView.Worksheet.RowCount, range.ColumnCount);
                    }
                    if (range.Column < 0)
                    {
                        range = new CellRange(range.Row, 0, range.RowCount, _sheetView.Worksheet.ColumnCount);
                    }
                    switch (savedHitTestInformation.FormulaSelectionInfo.Position)
                    {
                        case PositionInFormulaSelection.LeftTop:
                            _resizingAnchorColumn = (range.Column + range.ColumnCount) - 1;
                            _resizingAnchorRow = (range.Row + range.RowCount) - 1;
                            break;

                        case PositionInFormulaSelection.RightTop:
                            _resizingAnchorColumn = range.Column;
                            _resizingAnchorRow = (range.Row + range.RowCount) - 1;
                            break;

                        case PositionInFormulaSelection.LeftBottom:
                            _resizingAnchorColumn = (range.Column + range.ColumnCount) - 1;
                            _resizingAnchorRow = range.Row;
                            break;

                        case PositionInFormulaSelection.RightBottom:
                            _resizingAnchorColumn = range.Column;
                            _resizingAnchorRow = range.Row;
                            break;
                    }
                    using (IEnumerator<FormulaSelectionItem> enumerator = Items.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            enumerator.Current.IsFlickering = false;
                        }
                    }
                    CanSelectFormula = false;
                    _sheetView.StartScrollTimer();
                }
            }

            void StartRowSelecting()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                int row = savedHitTestInformation.HeaderInfo.Row;
                _sheetView.GetViewportLeftColumn((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.IsWorking = true;
                _isSelectingRows = true;
                _sheetView.SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                if (savedHitTestInformation.HeaderInfo.Row > -1)
                {
                    bool flag;
                    bool flag2;
                    KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                    if (flag2)
                    {
                        AddSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1, false);
                    }
                    else if (flag)
                    {
                        ExtendSelection(savedHitTestInformation.HeaderInfo.Row, -1);
                    }
                    else
                    {
                        AddSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1, true);
                    }
                    if (!_sheetView.IsWorking)
                    {
                        EndSelecting();
                    }
                    _sheetView.StartScrollTimer();
                }
            }

            internal bool StartSelecting(SheetArea area)
            {
                if (CanSelectFormula)
                {
                    if (area == SheetArea.Cells)
                    {
                        StartCellSelecting();
                        return true;
                    }
                    if (area == (SheetArea.CornerHeader | SheetArea.RowHeader))
                    {
                        StartRowSelecting();
                        return true;
                    }
                    if (area == SheetArea.ColumnHeader)
                    {
                        StartColumnSelecting();
                        return true;
                    }
                    if (area == SheetArea.CornerHeader)
                    {
                        StartSheetSelecting();
                        return true;
                    }
                }
                else
                {
                    EndFormulaSelection();
                }
                return false;
            }

            void StartSheetSelecting()
            {
                bool flag;
                bool flag2;
                SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                _sheetView.SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                KeyboardHelper.GetMetaKeyState(out flag, out flag2);
                AddSelection(0, -1, _sheetView.Worksheet.RowCount, -1, !flag2);
            }

            internal bool TouchHitTest(double mouseX, double mouseY, HitTestInformation hi)
            {
                Worksheet worksheet = _sheetView.Worksheet;
                if (worksheet != null)
                {
                    if (Items.Count == 0)
                    {
                        return false;
                    }
                    for (int i = 0; i < Items.Count; i++)
                    {
                        FormulaSelectionItem item = Items[i];
                        if (item.CanChangeBoundsByUI)
                        {
                            int row = item.Range.Row;
                            int column = item.Range.Column;
                            int rowCount = item.Range.RowCount;
                            int columnCount = item.Range.ColumnCount;
                            if ((row != -1) || (column != -1))
                            {
                                if (row == -1)
                                {
                                    row = 0;
                                    rowCount = worksheet.RowCount;
                                }
                                if (column == -1)
                                {
                                    column = 0;
                                    columnCount = worksheet.ColumnCount;
                                }
                                SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                                RowLayout layout2 = _sheetView.GetViewportRowLayoutModel(activeRowViewportIndex).Find(row);
                                RowLayout layout3 = _sheetView.GetViewportRowLayoutModel(activeRowViewportIndex).Find((row + rowCount) - 1);
                                ColumnLayout layout4 = _sheetView.GetViewportColumnLayoutModel(activeColumnViewportIndex).Find(column);
                                ColumnLayout layout5 = _sheetView.GetViewportColumnLayoutModel(activeColumnViewportIndex).Find((column + columnCount) - 1);
                                if ((((rowCount >= worksheet.RowCount) || (layout2 != null)) || (layout3 != null)) && (((columnCount >= worksheet.ColumnCount) || (layout4 != null)) || (layout5 != null)))
                                {
                                    double num8 = Math.Ceiling((layout4 == null) ? sheetLayout.GetViewportX(activeColumnViewportIndex) : layout4.X);
                                    double num9 = Math.Ceiling((layout5 == null) ? ((double)((sheetLayout.GetViewportX(activeColumnViewportIndex) + sheetLayout.GetViewportWidth(activeColumnViewportIndex)) - 1.0)) : ((double)((layout5.X + layout5.Width) - 1.0)));
                                    double num10 = Math.Ceiling((layout2 == null) ? sheetLayout.GetViewportY(activeRowViewportIndex) : layout2.Y);
                                    double num11 = Math.Ceiling((layout3 == null) ? ((double)((sheetLayout.GetViewportY(activeRowViewportIndex) + sheetLayout.GetViewportHeight(activeRowViewportIndex)) - 1.0)) : ((double)((layout3.Y + layout3.Height) - 1.0)));
                                    if (((mouseX >= (num8 - 20.0)) && (mouseX <= (num8 + 20.0))) && ((mouseY >= (num10 - 20.0)) && (mouseY <= (num10 + 20.0))))
                                    {
                                        ViewportFormulaSelectionHitTestInformation information = new ViewportFormulaSelectionHitTestInformation
                                        {
                                            SelectionIndex = i,
                                            Position = PositionInFormulaSelection.LeftTop
                                        };
                                        hi.FormulaSelectionInfo = information;
                                        hi.HitTestType = HitTestType.FormulaSelection;
                                        hi.RowViewportIndex = activeRowViewportIndex;
                                        hi.ColumnViewportIndex = activeColumnViewportIndex;
                                        return true;
                                    }
                                    if (((mouseX >= (num9 - 20.0)) && (mouseX <= (num9 + 20.0))) && ((mouseY >= (num11 - 20.0)) && (mouseY <= (num11 + 20.0))))
                                    {
                                        ViewportFormulaSelectionHitTestInformation information2 = new ViewportFormulaSelectionHitTestInformation
                                        {
                                            SelectionIndex = i,
                                            Position = PositionInFormulaSelection.RightBottom
                                        };
                                        hi.FormulaSelectionInfo = information2;
                                        hi.HitTestType = HitTestType.FormulaSelection;
                                        hi.RowViewportIndex = activeRowViewportIndex;
                                        hi.ColumnViewportIndex = activeColumnViewportIndex;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                return false;
            }

            internal bool TouchSelect(SheetArea area)
            {
                bool flag = false;
                if (CanSelectFormula)
                {
                    if (area == SheetArea.Cells)
                    {
                        TouchSelectCell();
                        flag = true;
                    }
                    else if (area == (SheetArea.CornerHeader | SheetArea.RowHeader))
                    {
                        TouchSelectRow();
                        flag = true;
                    }
                    else if (area == SheetArea.ColumnHeader)
                    {
                        TouchSelectColumn();
                        flag = true;
                    }
                    else if (area == SheetArea.CornerHeader)
                    {
                        TouchSelectSheet();
                        flag = true;
                    }
                }
                else
                {
                    EndFormulaSelection();
                }
                IsTouching = true;
                if (flag)
                {
                    SheetView.ShowFormulaSelectionTouchGrippers();
                }
                return flag;
            }

            void TouchSelectCell()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                int row = savedHitTestInformation.ViewportInfo.Row;
                int column = savedHitTestInformation.ViewportInfo.Column;
                int rowCount = 1;
                int columnCount = 1;
                if ((savedHitTestInformation.ViewportInfo.Row > -1) && (savedHitTestInformation.ViewportInfo.Column > -1))
                {
                    bool flag;
                    bool flag2;
                    CellLayout layout = _sheetView.GetViewportCellLayoutModel(savedHitTestInformation.RowViewportIndex, savedHitTestInformation.ColumnViewportIndex).FindCell(savedHitTestInformation.ViewportInfo.Row, savedHitTestInformation.ViewportInfo.Column);
                    KeyboardHelper.GetMetaKeyState(out flag2, out flag);
                    if (layout != null)
                    {
                        row = layout.Row;
                        column = layout.Column;
                        rowCount = layout.RowCount;
                        columnCount = layout.ColumnCount;
                    }
                    _sheetView.SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                    _sheetView.SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                    AddSelection(row, column, rowCount, columnCount, true);
                }
            }

            void TouchSelectColumn()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                if ((savedHitTestInformation.HitTestType == HitTestType.Empty) || (savedHitTestInformation.HeaderInfo == null))
                {
                    savedHitTestInformation = _sheetView.HitTest(_sheetView._touchStartPoint.X, _sheetView._touchStartPoint.Y);
                }
                if (savedHitTestInformation.HeaderInfo != null)
                {
                    SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                    _sheetView.GetViewportTopRow((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                    int column = savedHitTestInformation.HeaderInfo.Column;
                    _sheetView.SetActiveColumnViewportIndex(savedHitTestInformation.ColumnViewportIndex);
                    _sheetView.SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                    if (savedHitTestInformation.HeaderInfo.Column > -1)
                    {
                        AddSelection(-1, savedHitTestInformation.HeaderInfo.Column, -1, 1, true);
                    }
                }
            }

            void TouchSelectRow()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                int row = savedHitTestInformation.HeaderInfo.Row;
                _sheetView.GetViewportLeftColumn((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.SetActiveRowViewportIndex(savedHitTestInformation.RowViewportIndex);
                if (savedHitTestInformation.HeaderInfo.Row > -1)
                {
                    AddSelection(savedHitTestInformation.HeaderInfo.Row, -1, 1, -1, true);
                }
            }

            void TouchSelectSheet()
            {
                SheetLayout sheetLayout = _sheetView.GetSheetLayout();
                _sheetView.SetActiveColumnViewportIndex((sheetLayout.FrozenWidth > 0.0) ? -1 : 0);
                _sheetView.SetActiveRowViewportIndex((sheetLayout.FrozenHeight > 0.0) ? -1 : 0);
                AddSelection(0, -1, _sheetView.Worksheet.RowCount, -1, true);
            }

            static CellRange UnionCellRange(CellRange range1, CellRange range2)
            {
                int row = Math.Min(range1.Row, range2.Row);
                int column = Math.Min(range1.Column, range2.Column);
                int num3 = Math.Max((int)((range1.Row + range1.RowCount) - 1), (int)((range2.Row + range2.RowCount) - 1));
                int num4 = Math.Max((int)((range1.Column + range1.ColumnCount) - 1), (int)((range2.Column + range2.ColumnCount) - 1));
                if ((row >= 0) && (column >= 0))
                {
                    return new CellRange(row, column, (num3 - row) + 1, (num4 - column) + 1);
                }
                if (row >= 0)
                {
                    return new CellRange(row, -1, (num3 - row) + 1, -1);
                }
                if (column >= 0)
                {
                    return new CellRange(-1, column, -1, (num4 - column) + 1);
                }
                return new CellRange(-1, -1, -1, -1);
            }

            void UpdateSelection()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                FormulaSelectionItem item = Items[savedHitTestInformation.FormulaSelectionInfo.SelectionIndex];
                CellRange range = item.Range;
                int row = _sheetView._dragToRow - _sheetView._rowOffset;
                if ((range.Row == -1) && (range.RowCount == -1))
                {
                    row = -1;
                }
                else if (row < 0)
                {
                    row = 0;
                }
                else if ((row + range.RowCount) > _sheetView.Worksheet.RowCount)
                {
                    row = _sheetView.Worksheet.RowCount - range.RowCount;
                }
                int column = _sheetView._dragToColumn - _sheetView._columnOffset;
                if ((range.Column == -1) && (range.ColumnCount == -1))
                {
                    column = -1;
                }
                else if (column < 0)
                {
                    column = 0;
                }
                else if ((column + range.ColumnCount) > _sheetView.Worksheet.ColumnCount)
                {
                    column = _sheetView.Worksheet.ColumnCount - range.ColumnCount;
                }
                range = new CellRange(row, column, range.RowCount, range.ColumnCount);
                item.Range = range;
            }

            void UpdateSelectionForResize()
            {
                HitTestInformation savedHitTestInformation = _sheetView.GetHitInfo();
                FormulaSelectionItem item = Items[savedHitTestInformation.FormulaSelectionInfo.SelectionIndex];
                int column = Math.Min(_sheetView._dragToColumn, _resizingAnchorColumn);
                int row = Math.Min(_sheetView._dragToRow, _resizingAnchorRow);
                int num3 = Math.Max(_sheetView._dragToColumn, _resizingAnchorColumn);
                int num4 = Math.Max(_sheetView._dragToRow, _resizingAnchorRow);
                CellRange range = new CellRange(row, column, (num4 - row) + 1, (num3 - column) + 1);
                if ((range.Column == 0) && (range.ColumnCount == _sheetView.Worksheet.ColumnCount))
                {
                    range = new CellRange(range.Row, -1, range.RowCount, -1);
                }
                else if ((range.Row == 0) && (range.RowCount == _sheetView.Worksheet.RowCount))
                {
                    range = new CellRange(-1, range.Column, -1, range.ColumnCount);
                }
                item.Range = range;
            }

            public int ActiveColumnViewportIndex
            {
                get { return _activeColumnViewportIndex; }
                set
                {
                    if (_activeColumnViewportIndex != value)
                    {
                        _activeColumnViewportIndex = value;
                    }
                }
            }

            public int ActiveRowViewportIndex
            {
                get { return _activeRowViewportIndex; }
                set
                {
                    if (_activeRowViewportIndex != value)
                    {
                        _activeRowViewportIndex = value;
                    }
                }
            }

            public int AnchorColumn
            {
                get
                {
                    if (((_anchorColumn == -1) && (_sheetView != null)) && (_sheetView.Worksheet != null))
                    {
                        return _sheetView.Worksheet.ActiveColumnIndex;
                    }
                    return _anchorColumn;
                }
            }

            public int AnchorRow
            {
                get
                {
                    if (((_anchorRow == -1) && (_sheetView != null)) && (_sheetView.Worksheet != null))
                    {
                        return _sheetView.Worksheet.ActiveRowIndex;
                    }
                    return _anchorRow;
                }
            }

            internal bool CanSelectFormula
            {
                get
                {
                    if (!_canSelectFormula)
                    {
                        return ForceSelection;
                    }
                    return true;
                }
                set
                {
                    if ((!IsInOtherSheet && IsSelectionBegined) && (_canSelectFormula != value))
                    {
                        _canSelectFormula = value;
                        if (!_canSelectFormula)
                        {
                            using (IEnumerator<FormulaSelectionItem> enumerator = Items.GetEnumerator())
                            {
                                while (enumerator.MoveNext())
                                {
                                    enumerator.Current.IsFlickering = false;
                                }
                            }
                        }
                    }
                }
            }

            public bool ForceSelection
            {
                get { return _forceSelection; }
                set { _forceSelection = value; }
            }

            public Dt.Cells.UI.SheetView.FormulaEditorConnector FormulaEditorConnector
            {
                get
                {
                    if (_formulaEditorConnector == null)
                    {
                        _formulaEditorConnector = new Dt.Cells.UI.SheetView.FormulaEditorConnector(this);
                    }
                    return _formulaEditorConnector;
                }
            }

            public bool IsDragging
            {
                get
                {
                    if (!IsSelecting && !_isDragDropping)
                    {
                        return _isDragResizing;
                    }
                    return true;
                }
            }

            public bool IsFlicking
            {
                get
                {
                    using (IEnumerator<FormulaSelectionItem> enumerator = _items.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.Current.IsFlickering)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }

            public bool IsInOtherSheet
            {
                get { return _isInOtherSheet; }
                set
                {
                    if (_isInOtherSheet != value)
                    {
                        _isInOtherSheet = value;
                    }
                }
            }

            bool IsSelecting
            {
                get
                {
                    if (!_isSelectingCells && !_isSelectingRows)
                    {
                        return _isSelectingColumns;
                    }
                    return true;
                }
            }

            public bool IsSelectionBegined
            {
                get
                {
                    if (!_isSelectionBegined)
                    {
                        return ForceSelection;
                    }
                    return true;
                }
                set { _isSelectionBegined = value; }
            }

            public static bool IsTouching { get; set; }

            public IList<FormulaSelectionItem> Items
            {
                get { return (IList<FormulaSelectionItem>)_items; }
            }

            public Dt.Cells.UI.SheetView.SpreadXFormulaNavigation Navigation
            {
                get
                {
                    if (_navigation == null)
                    {
                        _navigation = new Dt.Cells.UI.SheetView.SpreadXFormulaNavigation(this);
                    }
                    return _navigation;
                }
            }

            public Dt.Cells.UI.SheetView.SpreadXFormulaSelection Selection
            {
                get
                {
                    if (_selection == null)
                    {
                        _selection = new Dt.Cells.UI.SheetView.SpreadXFormulaSelection(this);
                    }
                    return _selection;
                }
            }

            public Dt.Cells.UI.SheetView SheetView
            {
                get { return _sheetView; }
            }
        }

        internal class GripperLocationsStruct
        {
            public Rect BottomRight { get; set; }

            public Rect TopLeft { get; set; }
        }

        internal class SpreadXFormulaNavigation
        {
            SheetView.FormulaSelectionFeature _formulaSelectionFeature;
            SheetView _sheetView;
            SheetView.SpreadXFormulaTabularNavigator _tabularNavigator;

            public SpreadXFormulaNavigation(SheetView.FormulaSelectionFeature formulaSelectionFeature)
            {
                _formulaSelectionFeature = formulaSelectionFeature;
                _sheetView = formulaSelectionFeature.SheetView;
                _tabularNavigator = new SheetView.SpreadXFormulaTabularNavigator(_sheetView);
            }

            bool MoveActiveCell(NavigationDirection direction)
            {
                if (_sheetView.Worksheet == null)
                {
                    return false;
                }
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                _sheetView.GetViewportTopRow(activeRowViewportIndex);
                _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                int activeRowIndex = _sheetView.Worksheet.ActiveRowIndex;
                int activeColumnIndex = _sheetView.Worksheet.ActiveColumnIndex;
                _tabularNavigator.GetNavigationStartPosition();
                TabularPosition position = MoveCurrent(direction);
                if (position.IsEmpty)
                {
                    NavigatorHelper.BringCellToVisible(_sheetView, _formulaSelectionFeature.AnchorRow, _formulaSelectionFeature.AnchorColumn);
                    return false;
                }
                int row = position.Row;
                int column = position.Column;
                _formulaSelectionFeature.AddSelection(row, column, 1, 1, true);
                int num5 = _sheetView.GetActiveRowViewportIndex();
                int num6 = _sheetView.GetActiveColumnViewportIndex();
                if ((activeRowViewportIndex != num5) || (activeColumnViewportIndex != num6))
                {
                    NavigatorHelper.BringCellToVisible(_sheetView, row, column);
                }
                return true;
            }

            TabularPosition MoveCurrent(NavigationDirection direction)
            {
                int anchorRow = _formulaSelectionFeature.AnchorRow;
                int anchorColumn = _formulaSelectionFeature.AnchorColumn;
                if ((anchorRow != -1) && (anchorColumn != -1))
                {
                    _tabularNavigator.CurrentCell = new TabularPosition(SheetArea.Cells, anchorRow, anchorColumn);
                    if (_tabularNavigator.MoveCurrent(direction))
                    {
                        TabularPosition currentCell = _tabularNavigator.CurrentCell;
                        return new TabularPosition(SheetArea.Cells, currentCell.Row, currentCell.Column);
                    }
                }
                return TabularPosition.Empty;
            }

            public void ProcessNavigation(NavigationDirection? direction)
            {
                if (((_formulaSelectionFeature.Items.Count == 0) || _formulaSelectionFeature.IsFlicking) && direction.HasValue)
                {
                    MoveActiveCell(direction.Value);
                }
            }

            bool SetActiveCell(int row, int column, bool clearSelection)
            {
                Worksheet worksheet = _sheetView.Worksheet;
                if (!_sheetView.RaiseLeaveCell(worksheet.ActiveRowIndex, worksheet.ActiveColumnIndex, row, column))
                {
                    worksheet.SetActiveCell(row, column, clearSelection);
                    _sheetView.RaiseEnterCell(row, column);
                    return true;
                }
                return false;
            }
        }

        internal class SpreadXFormulaSelection
        {
            SheetView.FormulaSelectionFeature _formulaSelectionFeature;
            KeyboardSelectNavigator _keyboardNavigator;
            SheetView _sheetView;

            internal SpreadXFormulaSelection(SheetView.FormulaSelectionFeature formulaSelectionFeature)
            {
                _formulaSelectionFeature = formulaSelectionFeature;
                _sheetView = formulaSelectionFeature.SheetView;
                _keyboardNavigator = new KeyboardSelectNavigator(_sheetView);
            }

            static CellRange CellRangeUnion(CellRange range1, CellRange range2)
            {
                int row = Math.Min(range1.Row, range2.Row);
                int column = Math.Min(range1.Column, range2.Column);
                int num3 = Math.Max((int)((range1.Row + range1.RowCount) - 1), (int)((range2.Row + range2.RowCount) - 1));
                int num4 = Math.Max((int)((range1.Column + range1.ColumnCount) - 1), (int)((range2.Column + range2.ColumnCount) - 1));
                return new CellRange(row, column, (num3 - row) + 1, (num4 - column) + 1);
            }

            CellRange ExpandRange(List<CellRange> spans, CellRange range)
            {
                if ((spans != null) && (spans.Count > 0))
                {
                    for (int i = 0; i < spans.Count; i++)
                    {
                        CellRange range2 = spans[i];
                        if (range.Intersects(range2.Row, range2.Column, range2.RowCount, range2.ColumnCount))
                        {
                            spans.RemoveAt(i--);
                            return ExpandRange(spans, CellRangeUnion(range, range2));
                        }
                    }
                }
                return range;
            }

            CellRange GetActiveCell()
            {
                int anchorRow = _formulaSelectionFeature.AnchorRow;
                int anchorColumn = _formulaSelectionFeature.AnchorColumn;
                CellRange range = new CellRange(anchorRow, anchorColumn, 1, 1);
                CellRange range2 = _sheetView.Worksheet.SpanModel.Find(anchorRow, anchorColumn);
                if (range2 != null)
                {
                    range = range2;
                }
                return range;
            }

            static void GetAdjustedEdge(int row, int column, int rowCount, int columnCount, NavigationDirection navigationDirection, bool shrink, out TabularPosition startPosition, out TabularPosition endPosition)
            {
                startPosition = TabularPosition.Empty;
                endPosition = TabularPosition.Empty;
                KeyboardSelectDirection none = KeyboardSelectDirection.None;
                switch (navigationDirection)
                {
                    case NavigationDirection.Left:
                    case NavigationDirection.PageLeft:
                    case NavigationDirection.Home:
                        none = KeyboardSelectDirection.Left;
                        break;

                    case NavigationDirection.Right:
                    case NavigationDirection.PageRight:
                    case NavigationDirection.End:
                        none = KeyboardSelectDirection.Right;
                        break;

                    case NavigationDirection.Up:
                    case NavigationDirection.PageUp:
                    case NavigationDirection.Top:
                    case NavigationDirection.First:
                        none = KeyboardSelectDirection.Top;
                        break;

                    case NavigationDirection.Down:
                    case NavigationDirection.PageDown:
                    case NavigationDirection.Bottom:
                    case NavigationDirection.Last:
                        none = KeyboardSelectDirection.Bottom;
                        break;
                }
                if (shrink)
                {
                    switch (navigationDirection)
                    {
                        case NavigationDirection.Left:
                            none = KeyboardSelectDirection.Right;
                            break;

                        case NavigationDirection.Right:
                            none = KeyboardSelectDirection.Left;
                            break;

                        case NavigationDirection.Up:
                            none = KeyboardSelectDirection.Bottom;
                            break;

                        case NavigationDirection.Down:
                            none = KeyboardSelectDirection.Top;
                            break;
                    }
                }
                switch (none)
                {
                    case KeyboardSelectDirection.Left:
                        startPosition = new TabularPosition(SheetArea.Cells, row, (column + columnCount) - 1);
                        endPosition = new TabularPosition(SheetArea.Cells, (row + rowCount) - 1, column);
                        return;

                    case KeyboardSelectDirection.Top:
                        startPosition = new TabularPosition(SheetArea.Cells, (row + rowCount) - 1, column);
                        endPosition = new TabularPosition(SheetArea.Cells, row, (column + columnCount) - 1);
                        return;

                    case KeyboardSelectDirection.Right:
                        startPosition = new TabularPosition(SheetArea.Cells, row, column);
                        endPosition = new TabularPosition(SheetArea.Cells, (row + rowCount) - 1, (column + columnCount) - 1);
                        return;

                    case KeyboardSelectDirection.Bottom:
                        startPosition = new TabularPosition(SheetArea.Cells, row, column);
                        endPosition = new TabularPosition(SheetArea.Cells, (row + rowCount) - 1, (column + columnCount) - 1);
                        return;
                }
            }

            CellRange GetExpandIntersectedRange(CellRange range)
            {
                if (_sheetView.Worksheet.SpanModel.IsEmpty())
                {
                    return range;
                }
                List<CellRange> spans = new List<CellRange>();
                foreach (object obj2 in _sheetView.Worksheet.SpanModel)
                {
                    spans.Add((CellRange)obj2);
                }
                return ExpandRange(spans, range);
            }

            static KeyboardSelectKind GetKeyboardSelectionKind(NavigationDirection navigationDirection)
            {
                switch (navigationDirection)
                {
                    case NavigationDirection.Left:
                    case NavigationDirection.Right:
                    case NavigationDirection.Up:
                    case NavigationDirection.Down:
                        return KeyboardSelectKind.Line;

                    case NavigationDirection.PageUp:
                    case NavigationDirection.PageDown:
                    case NavigationDirection.PageLeft:
                    case NavigationDirection.PageRight:
                        return KeyboardSelectKind.Page;

                    case NavigationDirection.Home:
                    case NavigationDirection.End:
                    case NavigationDirection.Top:
                    case NavigationDirection.Bottom:
                    case NavigationDirection.First:
                    case NavigationDirection.Last:
                        return KeyboardSelectKind.Through;
                }
                return KeyboardSelectKind.None;
            }

            CellRange GetSelectionRange()
            {
                if (_formulaSelectionFeature.Items.Count > 0)
                {
                    return _formulaSelectionFeature.Items[_formulaSelectionFeature.Items.Count - 1].Range;
                }
                return null;
            }

            CellRange KeyboardLineSelect(CellRange currentRange, NavigationDirection navigationDirection, bool shrink)
            {
                TabularPosition position;
                TabularPosition position2;
                TabularPosition currentCell;
                CellRange expandIntersectedRange;
                int row = (currentRange.Row < 0) ? 0 : currentRange.Row;
                int column = (currentRange.Column < 0) ? 0 : currentRange.Column;
                int rowCount = (currentRange.Row < 0) ? _sheetView.Worksheet.RowCount : currentRange.RowCount;
                int columnCount = (currentRange.Column < 0) ? _sheetView.Worksheet.ColumnCount : currentRange.ColumnCount;
                GetAdjustedEdge(row, column, rowCount, columnCount, navigationDirection, shrink, out position, out position2);
                if ((position == TabularPosition.Empty) || (position2 == TabularPosition.Empty))
                {
                    return null;
                }
                _keyboardNavigator.CurrentCell = position2;
                CellRange activeCell = GetActiveCell();
                do
                {
                    if (!_keyboardNavigator.MoveCurrent(navigationDirection))
                    {
                        return null;
                    }
                    currentCell = _keyboardNavigator.CurrentCell;
                    expandIntersectedRange = GetExpandIntersectedRange(TabularPositionUnion(position, currentCell));
                    if (!expandIntersectedRange.Contains(activeCell))
                    {
                        return null;
                    }
                }
                while (expandIntersectedRange.Equals(row, column, rowCount, columnCount));
                bool flag = true;
                int viewCellRow = currentCell.Row;
                int viewCellColumn = currentCell.Column;
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                int viewportTopRow = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                int viewportBottomRow = _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                int viewportLeftColumn = _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                int viewportRightColumn = _sheetView.GetViewportRightColumn(activeColumnViewportIndex);
                if ((navigationDirection == NavigationDirection.Up) || (navigationDirection == NavigationDirection.Down))
                {
                    if ((expandIntersectedRange.Column == 0) && (expandIntersectedRange.ColumnCount == _sheetView.Worksheet.ColumnCount))
                    {
                        if ((currentCell.Row >= viewportTopRow) && (currentCell.Row < viewportBottomRow))
                        {
                            flag = false;
                        }
                        else
                        {
                            viewCellColumn = viewportLeftColumn;
                        }
                    }
                }
                else if (((navigationDirection == NavigationDirection.Left) || (navigationDirection == NavigationDirection.Right)) && ((expandIntersectedRange.Row == 0) && (expandIntersectedRange.RowCount == _sheetView.Worksheet.RowCount)))
                {
                    if ((currentCell.Column >= viewportLeftColumn) && (currentCell.Column < viewportRightColumn))
                    {
                        flag = false;
                    }
                    else
                    {
                        viewCellRow = viewportTopRow;
                    }
                }
                if (flag)
                {
                    NavigatorHelper.BringCellToVisible(_sheetView, viewCellRow, viewCellColumn);
                }
                return expandIntersectedRange;
            }

            CellRange KeyboardPageSelect(CellRange currentRange, NavigationDirection direction)
            {
                int row = (currentRange.Row < 0) ? 0 : currentRange.Row;
                int rowCount = (currentRange.Row < 0) ? _sheetView.Worksheet.RowCount : currentRange.RowCount;
                int column = (currentRange.Column < 0) ? 0 : currentRange.Column;
                int columnCount = (currentRange.Column < 0) ? _sheetView.Worksheet.ColumnCount : currentRange.ColumnCount;
                int num5 = (row + rowCount) - 1;
                int num6 = (column + columnCount) - 1;
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                int num9 = _sheetView.Worksheet.RowCount;
                int num10 = _sheetView.Worksheet.ColumnCount;
                int viewportTopRow = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                int viewportLeftColumn = _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                _sheetView.GetViewportRightColumn(activeColumnViewportIndex);
                int num13 = GetActiveCell().Row;
                int num14 = GetActiveCell().Column;
                CellRange range = null;
                if (direction == NavigationDirection.PageDown)
                {
                    NavigatorHelper.ScrollToNextPageOfRows(_sheetView);
                    int num15 = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                    int viewportBottomRow = _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                    int num17 = num15 - viewportTopRow;
                    if (num17 > 0)
                    {
                        int num18 = num13;
                        int num19 = num5 + num17;
                        if (row != num13)
                        {
                            num18 = row + num17;
                            num19 = num5;
                            if (num18 >= num13)
                            {
                                num18 = num13;
                                num19 = num5 + (num17 - (num13 - row));
                            }
                        }
                        if (num19 < num15)
                        {
                            num19 = num15;
                        }
                        else if (num18 > viewportBottomRow)
                        {
                            num18 = viewportBottomRow;
                            num19 = num13;
                        }
                        else if ((num19 > viewportBottomRow) && (num13 <= viewportBottomRow))
                        {
                            num19 = viewportBottomRow;
                        }
                        return new CellRange(num18, column, (num19 - num18) + 1, columnCount);
                    }
                    int num20 = (num9 - row) - rowCount;
                    if ((num20 > 0) && (_sheetView.Worksheet.FrozenTrailingRowCount == 0))
                    {
                        int num21 = num13;
                        int num22 = num9 - 1;
                        range = new CellRange(num21, column, (num22 - num21) + 1, columnCount);
                    }
                    return range;
                }
                if (direction == NavigationDirection.PageUp)
                {
                    NavigatorHelper.ScrollToPreviousPageOfRows(_sheetView);
                    int num23 = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                    int num24 = _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                    int num25 = viewportTopRow - num23;
                    if (num25 > 0)
                    {
                        int num26 = row - num25;
                        int num27 = num5;
                        if (num5 != num13)
                        {
                            num26 = row;
                            num27 = num5 - num25;
                            if (num27 <= num13)
                            {
                                num26 = row - (num25 - (num5 - num13));
                                num27 = num13;
                            }
                        }
                        if (num27 < num23)
                        {
                            num26 = num13;
                            num27 = num23;
                        }
                        else if (num26 > num24)
                        {
                            num26 = num24;
                        }
                        else if ((num26 < num23) && (num13 >= num23))
                        {
                            num26 = num23;
                        }
                        return new CellRange(num26, column, (num27 - num26) + 1, columnCount);
                    }
                    if ((row > 0) && (_sheetView.Worksheet.FrozenRowCount == 0))
                    {
                        int num28 = 0;
                        int num29 = num13;
                        range = new CellRange(num28, column, (num29 - num28) + 1, columnCount);
                    }
                    return range;
                }
                if (direction == NavigationDirection.PageRight)
                {
                    NavigatorHelper.ScrollToNextPageOfColumns(_sheetView);
                    int num30 = _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                    int viewportRightColumn = _sheetView.GetViewportRightColumn(activeColumnViewportIndex);
                    int num32 = num30 - viewportLeftColumn;
                    if (num32 > 0)
                    {
                        int num33 = num14;
                        int num34 = num6 + num32;
                        if (column != num14)
                        {
                            num33 = column + num32;
                            num34 = num6;
                            if (num33 >= num14)
                            {
                                num33 = num14;
                                num34 = num6 + (num32 - (num14 - column));
                            }
                        }
                        if (num34 < num30)
                        {
                            num34 = num30;
                        }
                        else if (num33 > viewportRightColumn)
                        {
                            num33 = viewportRightColumn;
                            num34 = num14;
                        }
                        else if ((num34 > viewportRightColumn) && (num14 <= viewportRightColumn))
                        {
                            num34 = viewportRightColumn;
                        }
                        return new CellRange(row, num33, rowCount, (num34 - num33) + 1);
                    }
                    int num35 = (num10 - column) - columnCount;
                    if ((num35 > 0) && (_sheetView.Worksheet.FrozenTrailingColumnCount == 0))
                    {
                        int num36 = num14;
                        int num37 = num10 - 1;
                        range = new CellRange(row, num36, rowCount, (num37 - num36) + 1);
                    }
                    return range;
                }
                if (direction == NavigationDirection.PageLeft)
                {
                    NavigatorHelper.ScrollToPreviousPageOfColumns(_sheetView);
                    int num38 = _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                    int num39 = _sheetView.GetViewportRightColumn(activeColumnViewportIndex);
                    int num40 = viewportLeftColumn - num38;
                    if (num40 > 0)
                    {
                        int num41 = column - num40;
                        int num42 = num6;
                        if (num6 != num14)
                        {
                            num41 = column;
                            num42 = num6 - num40;
                            if (num42 <= num14)
                            {
                                num41 = column - (num40 - (num6 - num14));
                                num42 = num14;
                            }
                        }
                        if (num42 < num38)
                        {
                            num41 = num14;
                            num42 = num38;
                        }
                        else if (num41 > num39)
                        {
                            num41 = num39;
                        }
                        else if ((num41 < num38) && (num14 >= num38))
                        {
                            num41 = num38;
                        }
                        return new CellRange(row, num41, rowCount, (num42 - num41) + 1);
                    }
                    if ((column > 0) && (_sheetView.Worksheet.FrozenColumnCount == 0))
                    {
                        int num43 = 0;
                        int num44 = num14;
                        range = new CellRange(row, num43, rowCount, (num44 - num43) + 1);
                    }
                }
                return range;
            }

            public void KeyboardSelect(NavigationDirection direction)
            {
                if ((_formulaSelectionFeature.Items.Count == 0) || _formulaSelectionFeature.IsFlicking)
                {
                    CellRange selectionRange = GetSelectionRange();
                    if (((selectionRange == null) && (_sheetView != null)) && (_sheetView.Worksheet != null))
                    {
                        selectionRange = GetActiveCell();
                    }
                    if (selectionRange != null)
                    {
                        KeyboardSelectKind keyboardSelectionKind = GetKeyboardSelectionKind(direction);
                        CellRange range = null;
                        switch (keyboardSelectionKind)
                        {
                            case KeyboardSelectKind.Line:
                                range = KeyboardLineSelect(selectionRange, direction, true);
                                if (range == null)
                                {
                                    range = KeyboardLineSelect(selectionRange, direction, false);
                                }
                                break;

                            case KeyboardSelectKind.Page:
                                range = KeyboardPageSelect(selectionRange, direction);
                                break;

                            case KeyboardSelectKind.Through:
                                range = KeyboardThroughSelect(selectionRange, direction);
                                break;
                        }
                        if ((range != null) && !range.Equals(selectionRange))
                        {
                            range = GetExpandIntersectedRange(range);
                            if (selectionRange.Row < 0)
                            {
                                range = new CellRange(-1, range.Column, -1, range.ColumnCount);
                            }
                            if (selectionRange.Column < 0)
                            {
                                range = new CellRange(range.Row, -1, range.RowCount, -1);
                            }
                            _formulaSelectionFeature.ChangeLastSelection(range, false);
                        }
                    }
                }
            }

            CellRange KeyboardThroughSelect(CellRange currentRange, NavigationDirection direction)
            {
                int row = (currentRange.Row < 0) ? 0 : currentRange.Row;
                int column = (currentRange.Column < 0) ? 0 : currentRange.Column;
                int rowCount = (currentRange.Row < 0) ? _sheetView.Worksheet.RowCount : currentRange.RowCount;
                int columnCount = (currentRange.Column < 0) ? _sheetView.Worksheet.ColumnCount : currentRange.ColumnCount;
                CellRange activeCell = GetActiveCell();
                CellRange range2 = null;
                if (direction == NavigationDirection.Home)
                {
                    range2 = new CellRange(row, 0, rowCount, activeCell.Column + activeCell.ColumnCount);
                }
                else if (direction == NavigationDirection.End)
                {
                    range2 = new CellRange(row, activeCell.Column, rowCount, _sheetView.Worksheet.ColumnCount - activeCell.Column);
                }
                else if (direction == NavigationDirection.Top)
                {
                    range2 = new CellRange(0, column, activeCell.Row + activeCell.RowCount, columnCount);
                }
                else if (direction == NavigationDirection.Bottom)
                {
                    range2 = new CellRange(activeCell.Row, column, _sheetView.Worksheet.RowCount - activeCell.Row, columnCount);
                }
                else if (direction == NavigationDirection.First)
                {
                    range2 = new CellRange(_sheetView.Worksheet.FrozenRowCount, _sheetView.Worksheet.FrozenColumnCount, (activeCell.Row + activeCell.RowCount) - _sheetView.Worksheet.FrozenRowCount, (activeCell.Column + activeCell.ColumnCount) - _sheetView.Worksheet.FrozenColumnCount);
                }
                else if (direction == NavigationDirection.Last)
                {
                    range2 = new CellRange(activeCell.Row, activeCell.Column, (_sheetView.Worksheet.RowCount - _sheetView.Worksheet.FrozenTrailingRowCount) - activeCell.Row, (_sheetView.Worksheet.ColumnCount - _sheetView.Worksheet.FrozenTrailingColumnCount) - activeCell.Column);
                }
                if (range2 != null)
                {
                    int viewCellRow = range2.Row;
                    int num6 = (range2.Row + range2.RowCount) - 1;
                    int viewCellColumn = range2.Column;
                    int num8 = (range2.Column + range2.ColumnCount) - 1;
                    if ((direction == NavigationDirection.Top) || (direction == NavigationDirection.First))
                    {
                        NavigatorHelper.BringCellToVisible(_sheetView, viewCellRow, viewCellColumn);
                        return range2;
                    }
                    if ((direction == NavigationDirection.Home) || (direction == NavigationDirection.End))
                    {
                        int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                        int viewportTopRow = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                        int viewportBottomRow = _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                        if (direction == NavigationDirection.Home)
                        {
                            if (num6 < viewportTopRow)
                            {
                                NavigatorHelper.BringCellToVisible(_sheetView, row, viewCellColumn);
                                return range2;
                            }
                            if (viewCellRow > viewportBottomRow)
                            {
                                NavigatorHelper.BringCellToVisible(_sheetView, num6, viewCellColumn);
                                return range2;
                            }
                            NavigatorHelper.BringCellToVisible(_sheetView, viewportTopRow, viewCellColumn);
                            return range2;
                        }
                        if (num6 < viewportTopRow)
                        {
                            NavigatorHelper.BringCellToVisible(_sheetView, row, num8);
                            return range2;
                        }
                        if (viewCellRow > viewportBottomRow)
                        {
                            NavigatorHelper.BringCellToVisible(_sheetView, num6, num8);
                            return range2;
                        }
                        NavigatorHelper.BringCellToVisible(_sheetView, viewportTopRow, num8);
                        return range2;
                    }
                    if ((direction == NavigationDirection.Bottom) || (direction == NavigationDirection.Last))
                    {
                        NavigatorHelper.BringCellToVisible(_sheetView, num6, num8);
                    }
                }
                return range2;
            }

            static CellRange TabularPositionUnion(TabularPosition startPosition, TabularPosition endPosition)
            {
                int row = Math.Min(startPosition.Row, endPosition.Row);
                int column = Math.Min(startPosition.Column, endPosition.Column);
                int rowCount = Math.Abs((int)(startPosition.Row - endPosition.Row)) + 1;
                return new CellRange(row, column, rowCount, Math.Abs((int)(startPosition.Column - endPosition.Column)) + 1);
            }

            enum KeyboardSelectDirection
            {
                None,
                Left,
                Top,
                Right,
                Bottom
            }

            enum KeyboardSelectKind
            {
                None,
                Line,
                Page,
                Through
            }

            class KeyboardSelectNavigator : SpreadXTabularNavigator
            {
                public KeyboardSelectNavigator(SheetView sheetView)
                    : base(sheetView)
                {
                }

                public override void BringCellToVisible(TabularPosition position)
                {
                }

                public override bool CanMoveCurrentTo(TabularPosition cellPosition)
                {
                    return (((((base._sheetView.Worksheet != null) && (cellPosition.Row >= 0)) && ((cellPosition.Row < base._sheetView.Worksheet.RowCount) && (cellPosition.Column >= 0))) && ((cellPosition.Column < base._sheetView.Worksheet.ColumnCount) && GetRowIsVisible(cellPosition.Row))) && GetColumnIsVisible(cellPosition.Column));
                }
            }
        }

        internal class SpreadXFormulaTabularNavigator : TabularNavigator
        {
            internal SheetView _sheetView;

            public SpreadXFormulaTabularNavigator(SheetView sheetView)
            {
                _sheetView = sheetView;
            }

            public override void BringCellToVisible(TabularPosition position)
            {
                if ((!position.IsEmpty && (position.Area == SheetArea.Cells)) && (_sheetView.Worksheet != null))
                {
                    NavigatorHelper.BringCellToVisible(_sheetView, position.Row, position.Column);
                }
            }

            public override bool CanHorizontalScroll(bool isBackward)
            {
                if (_sheetView == null)
                {
                    return base.CanHorizontalScroll(isBackward);
                }
                if (!_sheetView.HorizontalScrollable)
                {
                    return false;
                }
                int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                if (isBackward)
                {
                    return (_sheetView.GetNextPageColumnCount(activeColumnViewportIndex) > 0);
                }
                return (_sheetView.GetPrePageColumnCount(activeColumnViewportIndex) > 0);
            }

            public override bool CanMoveCurrentTo(TabularPosition cellPosition)
            {
                return (((((_sheetView.Worksheet != null) && (cellPosition.Row >= 0)) && ((cellPosition.Row < _sheetView.Worksheet.RowCount) && (cellPosition.Column >= 0))) && (((cellPosition.Column < _sheetView.Worksheet.ColumnCount) && _sheetView.Worksheet.Cells[cellPosition.Row, cellPosition.Column].ActualFocusable) && GetRowIsVisible(cellPosition.Row))) && GetColumnIsVisible(cellPosition.Column));
            }

            public override bool CanVerticalScroll(bool isBackward)
            {
                if (_sheetView == null)
                {
                    return base.CanVerticalScroll(isBackward);
                }
                if (!_sheetView.VerticalScrollable)
                {
                    return false;
                }
                int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                if (isBackward)
                {
                    return (_sheetView.GetNextPageRowCount(activeRowViewportIndex) > 0);
                }
                return (_sheetView.GetPrePageRowCount(activeRowViewportIndex) > 0);
            }

            public override bool GetColumnIsVisible(int columnIndex)
            {
                if ((_sheetView == null) || (_sheetView.Worksheet == null))
                {
                    return base.GetColumnIsVisible(columnIndex);
                }
                return (_sheetView.Worksheet.GetActualColumnVisible(columnIndex, SheetArea.Cells) && (_sheetView.Worksheet.GetActualColumnWidth(columnIndex, SheetArea.Cells) > 0.0));
            }

            public override bool GetRowIsVisible(int rowIndex)
            {
                if ((_sheetView == null) || (_sheetView.Worksheet == null))
                {
                    return base.GetRowIsVisible(rowIndex);
                }
                return (_sheetView.Worksheet.GetActualRowVisible(rowIndex, SheetArea.Cells) && (_sheetView.Worksheet.GetActualRowHeight(rowIndex, SheetArea.Cells) > 0.0));
            }

            public override bool IsMerged(TabularPosition position, out TabularRange range)
            {
                range = new TabularRange(position, 1, 1);
                if ((_sheetView.Worksheet != null) && (_sheetView.Worksheet.SpanModel != null))
                {
                    CellRange range2 = _sheetView.Worksheet.SpanModel.Find(position.Row, position.Column);
                    if (range2 != null)
                    {
                        range = new TabularRange(position.Area, range2.Row, range2.Column, range2.RowCount, range2.ColumnCount);
                        return true;
                    }
                }
                return false;
            }

            public override void ScrollToNextPageOfColumns()
            {
                NavigatorHelper.ScrollToNextPageOfColumns(_sheetView);
            }

            public override void ScrollToNextPageOfRows()
            {
                NavigatorHelper.ScrollToNextPageOfRows(_sheetView);
            }

            public override void ScrollToPreviousPageOfColumns()
            {
                NavigatorHelper.ScrollToPreviousPageOfColumns(_sheetView);
            }

            public override void ScrollToPreviousPageOfRows()
            {
                NavigatorHelper.ScrollToPreviousPageOfRows(_sheetView);
            }

            public override TabularRange ContentBounds
            {
                get
                {
                    if ((_sheetView == null) || (_sheetView.Worksheet == null))
                    {
                        return base.ContentBounds;
                    }
                    Worksheet worksheet = _sheetView.Worksheet;
                    ViewportInfo viewportInfo = worksheet.GetViewportInfo();
                    int activeRowViewportIndex = worksheet.GetActiveRowViewportIndex();
                    int activeColumnViewportIndex = worksheet.GetActiveColumnViewportIndex();
                    int row = 0;
                    int column = 0;
                    int rowCount = worksheet.RowCount;
                    int columnCount = worksheet.ColumnCount;
                    if (viewportInfo.RowViewportCount > 1)
                    {
                        if (activeRowViewportIndex > 0)
                        {
                            row = worksheet.FrozenRowCount;
                            rowCount -= worksheet.FrozenRowCount;
                        }
                        if (activeRowViewportIndex < (viewportInfo.RowViewportCount - 1))
                        {
                            rowCount -= worksheet.FrozenTrailingRowCount;
                        }
                    }
                    if (viewportInfo.ColumnViewportCount > 1)
                    {
                        if (activeColumnViewportIndex > 0)
                        {
                            column = worksheet.FrozenColumnCount;
                            columnCount -= worksheet.FrozenColumnCount;
                        }
                        if (activeColumnViewportIndex < (viewportInfo.ColumnViewportCount - 1))
                        {
                            columnCount -= worksheet.FrozenTrailingColumnCount;
                        }
                    }
                    return new TabularRange(SheetArea.Cells, row, column, rowCount, columnCount);
                }
            }

            public override TabularRange CurrentViewport
            {
                get
                {
                    int activeColumnViewportIndex = _sheetView.GetActiveColumnViewportIndex();
                    int activeRowViewportIndex = _sheetView.GetActiveRowViewportIndex();
                    if (activeColumnViewportIndex == -1)
                    {
                        activeColumnViewportIndex = 0;
                    }
                    else if (activeColumnViewportIndex == _sheetView.Worksheet.GetViewportInfo().ColumnViewportCount)
                    {
                        activeColumnViewportIndex = _sheetView.Worksheet.GetViewportInfo().ColumnViewportCount - 1;
                    }
                    if (activeRowViewportIndex == -1)
                    {
                        activeRowViewportIndex = 0;
                    }
                    else if (activeRowViewportIndex == _sheetView.Worksheet.GetViewportInfo().RowViewportCount)
                    {
                        activeRowViewportIndex = _sheetView.Worksheet.GetViewportInfo().RowViewportCount - 1;
                    }
                    int viewportLeftColumn = _sheetView.GetViewportLeftColumn(activeColumnViewportIndex);
                    int viewportRightColumn = _sheetView.GetViewportRightColumn(activeColumnViewportIndex);
                    int viewportTopRow = _sheetView.GetViewportTopRow(activeRowViewportIndex);
                    int viewportBottomRow = _sheetView.GetViewportBottomRow(activeRowViewportIndex);
                    double viewportWidth = _sheetView.GetViewportWidth(activeColumnViewportIndex);
                    double viewportHeight = _sheetView.GetViewportHeight(activeRowViewportIndex);
                    if (NavigatorHelper.GetColumnWidth(_sheetView.Worksheet, viewportLeftColumn, viewportRightColumn) > viewportWidth)
                    {
                        viewportRightColumn--;
                    }
                    if (NavigatorHelper.GetRowHeight(_sheetView.Worksheet, viewportTopRow, viewportBottomRow) > viewportHeight)
                    {
                        viewportBottomRow--;
                    }
                    return new TabularRange(SheetArea.Cells, viewportTopRow, viewportLeftColumn, Math.Max(1, (viewportBottomRow - viewportTopRow) + 1), Math.Max(1, (viewportRightColumn - viewportLeftColumn) + 1));
                }
            }

            public override int TotalColumnCount
            {
                get { return _sheetView.Worksheet.ColumnCount; }
            }

            public override int TotalRowCount
            {
                get { return _sheetView.Worksheet.RowCount; }
            }
        }
    }
}

