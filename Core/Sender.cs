using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;
using Spout.Interop;

namespace Ruminoid.LIVE.Core
{
    public sealed class Sender : IDisposable
    {
        #region Static

        private static DeviceContext _deviceContext;
        private static IntPtr _glContext = IntPtr.Zero;

        static Sender()
        {
            
        }

        #endregion

        #region Core Data

        private SpoutSender _sender;

        #endregion

        #region User Data

        private uint _width, _height;

        #endregion

        #region Constructor

        public Sender(string name, uint width, uint height)
        {
            // Inject User Data
            _width = width;
            _height = height;

            // Initialize Static Core
            if (_deviceContext is null)
            {
                _deviceContext = DeviceContext.Create();
                _glContext = _deviceContext.CreateContext(IntPtr.Zero);
                _deviceContext.MakeCurrent(_glContext);
            }

            // Initialize Core
            _sender = new SpoutSender();
            _sender.CreateSender(name, _width, _height, 0);
        }

        #endregion

        #region Methods

        public unsafe void Send(byte[] data)
        {
            fixed (byte* pData = data)
                _sender.SendImage(
                    pData,
                    _width,
                    _height,
                    Gl.RGBA,
                    true,
                    0);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _sender?.ReleaseSender(0);
            _sender?.Dispose();
        }

        #endregion
    }
}
