using DynamicWrapPanel.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace DynamicWrapPanel
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        ObservableCollection<MyModel> data = new ObservableCollection<MyModel>();
        private DispatcherTimer dispatcherTimer;

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.Loaded += MainPage_Loaded;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, object e) {
            ulong AppMemoryUsageUlong = MemoryManager.AppMemoryUsage;
            AppMemoryUsageUlong /= 1024; // convert to KB
            this.tbStatus.Text = AppMemoryUsageUlong.ToString("N") + " KB";
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e) {
            //WrapPanel.HorizontalMargin = 5;
            //WrapPanel.VerticalMargin = 5;

            int n = 5;
            for(int i = 0; i < n; ++i) {
                data.Add(new MyModel());
            }

            MyListBox.ItemsSource = data;

        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e) {
            data.Add(new MyModel());
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e) {
            if(data.Count > 0) {
                data.RemoveAt(new Random().Next(0, data.Count));
            }
        }

        private void BtnNoSelect_Click(object sender, RoutedEventArgs e) {
            if (MyListBox.IsSelecting) {
                MyListBox.DisableSelection();

                (sender as Button).Content = "Select";
            }
            else {
                MyListBox.EnableSelection();

                (sender as Button).Content = "No Select";
            }
        }
    }
}
