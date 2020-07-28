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

        #region Methods

        public TimeSpan JumpDuration(long duration)
        {
            var target = MediaElement.Position + TimeSpan.FromMilliseconds(duration);
            if (target.TotalMilliseconds < 0)
                target = TimeSpan.Zero;
            MediaElement.Seek(target).GetAwaiter().GetResult();
            return target;
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            MediaElement?.Close();
            //MediaElement?.Dispose();
            MediaElement = null;
        }

        #endregion
    }
}
