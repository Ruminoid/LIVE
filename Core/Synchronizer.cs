﻿using System;
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

        private Renderer _renderer;
        private Player _player;
        private DispatcherTimer _timer;

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
            Loaded = true;
            long audioLength = _player.AudioFile.Length;
            Position = new Position(audioLength);

            // Initialize Core
            _player = new Player(_audioPath);
            _renderer = new Renderer(_name, _assPath, _width, _height, (int) audioLength);
            _timer = new DispatcherTimer(
                TimeSpan.FromSeconds(1 / 60.0),
                DispatcherPriority.Normal,
                PositionUpdateTick,
                Dispatcher.CurrentDispatcher);
        }

        private void PositionUpdateTick(object sender, EventArgs e)
        {
            Position.Time = _player.AudioOutput.GetPosition();
            _renderer.Send((int) Position.Time);
        }

        public void PreRender() => _renderer.PreRender();

        public void Release()
        {
            _timer.Stop();
            _timer = null;
            AssPath = "";
            Width = 0;
            Height = 0;
            AudioPath = "";
            Name = "";
            Loaded = false;
            Position = new Position();
            _player.Dispose();
            _renderer.Dispose();
        }

        public void Seek(int milliSec)
        {

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
                {
                    _player.AudioOutput.Play();
                    _timer.Start();
                }
                else
                {
                    _player.AudioOutput.Pause();
                    _timer.Stop();
                }
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
