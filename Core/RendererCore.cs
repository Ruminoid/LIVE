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

        public static byte[] Render(IntPtr imageRaw)
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
