using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Ruminoid.LIVE.Core;

namespace Ruminoid.LIVE.Windows
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region KeyEvent Processor

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) return;
            if (e.Key == Key.L ||
                e.Key == Key.Space ||
                e.Key == Key.Left ||
                e.Key == Key.Right) e.Handled = true;
            switch (e.Key)
            {
                case Key.L:
                    if (Synchronizer.Current.Loaded) LoadToggle_OnUnchecked(Wnd1, new RoutedEventArgs());
                    else LoadToggle_OnChecked(Wnd1, new RoutedEventArgs());
                    break;
                case Key.Space:
                    if (Synchronizer.Current.Loaded) Synchronizer.Current.Playing = !Synchronizer.Current.Playing;
                    break;
                case Key.Left:
                    if (Synchronizer.Current.Loaded) Synchronizer.Current.JumpDuration(-1000);
                    break;
                case Key.Right:
                    if (Synchronizer.Current.Loaded) Synchronizer.Current.JumpDuration(+1000);
                    break;
            }
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
