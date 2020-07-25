using System;
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
        #region Current

        private static RendererCore _current;

        public static RendererCore Current
        {
            get => _current;
            set
            {
                _current?.Dispose();
                _current = value;
            }
        }

        #endregion

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
        private IntPtr _origString = IntPtr.Zero;

        #endregion

        #region User Data

        private IntPtr _assStringPtr;

        public int Width, Height;

        #endregion

        #region Constructor

        public RendererCore(string assString, int width, int height)
        {
            Width = width;
            Height = height;
            _assStringPtr = Marshal.StringToHGlobalAnsi(assString);
            _track = ass_read_memory(_library, _assStringPtr, assString.Length, _assCodepagePtr);
            var track = Marshal.PtrToStructure<ASS_Track>(_track);
            _event = track.events;
            _eventMarshaled = Marshal.PtrToStructure<ASS_Event>(_event);
            _origString = _eventMarshaled.Text;
            ass_set_frame_size(_renderer, width, height);
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _eventMarshaled.Text = _origString;
            Marshal.StructureToPtr(_eventMarshaled, _event, false);
            ass_free_track(_track);
        }

        #endregion
    }
}
