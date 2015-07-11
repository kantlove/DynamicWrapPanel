using System;
using System.ComponentModel;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace DynamicWrapPanel.Models {
    public class MyModel : INotifyPropertyChanged {
        private int _height;
        private string _message;
        private SolidColorBrush _color;

        private static Random randomizer = new Random();

        public int Height {
            get { return _height; }
            set {
                _height = value;
                NotifyPropertyChanged();
            }
        }

        public string Message {
            get { return _message; }
            set {
                _message = value;
                NotifyPropertyChanged();
            }
        }

        public SolidColorBrush BackgroundColor {
            get {
                return _color;
            }

            set {
                _color = value;
                NotifyPropertyChanged();
            }
        }

        public MyModel() {
            Height = randomizer.Next(50, 300);
            Message = randomizer.Next(0, 1000).ToString();
            BackgroundColor = new SolidColorBrush(Color.FromArgb(255, (byte)randomizer.Next(0, 256), (byte)randomizer.Next(0, 256), (byte)randomizer.Next(0, 256))); 
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] String propertyname = "") {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }
        }
    }
}
