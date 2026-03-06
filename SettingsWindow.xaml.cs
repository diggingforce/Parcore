using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Parcore
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            ThemeComboBox.SelectedIndex = SettingsManager.Current.Theme switch
            {
                "Dark" => 1,
                "Light" => 2,
                _ => 0
            };

            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            AutoStartCheckBox.IsChecked = key?.GetValue("Parcore") != null;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ThemeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            string theme = ThemeComboBox.SelectedIndex switch
            {
                1 => "Dark",
                2 => "Light",
                _ => "Auto"
            };
            SettingsManager.Current.Theme = theme;
            SettingsManager.Save();
            SettingsManager.ApplyTheme();
        }

        private void AutoStart_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (AutoStartCheckBox.IsChecked == true)
            {
                key?.SetValue("Parcore", $"\"{Environment.ProcessPath}\"");
            }
            else
            {
                key?.DeleteValue("Parcore", false);
            }
        }
    }

    public class AppSettings
    {
        public string Theme { get; set; } = "Auto";
    }

    public static class SettingsManager
    {
        public static AppSettings Current { get; set; } = new AppSettings();
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Parcore", "settings.json");

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                string json = JsonSerializer.Serialize(Current);
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }

        public static void ApplyTheme()
        {
            string theme = Current.Theme;
            if (theme == "Auto")
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int val && val == 1)
                {
                    theme = "Light";
                }
                else
                {
                    theme = "Dark";
                }
            }

            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Themes/{theme}.xaml") };
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}
