using DynamicWrapPanel.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace DynamicWrapPanel.Views {
    public sealed partial class Wrapper : UserControl {

        private ObservableCollection<FrameworkElement> Children;

        private FrameworkElement selectingElement;
        private FrameworkElement dummyMovingElement; // element moving in the canvas
        private FrameworkElement emptyPlaceholderElement; // a placeholder to create an empty space in the panel
        public int emptyPlaceholderIndex = -1;

        public bool IsSelecting = false;
        public IEnumerable<Object> ItemsSource {
            get { return (IEnumerable<Object>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }
        public DataTemplate ItemTemplate { get; set; }
        public DynamicWrapPanel ItemPanel;
        public ScrollViewer ListScrollViewer;

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        "ItemSource", typeof(IEnumerable<Object>), typeof(Wrapper), new PropertyMetadata(0, ItemsSourceChangedCallback));

        #region ItemsSource methods
        /// <summary>
        /// Custom behavior when adding, removing items
        /// </summary>
        private static void ItemsSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue == null || e.NewValue == e.OldValue) {
                return;
            }

            Wrapper wrapPanel = d as Wrapper;

            if (wrapPanel == null)
                return;

            var obsList = e.NewValue as INotifyCollectionChanged;

            if (obsList != null) {
                obsList.CollectionChanged += (sender, eventArgs) => {
                    switch (eventArgs.Action) {
                        case NotifyCollectionChangedAction.Remove:
                            foreach (var oldItem in eventArgs.OldItems) {
                                for (int i = 0; i < wrapPanel.Children.Count; i++) {
                                    var fxElement = wrapPanel.Children[i] as FrameworkElement;
                                    if (fxElement == null || fxElement.DataContext != oldItem) continue;
                                    wrapPanel.RemoveAt(i);
                                }
                            }

                            break;

                        case NotifyCollectionChangedAction.Add:
                            foreach (var newItem in eventArgs.NewItems)
                                wrapPanel.CreateItem(newItem);
                            break;
                    }
                };
            }

            wrapPanel.Bind();
        }

        /// <summary>
        /// Create and add items from their DataTemplate
        /// </summary>
        private void Bind() {
            if (this.ItemsSource == null)
                return;

            this.Children.Clear();

            foreach (object item in this.ItemsSource)
                this.CreateItem(item);
        }

        private FrameworkElement CreateItem(object item) {
            FrameworkElement element = ItemTemplate.LoadContent() as FrameworkElement;
            if (element == null)
                return null;

            element.DataContext = item;
            this.Children.Add(element);

            element.Holding += Element_Holding;

            return element;
        }

        private void RemoveAt(int index) {
            this.Children.RemoveAt(index);
        }
        #endregion

        public Wrapper() {
            this.InitializeComponent();

            Children = new ObservableCollection<FrameworkElement>();

            this.listBox.ItemsSource = Children;
            this.listBox.SelectionChanged += ListBox_SelectionChanged;

            Loaded += Wrapper_Loaded;
        }

        private void Wrapper_Loaded(object sender, RoutedEventArgs e) {
            this.ListScrollViewer = Library.GetScrollViewer(this.listBox);
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // When the manipulation on an item begins, listBox selection will be 
            // disabled, therefore it will have selectedIndex = -1
            if (!IsSelecting || this.listBox.SelectedIndex < 0)
                return;

            FrameworkElement element = (FrameworkElement)(this.listBox.SelectedItem);

            // Selection effect
            element.Opacity = 0.4;

            // Take all touch input
            element.ManipulationMode = ManipulationModes.All;

            element.ManipulationStarting += Element_ManipulationStarting;
            element.ManipulationDelta += Element_ManipulationDelta;
            element.ManipulationCompleted += Element_ManipulationCompleted;

            Status("Selected at " + listBox.SelectedIndex);
        }

        private void Status(string text = "") {
            this.tbStatus.Text = text;
        }

        private void Element_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e) {
            Status("Completed!");

            FrameworkElement element = (FrameworkElement)sender;

            // Give back touch input to ListBox
            element.ManipulationMode = ManipulationModes.System;

            // Remove all event handlers to turn off manipulation
            element.ManipulationStarting -= Element_ManipulationStarting;
            element.ManipulationDelta -= Element_ManipulationDelta;
            element.ManipulationCompleted -= Element_ManipulationCompleted;

            int indexToInsert = this.ItemPanel.GetEmptyCurrentIndex();
            
            // Remove the dummy
            MainCanvas.Children.Clear();
            this.dummyMovingElement = null;

            this.ItemPanel.SetManipulation(false);

            // Re-show the selecting element in the wrap panel
            // but move it to where the empty space is 
            this.ItemPanel.UnexcludeItem(this.selectingElement);
            this.Children.Remove(this.selectingElement);
            this.Children.Insert(indexToInsert, this.selectingElement);
            this.selectingElement.Opacity = 1;
            this.listBox.EmptySelection();

            // Remove the empty placeholder
            //if (this.Children.IndexOf(this.emptyPlaceholderElement) >= 0) {
            //    this.Children.Remove(this.emptyPlaceholderElement);
            //    this.emptyPlaceholderIndex = -1;
            //}
        }

        private void Element_Holding(object sender, HoldingRoutedEventArgs e) {
            
        }

        private void Element_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e) {
            
            double deltaX = e.Delta.Translation.X, deltaY = e.Delta.Translation.Y;
            double currentX = Canvas.GetLeft(this.dummyMovingElement) + deltaX;
            double currentY = Canvas.GetTop(this.dummyMovingElement) + deltaY;



            // Move the dummy
            Canvas.SetTop(this.dummyMovingElement, currentY);
            Canvas.SetLeft(this.dummyMovingElement, currentX);

            if (Math.Abs(e.Velocities.Linear.X) > 0.5 || Math.Abs(e.Velocities.Linear.Y) > 0.5) // moving too fast!
                return;

            // Insert an empty element as a space to this wrap panel
            Point G = new Point(currentX + this.dummyMovingElement.Width / 2, currentY + this.dummyMovingElement.Height / 2);
            int indexToInsert = this.ItemPanel.SetEmptyPosition(G);
            //InsertEmptyElement(indexToInsert);

            Status(string.Format("Insert to {0} - Offset {1} - Velocity {2}", indexToInsert, e.Cumulative.Translation, e.Velocities.Linear));
        }

        private void Element_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e) {
            int id = this.listBox.SelectedIndex;
            Status("Dragging element " + id);

            this.selectingElement = (FrameworkElement)sender;
            this.dummyMovingElement = DummyFactory.Create(this.selectingElement);
            //this.emptyPlaceholderElement = DummyFactory.CreateEmpty(this.selectingElement);

            /*
             * Create a dummy and add it to the canvas at the same position
             */
            Point pos = GetPosition(this.selectingElement);
            this.MainCanvas.Children.Add(this.dummyMovingElement);
            Canvas.SetLeft(this.dummyMovingElement, pos.X);
            Canvas.SetTop(this.dummyMovingElement, pos.Y);

            // Set empty placeholder size
            this.ItemPanel.SetEmptySize(this.selectingElement.DesiredSize);

            // Hide the selecting item, but still receive touch
            this.ItemPanel.ExcludeItem(this.selectingElement);

            // Enable Manipulation mod in wrappanel
            this.ItemPanel.SetManipulation(true);
        }

        public void EnableSelection() {
            if (IsSelecting)
                return;

            listBox.Background = new SolidColorBrush(Colors.DarkKhaki);
            listBox.SelectionMode = SelectionMode.Single;
            listBox.EmptySelection();

            //Style selectStyle = Resources["ListBoxItemMultiSelectStyle"] as Style;
            //listBox.ItemContainerStyle = selectStyle;

            this.IsSelecting = true;
        }

        public void DisableSelection() {
            this.IsSelecting = false;
            this.selectingElement = null;
            this.listBox.EmptySelection();

            listBox.Background = new SolidColorBrush(Colors.Transparent);

            Style normalStyle = Resources["ListBoxItemNormalStyle"] as Style;
            listBox.ItemContainerStyle = normalStyle;
        }

        private void InsertEmptyElement(int index) {
            if(this.emptyPlaceholderIndex >= 0) {
                this.Children.RemoveAt(this.emptyPlaceholderIndex);
            }
            this.Children.Insert(index, this.emptyPlaceholderElement);
            this.emptyPlaceholderIndex = index;
        }

        private Point GetPosition(FrameworkElement element) {
            return element.TransformToVisual(this.listBox).TransformPoint(new Point(0, 0));
        }

        private void ListBoxItemPanelLoaded(object sender, RoutedEventArgs e) {
            this.ItemPanel = (sender as DynamicWrapPanel);
        }
    }
}
