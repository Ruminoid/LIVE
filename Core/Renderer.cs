using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ruminoid.Common.Renderer.LibAss;
using static Ruminoid.Common.Renderer.LibAss.LibASSInterop;

namespace Ruminoid.LIVE.Core
{
    class Renderer : IDisposable
    {
        #region Core Data

        private RendererCore _rendererCore;
        private Sender _sender;

        private IntPtr[] _renderedData;

        #endregion

        #region User Data

        private int _width, _height, _total;

        #endregion

        #region Constructor

        public Renderer(string name, string assPath, int width, int height, int total)
        {
            // Apply User Data
            _width = width;
            _height = height;
            _total = total;

            // Initialize Core
            _rendererCore = new RendererCore(File.ReadAllText(assPath), width, height);
            _sender = new Sender(name, (uint)width, (uint)height);
        }

        #endregion

        #region Methods

        public void PreRender()
        {
            _renderedData = new IntPtr[_total];
            for (int i = 0; i < _total; i++)
                _renderedData[i] = _rendererCore.PreRender(i);
        }

        public void Send(int miliSec) => _sender.Send(RendererCore.Render(_renderedData[miliSec]));

        #endregion

        #region Dispose

        public void Dispose()
        {
            _rendererCore?.Dispose();
            _sender?.Dispose();
        }

        #endregion
    }
}
