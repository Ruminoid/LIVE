﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Ruminoid.Common.Renderer.LibAss;
using static Ruminoid.Common.Renderer.LibAss.LibASSInterop;

namespace Ruminoid.LIVE.Core
{
    public sealed class RendererCore : IDisposable
    {
        #region Ass Static Utilities

        private const string DefaultCodepage = "UTF-8";
        private static IntPtr _library;
        private static IntPtr _renderer;
        private static IntPtr _assCodepagePtr;

        static RendererCore()
        {
            _library = ass_library_init();
            _renderer = ass_renderer_init(_library);
            _assCodepagePtr = Marshal.StringToHGlobalAnsi(DefaultCodepage);
            ass_set_fonts(_renderer, IntPtr.Zero, "sans-serif", 1, IntPtr.Zero, 1);
        }

        #endregion

        #region Ass Data

        private IntPtr _track;
        private IntPtr _event;
        private ASS_Event _eventMarshaled;

        #endregion

        #region User Data

        private IntPtr _assStringPtr;

        private int _width, _height;

        #endregion

        #region Constructor

        public RendererCore(string assString, int width, int height)
        {
            // Apply User Data
            _width = width;
            _height = height;

            // Initialize Core
            _assStringPtr = Marshal.StringToHGlobalAnsi(assString);
            _track = ass_read_memory(_library, _assStringPtr, assString.Length, _assCodepagePtr);
            var track = Marshal.PtrToStructure<ASS_Track>(_track);
            _event = track.events;
            _eventMarshaled = Marshal.PtrToStructure<ASS_Event>(_event);
            ass_set_frame_size(_renderer, width, height);
        }

        #endregion

        #region Methods

        public IntPtr PreRender(int milliSec)
        {
            int updated = 0;
            return ass_render_frame(_renderer, _track, milliSec, ref updated);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            Marshal.StructureToPtr(_eventMarshaled, _event, false);
            ass_free_track(_track);
        }

        #endregion
    }
}
