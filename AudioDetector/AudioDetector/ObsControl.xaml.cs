using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;

namespace AudioDetector {
    /// <summary>
    /// Interaction logic for ObsControl.xaml
    /// </summary>
    public partial class ObsControl : UserControl {
        private OBSWebsocket obs;
        private Stopwatch lastGoLiveSignal, lastGoUnLiveSignal;
        private static readonly TimeSpan MaxSignalRate = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan RestartStreamAfter = TimeSpan.FromMinutes(11 * 60 + 45);
        private Stopwatch lastStreamRestart;
        private object restartStreamLock = new object();
        private bool restarting = false;


        public ObsControl() {
            InitializeComponent();

            if (new Random().NextDouble() < 0.5) {
                this.uiGradient.GradientStops.Clear();
                this.uiGradient.GradientStops = new GradientStopCollection() {
                    new GradientStop(Colors.Magenta, 0),
                    new GradientStop(Color.FromRgb(0xFF, 0xDD, 0x00), 1),
                };
            }
        }


        private void OnLoaded(object sender, RoutedEventArgs e) {
            obs = new OBSWebsocket();

            obs.Connected += OnConnect;
            obs.Disconnected += OnDisconnect;

            obs.StreamingStateChanged += OnStreamingStateChange;

            obs.StreamStatus += OnStreamData;

            MainWindow.UiAction(() => {
                ConnectButton_Click(null, null);
            });
        }

        public void Signal(bool goLive) {
            MainWindow.UiAction(() => {
                if (uiEnabled.IsChecked.HasValue && !uiEnabled.IsChecked.Value) {
                    return;
                }

                if (restarting) {
                    return;
                }

                MainWindow.BgAction(() => {
                    try {
                        if (obs.IsConnected) {
                            var streamStatus = obs.GetStreamingStatus();
                            if (goLive) {
                                if (!streamStatus.IsStreaming) {
                                    if (lastGoLiveSignal == null) {
                                        lastGoLiveSignal = Stopwatch.StartNew();
                                    } else {
                                        if (lastGoLiveSignal.Elapsed < MaxSignalRate) {
                                            return;
                                        }

                                        lastGoLiveSignal.Restart();
                                    }

                                    obs.StartStreaming();
                                }
                            } else {
                                if (streamStatus.IsStreaming) {
                                    if (lastGoUnLiveSignal == null) {
                                        lastGoUnLiveSignal = Stopwatch.StartNew();
                                    } else {
                                        if (lastGoUnLiveSignal.Elapsed < MaxSignalRate) {
                                            return;
                                        }

                                        lastGoUnLiveSignal.Restart();
                                    }

                                    obs.StopStreaming();
                                }
                            }
                        }
                    } catch (Exception) {
                        return;
                    }
                });
            });
        }

        private void OnStreamData(OBSWebsocket sender, StreamStatus status) {

            TimeSpan streamTime = TimeSpan.FromSeconds(status.TotalStreamTime);

            // Update UI
            MainWindow.UiAction(() => {
                uiStreamStats.Text = $"StreamTime={streamTime}, FPS={(int)status.FPS}, Strain={(int)(status.Strain * 100)}%";
            });
            
            MainWindow.UiAction(() => {
                if (uiEnabled.IsChecked.HasValue && !uiEnabled.IsChecked.Value) {
                    return;
                }

                MainWindow.BgAction(() => {
                    lock (restartStreamLock) {
                        if (obs.IsConnected && status.Streaming && streamTime > RestartStreamAfter) {
                            if (lastStreamRestart != null && lastStreamRestart.Elapsed < RestartStreamAfter) {
                                return;
                            }

                            // Do restart
                            try {
                                restarting = true;
                                lastStreamRestart = Stopwatch.StartNew();
                                MainWindow.UiAction(() => {
                                    uiStreamRestart.Text = "RESTARTING... PLEASE WAIT";
                                });

                                obs.StopStreaming();

                                Thread.Sleep(TimeSpan.FromSeconds(30));

                                obs.StartStreaming();

                                MainWindow.UiAction(() => {
                                    uiStreamRestart.Text = $"RESTARTED AT {DateTimeOffset.Now}";
                                });
                            } finally {
                                restarting = false;
                            }
                        }
                    }
                });
            });
        }

        private void OnStreamingStateChange(OBSWebsocket sender, OutputState type) {
            MainWindow.UiAction(() => {
                uiStatusStreaming.Text = type.ToString();// == OutputState.Started ? "Yes" : "No";
            });
        }

        private void OnDisconnect(object sender, EventArgs e) {
            MainWindow.UiAction(() => {
                uiStatus.Text = "Disconnected";
            });
        }

        private void OnConnect(object sender, EventArgs e) {
            MainWindow.UiAction(() => {
                uiStatus.Text = "Connected";
            });

            MainWindow.BgAction(() => {
                var streamStatus = obs.GetStreamingStatus();
                OnStreamingStateChange(obs, streamStatus.IsStreaming ? OutputState.Started : OutputState.Stopped);
            });
        }

        private void SetConnectionParams_Click(object sender, RoutedEventArgs e) {
            GetConnectionParams inst = new GetConnectionParams();
            inst.Owner = MainWindow.Instance;
            inst.ShowDialog();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e) {

            if (string.IsNullOrEmpty(MainWindow.Settings.ConnectionHost)) {
                return;
            }

            if (obs.IsConnected) {
                return;
            }

            MainWindow.UiAction(() => {
                uiStatus.Text = "Connecting...";
            });


            MainWindow.BgAction(() => {
                try {
                    obs.Connect(MainWindow.Settings.ConnectionHost, MainWindow.Settings.ConnectionPassword);
                } catch (AuthFailureException) {
                    MessageBox.Show(MainWindow.Instance, "Authentication failed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                } catch (ErrorResponseException ex) {
                    MessageBox.Show(MainWindow.Instance, "Connect failed : " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            });
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e) {
            if (obs.IsConnected) {
                obs.Disconnect();
            }
        }
    }
}
