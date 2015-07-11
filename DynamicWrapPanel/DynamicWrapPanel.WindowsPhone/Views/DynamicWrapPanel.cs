using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace DynamicWrapPanel.Views {
    public class DynamicWrapPanel : Panel {

        private List<FrameworkElement>[] renderedChildren; // Contains items info after arranged
        private HashSet<FrameworkElement>[] checkExistence; // A clone, used to check existence
        private List<RelocateInfo> relocations; // Store info for relocate animation
        private double columnWidth;
        private List<double>[] heightInColumn; // Item heights in column
        private Dictionary<int, int>[] columnToChildren; // Map index in column to index in this.Children
        private FrameworkElement excludedItem;
        private Size emptyPlaceholderSize;
        private int emptyPlaceholderIndex;

        public int NumberOfColumns { get; set; }
        public double HorizontalMargin { get; set; }
        public double VerticalMargin { get; set; }

        public double DefinedHeight {
            get {
                return _definedHeight;
            }

            set {
                if (_definedHeight < 0)
                    _definedHeight = value;
            }
        }

        private double _definedHeight = -1;

        public DynamicWrapPanel() {
            NumberOfColumns = 2;

            this.renderedChildren = new List<FrameworkElement>[this.NumberOfColumns];
            for (int i = 0; i < this.renderedChildren.Length; ++i)
                this.renderedChildren[i] = new List<FrameworkElement>();
            this.checkExistence = new HashSet<FrameworkElement>[this.NumberOfColumns];
            for (int i = 0; i < this.checkExistence.Length; ++i)
                this.checkExistence[i] = new HashSet<FrameworkElement>();

            this.heightInColumn = new List<double>[this.NumberOfColumns];
            for (int i = 0; i < this.heightInColumn.Length; ++i)
                this.heightInColumn[i] = new List<double>();

            this.columnToChildren = new Dictionary<int, int>[this.NumberOfColumns];
            for (int i = 0; i < this.columnToChildren.Length; ++i)
                this.columnToChildren[i] = new Dictionary<int, int>();

            this.relocations = new List<RelocateInfo>();
            this.emptyPlaceholderIndex = -1;

            Loaded += DynamicWrapPanel_Loaded;
        }

        private void DynamicWrapPanel_Loaded(object sender, RoutedEventArgs e) { }

        /// <summary>
        /// Size of each item is calculated here
        /// </summary>
        protected override Size MeasureOverride(Size availableSize) {
            DefinedHeight = availableSize.Height;

            double availWidth = availableSize.Width;
            double leftMargin = this.Margin.Left, rightMargin = this.Margin.Right; // margin to the edge of the screen
            columnWidth = (availWidth - leftMargin - rightMargin - (this.NumberOfColumns - 1) * HorizontalMargin) / this.NumberOfColumns;

            int currentCol = 0;
            double[] columnHeight = new double[this.NumberOfColumns];

            for (int i = 0; i < this.NumberOfColumns; ++i) {
                columnHeight[i] = this.Margin.Top;
            }

            foreach (FrameworkElement child in Children) {
                // Let the element calculate its size freely
                child.Measure(availableSize);

                double itemDesiredHeight = child.DesiredSize.Height; // size set in xaml

                // Apply the correct size to this item
                child.Measure(new Size(columnWidth, itemDesiredHeight));

                columnHeight[currentCol] += HeightOfMargin(itemDesiredHeight);
                currentCol = (columnHeight[0].CompareTo(columnHeight[1]) > 0) ? 1 : 0;
            }

            double finalHeight = columnHeight.Max();
            Size desiredSize = new Size(availWidth, finalHeight);

            return desiredSize;
        }

        /// <summary>
        /// Position of each item is calculated here
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize) {
            ResetSavedData();

            double availWidth = finalSize.Width;
            double leftMargin = this.Margin.Left, rightMargin = this.Margin.Right; // margin to the edge of the screen
            this.columnWidth = (availWidth - leftMargin - rightMargin - (this.NumberOfColumns - 1) * HorizontalMargin) / this.NumberOfColumns;
            int currentCol = 0;
            double[] columnHeight = new double[this.NumberOfColumns];
            double[] columnOffsetX = new double[this.NumberOfColumns];

            // First, add the top margin to all columns
            for (int i = 0; i < this.NumberOfColumns; ++i) {
                columnHeight[i] = this.Margin.Top;
            }
            // Pre-calculate x offset of each column
            for (int i = 0; i < this.NumberOfColumns; ++i) {
                columnOffsetX[i] = leftMargin + i * (columnWidth + HorizontalMargin);
            }

            for (int i = 0; i < Children.Count; ++i) {
                // Leave a blank space as an empty item
                if (i == this.emptyPlaceholderIndex) {
                    columnHeight[currentCol] += HeightOfMargin(this.emptyPlaceholderSize.Height);
                    currentCol = columnHeight.MinIndex();
                }

                FrameworkElement child = (FrameworkElement)Children[i];

                double dx = columnOffsetX[currentCol];
                double dy = Math.Min(columnHeight[0], columnHeight[1]);
                double dw = columnWidth;
                double dh = child.DesiredSize.Height; // Height calculated in MeasureOverride method

                child.Arrange(new Rect(dx, dy, dw, dh));

                // Ignore excluded item's height
                if (IsExcludedItem(child))
                    continue;
                columnHeight[currentCol] += HeightOfMargin(dh);

                // Save this item info
                SaveItemInfo(currentCol, i, new Rect(dx, dy, dw, dh), child);

                currentCol = columnHeight.MinIndex();
            }

            return finalSize;
        }

        private bool IsExcludedItem(FrameworkElement item) {
            return item.DataContext != null && this.excludedItem != null
                    && (item.DataContext as FrameworkElement).DataContext == this.excludedItem.DataContext;
        }

        private void SaveItemInfo(int column, int index, Rect itemRect, FrameworkElement origin) {
            this.heightInColumn[column].Add(itemRect.Bottom);
            this.columnToChildren[column][this.heightInColumn[column].Count - 1] = index;

            //ExtendedFrameworkElement toSave = new ExtendedFrameworkElement(itemRect, origin, index);
            //if (this.checkExistence[column].Contains(origin))
            //    return;

            origin.Tag = index;
            this.renderedChildren[column].Add(origin);
            //this.checkExistence[column].Add(origin);

        }

        private void ResetSavedData() {
            //this.renderedChildren = new SortedSet<ExtendedFrameworkElement>[this.NumberOfColumns];
            //this.relocations = new List<RelocateInfo>();
            for (int i = 0; i < this.NumberOfColumns; ++i) {
                this.heightInColumn[i].Clear();
                this.columnToChildren[i].Clear();
                this.renderedChildren[i].Clear();
            }

            GC.Collect();
        }

        private double HeightOfMargin(double originHeight) {
            return originHeight + VerticalMargin;
        }

        public int ItemIndexByPosition(Point pos) {
            if (pos.X < 0)
                pos.X = 0;
            int column = (int)(pos.X / this.columnWidth);

            int index = -1;
            List<double> heights = this.heightInColumn[column];
            for (int i = 0; i < heights.Count; ++i) {
                if (heights[i] > pos.Y)
                    break;
                index = i;
            }

            if (index >= 0)
                return this.columnToChildren[column][index];

            // pos.Y is over the top, return the first item in that column
            return this.columnToChildren[column][0];
        }

        public IntPair ItemColumnIndexByPosition(Point pos) {
            if (pos.X < 0)
                pos.X = 0;
            int column = (int)(pos.X / this.columnWidth);

            int index = -1;
            List<double> heights = this.heightInColumn[column];
            for (int i = 0; i < heights.Count; ++i) {
                if (heights[i] > pos.Y)
                    break;
                index = i;
            }

            if (index >= 0)
                return new IntPair(column, index);
            return new IntPair(column, 0);
        }

        public void SetEmptySize(Size size) {
            this.emptyPlaceholderSize = size;
        }

        public int SetEmptyPosition(Point pos) {
            ArrangeWithEmpty(pos);

            return this.emptyPlaceholderIndex;
        }

        public void ArrangeWithEmpty(Point pos) {
            IntPair info = ItemColumnIndexByPosition(pos);
            int column = info.A, id_in_column = info.B;
            Queue<FrameworkElement> stash = new Queue<FrameworkElement>();
            FrameworkElement target = null;

            // First, take out the manipulating item (i.e target)
            for(int col = 0; col < this.NumberOfColumns; ++col) {
                for(int i = 0; i < this.renderedChildren[col].Count; ++i) {
                    var item = this.renderedChildren[col][i];
                    if (IsExcludedItem(item)) {
                        target = item;
                        this.renderedChildren[col].Remove(target);
                        this.heightInColumn[col].RemoveAt(i);
                        col = 100000; i = 100000; // break
                    }
                }
            }

            for (int col = 0; col < this.NumberOfColumns; ++col) {
                for (int i = 0; i < this.renderedChildren[col].Count; ++i) {
                    var item = this.renderedChildren[col][i];
                    if (heightInColumn[col][i] > pos.Y) {
                        stash.Enqueue(item);
                    }
                }
            }

            for (int col = 0; col < this.NumberOfColumns; ++col) {
                int count = this.heightInColumn[col].Count;
                while (count > 0 && this.heightInColumn[col].Last() > pos.Y) {
                    this.heightInColumn[col].RemoveAt(count - 1);
                    this.renderedChildren[col].RemoveAt(count - 1);
                    count = this.heightInColumn[col].Count;
                }
            }

            // Temporarily add blank space
            int insertedEmptyIndex = this.heightInColumn[column].Count;
            this.heightInColumn[column].Add(HeightOfMargin(target.DesiredSize.Height));
            this.renderedChildren[column].Add(target);

            ArrangeManually(stash);

            // Remove the empty
            //this.heightInColumn[column].RemoveAt(insertedEmptyIndex);
        }

        private void ArrangeManually(Queue<FrameworkElement> stash) {
            double leftMargin = this.Margin.Left, rightMargin = this.Margin.Right;
            // Now we arrange the stashed items
            while (stash.Count > 0) {
                int currentCol = this.heightInColumn.MinIndex();
                var item = stash.Dequeue();

                double dx = leftMargin + currentCol * (columnWidth + HorizontalMargin);
                double dy = this.heightInColumn[currentCol].LastOrDefault();
                double dw = columnWidth;
                double dh = item.DesiredSize.Height;

                item.Arrange(new Rect(dx, dy, dw, dh));

                this.heightInColumn[currentCol].Add(HeightOfMargin(dh));
                this.renderedChildren[currentCol].Add(item);
            }
        }

        public int GetEmptyCurrentIndex() {
            return this.emptyPlaceholderIndex;
        }

        public void SetManipulation(bool isOn) {
            if (!isOn) {
                this.emptyPlaceholderIndex = -1;
            }
        }

        /// <summary>
        /// Hide this item, do not re-measure, do not re-arrange
        /// </summary>
        public void ExcludeItem(FrameworkElement item) {
            this.excludedItem = item;
            item.Opacity = 0;

            Refresh();
        }

        /// <summary>
        /// Not exclude this item anymore
        /// </summary>
        public void UnexcludeItem(FrameworkElement item) {
            this.excludedItem = null;
            item.Opacity = 1;

            Refresh();
        }

        /// <summary>
        /// Re-measure and Re-arrange
        /// </summary>
        public void Refresh() {
            UpdateLayout();
        }

        /// <summary>
        /// Refresh asynchronously
        /// </summary>
        public void RefreshAsync() {
            InvalidateMeasure(); // this automatically call InvalidateArrange
        }
    }


}
