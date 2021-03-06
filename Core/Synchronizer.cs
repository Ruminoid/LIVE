﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Threading;
using Ruminoid.Common.Timing;
using Ruminoid.Common.Utilities;
using Unosquare.FFME.Common;

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

        public Position Position { get; } = new Position();

        private int _frameRate;

        public int FrameRate
        {
            get => _frameRate;
            set
            {
                _frameRate = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Core Data

        private Renderer _renderer;
        private Player _player;
        private Timer _timer;

        private Timer _stateResetTimer;
        private Timer _progressResetTimer;
        private bool _stateResetFlag;
        private bool _progressResetFlag;
        private KeyValuePair<string, WorkingState>? _stateResetState;
        private Tuple<ulong, ulong, int, int> _progressResetState;

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

        private int _memSize;

        public int MemSize
        {
            get => _memSize;
            set
            {
                _memSize = value;
                OnPropertyChanged();
            }
        }

        private int _minRenderFrame;

        public int MinRenderFrame
        {
            get => _minRenderFrame;
            set
            {
                _minRenderFrame = value;
                OnPropertyChanged();
            }
        }

        private int _maxRenderFrame;

        public int MaxRenderFrame
        {
            get => _maxRenderFrame;
            set
            {
                _maxRenderFrame = value;
                OnPropertyChanged();
            }
        }

        private int _threadCount;

        public int ThreadCount
        {
            get => _threadCount;
            set
            {
                _threadCount = value;
                OnPropertyChanged();
            }
        }

        private int _glyphMax;

        public int GlyphMax
        {
            get => _glyphMax;
            set
            {
                _glyphMax = value;
                OnPropertyChanged();
            }
        }

        private int _bitmapMax;

        public int BitmapMax
        {
            get => _bitmapMax;
            set
            {
                _bitmapMax = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Constructors

        public Synchronizer()
        {
            Sender.Current.Initialize();
        }

        public void Initialize()
        {
            if (Loaded) return;

            // Start Initialize
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Sender", WorkingState.Working));

            // Initialize Core
            _player = new Player();
            _player.MediaElement.MediaOpened += PlayerOnMediaOpened;
            _player.MediaElement.PositionChanged += PlayerOnPositionChanged;
            Position.OnPositionActiveChanged += PositionOnOnPositionActiveChanged;
            _player.MediaElement.Open(new Uri(AudioPath));
        }

        #endregion

        #region Methods

        private void PositionOnOnPositionActiveChanged()
        {
            _player.MediaElement.Seek(TimeSpan.FromMilliseconds(Position.Time));
            Task.Run(() => _renderer.Send((int) Position.Time));
        }

        private void PlayerOnMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            Position.Total = (long) e.Info.Duration.TotalMilliseconds;
            int audioLength = (int) e.Info.Duration.TotalMilliseconds;
            _renderer = new Renderer(
                AssPath,
                Width,
                Height,
                audioLength,
                MemSize,
                MinRenderFrame,
                MaxRenderFrame,
                FrameRate,
                ThreadCount,
                GlyphMax,
                BitmapMax);
            _renderer.StateChanged += RendererOnStateChanged;
            _renderer.ProgressChanged += RendererOnProgressChanged;
            
            _stateResetTimer = new Timer(1000)
            {
                AutoReset = false,
                Enabled = false
            };
            _stateResetTimer.Elapsed += (o, args) =>
            {
                if (_stateResetState is null) _stateResetFlag = false;
                else
                {
                    StateChanged?.Invoke(this, (KeyValuePair<string, WorkingState>) _stateResetState);
                    _stateResetState = null;
                    _stateResetFlag = true;
                    _stateResetTimer.Stop();
                    _stateResetTimer.Start();
                }
            };
            _progressResetTimer = new Timer(1000)
            {
                AutoReset = false,
                Enabled = false
            };
            _progressResetTimer.Elapsed += (o, args) =>
            {
                if (_progressResetState is null) _progressResetFlag = false;
                else
                {
                    ProgressChanged?.Invoke(this, _progressResetState);
                    _progressResetState = null;
                    _progressResetFlag = true;
                    _progressResetTimer.Stop();
                    _progressResetTimer.Start();
                }
            };

            _timer = new Timer(1000 / (double) FrameRate)
            {
                AutoReset = true,
                Enabled = false
            };
            _timer.Elapsed += TimerTick;
            Loaded = true;
            InitializeCompleted?.Invoke(this, EventArgs.Empty);
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Render", WorkingState.Working));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Sender", WorkingState.Completed));
        }

        private void TimerTick(object sender, ElapsedEventArgs e) => _renderer.Send((int) Position.Time);

        private void RendererOnStateChanged(object sender, KeyValuePair<string, WorkingState> e)
        {
            if (_stateResetFlag)
            {
                _stateResetState = e;
            }
            else
            {
                StateChanged?.Invoke(this, e);
                _stateResetFlag = true;
                _stateResetState = null;
                _stateResetTimer.Start();
            }
        }

        private void RendererOnProgressChanged(object sender, Tuple<ulong, ulong, int, int> e)
        {
            if (_progressResetFlag)
            {
                _progressResetState = e;
            }
            else
            {
                ProgressChanged?.Invoke(this, e);
                _progressResetFlag = true;
                _progressResetState = null;
                _progressResetTimer.Start();
            }
        }

        public event EventHandler InitializeCompleted;

        public event EventHandler<KeyValuePair<string, WorkingState>> StateChanged;
        public event EventHandler<Tuple<ulong, ulong, int, int>> ProgressChanged;

        private void PlayerOnPositionChanged(object sender, PositionChangedEventArgs e) =>
            Position.Time = (long) e.Position.TotalMilliseconds;

        public void JumpDuration(long duration)
        {
            Position.Time = (long) _player.JumpDuration(duration).TotalMilliseconds;
        }

        public void Release()
        {
            if (!Loaded) return;

            if (_timer.Enabled) _timer.Stop();
            _timer.Elapsed -= TimerTick;
            _timer.Dispose();
            Loaded = false;
            _player.MediaElement.MediaOpened -= PlayerOnMediaOpened;
            _player.MediaElement.PositionChanged -= PlayerOnPositionChanged;
            Position.OnPositionActiveChanged -= PositionOnOnPositionActiveChanged;
            Position.Time = 0;
            Position.Total = 0;
            _player.Dispose();
            _renderer.ProgressChanged -= RendererOnProgressChanged;
            _renderer.StateChanged -= RendererOnStateChanged;
            _renderer.Dispose();
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Memory", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Render", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Sender", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Purge", WorkingState.Unknown));
            GC.Collect();
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
                    _timer.Start();
                    _player.MediaElement.Play();
                }
                else
                {
                    _timer.Stop();
                    _player.MediaElement.Pause();
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
