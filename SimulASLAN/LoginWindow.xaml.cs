// ... existing imports ...
using System;
using System.Windows;
using System.Windows.Controls;
using System.Media;
using System.IO;
using System.Threading.Tasks;

namespace WpfApp1
{
    public partial class LoginWindow : Window
    {
        // Current language state set to Turkish default
        private string currentLang = "TR";

        public LoginWindow()
        {
            InitializeComponent();
            UpdateCoverageLabel();
            PlayGreetingWithDelay();
        }

        private async void PlayGreetingWithDelay()
        {
            await Task.Delay(1000);
            PlayAudioFile("greet_tr.wav");
        }

        // ... (BtnLang_Click, SetLanguage, PlayAudioFile remain the same) ...

        // RE-INCLUDING THEM HERE FOR COMPLETENESS SO THE FILE IS COPY-PASTE READY
        private void BtnLang_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string selectedLang = btn.Tag.ToString();

                if (selectedLang == "EN") PlayAudioFile("english.wav");
                else if (selectedLang == "TR") PlayAudioFile("turkish.wav");

                SetLanguage(selectedLang);
            }
        }

        private void SetLanguage(string langCode)
        {
            currentLang = langCode;

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
                if (File.Exists(path)) { SoundPlayer player = new SoundPlayer(path); player.Play(); }
                else { SystemSounds.Beep.Play(); }
            }
            catch { }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            SystemSounds.Beep.Play();

            if (txtUser.Text == "admin" && txtPass.Password == "password")
            {
                string coords = txtCoords.Text;
                if (coords.Contains(","))
                {
                    try
                    {
                        string[] parts = coords.Split(',');
                        double lat = double.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                        double lon = double.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);

                        int quality = (int)sliderQuality.Value;
                        double coverage = sliderCoverage.Value;

                        // UPDATED LINE: Pass currentLang to MainWindow
                        MainWindow main = new MainWindow(lat, lon, quality, currentLang, coverage);
                        main.Show();
                        this.Close();
                    }
                    catch
                    {
                        txtError.Text = currentLang == "TR" ? "Geçersiz Koordinat Formatı." : "Invalid Coordinate Format.";
                        PlayAudioFile(currentLang == "TR" ? "invalid_coordinate_format_tr.wav" : "invalid_coordinate_format_en.wav");
                    }
                }
                else
                {
                    txtError.Text = currentLang == "TR" ? "Koordinatlar virgül ile ayrılmalıdır." : "Coordinates must be separated by comma";
                    PlayAudioFile(currentLang == "TR" ? "coordinates_must_be_separated_by_comma_tr.wav" : "coordinates_must_be_separated_by_comma_en.wav");
                }
            }
            else
            {
                txtError.Text = currentLang == "TR" ? "Geçersiz Kimlik Bilgileri" : "Invalid Credentials";
                PlayAudioFile(currentLang == "TR" ? "invalid_credidentials_tr.wav" : "invalid_credidentials_en.wav");
            }
        }

        private void SliderCoverage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateCoverageLabel();
        }

        private void UpdateCoverageLabel()
        {
            if (sliderCoverage == null || lblCoverageValue == null)
            {
                return;
            }

            lblCoverageValue.Text = $"{sliderCoverage.Value:F0} m";
        }
    }
}