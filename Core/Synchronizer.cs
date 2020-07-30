using System;
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

        private int _fps;

        public int FPS
        {
            get => _fps;
            set
            {
                _fps = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Core Data

        private Renderer _renderer;
        private Player _player;
        private Timer _timer;

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
            _renderer.Send((int) Position.Time, true);
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
                MaxRenderFrame);
            _renderer.StateChanged += RendererOnStateChanged;
            _timer = new Timer(1000 / (double) FPS)
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

        private void RendererOnStateChanged(object sender, KeyValuePair<string, WorkingState> e) =>
            StateChanged?.Invoke(this, e);

        public event EventHandler InitializeCompleted;

        public event EventHandler<KeyValuePair<string, WorkingState>> StateChanged;

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
            _renderer.StateChanged -= RendererOnStateChanged;
            _renderer.Dispose();
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Memory", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Render", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Sender", WorkingState.Unknown));
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Purge", WorkingState.Unknown));
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
