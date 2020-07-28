﻿using System;
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
        #region Current

        internal static Sender Current = new Sender();

        #endregion

        private const string SenderName = "Ruminoid LIVE";

        #region OpenGL

        private DeviceContext _deviceContext;
        private IntPtr _glContext = IntPtr.Zero;

        #endregion

        #region Core Data

        private SpoutSender _sender;

        #endregion

        #region User Data

        private uint _width = 1920, _height = 1080;

        #endregion

        #region Constructor

        public Sender()
        {
            // Initialize Static Core
            _deviceContext = DeviceContext.Create();
            _glContext = _deviceContext.CreateContext(IntPtr.Zero);
            _deviceContext.MakeCurrent(_glContext);

            // Initialize Core
            _sender = new SpoutSender();
            _sender.CreateSender(SenderName, _width, _height, 0);
        }

        public void Initialize(uint width = 1920, uint height = 1080)
        {
            _width = width;
            _height = height;

            _sender.UpdateSender(SenderName, _width, _height);
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
