using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using Ruminoid.Common.Helpers;
using Ruminoid.LIVE.Core;

namespace Ruminoid.LIVE.Windows
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            Closing += OnClosing;

            Closed += (sender, args) => Application.Current.Shutdown(0);
        }

        #region Closing

        private void OnClosing(object sender, CancelEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "确定退出吗？",
                "退出",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (result == MessageBoxResult.No) e.Cancel = true;
            if (e.Cancel) return;
            ConfigHelper<Config>.SaveConfig(Config.Current);
        }

        #endregion

        #region Loaded

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            #region Add Command Bindings

            CommandBindings.Add(new CommandBinding(
                ApplicationCommands.Close,
                (o, args) =>
                {
                    args.Handled = true;
                    Close();
                },
                (o, args) => args.CanExecute = true));

            #endregion

            #region Add CaptionBar Hook

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

            wndList = new List<FrameworkElement>
            {
                Wnd1, Wnd2, Wnd3
            };

            #endregion
        }

        #endregion

        #region CaptionBar Hook

        private List<FrameworkElement> wndList;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                if (wndList is null) return IntPtr.Zero;
                Point p = new Point();
                int pInt = lParam.ToInt32();
                p.X = (pInt << 16) >> 16;
                p.Y = pInt >> 16;
                if (WndCaption.PointFromScreen(p).Y > WndCaption.ActualHeight) return IntPtr.Zero;
                foreach (FrameworkElement element in wndList)
                {
                    Point rel = element.PointFromScreen(p);
                    if (rel.X >= 0 && rel.X <= element.ActualWidth && rel.Y >= 0 && rel.Y <= element.ActualHeight)
                    {
                        return IntPtr.Zero;
                    }
                }
                handled = true;
                return new IntPtr(2);
            }

            return IntPtr.Zero;
        }

        private const int WM_NCHITTEST = 0x0084;

        #endregion

        #region Event Processor

        private void LoadToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggle = sender as ToggleButton;
            string err = "";
            int width = 0, height = 0;
            if (!int.TryParse(Config.Current.RenderWidth, out width) ||
                !int.TryParse(Config.Current.RenderHeight, out height)) err = "渲染大小格式有误";
            if (err == "" && (width % 2 != 0 || height % 2 != 0 || width <= 0 || height <= 0))
                err = "渲染大小不正确";
            if (!File.Exists(Synchronizer.Current.AudioPath)) err = "音频文件路径有误";
            if (!File.Exists(Synchronizer.Current.AssPath)) err = "ASS字幕文件路径有误";
            if (err != "")
            {
                toggle.IsChecked = false;
                MessageBox.Show(
                    $"{err}。请检查您的配置。",
                    "配置错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
                return;
            }

            Wnd1.IsEnabled = false;
            Synchronizer.Current.Width = width;
            Synchronizer.Current.Height = height;
            ((SolidColorBrush) Resources["RenderControlBackgroundBrush"]).Color = Colors.OrangeRed;
            Synchronizer.Current.Initialize();
            ((SolidColorBrush) Resources["RenderControlBackgroundBrush"]).Color = Colors.Green;
            Wnd1.IsEnabled = true;
        }

        private void LoadToggle_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Synchronizer.Current.Release();
            ((SolidColorBrush) Resources["RenderControlBackgroundBrush"]).Color =
                Color.FromArgb(0xFF, 0x1B, 0x1B, 0x1C);
        }

        #endregion

        private void ChooseFileButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            CommonOpenFileDialog dialog = new CommonOpenFileDialog
            {
                Title = button?.Tag switch
                {
                    "AssPath" => "选择ASS字幕文件",
                    "AudioPath" => "选择音频文件",
                    _ => "选择文件"
                },
                DefaultDirectory = Environment.CurrentDirectory,
                IsFolderPicker = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                Multiselect = false,
                Filters =
                {
                    button?.Tag switch
                    {
                        "AssPath" => new CommonFileDialogFilter("ASS字幕", ".ass"),
                        "AudioPath" => new CommonFileDialogFilter("媒体文件", ".*"),
                        _ => new CommonFileDialogFilter()
                    }
                }
            };

            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;

            switch (button?.Tag)
            {
                case "AssPath":
                    Synchronizer.Current.AssPath = dialog.FileName;
                    break;
                case "AudioPath":
                    Synchronizer.Current.AudioPath = dialog.FileName;
                    break;
            }
        }
    }
}
