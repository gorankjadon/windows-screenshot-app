using System.Runtime.Serialization;

namespace GstackScreenshot
{
    [DataContract]
    internal sealed class AppSettings
    {
        [DataMember]
        public string SaveFolder { get; set; }

        [DataMember]
        public string RegionHotkey { get; set; }

        [DataMember]
        public string FullScreenHotkey { get; set; }

        [DataMember]
        public string ActiveWindowHotkey { get; set; }

        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                SaveFolder = SettingsStore.GetDefaultSaveFolder(),
                RegionHotkey = "Ctrl+Alt+S",
                FullScreenHotkey = "Ctrl+Alt+F",
                ActiveWindowHotkey = "Ctrl+Alt+A"
            };
        }
    }
}
