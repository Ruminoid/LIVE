using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ruminoid.Common.Utilities;

namespace Ruminoid.LIVE.Core
{
    public sealed class MemoryMonitor : IDisposable
    {
        #region Core

        private Process _currentProcess;

        private double getMemory() => _currentProcess.WorkingSet64 / 1024.0 / 1024.0;

        private Timer _timer;

        #endregion

        #region State Data

        private int _memSize;

        private WorkingState _warningState;

        private WorkingState WarningState
        {
            get => _warningState;
            set
            {
                if (_warningState != value) StateChanged?.Invoke(this, value);
                _warningState = value;
            }
        }

        #endregion

        #region Constructors

        public MemoryMonitor(int memSize)
        {
            // Initialize State
            _memSize = memSize;
            WarningState = WorkingState.Completed;

            // Initialize Core
            _currentProcess = Process.GetCurrentProcess();
            _timer = new Timer(
                TimerCallback,
                null,
                2000,
                Timeout.Infinite);
        }

        #endregion

        #region Methods

        private void TimerCallback(object state)
        {
            double mem = getMemory();

            if (mem > _memSize)
                WarningState = WorkingState.Failed;
            else if (mem > _memSize - 50)
                WarningState = WorkingState.Working;
            else
                WarningState = WorkingState.Completed;
        }

        #endregion

        #region Event Trigger

        public event EventHandler<WorkingState> StateChanged;

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
