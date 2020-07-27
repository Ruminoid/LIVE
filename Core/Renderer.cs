using System;
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
using Ruminoid.Common.Renderer.LibAss;
using Ruminoid.Common.Utilities;
using static Ruminoid.Common.Renderer.LibAss.LibASSInterop;

namespace Ruminoid.LIVE.Core
{
    class Renderer : IDisposable
    {
        #region Core Data

        private RendererCore _rendererCore;
        private Sender _sender;
        private MemoryMonitor _memoryMonitor;
        private DispatcherTimer _timer;

        #endregion

        #region Worker Data

        private IntPtr[] _renderedData;

        private BackgroundWorker _purgeWorker;
        private BackgroundWorker _renderWorker;

        private int _purgeIndex;
        private int _renderIndex;
        private int _playerIndex;

        #endregion

        #region User Data

        private int _width;
        private int _height;
        private int _total;
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
            string name,
            string assPath,
            int width,
            int height,
            int total,
            int memSize,
            int minRenderFrame,
            int maxRenderFrame)
        {
            // Initialize User Data
            _width = width;
            _height = height;
            _total = total;
            _minRenderFrame = minRenderFrame;
            _maxRenderFrame = maxRenderFrame;

            // Initialize Core Data
            _renderedData = new IntPtr[total];
            for (int i = 0; i < total; i++) _renderedData[i] = IntPtr.Zero;

            // Initialize Worker Data
            _purgeIndex = 0;
            _renderIndex = 0;
            _playerIndex = 0;

            // Initialize Core
            _memoryMonitor = new MemoryMonitor(memSize);
            _memoryMonitor.StateChanged += MemoryMonitorOnStateChanged;
            _rendererCore = new RendererCore(File.ReadAllText(assPath), width, height);
            _sender = new Sender(name, (uint)width, (uint)height);

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
                TimeSpan.FromSeconds(2),
                DispatcherPriority.Normal,
                TimerTick,
                Dispatcher.CurrentDispatcher);
            _timer.Start();
        }

        #endregion

        #region Methods

        private byte[] Render(IntPtr imageRaw)
        {
            if (imageRaw == IntPtr.Zero)
                return new byte[_width * _height * 4];
            byte[] result = new byte[_width * _height * 4];
            try
            {
                unsafe
                {
                    while (imageRaw != IntPtr.Zero)
                    {
                        var image = Marshal.PtrToStructure<ASS_Image>(imageRaw);
                        var imgRaw = (byte*)image.bitmap.ToPointer();
                        int h = image.h, w = image.w;
                        if (h != 0 && w != 0)
                        {
                            int dstStride = image.stride;
                            uint color = image.color;
                            int dstCurrentPixel;
                            int image1PixelPos;
                            byte image2Pixel;
                            uint srcRed, dstRed, finRed;
                            uint srcGreen, dstGreen, finGreen;
                            uint srcBlue, dstBlue, finBlue;
                            uint dstAlpha, srcAlpha, finAlpha;

                            // RGBA
                            dstAlpha = 255 - (color & 0xFF);
                            dstRed = ((color >> 24) & 0xFF) * dstAlpha / 255;
                            dstGreen = ((color >> 16) & 0xFF) * dstAlpha / 255;
                            dstBlue = ((color >> 8) & 0xFF) * dstAlpha / 255;

                            for (var x = 0; x < w; x++)
                            {
                                for (var y = 0; y < h; y++)
                                {
                                    dstCurrentPixel = y * dstStride + x;
                                    image1PixelPos = (y + image.dst_y) * (x + image.dst_x) * 4;
                                    image2Pixel = imgRaw[dstCurrentPixel];

                                    srcRed = result[image1PixelPos];
                                    srcGreen = result[image1PixelPos + 1];
                                    srcBlue = result[image1PixelPos + 2];
                                    srcAlpha = result[image1PixelPos + 3];

                                    uint dstAlpha2 = image2Pixel;
                                    uint srcAlpha2 = 255 - dstAlpha2;

                                    finRed = dstRed * dstAlpha2 / 255 + srcRed * srcAlpha2 / 255;
                                    finGreen = dstGreen * dstAlpha2 / 255 + srcGreen * srcAlpha2 / 255;
                                    finBlue = dstBlue * dstAlpha2 / 255 + srcBlue * srcAlpha2 / 255;
                                    finAlpha = dstAlpha2 + srcAlpha * srcAlpha2 / 256;

                                    result[image1PixelPos] = (byte)finRed;
                                    result[image1PixelPos + 1] = (byte)finGreen;
                                    result[image1PixelPos + 2] = (byte)finBlue;
                                    result[image1PixelPos + 3] = (byte)finAlpha;
                                }
                            }
                        }

                        imageRaw = image.next;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        public void Send(int milliSec, bool seek = false)
        {
            IntPtr imageRaw = _renderedData[milliSec];
            if (imageRaw != IntPtr.Zero)
                _sender.Send(Render(imageRaw));
            _playerIndex = milliSec;
            if (seek)
            {
                RenderState = WorkingState.Failed;
                TriggerRender(milliSec, true);
            }
        }

        private void TimerTick(object sender, EventArgs e)
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

        private void TriggerRender(int milliSec, bool restart)
        {
            if (restart)
            {
                _renderWorker.CancelAsync();
                while (_renderWorker.IsBusy)
                {
                    // Ignore
                }
            }
            if (!_renderWorker.IsBusy) _renderWorker.RunWorkerAsync(milliSec);
        }

        #endregion

        #region Workers

        private void DoPurgeWork(object sender, DoWorkEventArgs e)
        {
            while (!_purgeWorker.CancellationPending)
            {
                if (_purgeIndex >= _playerIndex && _purgeIndex < _playerIndex + _maxRenderFrame + 50)
                    _purgeIndex = _playerIndex + _maxRenderFrame + 50;
                if (_purgeIndex >= _total)
                {
                    Thread.Sleep(5000);
                    _purgeIndex = 0;
                }
                FreeImageData(_renderedData[_purgeIndex]);
                _renderedData[_purgeIndex] = IntPtr.Zero;
                _purgeIndex++;
            }

            e.Cancel = true;
        }

        private void DoRenderWork(object sender, DoWorkEventArgs e)
        {
            int milliSec = (int)e.Argument;
            _renderIndex = milliSec;
            while (!_renderWorker.CancellationPending && _renderIndex < _total)
            {
                if (_renderedData[_renderIndex] == IntPtr.Zero)
                {
                    IntPtr data = _rendererCore.PreRender(_renderIndex);
                    lock (_renderedData)
                    {
                        _renderedData[_renderIndex] = data;
                    }
                }
                _renderIndex++;
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

        #region Utilities

        private void FreeImageData(IntPtr ptr)
        {
            IntPtr p = ptr;
            Collection<IntPtr> ptrs = new Collection<IntPtr>();
            while (p != IntPtr.Zero)
            {
                ptrs.Add(p);
                p = Marshal.PtrToStructure<ASS_Image>(p).next;
            }

            foreach (IntPtr intPtr in ptrs) Marshal.FreeHGlobal(intPtr);
        }

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
            _memoryMonitor.StateChanged -= MemoryMonitorOnStateChanged;
            _memoryMonitor?.Dispose();
            _rendererCore?.Dispose();
            _sender?.Dispose();
            for (int i = 0; i < _total; i++)
                if (_renderedData[i] != IntPtr.Zero)
                    FreeImageData(_renderedData[i]);
        }

        #endregion
    }
}
