using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DynamicControlGenerator;

namespace HNotifyIcon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        Lazy<Assembly> ToastNotificationLibrary = new Lazy<Assembly>(Compiler.Compile, true);

        private async void ToastTrigger_Click(object sender, RoutedEventArgs e)
        {
            await Task.Delay(5000);

            var asm = ToastNotificationLibrary.Value;
            var loaderType = asm.GetTypes().FirstOrDefault(type => type.Name == "ToastNotificationLoader");

            // Python ease without Python's danger!
            dynamic loader = Activator.CreateInstance(loaderType);

            loader.AppName = "DemoApp";
            loader.Title = "Hello World!";
            loader.Description = "Hello from .NET Standard library!";

            loader.PrimaryButtonText = "OK!";
            loader.SecondaryButtonText = "Great!";

            loader.NotificationClick += ((EventHandler)((s, a) =>
            {
                Application.Current.MainWindow.Activate();
                Debug.WriteLine("Notification Clicked");
            }));

            loader.PrimaryButtonClick += ((EventHandler)((s, a) =>
            {
                Debug.WriteLine("PrimaryButton Clicked");
            }));

            loader.SecondaryButtonClick += ((EventHandler)((s, a) =>
            {
                Debug.WriteLine("SecondaryButton Clicked");
            }));

            loader.CloseRequested += ((EventHandler)((s, a) =>
            {
                loader.HideAsync();
            }));

            var image = new BitmapImage(new Uri("https://raw.githubusercontent.com/AzureAms/ToastNotification.Uno/master/ToastNotificationDemo/ToastNotificationDemo.Shared/Assets/sample.png"));

            loader.ContentImageSource = image;
            
            await loader.ShowAsync();
            await Task.Delay(10000);
            await loader.HideAsync();
        }

        private void InspectButton_Click(object sender, RoutedEventArgs e)
        {
            Inspector.Inspect();
        }

        private async void DynamicToastTrigger_Click(object sender, RoutedEventArgs e)
        {
            var loader = new ToastNotificationLoader()
            {
                AppName = "DemoApp",
                Title = "Hello World!",
                Description = "Hello from Demo app!",
            };

            loader.NotificationClick += (s, a) =>
            {
                Debug.WriteLine("Notification Clicked");
            };

            loader.PrimaryButtonClick += (s, a) =>
            {
                Debug.WriteLine("PrimaryButton Clicked");
            };

            loader.SecondaryButtonClick += (s, a) =>
            {
                Debug.WriteLine("SecondaryButton Clicked");
            };

            loader.CloseRequested += (s, a) =>
            {
                loader.HideAsync();
            };

            var image = new BitmapImage(new Uri("https://raw.githubusercontent.com/AzureAms/ToastNotification.Uno/master/ToastNotificationDemo/ToastNotificationDemo.Shared/Assets/sample.png"));

            loader.ContentImageSource = image;

            await loader.ShowAsync();
            await Task.Delay(10000);
            await loader.HideAsync();
        }

        private void CompileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Compiler.Compile();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
