using System.Diagnostics;
using System.Windows;

namespace CzkawkaWin.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow(string version)
        {
            InitializeComponent();
            TxtVersion.Text = $"Version {version}";
        }

        private void BtnGitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/SauloVictorS/CzkawkaWin");
        }

        private void BtnCzkawka_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/qarmin/czkawka");
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently ignore if browser can't be opened
            }
        }
    }
}
