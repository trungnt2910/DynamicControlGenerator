using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HNotifyIcon
{
    /// <summary>
    /// Interaction logic for ToastNotification.xaml
    /// </summary>
    public partial class ToastNotification : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty AppNameProperty =
            DependencyProperty.Register(
                "AppName", typeof(string), typeof(ToastNotification)
                );
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                "Title", typeof(string), typeof(ToastNotification)
                );
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(
                "Description", typeof(string), typeof(ToastNotification)
                );
        public static readonly DependencyProperty TimeProperty =
            DependencyProperty.Register(
                "Time", typeof(string), typeof(ToastNotification)
                );

        public static readonly DependencyProperty PrimaryButtonTextProperty =
            DependencyProperty.Register(
                "PrimaryButtonText", typeof(string), typeof(ToastNotification),
                new PropertyMetadata() { DefaultValue = "OK" }
                );
        public static readonly DependencyProperty SecondaryButtonTextProperty =
            DependencyProperty.Register(
                "SecondaryButtonText", typeof(string), typeof(ToastNotification),
                new PropertyMetadata() { DefaultValue = "Cancel" }
                );

        public static readonly DependencyProperty ContentImageSourceProperty =
            DependencyProperty.Register(
                "ContentImageSource", typeof(ImageSource), typeof(ToastNotification)
                );

        public static readonly DependencyProperty AppIconImageSourceProperty =
            DependencyProperty.Register(
                "AppIconImageSource", typeof(ImageSource), typeof(ToastNotification)
                );

        public static readonly DependencyProperty PrimaryButtonClickCommandProperty =
            DependencyProperty.Register(
                "PrimaryButtonClickCommand", typeof(RelayCommand), typeof(ToastNotification)
                );

        public static readonly DependencyProperty SecondaryButtonClickCommandProperty =
            DependencyProperty.Register(
                "SecondaryButtonClickCommand", typeof(RelayCommand), typeof(ToastNotification)
                );

        public static readonly DependencyProperty CloseButtonClickCommandProperty =
            DependencyProperty.Register(
                "CloseButtonClickCommand", typeof(RelayCommand), typeof(ToastNotification)
                );

        public string AppName 
        {
            get => (string)GetValue(AppNameProperty);
            set => SetValue(AppNameProperty, value);
        }
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }
        public string Time
        {
            get => (string)GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public string PrimaryButtonText
        {
            get => (string)GetValue(PrimaryButtonTextProperty);
            set => SetValue(PrimaryButtonTextProperty, value);
        }
        public string SecondaryButtonText
        {
            get => (string)GetValue(SecondaryButtonTextProperty);
            set => SetValue(SecondaryButtonTextProperty, value);
        }

        public ImageSource ContentImageSource
        {
            get => (ImageSource)GetValue(ContentImageSourceProperty);
            set => SetValue(ContentImageSourceProperty, value);
        }

        public ImageSource AppIconImageSource
        {
            get => (ImageSource)GetValue(AppIconImageSourceProperty);
            set => SetValue(AppIconImageSourceProperty, value);
        }

        public RelayCommand PrimaryButtonClickCommand
        {
            get => (RelayCommand)GetValue(PrimaryButtonClickCommandProperty);
            private set => SetValue(PrimaryButtonClickCommandProperty, value);
        }

        public RelayCommand SecondaryButtonClickCommand
        {
            get => (RelayCommand)GetValue(SecondaryButtonClickCommandProperty);
            private set => SetValue(SecondaryButtonClickCommandProperty, value);
        }

        public RelayCommand CloseButtonClickCommand
        {
            get => (RelayCommand)GetValue(CloseButtonClickCommandProperty);
            private set => SetValue(CloseButtonClickCommandProperty, value);
        }

        public ToastNotification()
        {
            DataContext = this;
            Time = DateTime.Now.ToString("h:mm tt");
            InitializeComponent();
            MouseLeftButtonDown += ToastNotification_MouseLeftButtonDown;
            MouseLeftButtonUp += ToastNotification_MouseLeftButtonUp;
            PrimaryButtonClickCommand = new RelayCommand((o) => IsEnabled, (o) => PrimaryButton_Click(this, null));
            SecondaryButtonClickCommand = new RelayCommand((o) => IsEnabled, (o) => SecondaryButton_Click(this, null));
            CloseButtonClickCommand = new RelayCommand(o => IsEnabled, (o) => CloseButton_MouseLeftButtonDown(this, null));
        }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raises the PropertyChange event for the property specified
        /// </summary>
        /// <param name="propertyName">Property name to update. Is case-sensitive.</param>
        public virtual void RaisePropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion // INotifyPropertyChanged Members

        public event EventHandler CloseRequested;
        public event EventHandler PrimaryButtonClick;
        public event EventHandler SecondaryButtonClick;

        public event EventHandler NotificationClick;

        private void CloseButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CloseRequested?.Invoke(this, null);
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            PrimaryButtonClick?.Invoke(this, null);
            CloseRequested?.Invoke(this, null);
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            SecondaryButtonClick?.Invoke(this, null);
            CloseRequested?.Invoke(this, null);
        }

        private bool _mouseWasDown = false;

        private void ToastNotification_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseWasDown = true;
        }

        private void ToastNotification_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseWasDown)
            {
                NotificationClick?.Invoke(this, null);
                CloseRequested?.Invoke(this, null);
            }
            _mouseWasDown = false;
        }

        private Popup _popup;
        private object _locker = new object();

        public Task ShowAsync()
        {
            lock (_locker)
            {
                if (_popup != null)
                {
                    return Task.CompletedTask;
                }

                _popup = new Popup()
                {
                    AllowsTransparency = true,
                    Child = this,
                    Placement = PlacementMode.Absolute,
                    StaysOpen = true,
                };
            }

            var contentImage = FindName("ContentImage") as Image;
            if (ContentImageSource == null)
            {
                contentImage.Visibility = Visibility.Collapsed;
            }

            var tcs = new TaskCompletionSource<object>();

            var rect = Interop.GetWorkArea();

            Measure(new Size { Height = rect.Height, Width = rect.Width });

            // Just need a very big value for horizontal.
            _popup.HorizontalOffset = rect.Right + DesiredSize.Width;
            _popup.VerticalOffset = rect.Bottom - DesiredSize.Height - 10;

            Debug.WriteLine(_popup.HorizontalOffset);
            Debug.WriteLine(_popup.VerticalOffset);

            _popup.IsOpen = true;

            // This seems to work, because:
            // When setting Margin to a huge negative size, the whole Control 
            // becomes invisible. This makes the Popup accept a big offet.
            // When the Margin increases, the visible portion does, too,
            // and the Popup responds by shifting everything left.
            Margin = new Thickness(0, 0, -DesiredSize.Width, 0);
            var thicknessAnimation = new ThicknessAnimation
            {
                From = Margin,
                To = new Thickness(0, 0, 0, 0),
                Duration = new Duration(TimeSpan.FromSeconds(0.1))
            };

            thicknessAnimation.Completed += AnimationComplete;
            BeginAnimation(MarginProperty, thicknessAnimation);

            return tcs.Task;

            async void AnimationComplete(object sender, EventArgs args)
            {
                thicknessAnimation.Completed -= AnimationComplete;
                Visibility = Visibility.Visible;
                // LMAO dunno why.
                await Task.Delay(100);
                _popup.HorizontalOffset = rect.Right - ActualWidth - 10;
                _popup.VerticalOffset = rect.Bottom - ActualHeight - 10;

                tcs.SetResult(null);
            };
        }

        public Task HideAsync()
        {
            lock (_locker)
            {
                if (_popup != null)
                {
                    var tcs = new TaskCompletionSource<object>();
                    var rect = Interop.GetWorkArea();
                    _popup.HorizontalOffset = rect.Right + DesiredSize.Width + 100;
                    var thicknessAnimation = new ThicknessAnimation
                    {
                        From = Margin,
                        To = new Thickness(0, 0, -Width, 0),
                        Duration = new Duration(TimeSpan.FromSeconds(0.2))
                    };

                    thicknessAnimation.Completed += AnimationComplete;
                    BeginAnimation(MarginProperty, thicknessAnimation);

                    return tcs.Task;

                    void AnimationComplete(object sender, EventArgs args)
                    {
                        thicknessAnimation.Completed -= AnimationComplete;
                        Visibility = Visibility.Collapsed;
                        _popup.IsOpen = false;
                        _popup.Child = null;
                        _popup = null;
                        tcs.SetResult(null);
                    }
                }
                else
                {
                    return Task.CompletedTask;
                }
            }
        }

        public class RelayCommand : ICommand
        {
            private readonly Predicate<object> _canExecute;
            private readonly Action<object> _execute;

            public RelayCommand(Predicate<object> canExecute, Action<object> execute)
            {
                _canExecute = canExecute;
                _execute = execute;
            }

            public event EventHandler CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }
        }
    }
}
