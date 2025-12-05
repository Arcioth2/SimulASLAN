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
    public enum AppLanguage
    {
        Turkish,
        English
    }

    public partial class LoginWindow : Window
    {
        private AppLanguage _currentLanguage = AppLanguage.Turkish;

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SetLanguage(_currentLanguage);
            ClearErrorMessage();
            sliderCoverage.ValueChanged += (_, __) => UpdateCoverageLabel();
            txtUser.TextChanged += (_, __) => ClearErrorMessage();
            txtPass.PasswordChanged += (_, __) => ClearErrorMessage();
            txtCoords.TextChanged += (_, __) => ClearErrorMessage();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetLanguage(_currentLanguage);
            ClearErrorMessage();
            _ = PlayGreetingWithDelay();
        }

        private async Task PlayGreetingWithDelay()
        {
            await Task.Delay(1000);
            PlayAudioFile(IsTurkish ? "greet_tr.wav" : "greet_en.wav");
        }

        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string tag })
            {
                return;
            }

            if (!TryParseLanguage(tag, out AppLanguage lang))
            {
                return;
            }

            if (lang == AppLanguage.English)
            {
                PlayAudioFile("english.wav");
            }
            else if (lang == AppLanguage.Turkish)
            {
                PlayAudioFile("turkish.wav");
            }

            SetLanguage(lang);
        }

        private void SetLanguage(AppLanguage langCode)
        {
            _currentLanguage = langCode;

            if (IsTurkish)
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
                txtError.Text = IsTurkish ? "Geçersiz Kimlik Bilgileri" : "Invalid Credentials";
                PlayAudioFile(IsTurkish ? "invalid_credidentials_tr.wav" : "invalid_credidentials_en.wav");
                return;
            }

            string coords = txtCoords.Text;
            if (!coords.Contains(','))
            {
                txtError.Text = IsTurkish ? "Koordinatlar virgül ile ayrılmalıdır." : "Coordinates must be separated by comma";
                PlayAudioFile(IsTurkish ? "coordinates_must_be_separated_by_comma_tr.wav" : "coordinates_must_be_separated_by_comma_en.wav");
                return;
            }

            string[] parts = coords.Split(',');
            if (parts.Length < 2 ||
                !double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double lon))
            {
                txtError.Text = IsTurkish ? "Geçersiz Koordinat Formatı." : "Invalid Coordinate Format.";
                PlayAudioFile(IsTurkish ? "invalid_coordinate_format_tr.wav" : "invalid_coordinate_format_en.wav");
                return;
            }

            int quality = (int)sliderQuality.Value;
            double coverage = sliderCoverage.Value;

            MainWindow main = new(lat, lon, quality, GetLanguageCode(_currentLanguage), coverage);
            main.Show();
            Close();
        }

        private void UpdateCoverageLabel()
        {
            lblCoverageValue.Text = string.Format(CultureInfo.InvariantCulture, "{0:F0} m", sliderCoverage.Value);
        }

        private void ClearErrorMessage()
        {
            txtError.Text = string.Empty;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private bool IsTurkish => _currentLanguage == AppLanguage.Turkish;

        private static bool TryParseLanguage(string tag, out AppLanguage language)
        {
            switch (tag)
            {
                case "TR":
                    language = AppLanguage.Turkish;
                    return true;
                case "EN":
                    language = AppLanguage.English;
                    return true;
                default:
                    language = AppLanguage.Turkish;
                    return false;
            }
        }

        private static string GetLanguageCode(AppLanguage lang) => lang == AppLanguage.Turkish ? "TR" : "EN";
    }
}
