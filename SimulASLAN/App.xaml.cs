using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace WpfApp1
{
    internal static class BrowserEmulationHelper
    {
        // Force the WebBrowser control to use the installed IE11 engine so Leaflet works without script errors.
        private const int EmulationLevel = 11001;

        public static void EnsureLatestEmulation()
        {
            try
            {
                ApplyRegistryKey();
            }
            catch
            {
                // Best-effort; fall back to default engine if registry is locked down.
            }
        }

        private static void ApplyRegistryKey()
        {
            string exeName = Process.GetCurrentProcess().ProcessName + ".exe";
            using var key = Registry.CurrentUser.CreateSubKey(
                @"Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION");

            if (key is null)
                return;

            object? currentValue = key.GetValue(exeName);
            if (currentValue is null || (currentValue is int existing && existing < EmulationLevel))
            {
                key.SetValue(exeName, EmulationLevel, RegistryValueKind.DWord);
            }
        }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            BrowserEmulationHelper.EnsureLatestEmulation();
            base.OnStartup(e);
        }
    }

}
