using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ruminoid.LIVE.Windows
{
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region KeyEvent Processor

        private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ignore
        }

        private void MainWindow_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            // Ignore
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
