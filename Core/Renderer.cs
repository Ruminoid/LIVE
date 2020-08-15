using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Ruminoid.Common.Renderer.Core;
using Ruminoid.Common.Renderer.Utilities;
using Ruminoid.Common.Utilities;
using Ruminoid.Common.Utilities.Tasks;

namespace Ruminoid.LIVE.Core
{
    class Renderer : IDisposable
    {
        private AssRenderCore _renderCore;
        private FrameAdaptor _frameAdaptor;

        private Dictionary<int, RenderedImage> _renderedData = new Dictionary<int, RenderedImage>();
        private ulong _totalDataBytes = 0;

        private DedicatedThreadPool _renderPool;
        private DedicatedThreadPool _manipulatePool;
        private ThreadLocal<RenderCoreRenderer> _renderers;

        private int _width, _height;
        private int _playerIndex, _totalFrames;
        private int _currentRenderTasks;
        private int _maxPerBunch, _maxPerSubbunch, _minPrerender, _maxPrerender;
        private ulong _maxMemory;

        private bool IsDispatching => _currentRenderTasks > 0;

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

        private WorkingState _purgeState;

        private WorkingState PurgeState
        {
            get => _purgeState;
            set
            {
                if (_purgeState != value)
                    StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Purge", value));
                _purgeState = value;
            }
        }

        private WorkingState _memoryState;

        private WorkingState MemoryState
        {
            get => _memoryState;
            set
            {
                if (_memoryState != value)
                    StateChanged?.Invoke(this, new KeyValuePair<string, WorkingState>("Memory", value));
                _memoryState = value;
            }
        }

        #endregion

        public Renderer(string assPath,
            int width,
            int height,
            int total,
            int memSize,
            int minRenderFrame,
            int maxRenderFrame,
            int frameRate,
            int threadCount,
            int glyphMax,
            int bitmapMax) // todo more parameter
        {
            _width = width;
            _height = height;
            _frameAdaptor = new FrameAdaptor(frameRate, total);
            _minPrerender = minRenderFrame;
            _maxPrerender = maxRenderFrame;
            _maxMemory = (ulong)(memSize * 1024 * 1024);
            _maxPerBunch = threadCount;
            _maxPerSubbunch = 8;
            _playerIndex = 0;
            _totalFrames = total;

            string subData = File.ReadAllText(assPath);
            _renderCore = new AssRenderCore(ref subData, width, height, glyphMax, bitmapMax);
            Sender.Current.Initialize((uint)width, (uint)height);

            _renderPool =
                new DedicatedThreadPool(new DedicatedThreadPoolSettings(threadCount, ThreadType.Background,
                    "Render-Pool"));
            _manipulatePool =
                new DedicatedThreadPool(new DedicatedThreadPoolSettings(1, ThreadType.Background, // Must be 1 here
                    "Render-Manipulate-Pool"));

            _renderers = new ThreadLocal<RenderCoreRenderer>(() => _renderCore.CreateRenderer(), true);
            _manipulatePool.QueueUserWorkItem(DispatchRender);
        }

        public void Send(int time)
        {
            _manipulatePool.QueueUserWorkItem(() => SendOnDispatcher(_frameAdaptor.GetFrameIndex(time)));
        }

        private void DispatchRender()
        {
            if (IsDispatching || _totalDataBytes > _maxMemory) return;

            RenderState = WorkingState.Working;

            _currentRenderTasks = 0;
            int cur = _playerIndex;

            var bunch = new List<int>();
            for (int i = 0; i < _maxPerBunch * _maxPerSubbunch && cur < _totalFrames && cur <= _playerIndex + _maxPrerender; i++)
            {
                // todo better algorithm
                while (_renderedData.ContainsKey(cur)) cur++;
                if (!(cur < _totalFrames && cur <= _playerIndex + _maxPrerender)) break;

                _renderedData[cur] = RenderedImage.Empty;
                _currentRenderTasks++;
                bunch.Add(cur);

                if (bunch.Count <= _maxPerSubbunch) continue;
                var bunchLocal = bunch;
                _renderPool.QueueUserWorkItem(() => Render(bunchLocal));
                bunch = new List<int>();
            }
            if (bunch.Count > 0)
                _renderPool.QueueUserWorkItem(() => Render(bunch));
        }

        private void CheckPurge(bool urgent = false)
        {
            bool memoryUrgent = _totalDataBytes > _maxMemory;

            if (!urgent && !memoryUrgent)
            {
                PurgeState = WorkingState.Completed;
                MemoryState = WorkingState.Completed;
                return;
            }

            PurgeState = urgent ? WorkingState.Failed : WorkingState.Working;
            MemoryState = memoryUrgent ? WorkingState.Failed : WorkingState.Working;

            foreach (var s in _renderedData.Where(it => !WithinPrerenderRange(it.Key, urgent)).ToList())
            {
                _totalDataBytes -= (uint)s.Value.Buffer.Length;
                _renderedData.Remove(s.Key);
            }
        }

        private bool WithinPrerenderRange(int index, bool urgent)
        {
            if (urgent)
                return index >= _playerIndex && index <= _playerIndex - 50;
            return index >= _playerIndex - _maxPrerender / 10 && index <= _playerIndex + _maxPrerender;
        }

        private void OnRenderFinish(List<KeyValuePair<int, RenderedImage>> results)
        {
            foreach (var result in results)
            {
                _renderedData[result.Key] = result.Value;
                _totalDataBytes += (ulong)result.Value.Buffer.Length;
            }

            _currentRenderTasks -= results.Count;

            if (_currentRenderTasks == 0) RenderState = WorkingState.Completed;

            if (!IsDispatching)
            {
                CheckPurge();
                DispatchRender();
            }
        }

        private void SendOnDispatcher(int frame)
        {
            _playerIndex = frame;
            if (!_renderedData.TryGetValue(frame, out RenderedImage image))
            {
                RenderState = WorkingState.Failed;
                if (IsDispatching) return;
                CheckPurge(true);
                DispatchRender();
                return;
            }

            if (ReferenceEquals(image, RenderedImage.Empty))
                return;

            Sender.Current.Send(image);

            if (!IsDispatching && !_renderedData.ContainsKey(frame + _minPrerender))
                DispatchRender();
        }

        private void Render(List<int> frames)
        {
            var results = new List<KeyValuePair<int, RenderedImage>>();
            foreach (var frame in frames)
            {
                var ms = _frameAdaptor.GetMilliSec(frame);
                results.Add(new KeyValuePair<int, RenderedImage>(frame, _renderers.Value.Render(_width, _height, ms)));
            }
            _manipulatePool.QueueUserWorkItem(() => OnRenderFinish(results));
        }

        #region Event Triggers & Processors

        public event EventHandler<KeyValuePair<string, WorkingState>> StateChanged;

        #endregion

        public void Dispose()
        {
            _manipulatePool.Dispose();
            _renderPool.Dispose();

            foreach (var renderer in _renderers.Values)
                renderer.Dispose();
            _renderCore.Dispose();
        }
    }
}
