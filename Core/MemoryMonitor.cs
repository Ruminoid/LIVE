using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ruminoid.LIVE.Core
{
    public sealed class MemoryMonitor : IDisposable
    {
        #region Core

        private Process _currentProcess;

        private double getMemory() => _currentProcess.WorkingSet64 / 1024.0 / 1024.0;

        private Timer _timer;

        #endregion

        #region Constructors

        public MemoryMonitor(double moniState)
        {
            _currentProcess = Process.GetCurrentProcess();
            _timer = new Timer(
                (state =>
                {
                    if (getMemory() > moniState) Purge?.Invoke(this, EventArgs.Empty);
                }),
                null,
                2000,
                Timeout.Infinite);
        }

        #endregion

        #region Event Trigger

        public event EventHandler Purge;

        #endregion

        #region Dispose

        public void Dispose()
        {
            _currentProcess?.Dispose();
            _timer?.Dispose();
        }

        #endregion
    }
}
