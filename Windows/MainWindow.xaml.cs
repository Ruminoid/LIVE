﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using Ruminoid.Common.Utilities;
using Ruminoid.LIVE.Core;
using Path = System.IO.Path;

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
                Wnd1, Wnd2, Wnd3, SettingsToggle
            };

            #endregion

            Synchronizer.Current.StateChanged += ChangeState;
            Synchronizer.Current.ProgressChanged += ChangeProgress;
            Synchronizer.Current.InitializeCompleted += InitializeCompleted;
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

        #region Utilities

        private void ChangeState(object sender, KeyValuePair<string, WorkingState> e)
        {
            Dispatcher.Invoke(() =>
            {
                ((SolidColorBrush) Resources[$"{e.Key}ControlBackgroundBrush"]).Color =
                    e.Value switch
                    {
                        WorkingState.Working => Colors.DarkOrange,
                        WorkingState.Completed => Colors.Green,
                        WorkingState.Failed => Colors.Red,
                        _ => Color.FromArgb(0xFF, 0x1B, 0x1B, 0x1C)
                    };
            });
        }

        private void ChangeProgress(object sender, Tuple<ulong, ulong, int, int> e)
        {
            if (e is null) return;
            Dispatcher.Invoke(() =>
            {
                MemoryProgressBar.Maximum = e.Item2;
                MemoryProgressBar.Value = e.Item1;
                RenderProgressBar.Maximum = e.Item4;
                RenderProgressBar.Value = e.Item3;
            });
        }

        #endregion

        #region Event Processor

        private void LoadToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            ToggleButton toggle = sender as ToggleButton;
            if (toggle is null) return;

            try
            {
                int width = 0,
                    height = 0,
                    minRenderFrame = 0,
                    maxRenderFrame = 0,
                    memSize = 0,
                    frameRate = 0,
                    threadCount = 0,
                    glyphMax = 0,
                    bitmapMax = 0;
                if (!int.TryParse(Config.Current.RenderWidth, out width) ||
                    !int.TryParse(Config.Current.RenderHeight, out height)) throw new Exception("渲染大小格式有误");
                if ((width % 2 != 0 || height % 2 != 0 || width <= 0 || height <= 0))
                    throw new Exception("渲染大小不正确");
                if (!int.TryParse(Config.Current.MemSize, out memSize)) throw new Exception("内存大小格式有误");
                if (!int.TryParse(Config.Current.MinRenderFrame, out minRenderFrame) ||
                    !int.TryParse(Config.Current.MaxRenderFrame, out maxRenderFrame))
                    throw new Exception("渲染缓冲时间格式不正确");
                if (memSize < 300) throw new Exception("预留内存过小");
                if (minRenderFrame <= 0 || maxRenderFrame <= 0) throw new Exception("渲染缓冲时间过小");
                if (!int.TryParse(Config.Current.FrameRate, out frameRate)) throw new Exception("帧率格式有误");
                if (frameRate < 1) throw new Exception("帧率过小");
                if (!int.TryParse(Config.Current.ThreadCount, out threadCount)) throw new Exception("线程数格式有误");
                if (threadCount < 1) throw new Exception("线程数过小");
                if (!int.TryParse(Config.Current.GlyphMax, out glyphMax)) throw new Exception("形状缓存格式有误");
                if (glyphMax < 1) throw new Exception("形状缓存数过小");
                if (!int.TryParse(Config.Current.BitmapMax, out bitmapMax)) throw new Exception("位图缓存格式有误");
                if (bitmapMax < 1) throw new Exception("位图缓存过小");
                if (!File.Exists(Synchronizer.Current.AudioPath)) throw new Exception("音频文件路径有误");
                if (!File.Exists(Synchronizer.Current.AssPath)) throw new Exception("ASS字幕文件路径有误");

                // All Checks Passed
                Wnd1.IsEnabled = false;
                Synchronizer.Current.Width = width;
                Synchronizer.Current.Height = height;
                Synchronizer.Current.MemSize = memSize;
                Synchronizer.Current.MinRenderFrame = minRenderFrame * frameRate;
                Synchronizer.Current.MaxRenderFrame = maxRenderFrame * frameRate;
                Synchronizer.Current.FrameRate = frameRate;
                Synchronizer.Current.ThreadCount = threadCount;
                Synchronizer.Current.GlyphMax = glyphMax;
                Synchronizer.Current.BitmapMax = bitmapMax;
                Synchronizer.Current.Initialize();
            }
            catch (Exception exception)
            {
                toggle.IsChecked = false;
                MessageBox.Show(
                    $"{exception.Message}。请检查您的配置。",
                    "配置错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK);
            }
        }

        private void InitializeCompleted(object sender, EventArgs e)
        {
            Wnd1.IsEnabled = true;
        }

        private void LoadToggle_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Synchronizer.Current.Release();
        }

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

        private void SpoutInstallButtonBase_OnClick(object sender, RoutedEventArgs e) =>
            Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Libraries/OBS_Spout_Installer.exe"));

        #endregion

        private void CollapseStoryBoard_OnCompleted(object sender, EventArgs e)
        {
            Width = 800;
            Height = 230;
        }
    }
}
