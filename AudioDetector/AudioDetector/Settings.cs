using System;
using System.Collections.Generic;

namespace AudioDetector {
    public class Settings : SettingsBase {
        /// <summary>
        /// Initializes settings store.
        /// </summary>
        public Settings() : base("AudioDetector") {
        }

        /// <summary>
        /// LastAudioDevice
        /// </summary>
        public string LastAudioDevice {
            get { return this.GetSetting(nameof(LastAudioDevice), ""); }
            set { this.SetSetting(nameof(LastAudioDevice), value); }
        }

        public string ConnectionHost {
            get { return this.GetSetting(nameof(ConnectionHost), "ws://localhost:4444"); }
            set { this.SetSetting(nameof(ConnectionHost), value); }
        }

        public string ConnectionPort {
            get { return this.GetSetting(nameof(ConnectionPort), ""); }
            set { this.SetSetting(nameof(ConnectionPort), value); }
        }

        public string ConnectionPassword {
            get { return this.GetSetting(nameof(ConnectionPassword), ""); }
            set { this.SetSetting(nameof(ConnectionPassword), value); }
        }

        // Signal settings

        public int SignalGoLiveMinuteCount {
            get { return this.GetSetting(nameof(SignalGoLiveMinuteCount), 60 * 10); }
            set { this.SetSetting(nameof(SignalGoLiveMinuteCount), value); }
        }

        public int SignalGoUnLiveMinuteCount {
            get { return this.GetSetting(nameof(SignalGoUnLiveMinuteCount), 60 * 10); }
            set { this.SetSetting(nameof(SignalGoUnLiveMinuteCount), value); }
        }

        public double SignalGoLiveAvgThreshold {
            get { return this.GetSetting(nameof(SignalGoLiveAvgThreshold), 0.5); }
            set { this.SetSetting(nameof(SignalGoLiveAvgThreshold), value); }
        }

        public double SignalGoUnLiveAvgThreshold {
            get { return this.GetSetting(nameof(SignalGoUnLiveAvgThreshold), 0.95); }
            set { this.SetSetting(nameof(SignalGoUnLiveAvgThreshold), value); }
        }
    }
}