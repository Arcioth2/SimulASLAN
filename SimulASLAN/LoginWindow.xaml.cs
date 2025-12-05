using System;
using System.Globalization;
using System.IO;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        private string _currentLanguage = "TR";

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            sliderCoverage.ValueChanged += (_, __) => UpdateCoverageLabel();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateCoverageLabel();
            _ = PlayGreetingWithDelay();
        }

        private async Task PlayGreetingWithDelay()
        {
            await Task.Delay(1000);
            PlayAudioFile(_currentLanguage == "TR" ? "greet_tr.wav" : "greet_en.wav");
        }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string tag })
            {
                return;
            }

            if (tag == "EN")
            {
                PlayAudioFile("english.wav");
            }
            else if (tag == "TR")
            {
                PlayAudioFile("turkish.wav");
            }

            SetLanguage(tag);
        }

        private void SetLanguage(string langCode)
        {
            _currentLanguage = langCode;

            if (langCode == "TR")
            {
                lblTitle.Text = "UÇUŞ YETKİLENDİRME";
                lblUser.Text = "Kullanıcı Adı";
                lblPass.Text = "Şifre";
                lblCoords.Text = "Hedef Koordinatlar (Enlem, Boylam)";
                lblQuality.Text = "Harita Kalitesi (1-5)";
                lblQualityHint.Text = "(Yüksek = Daha İyi Detay, Yavaş İndirme)";
                lblCoverage.Text = "Harita Kapsamı (metre)";
                lblCoverageHint.Text = "(Bağlantı açıldığında indirilecek kapsama alanı)";
                btnLogin.Content = "SİMÜLASYONU BAŞLAT";
                btnTr.FontWeight = FontWeights.Bold;
                btnEn.FontWeight = FontWeights.Normal;
            }
            else
            {
                lblTitle.Text = "FLIGHT AUTHORIZATION";
                lblUser.Text = "Username";
                lblPass.Text = "Password";
                lblCoords.Text = "Target Coordinates (Lat, Lon)";
                lblQuality.Text = "Map Quality (Scale 1-5)";
                lblQualityHint.Text = "(Higher = Better Detail, Slower Download)";
                lblCoverage.Text = "Map Coverage (meters)";
                lblCoverageHint.Text = "(Sets the area downloaded when the link opens)";
                btnLogin.Content = "INITIATE SIMULATION";
                btnEn.FontWeight = FontWeights.Bold;
                btnTr.FontWeight = FontWeights.Normal;
            }

            UpdateCoverageLabel();
        }

        private void PlayAudioFile(string fileName)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                if (File.Exists(path))
                {
                    using SoundPlayer player = new(path);
                    player.Play();
                }
                else
                {
                    SystemSounds.Beep.Play();
                }
            }
            catch
            {
                SystemSounds.Beep.Play();
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            SystemSounds.Beep.Play();

            if (txtUser.Text != "admin" || txtPass.Password != "password")
            {
                txtError.Text = _currentLanguage == "TR" ? "Geçersiz Kimlik Bilgileri" : "Invalid Credentials";
                PlayAudioFile(_currentLanguage == "TR" ? "invalid_credidentials_tr.wav" : "invalid_credidentials_en.wav");
                return;
            }

            string coords = txtCoords.Text;
            if (!coords.Contains(','))
            {
                txtError.Text = _currentLanguage == "TR" ? "Koordinatlar virgül ile ayrılmalıdır." : "Coordinates must be separated by comma";
                PlayAudioFile(_currentLanguage == "TR" ? "coordinates_must_be_separated_by_comma_tr.wav" : "coordinates_must_be_separated_by_comma_en.wav");
                return;
            }

            string[] parts = coords.Split(',');
            if (parts.Length < 2 ||
                !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                txtError.Text = _currentLanguage == "TR" ? "Geçersiz Koordinat Formatı." : "Invalid Coordinate Format.";
                PlayAudioFile(_currentLanguage == "TR" ? "invalid_coordinate_format_tr.wav" : "invalid_coordinate_format_en.wav");
                return;
            }

            int quality = (int)sliderQuality.Value;
            double coverage = sliderCoverage.Value;

            MainWindow main = new(lat, lon, quality, _currentLanguage, coverage);
            main.Show();
            Close();
        }

        private void UpdateCoverageLabel()
        {
            lblCoverageValue.Text = string.Format(CultureInfo.InvariantCulture, "{0:F0} m", sliderCoverage.Value);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }
    }
}
