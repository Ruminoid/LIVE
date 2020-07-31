﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Ruminoid.Common.Renderer.Core;
using Ruminoid.Common.Renderer.LibAss;
using Ruminoid.Common.Renderer.Utilities;
using Ruminoid.Common.Utilities;

namespace Ruminoid.LIVE.Core
{
    class Renderer : IDisposable
    {
        #region Core Data

        private AssRenderCore _rendererCore;
        private MemoryMonitor _memoryMonitor;
        private DispatcherTimer _timer;
        private FrameAdaptor _frameAdaptor;

        #endregion

        #region Worker Data

        private RuminoidImageT[] _renderedData;

        private BackgroundWorker _purgeWorker;
        private BackgroundWorker _renderWorker;

        private int _purgeIndex;
        private int _renderIndex;
        private int _playerIndex;

        private object _renderLocker;

        #endregion

        #region User Data

        private int _width;
        private int _height;
        private int _minRenderFrame;
        private int _maxRenderFrame;

        #endregion

        #region Display Data

        private WorkingState _renderState;

        private WorkingState RenderState
        {
            get => _renderState;
            set
            {
                if (_renderState != value)
                    StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Render", value));
                _renderState = value;
            }
        }

        #endregion

        #region Constructor

        public Renderer(
            string assPath,
            int width,
            int height,
            int total,
            int memSize,
            int minRenderFrame,
            int maxRenderFrame,
            int frameRate)
        {
            // Initialize User Data
            _width = width;
            _height = height;
            _minRenderFrame = minRenderFrame;
            _maxRenderFrame = maxRenderFrame;

            // Initialize Core Data
            _frameAdaptor = new FrameAdaptor(frameRate, total);
            _renderedData = new RuminoidImageT[_frameAdaptor.TotalFrame];

            // Initialize Worker Data
            _purgeIndex = 0;
            _renderIndex = 0;
            _playerIndex = 0;

            _renderLocker = new object();

            // Initialize Core
            _memoryMonitor = new MemoryMonitor(memSize);
            _memoryMonitor.StateChanged += MemoryMonitorOnStateChanged;
            _rendererCore = new AssRenderCore(File.ReadAllText(assPath), width, height);
            Sender.Current.Initialize((uint) _width, (uint) _height);

            // Initialize Worker
            _purgeWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _purgeWorker.DoWork += DoPurgeWork;

            _renderWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _renderWorker.DoWork += DoRenderWork;
            TriggerRender(0, true);

            _timer = new DispatcherTimer(
                TimeSpan.FromSeconds(1),
                DispatcherPriority.Normal,
                TimerTick,
                Dispatcher.CurrentDispatcher);
            _timer.Start();
        }

        #endregion

        #region Methods

        public void Send(int milliSec, bool seek = false)
        {
            int frameIndex = _frameAdaptor.GetFrameIndex(milliSec);
            RuminoidImageT imageRaw = _renderedData[frameIndex];
            if (!(imageRaw is null))
                Sender.Current.Send(AssRenderCore.Decode(imageRaw));
            _playerIndex = frameIndex;
            if (seek)
            {
                RenderState = WorkingState.Failed;
                TriggerRender(frameIndex, true);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            lock (_renderLocker)
            {
                if (_playerIndex <= _renderIndex)
                {
                    if (_renderIndex - _playerIndex < _minRenderFrame)
                    {
                        TriggerRender(_playerIndex, false);
                    }
                    else if (_renderIndex - _playerIndex < _maxRenderFrame)
                    {
                        RenderState = WorkingState.Working;
                    }
                    else
                    {
                        _renderWorker.CancelAsync();
                        RenderState = WorkingState.Completed;
                    }
                }
                else
                {
                    RenderState = WorkingState.Failed;
                }
            }
        }

        private void TriggerPurge()
        {
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Purge", WorkingState.Working));
            if (!_purgeWorker.IsBusy) _purgeWorker.RunWorkerAsync();
        }

        private void CancelPurge()
        {
            if (_purgeWorker.IsBusy) _purgeWorker.CancelAsync();
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Purge", WorkingState.Unknown));
        }

        private void TriggerRender(int frameIndex, bool restart)
        {
            if (restart)
            {
                _renderWorker.CancelAsync();
                while (_renderWorker.IsBusy)
                {
                    // Ignore
                }
            }
            if (!_renderWorker.IsBusy) _renderWorker.RunWorkerAsync(frameIndex);
        }

        #endregion

        #region Workers

        private void DoPurgeWork(object sender, DoWorkEventArgs e)
        {
            while (!_purgeWorker.CancellationPending)
            {
                if (_purgeIndex >= _playerIndex && _purgeIndex < _playerIndex + _maxRenderFrame + 10)
                    _purgeIndex = _playerIndex + _maxRenderFrame + 10;
                if (_purgeIndex >= _frameAdaptor.TotalFrame)
                {
                    Thread.Sleep(5000);
                    _purgeIndex = 0;
                }
                _renderedData[_purgeIndex].Dispose();
                _renderedData[_purgeIndex] = null;
                _purgeIndex++;
            }

            e.Cancel = true;
        }

        private void DoRenderWork(object sender, DoWorkEventArgs e)
        {
            lock (_renderLocker) _renderIndex = (int) e.Argument; // frameIndex

            while (!_renderWorker.CancellationPending && _renderIndex < _frameAdaptor.TotalFrame)
            {
                lock (_renderLocker)
                {
                    for (int i = _renderIndex; i < _renderIndex + 4; i++)
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            int r = (int) state;
                            if (_renderedData[r] is null)
                            {
                                RuminoidImageT data = _rendererCore.Render(_frameAdaptor.GetMilliSec(_renderIndex));
                                lock (_renderedData)
                                {
                                    _renderedData[r] = data;
                                }
                            }
                        }, i);
                    _renderIndex += 4;
                }
            }

            e.Cancel = true;
        }

        #endregion

        #region Event Triggers & Processors

        private void MemoryMonitorOnStateChanged(object sender, WorkingState e)
        {
            StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Memory", e));
            switch (e)
            {
                case WorkingState.Completed:
                    CancelPurge();
                    break;
                case WorkingState.Failed:
                    TriggerPurge();
                    break;
            }
        }

        public event EventHandler<KeyValuePair<string, WorkingState>> StateChanged;

        #endregion

        #region Dispose

        public void Dispose()
        {
            _timer.Stop();
            _timer.Tick -= TimerTick;
            _timer = null;
            _renderWorker.CancelAsync();
            _renderWorker.DoWork -= DoRenderWork;
            _renderWorker.Dispose();
            _purgeWorker.CancelAsync();
            _purgeWorker.DoWork -= DoPurgeWork;
            _purgeWorker.Dispose();
            _renderLocker = null;
            _memoryMonitor.StateChanged -= MemoryMonitorOnStateChanged;
            _memoryMonitor?.Dispose();
            _rendererCore?.Dispose();
            for (int i = 0; i < _frameAdaptor.TotalFrame; i++)
                if (!(_renderedData[i] is null))
                    _renderedData[i].Dispose();
        }

        #endregion
    }
}
