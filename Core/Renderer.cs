using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using C5;
using Ruminoid.Common.Renderer.Core;
using Ruminoid.Common.Renderer.Utilities;
using Ruminoid.Common.Utilities;
using Helios.Concurrency;

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
        private int _maxPerBunch, _minPrerender, _maxPrerender;
        private ulong _maxMemory;

        private bool IsDispatching => _currentRenderTasks > 0;

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
            _maxMemory = (ulong)memSize;
            _maxPerBunch = 16;
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

            _renderers = new ThreadLocal<RenderCoreRenderer>(() => _renderCore.CreateRenderer());
            _manipulatePool.QueueUserWorkItem(DispatchRender);
        }

        public void Send(int time)
        {
            _manipulatePool.QueueUserWorkItem(() => SendOnDispatcher(_frameAdaptor.GetFrameIndex(time)));
        }

        private void DispatchRender()
        {
            if (IsDispatching || _totalDataBytes > _maxMemory) return;
            _currentRenderTasks = 0;
            int cur = _playerIndex;
            for (int i = 0; i < _maxPerBunch && cur < _totalFrames && cur <= _playerIndex + _maxPrerender; i++)
            {
                // todo better algorithm
                while (_renderedData.ContainsKey(cur)) cur++;
                if (!(cur < _totalFrames && cur <= _playerIndex + _maxPrerender)) break;

                _renderedData[cur] = RenderedImage.Empty;;
                _currentRenderTasks++;
                var curLocal = cur;
                _renderPool.QueueUserWorkItem(() => Render(curLocal));
            }
        }

        private void CheckPurge()
        {
            if (_totalDataBytes <= _maxMemory)
                return;

            foreach (var s in _renderedData.Where(it => !WithinPrerenderRange(it.Key)).ToList())
            {
                _totalDataBytes -= (uint)s.Value.Buffer.Length;
                _renderedData.Remove(s.Key);
            }
        }

        private bool WithinPrerenderRange(int index)
        {
            return index >= _playerIndex - _maxPrerender / 10 && index <= _playerIndex + _maxPrerender;
        }

        private void OnRenderFinish(int frame, RenderedImage image)
        {
            _renderedData[frame] = image;
            _totalDataBytes += (ulong) image.Buffer.Length;
            _currentRenderTasks--;

            if (!IsDispatching)
            {
                CheckPurge();
                DispatchRender();
            }
        }

        private void SendOnDispatcher(int frame)
        {
            _playerIndex = frame;
            if (!_renderedData.TryGetValue(frame, out var image))
            {
                if (IsDispatching)
                    DispatchRender();
                return;
            }

            if (ReferenceEquals(image, RenderedImage.Empty))
                return;

            Sender.Current.Send(image);

            if (!IsDispatching && !_renderedData.ContainsKey(frame + _minPrerender))
                DispatchRender();
        }

        private void Render(int frame)
        {
            var ms = _frameAdaptor.GetMilliSec(frame);
            var rendered = _renderers.Value.Render(_width, _height, ms);
            _manipulatePool.QueueUserWorkItem(() => OnRenderFinish(frame, rendered));
        }

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
