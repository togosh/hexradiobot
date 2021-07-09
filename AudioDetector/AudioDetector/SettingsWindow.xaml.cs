using System;
using System.Globalization;
using System.Windows;

namespace AudioDetector {
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public SettingsWindow() {
            InitializeComponent();

            uiGoLiveMinutes.Text = MainWindow.Settings.SignalGoLiveMinuteCount.ToString();
            uiGoUnLiveMinutes.Text = MainWindow.Settings.SignalGoUnLiveMinuteCount.ToString();

            uiGoLiveTh.Text = MainWindow.Settings.SignalGoLiveAvgThreshold.ToString(CultureInfo.InvariantCulture);
            uiGoUnLiveTh.Text = MainWindow.Settings.SignalGoUnLiveAvgThreshold.ToString(CultureInfo.InvariantCulture);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            try {
                MainWindow.Settings.SignalGoLiveMinuteCount = int.Parse(uiGoLiveMinutes.Text);
                MainWindow.Settings.SignalGoUnLiveMinuteCount = int.Parse(uiGoUnLiveMinutes.Text);

                MainWindow.Settings.SignalGoLiveAvgThreshold = double.Parse(uiGoLiveTh.Text);
                MainWindow.Settings.SignalGoUnLiveAvgThreshold = double.Parse(uiGoUnLiveTh.Text);
            } catch (Exception exception) {
                return;
            }

            DialogResult = true;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
