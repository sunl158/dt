#region 文件描述
/******************************************************************************
* 创建: Daoting
* 摘要: 
* 日志: 2014-07-03 创建
******************************************************************************/
#endregion

#region 引用命名
using Dt.Cells.Data;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
#endregion

namespace Dt.Cells.UI
{
    internal partial class CellsPanel : Panel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            BuildSpanGraph();
            _rowsLayer.Measure(availableSize);
            _borderLayer.Measure(availableSize);

            BuildSelection();
            _selectionLayer.Measure(availableSize);

            if (_formulaSelectionLayer.Children.Count > 0)
                _formulaSelectionLayer.InvalidateMeasure();
            _formulaSelectionLayer.Measure(availableSize);

            _shapeLayer.Measure(availableSize);

            if (_dragFillLayer != null)
            {
                _dragFillLayer.Measure(availableSize);
            }

            if (_decorationLayer != null)
            {
                _decorationLayer.InvalidateMeasure();
                _decorationLayer.Measure(availableSize);
            }

            _dataValidationLayer.InvalidateMeasure();
            _editorLayer.Measure(availableSize);

            if (Excel._formulaSelectionGripperPanel != null)
            {
                Excel._formulaSelectionGripperPanel.InvalidateMeasure();
            }

            _floatingObjectLayer.Measure(availableSize);
            _floatingObjectsMovingResizingLayer.Measure(availableSize);

            return GetViewportSize(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect rc = new Rect(0.0, 0.0, finalSize.Width, finalSize.Height);
            _rowsLayer.Arrange(rc);
            _borderLayer.Arrange(rc);
            _selectionLayer.Arrange(rc);
            _formulaSelectionLayer.Arrange(rc);
            _shapeLayer.Arrange(rc);

            if (_dragFillLayer != null)
            {
                _dragFillLayer.Arrange(rc);
            }

            if (_decorationLayer != null)
            {
                _decorationLayer.Arrange(rc);
            }
            _dataValidationLayer.Arrange(rc);

            if (IsEditing())
                _editorLayer.InvalidateArrange();
            _editorLayer.Arrange(rc);

            _floatingObjectLayer.Arrange(rc);
            _floatingObjectsMovingResizingLayer.Arrange(rc);

            Size viewportSize = GetViewportSize(finalSize);
            if (Excel.IsTouching)
            {
                if (Clip == null)
                    Clip = new RectangleGeometry { Rect = new Rect(new Point(), viewportSize) };
            }
            else
            {
                Clip = new RectangleGeometry { Rect = new Rect(new Point(), viewportSize) };
            }

            if (Clip != null)
                _borderLayer.Clip = new RectangleGeometry { Rect = Clip.Rect };
            return viewportSize;
        }

        void BuildSpanGraph()
        {
            _cachedSpanGraph.Reset();
            SheetSpanModelBase spanModel = GetSpanModel();
            if ((spanModel != null) && !spanModel.IsEmpty())
            {
                int rowStart = Excel.GetViewportTopRow(RowViewportIndex);
                int rowEnd = Excel.GetViewportBottomRow(RowViewportIndex);
                int columnStart = Excel.GetViewportLeftColumn(ColumnViewportIndex);
                int columnEnd = Excel.GetViewportRightColumn(ColumnViewportIndex);

                if ((rowStart <= rowEnd) && (columnStart <= columnEnd))
                {
                    int num5 = -1;
                    for (int i = rowStart - 1; i > -1; i--)
                    {
                        if (Excel.ActiveSheet.GetActualRowVisible(i, SheetArea.Cells))
                        {
                            num5 = i;
                            break;
                        }
                    }
                    rowStart = num5;
                    int count = GetDataContext().Rows.Count;
                    for (int j = rowEnd + 1; j < count; j++)
                    {
                        if (Excel.ActiveSheet.GetActualRowVisible(j, SheetArea.Cells))
                        {
                            rowEnd = j;
                            break;
                        }
                    }
                    int num9 = -1;
                    for (int k = columnStart - 1; k > -1; k--)
                    {
                        if (Excel.ActiveSheet.GetActualColumnVisible(k, SheetArea.Cells))
                        {
                            num9 = k;
                            break;
                        }
                    }
                    columnStart = num9;
                    int num11 = GetDataContext().Columns.Count;
                    for (int m = columnEnd + 1; m < num11; m++)
                    {
                        if (Excel.ActiveSheet.GetActualColumnVisible(m, SheetArea.Cells))
                        {
                            columnEnd = m;
                            break;
                        }
                    }
                    _cachedSpanGraph.BuildGraph(columnStart, columnEnd, rowStart, rowEnd, GetSpanModel(), CellCache);
                }
            }
        }
    }
}

