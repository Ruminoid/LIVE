using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ruminoid.Common.Helpers;

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
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);
        }

        #endregion

        #region CaptionBar Hook

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                Point p = new Point();
                int pInt = lParam.ToInt32();
                p.X = (pInt << 16) >> 16;
                p.Y = pInt >> 16;
                if (WndIn.PointFromScreen(p).Y > WndIn.ActualHeight) return IntPtr.Zero;
                Point rel = WndOut.PointFromScreen(p);
                if (rel.X >= 0 && rel.X <= WndOut.ActualWidth && rel.Y >= 0 && rel.Y <= WndOut.ActualHeight)
                {
                    return IntPtr.Zero;
                }
                handled = true;
                return new IntPtr(2);
            }

            return IntPtr.Zero;
        }

        private const int WM_NCHITTEST = 0x0084;

        #endregion
    }
}
