using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace AudioDetector {
    /// <summary>
    /// Settings store for an application.
    /// </summary>
    public abstract class SettingsBase {
        /// <summary>
        /// Application name.
        /// </summary>
        private readonly string appName;

        /// <summary>
        /// The settings.
        /// </summary>
        private Dictionary<string, object> settings = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        private object syncLock = new object();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="appName">The application name.</param>
        protected SettingsBase(string appName) {
            this.appName = appName;

            this.Load();
        }

        /// <summary>
        /// Save.
        /// </summary>
        public void Save() {
            lock (syncLock) {
                try {
                    string settingsFileName = this.SettingsFileName();
                    Directory.CreateDirectory(Path.GetDirectoryName(settingsFileName));
                    using (FileStream fileStream = File.Open(settingsFileName, FileMode.Create)) {
                        this.Serialize(fileStream);
                    }
                } catch { }
            }
        }

        /// <summary>
        /// Load.
        /// </summary>
        public void Load() {
            lock (syncLock) {
                try {
                    using (FileStream fileStream = File.Open(this.SettingsFileName(), FileMode.Open)) {
                        this.Deserialize(fileStream);
                    }
                } catch (FileNotFoundException) { } catch { }
            }
        }

        /// <summary>
        /// Get setting as bool.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="defaultValue">Default value if setting does not exist or is invalid.</param>
        /// <returns>The setting value.</returns>
        protected bool GetSettingBool(string name, bool defaultValue) {
            lock (syncLock) {
                object value = this.GetSetting(name, defaultValue);
                if (!(value is bool)) {
                    return defaultValue;
                }

                return (bool)value;
            }
        }

        /// <summary>
        /// Get setting as int.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="defaultValue">Default value if setting does not exist or is invalid.</param>
        /// <returns>The setting value.</returns>
        protected int GetSettingInt(string name, int defaultValue) {
            lock (syncLock) {
                object value = this.GetSetting(name, defaultValue);
                if (!(value is int)) {
                    return defaultValue;
                }

                return (int)value;
            }
        }

        /// <summary>
        /// Get setting as string.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="defaultValue">Default value if setting does not exist or is invalid.</param>
        /// <returns>The setting value.</returns>
        protected string GetSettingString(string name, string defaultValue) {
            lock (syncLock) {
                return this.GetSetting(name, defaultValue);
            }
        }

        /// <summary>
        /// Get setting as DateTime.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="defaultValue">Default value if setting does not exist or is invalid.</param>
        /// <returns>The setting value.</returns>
        protected DateTime GetSettingDateTime(string name, DateTime defaultValue) {
            lock (syncLock) {
                return this.GetSetting(name, defaultValue);
            }
        }

        /// <summary>
        /// Set setting.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="name">Setting name.</param>
        /// <param name="value">Setting value.</param>
        protected void SetSetting<T>(string name, T value) {
            lock (syncLock) {
                this.settings[name] = value;
                this.Save();
            }
        }

        /// <summary>
        /// Get setting.
        /// </summary>
        /// <typeparam name="T">The setting type.</typeparam>
        /// <param name="name">Setting name.</param>
        /// <param name="defaultValue">Default value if setting does not exist or is invalid.</param>
        /// <returns>The setting value.</returns>
        protected T GetSetting<T>(string name, T defaultValue) {
            lock (syncLock) {
                if (!this.settings.ContainsKey(name)) {
                    this.settings[name] = defaultValue;
                }

                return (T)this.settings[name];
            }
        }

        /// <summary>
        /// Gets the setting file name.
        /// </summary>
        /// <returns>The setting file name</returns>
        private string SettingsFileName() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), this.appName, this.appName + ".settings.bin");
        }

        /// <summary>
        /// Serializes the settings to a stream.
        /// </summary>
        /// <param name="stream">The output stream.</param>
        private void Serialize(Stream stream) {
            try {
                using (stream) {
                    BinaryFormatter bin = new BinaryFormatter();
                    bin.Serialize(stream, this.settings);
                }
            } catch (IOException) {

            }
        }

        /// <summary>
        /// Deserializes the settings from a stream.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        private void Deserialize(Stream stream) {
            try {
                using (stream) {
                    BinaryFormatter bin = new BinaryFormatter();
                    this.settings = (Dictionary<string, object>)bin.Deserialize(stream);
                }
            } catch (IOException) {

            }
        }
    }
}