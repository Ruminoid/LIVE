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

        private byte[] Render(int milliSec)
        {
            var imageRaw = _renderedData[miliSec];
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

        private void Send(int miliSec) => _sender.Send(Render(miliSec));

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
