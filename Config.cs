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
    [RuminoidProduct("LIVE")]
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Config : INotifyPropertyChanged
    {
        #region Current

        public static Config Current { get; set; } = ConfigHelper<Config>.OpenConfig();

        #endregion

        #region Render

        [JsonProperty]
        private string renderWidth = "1920";

        public string RenderWidth
        {
            get => renderWidth;
            set
            {
                renderWidth = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string renderHeight = "1080";

        public string RenderHeight
        {
            get => renderHeight;
            set
            {
                renderHeight = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string memSize = "500";

        public string MemSize
        {
            get => memSize;
            set
            {
                memSize = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string minRenderTime = "190";

        public string MinRenderFrame
        {
            get => minRenderTime;
            set
            {
                minRenderTime = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string maxRenderTime = "200";

        public string MaxRenderFrame
        {
            get => maxRenderTime;
            set
            {
                maxRenderTime = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string frameRate = "60";

        public string FrameRate
        {
            get => frameRate;
            set
            {
                frameRate = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string threadCount = "3";

        public string ThreadCount
        {
            get => threadCount;
            set
            {
                threadCount = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string glyphMax = "1024";

        public string GlyphMax
        {
            get => glyphMax;
            set
            {
                glyphMax = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty]
        private string bitmapMax = "64";

        public string BitmapMax
        {
            get => bitmapMax;
            set
            {
                bitmapMax = value;
                OnPropertyChanged();
            }
        }

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
