using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace DynamicWrapPanel.Views {
    public class DynamicListBox : ListBox {

        public DynamicListBox() {
            //this.ItemsPanel = GetItemsPanelTemplate();

            this.Background = new SolidColorBrush(Colors.Transparent);
        }

        //private ItemsPanelTemplate GetItemsPanelTemplate() {
        //    string xaml = @"<ItemsPanelTemplate  xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'  xmlns:local='using:DynamicWrapPanel.Views'><local:DynamicWrapPanel/></ItemsPanelTemplate>";
        //    return XamlReader.Load(xaml) as ItemsPanelTemplate;
        //}
    }

    public class DummyFactory {
        public static FrameworkElement Create(FrameworkElement origin) {
            Border dummy = new Border();
            dummy.Width = origin.Width;
            dummy.Height = origin.Height;
            dummy.Background = new SolidColorBrush(Colors.DarkGray);
            dummy.IsHitTestVisible = false; // don't receive touch

            return dummy;
        }

        public static FrameworkElement CreateEmpty(FrameworkElement origin) {
            Border dummy = new Border();
            dummy.Width = origin.Width;
            dummy.Height = origin.Height;
            dummy.Background = new SolidColorBrush(Colors.Black);
            dummy.IsHitTestVisible = false; // don't receive touch
            dummy.Tag = ItemType.Empty;

            return dummy;
        }
    }

    public class ItemType {
        private int value;

        public static readonly ItemType Dummy = new ItemType(0);
        public static readonly ItemType Empty = new ItemType(1);

        public ItemType(int val) {
            value = val;
        }
    }

    public class IntPair {
        public int A { get; set; }
        public int B { get; set; }

        public IntPair(int a, int b) {
            A = a;
            B = b;
        }

        public override bool Equals(object obj) {
            IntPair pair = obj as IntPair;
            if (obj == null)
                return false;
            return (A == pair.A) && (B == pair.B);
        }

        public override int GetHashCode() {
            return A.GetHashCode() ^ B.GetHashCode();
        }
    }

    public class Library {
        /// <summary>
        /// Binary search for index of a maximum value that is <= value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int LargerSearch<T>(List<T> list, T value) where T : IComparable {
            int low = 0, hi = list.Count, ans = -1;
            while (low <= hi) {
                int mid = (low + hi) / 2;
                if (list[mid].CompareTo(value) <= 0) {
                    ans = (ans < 0) ? mid : (list[mid].CompareTo(list[ans]) <= 0 ? mid : ans);
                    if (ans < 0)
                        ans = mid;
                    else if (list[mid].CompareTo(list[ans]) >= 0)
                        ans = mid;

                    low = mid;
                }
                else {
                    hi = mid;
                }
            }

            return ans;
        }

        public static ScrollViewer GetScrollViewer(DependencyObject depObj) {
            if (depObj is ScrollViewer) return depObj as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }
    }

    public static class ExtensionLibrary {
        public static void EmptySelection(this ListBox list) {
            list.SelectedIndex = -1;
        }

        public static Point GetPosition(this FrameworkElement elem, FrameworkElement related) {
            var transform = elem.TransformToVisual(null);
            Point pos = transform.TransformPoint(new Point(0, 0));
            return pos;
        }

        public static int MinIndex(this double []array) {
            int rs = 0;
            for (int i = 1; i < array.Length; ++i)
                if (array[i] < array[rs])
                    rs = i;
            return rs;
        }

        public static int MinIndex<T>(this IList<T>[] array) where T:IComparable {
            int rs = 0;
            for (int i = 1; i < array.Length; ++i) {
                if (array[i].Count == 0 || array[rs].Count == 0)
                    continue;
                if (array[i].Last().CompareTo(array[rs].Last()) < 0)
                    rs = i;
            }
            return rs;
        }

        // Search for the nearest but smaller value
        public static int BinarySearchSmaller<T>(this SortedSet<T> set, T target) where T: IComparable<T> {
            int ans = -1;
            int low = 0, hi = set.Count - 1;
            while(low <= hi) {
                int mid = (low + hi) / 2;
                if(set.ElementAt(mid).CompareTo(target) <= 0) {
                    low = mid + 1;
                    ans = mid;
                }
                else {
                    hi = mid - 1;
                }
            }
            return ans;
        }
    }

    public class ExtendedFrameworkElement : IComparable<ExtendedFrameworkElement> {
        public Rect Position { get; set; }
        public FrameworkElement Content;
        public int IndexInParent { get; set; }

        public ExtendedFrameworkElement() {
            Position = new Rect(0, 0, 0, 0);
        }

        public ExtendedFrameworkElement(Rect pos, FrameworkElement content = null, int index = -1) {
            Position = pos;
            Content = content;
            IndexInParent = index;
        }

        public int CompareTo(ExtendedFrameworkElement other) {
            return Position.Bottom.CompareTo(other.Position.Bottom);
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is ExtendedFrameworkElement))
                return false;
            return Content.Equals(((ExtendedFrameworkElement)obj).Content);
        }

        public override int GetHashCode() {
            return Content.GetHashCode();
        }
    }

    public class RelocateInfo {
        public FrameworkElement target { get; set; }
        public Point oldPosition { get; set; }
        public Point newPosition { get; set; }
        public int newColumn { get; set; }
        public int newColumnIndex { get; set; }
    }
}
