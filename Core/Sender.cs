using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using OpenGL;
using Ruminoid.Common.Renderer.Core;
using Ruminoid.Common.Utilities.Tasks;
using Spout.Interop;

namespace Ruminoid.LIVE.Core
{
    public sealed class Sender : IDisposable
    {
        #region Current

        internal static Sender Current = new Sender();

        #endregion

        private const string SenderName = "Ruminoid LIVE";

        #region OpenGL

        private DeviceContext _deviceContext;
        private IntPtr _glContext = IntPtr.Zero;

        #endregion

        #region Core Data

        private DedicatedThreadPool _sendPool;
        private SpoutSender _sender;

        //private object _senderLocker;

        #endregion

        #region User Data

        private uint _width = 1920, _height = 1080;
        private byte[] buffer = null;

        #endregion

        #region Constructor

        public Sender()
        {
            _sendPool =
                new DedicatedThreadPool(new DedicatedThreadPoolSettings(1, ThreadType.Background,
                    "Send-Pool"));
            _sendPool.QueueUserWorkItem(() =>
            {
                // Initialize Static Core
                _deviceContext = DeviceContext.Create();
                _glContext = _deviceContext.CreateContext(IntPtr.Zero);
                _deviceContext.MakeCurrent(_glContext);

                //_senderLocker = new object();

                // Initialize Core
                _sender = new SpoutSender();
                _sender.CreateSender(SenderName, _width, _height, 0);
            });
        }

        public void Initialize(uint width = 1920, uint height = 1080)
        {
            _sendPool.QueueUserWorkItem(() =>
            {
                _width = width;
                _height = height;

                _deviceContext.MakeCurrent(_glContext);
                _sender.UpdateSender(SenderName, _width, _height);
            });
        }

        #endregion

        #region Methods

        public unsafe void Send(RenderedImage image)
        {
            _sendPool.QueueUserWorkItem(() =>
            {
                _deviceContext.MakeCurrent(_glContext);
                var (decoded, nBuffer) = AssRenderCore.Decode(buffer, image);
                buffer = nBuffer;

                fixed (byte* unmanaged = decoded.Buffer)
                {
                    _sender.SendImage(
                        unmanaged,
                        _width,
                        _height,
                        Gl.RGBA,
                        false,
                        0);
                }
            });
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _sendPool.QueueUserWorkItem(() =>
            {
                _sender?.ReleaseSender(0);
                _sender?.Dispose();
            });
            _sendPool.Dispose();
            _sendPool.WaitForThreadsExit();
            //_senderLocker = null;
        }

        #endregion
    }
}
