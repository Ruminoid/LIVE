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

        private AudioFileReader _audioFile;

        public WaveOutEvent AudioOutput;

        #endregion

        #region Constructor

        public Player(string audioPath)
        {
            // Initialize Core
            _audioFile = new AudioFileReader(audioPath);
            AudioOutput = new WaveOutEvent();
            AudioOutput.Init(_audioFile);
        }

        #endregion

        public void Dispose()
        {
            AudioOutput.Stop();
            AudioOutput?.Dispose();
            _audioFile?.Dispose();
        }
    }
}
