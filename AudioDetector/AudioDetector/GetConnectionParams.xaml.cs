using System.Windows;

namespace AudioDetector {
    /// <summary>
    /// Interaction logic for GetConnectionParams.xaml
    /// </summary>
    public partial class GetConnectionParams : Window {
        public GetConnectionParams() {
            InitializeComponent();

            uiHost.Text = MainWindow.Settings.ConnectionHost;
            //uiPort.Text = MainWindow.Settings.ConnectionPort;
            uiPassword.Text = MainWindow.Settings.ConnectionPassword;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            MainWindow.Settings.ConnectionHost = uiHost.Text;
            //MainWindow.Settings.ConnectionPort = uiPort.Text;
            MainWindow.Settings.ConnectionPassword = uiPassword.Text;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
