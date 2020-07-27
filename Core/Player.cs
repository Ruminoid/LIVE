using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using Unosquare.FFME;
using Unosquare.FFME.Common;

namespace Ruminoid.LIVE.Core
{
    public sealed class Player : IDisposable
    {
        #region Core Data

        public MediaElement MediaElement;

        #endregion

        #region Constructor

        public Player()
        {
            // Initialize Core
            MediaElement = new MediaElement
            {
                LoadedBehavior = MediaPlaybackState.Manual,
                UnloadedBehavior = MediaPlaybackState.Manual
            };
        }

        #endregion

        public void Dispose()
        {
            MediaElement?.Close();
            //MediaElement?.Dispose();
            MediaElement = null;
        }
    }
}
