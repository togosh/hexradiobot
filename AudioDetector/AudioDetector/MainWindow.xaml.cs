using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioDetector {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public static MainWindow Instance;

        public static readonly DependencyProperty AudioLevelTextProperty = DependencyProperty.Register(
            "AudioLevelText", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string AudioLevelText {
            get { return (string)GetValue(AudioLevelTextProperty); }
            set { SetValue(AudioLevelTextProperty, value); }
        }

        public static readonly DependencyProperty AudioLevelProperty = DependencyProperty.Register(
            "AudioLevel", typeof(double), typeof(MainWindow), new PropertyMetadata(default(double)));

        public double AudioLevel {
            get { return (double)GetValue(AudioLevelProperty); }
            set { SetValue(AudioLevelProperty, value); }
        }

        public static readonly DependencyProperty ThresholdLevelProperty = DependencyProperty.Register(
            "ThresholdLevel", typeof(double), typeof(MainWindow), new PropertyMetadata(0.1));

        public double ThresholdLevel {
            get { return (double)GetValue(ThresholdLevelProperty); }
            set { SetValue(ThresholdLevelProperty, value); }
        }

        public static readonly DependencyProperty DetectionTimeProperty = DependencyProperty.Register(
            "DetectionTime", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string DetectionTime {
            get { return (string)GetValue(DetectionTimeProperty); }
            set { SetValue(DetectionTimeProperty, value); }
        }

        public static Settings Settings = new Settings();

        private Timer timer, timerAgg;
        private double audioLevel, thresholdLevel;
        private double aggregationMax;
        private Stopwatch detectionStopwatch;
        private WasapiLoopbackCapture capture1;
        private LinkedList<double> aggData = new LinkedList<double>();

        /// <summary>
        /// Minimum amount of data to keep hot
        /// </summary>
        private const int MinAggDataCount = 3 * 60 * 60;

        public MainWindow() {
            Instance = this;

            InitializeComponent();

            // Enumerate all output devices
            var enumerator = new MMDeviceEnumerator();
            /*foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.All)) {
                Console.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
            }*/

            List<MMDevice> devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToList();

            uiAudioDevice.ItemsSource = devices;

            // Pre-select a device
            string preselected = Settings.LastAudioDevice;
            if (!string.IsNullOrEmpty(preselected)) {
                var device = devices.FirstOrDefault(d => d.ID == preselected);
                if (device != null) {
                    uiAudioDevice.SelectedItem = device;
                }
            }


            detectionStopwatch = Stopwatch.StartNew();

            // Set up graph
            uiGraph.AddSeries("levels", Brushes.Green);
            uiGraph.AddSeries("threshold", Brushes.PaleVioletRed);

            uiGraphSeconds.AddSeries("levels", Brushes.Green);
            uiGraphSeconds.AddSeries("threshold", Brushes.PaleVioletRed);

            uiGraphSignalGoLive.AddSeries("value", Brushes.Green);
            uiGraphSignalGoUnLive.AddSeries("value", Brushes.Green);

            UpdateUiThresholds();


            timer = new Timer(UpdateUi,
                null,
                TimeSpan.Zero,
                TimeSpan.FromMilliseconds(50));

            timerAgg = new Timer(UpdateAgg,
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1));
        }

        private void UpdateUiThresholds() {
            uiGoLiveSignalDesc.Text = string.Format("({0:0}% of {1}s >= th)",
                Settings.SignalGoLiveAvgThreshold * 100,
                Settings.SignalGoLiveMinuteCount);
            uiGoUnLiveSignalDesc.Text =
                string.Format("({0:0}% of {1}s < th)",
                    Settings.SignalGoUnLiveAvgThreshold * 100,
                    Settings.SignalGoUnLiveMinuteCount);
        }

        private void OnDataAvailable(object sender, WaveInEventArgs args) {
            // Calc peak
            // https://github.com/naudio/NAudio/blob/master/Docs/RecordingLevelMeter.md
            float max = 0;
            var buffer = new WaveBuffer(args.Buffer);
            // interpret as 32 bit floating point audio
            for (int index = 0; index < args.BytesRecorded / 4; index++) {
                var sample = buffer.FloatBuffer[index];

                // absolute value 
                if (sample < 0) sample = -sample;
                // is this the max value?
                if (sample > max) max = sample;
            }

            audioLevel = max;
            aggregationMax = Math.Max(aggregationMax, audioLevel);

            if (audioLevel >= thresholdLevel) {
                detectionStopwatch.Restart();
            }
        }

        private void UpdateUi(object state) {
            uiGraph.AddData("levels", audioLevel);
            uiGraph.AddData("threshold", thresholdLevel);
            UiAction(() => {
                thresholdLevel = ThresholdLevel;

                AudioLevel = audioLevel;
                AudioLevelText = audioLevel.ToString("F");
                DetectionTime = detectionStopwatch.Elapsed.ToString("hh\\:mm\\:ss");
            });
        }

        private void UpdateAgg(object state) {
            uiGraphSeconds.AddData("levels", aggregationMax);
            uiGraphSeconds.AddData("threshold", thresholdLevel);

            string goLiveStatus = "", goUnLiveStatus = "";

            lock (aggData) {
                aggData.AddFirst(aggregationMax);
                while (aggData.Count > Math.Max(Math.Max(MinAggDataCount, Settings.SignalGoLiveMinuteCount), Settings.SignalGoUnLiveMinuteCount)) {
                    aggData.RemoveLast();
                }

                // Calc signals
                bool signalGoLive = aggData.Count >= Settings.SignalGoLiveMinuteCount &&
                                    aggData
                                        .Take(Settings.SignalGoLiveMinuteCount)
                                        .Count(l => l >= thresholdLevel) >= (Settings.SignalGoLiveAvgThreshold * Settings.SignalGoLiveMinuteCount);
                uiGraphSignalGoLive.AddData("value", signalGoLive ? 0.95 : 0.05);

                bool signalGoUnLive = aggData.Count >= Settings.SignalGoUnLiveMinuteCount &&
                                      aggData
                                          .Take(Settings.SignalGoUnLiveMinuteCount)
                                          .Count(l => l < thresholdLevel) >= (Settings.SignalGoUnLiveAvgThreshold * Settings.SignalGoUnLiveMinuteCount);
                uiGraphSignalGoUnLive.AddData("value", signalGoUnLive ? 0.95 : 0.05);

                // Sanity check
                if (signalGoLive != signalGoUnLive) {
                    if (signalGoLive) {
                        uiObsControl.Signal(true);
                    } else if (signalGoUnLive) {
                        uiObsControl.Signal(false);
                    }
                }

                // Calc ui
                goLiveStatus = aggData.Count >= Settings.SignalGoLiveMinuteCount ? "Ready" : $"Aggregating {aggData.Count}/{Settings.SignalGoLiveMinuteCount}...";
                goUnLiveStatus = aggData.Count >= Settings.SignalGoUnLiveMinuteCount ? "Ready" : $"Aggregating {aggData.Count}/{Settings.SignalGoUnLiveMinuteCount}...";
            }

            aggregationMax = 0;

            // Update UI
            UiAction(() => {
                uiGoLiveSignalStatus.Text = goLiveStatus;
                uiGoUnLiveSignalStatus.Text = goUnLiveStatus;
            });
        }

        public static void UiAction(Action action) {
            Instance?.Dispatcher?.BeginInvoke(action);
        }

        public static void BgAction(Action action) {
            ThreadPool.QueueUserWorkItem(_ => {
                try {
                    // Do action
                    action();
                } catch (Exception ex) {
                    // TODO: Log
                }
            });
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
            timer.Dispose();
            capture1.Dispose();
        }

        private void UiAudioDevice_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            var selected = uiAudioDevice.SelectedItem as MMDevice;
            if (selected == null) {
                return;
            }

            Settings.LastAudioDevice = selected.ID;


            StartRecording(selected);
        }

        private void StartRecording(MMDevice selected) {
            // Stop any other device
            capture1?.StopRecording();
            capture1?.Dispose();

            try {
                capture1 = new WasapiLoopbackCapture(selected);
            } catch (System.Runtime.InteropServices.COMException e) {
                MessageBox.Show(this, "Failed to initialize capture:" + Environment.NewLine + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            capture1.DataAvailable += OnDataAvailable;

            // This breaks the capability to change device
            //capture1.RecordingStopped += (s, a) => { capture1.Dispose(); };

            try {
                capture1.StartRecording();
            } catch (System.Runtime.InteropServices.COMException e) {
                MessageBox.Show(this, "Failed to start capture:" + Environment.NewLine + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void ConfigureThreshold_Click(object sender, RoutedEventArgs e) {
            SettingsWindow inst = new SettingsWindow();
            inst.Owner = MainWindow.Instance;
            var res = inst.ShowDialog();

            if (res is true) {
                UpdateUiThresholds();
            }
        }
    }
}
