using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Ruminoid.Common.Timing;
using Unosquare.FFME.Common;

namespace Ruminoid.LIVE.Core
{
    public sealed class Synchronizer : INotifyPropertyChanged
    {
        #region Current

        public static Synchronizer Current { get; } = new Synchronizer();

        #endregion

        #region Synchro Data

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

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

        public Position Position { get; } = new Position();

        #endregion

        #region Core Data

        private Renderer _renderer;
        private Player _player;

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
            // Apply User Data
            AssPath = assPath;
            Width = width;
            Height = height;
            AudioPath = audioPath;

            // Calculate Synchro Data
            Name = $"{Path.GetFileNameWithoutExtension(_audioPath)} | {Process.GetCurrentProcess().Id}";

            // Initialize Core
            _player = new Player();
            _player.MediaElement.MediaOpened += PlayerOnMediaOpened;
            double audioLength = _player.MediaElement.Position.TotalMilliseconds;
            _player.MediaElement.PositionChanged += PlayerOnPositionChanged;
            Position.OnPositionActiveChanged += PositionOnOnPositionActiveChanged;
            _renderer = new Renderer(_name, _assPath, _width, _height, (int)audioLength);

            Loaded = true;
        }

        private void PositionOnOnPositionActiveChanged() => _player.MediaElement.Seek(TimeSpan.FromMilliseconds(Position.Time));

        private void PlayerOnMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            Position.Total = (long)e.Info.Duration.TotalMilliseconds;
        }

        private void PlayerOnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            Position.Time = (long)e.Position.TotalMilliseconds;
        }

        public void PreRender() => _renderer.PreRender();

        public void Release()
        {
            AssPath = "";
            Width = 0;
            Height = 0;
            AudioPath = "";
            Name = "";
            Loaded = false;
            _player.MediaElement.MediaOpened -= PlayerOnMediaOpened;
            _player.MediaElement.PositionChanged -= PlayerOnPositionChanged;
            Position.OnPositionActiveChanged -= PositionOnOnPositionActiveChanged;
            Position.Time = 0;
            Position.Total = 0;
            _player.Dispose();
            _renderer.Dispose();
        }

        #endregion

        #region Control Data

        private bool _playing;

        public bool Playing
        {
            get => _playing;
            set
            {
                if (_playing == value) return;
                _playing = value;
                OnPropertyChanged();
                if (value)
                    _player.MediaElement.Play();
                else
                    _player.MediaElement.Pause();
            }
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
