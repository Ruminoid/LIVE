using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Ruminoid.LIVE.Core
{
    public sealed class Player : IDisposable
    {
        #region Core Data

        public AudioFileReader AudioFile;

        public WaveOutEvent AudioOutput;

        #endregion

        #region Constructor

        public Player(string audioPath)
        {
            // Initialize Core
            AudioFile = new AudioFileReader(audioPath);
            AudioOutput = new WaveOutEvent();
            AudioOutput.Init(AudioFile);
        }

        #endregion

        public void Dispose()
        {
            AudioOutput.Stop();
            AudioOutput?.Dispose();
            AudioFile?.Dispose();
        }
    }
}
