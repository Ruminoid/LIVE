using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Ruminoid.Common.Timing;

namespace Ruminoid.LIVE.Core
{
    public sealed class Synchronizer : INotifyPropertyChanged
    {
        #region Current

        public static Synchronizer Current { get; } = new Synchronizer();

        #endregion

        #region Synchro Data

        private bool _loaded;

        public bool Loaded
        {
            get => _loaded;
            set
            {
                _loaded = value;
                OnPropertyChanged();
            }
        }

        private Position _position;

        public Position Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Core Data



        #endregion

        #region User Data

        private string _assPath = "";

        public string AssPath
        {
            get => _assPath;
            set
            {
                _assPath = value;
                OnPropertyChanged();
            }
        }

        private int _width;

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged();
            }
        }

        private int _height;

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged();
            }
        }

        private string _audioPath = "";

        public string AudioPath
        {
            get => _audioPath;
            set
            {
                _audioPath = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public void Initialize(
            string assPath,
            int width,
            int height,
            string audioPath)
        {
            // Inject User Data
            _assPath = assPath;
            _width = width;
            _height = height;
            _audioPath = audioPath;

            // Initialize Core
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
