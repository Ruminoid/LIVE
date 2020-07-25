using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ruminoid.Common.Helpers;

namespace Ruminoid.LIVE
{
    [RuminoidProduct("Dashboard")]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Config : INotifyPropertyChanged
    {
        #region Current

        public static Config Current { get; set; } = ConfigHelper<Config>.OpenConfig();

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
