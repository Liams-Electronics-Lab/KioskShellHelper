using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;

namespace KioskShellHelper
{
    public class ProcessCleanupInfo
    {
        public string ProcessName { get; set; } = string.Empty;
        public int KeepAlive { get; set; }
        public int DelayMs { get; set; }
    }

    public class ProcessStartInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public int DelayMs { get; set; }
    }

    public class SettingsManager
    {
        private readonly string SettingsFile;
        private Dictionary<string, string> settings;

        public SettingsManager()
        {
            // Always save settings.ini next to the executable (works with single-file publish)
            string exeDirectory = AppContext.BaseDirectory;
            SettingsFile = Path.Combine(exeDirectory, "settings.ini");
            
            settings = new Dictionary<string, string>();
            LoadOrCreateSettings();
        }

        private void LoadOrCreateSettings()
        {
            if (!File.Exists(SettingsFile))
            {
                CreateDefaultSettings();
            }
            else
            {
                LoadSettings();
            }
        }

        private void CreateDefaultSettings()
        {
            var defaultSettings = new[]
            {
                "# Kiosk Shell Helper",
                "# A Windows application for kiosk environments that launches applications with customizable startup overlays and process management upon closure.",
                "#",
                "# Made by Liam's Electronics Lab",
                "# www.youtube.com/channel/UCps0V_MhxlnIvX6RsPZBlxw",
                "# www.github.com/Liams-Electronics-Lab",
                "",
                "[General]",
                "DelaySeconds=5",
                "",
                "# Application to launch on startup",
                "[StartupApp]",
                "AppPath=C:\\Windows\\explorer.exe",
                "AppArguments=",
                "DelaySeconds=1",
                "OpenMaximizedExplorer=true",
                "",
                "# Overlay displayed during startup delay",
                "[OpenOverlay]",
                "DisplayMode=Text",
                "Text=Loading Explorer...",
                "BackgroundColor=0,0,0",
                "TextColor=255,255,255",
                "ImagePath=",
                "",
                "[CloseButton]",
                "BackgroundColor=192,0,0",
                "TextColor=255,255,255",
                "Text=Close Explorer",
                "X=0",
                "Y=auto",
                "Width=200",
                "CleanupDelay=5000",
                "",
                "# Overlay displayed when closing",
                "[CloseOverlay]",
                "BackgroundColor=0,0,0",
                "TextColor=255,255,255",
                "Text=Please Wait...",
                "",
                "# Close these processes when exiting the program",
                "[ProcessCleanup]",
                "Process1=explorer",
                "Process1_KeepAlive=0",
                "Process1_Delay=200",
                "Process2=",
                "Process2_KeepAlive=0",
                "Process2_Delay=200",
                "Process3=",
                "Process3_KeepAlive=0",
                "Process3_Delay=200",
                "Process4=",
                "Process4_KeepAlive=0",
                "Process4_Delay=200",
                "Process5=",
                "Process5_KeepAlive=0",
                "Process5_Delay=200",
                "Process6=",
                "Process6_KeepAlive=0",
                "Process6_Delay=200",
                "Process7=",
                "Process7_KeepAlive=0",
                "Process7_Delay=200",
                "Process8=",
                "Process8_KeepAlive=0",
                "Process8_Delay=200",
                "Process9=",
                "Process9_KeepAlive=0",
                "Process9_Delay=200",
                "Process10=",
                "Process10_KeepAlive=0",
                "Process10_Delay=200",
                "",
                "# The following processes will be run after cleanup",
                "[ProcessStart]",
                "Process1=",
                "Process1_Delay=200",
                "Process2=",
                "Process2_Delay=200",
                "Process3=",
                "Process3_Delay=200",
                "Process4=",
                "Process4_Delay=200",
                "Process5=",
                "Process5_Delay=200",
                "Process6=",
                "Process6_Delay=200",
                "Process7=",
                "Process7_Delay=200",
                "Process8=",
                "Process8_Delay=200",
                "Process9=",
                "Process9_Delay=200",
                "Process10=",
                "Process10_Delay=200"
            };

            File.WriteAllLines(SettingsFile, defaultSettings);
            LoadSettings();
        }

        private void LoadSettings()
        {
            settings.Clear();
            string currentSection = "";

            foreach (var line in File.ReadAllLines(SettingsFile))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    continue;
                }

                var parts = trimmed.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    var key = $"{currentSection}.{parts[0].Trim()}";
                    settings[key] = parts[1].Trim();
                }
            }
        }

        public int GetDelaySeconds()
        {
            if (settings.TryGetValue("General.DelaySeconds", out string value) && int.TryParse(value, out int delay))
            {
                return delay;
            }
            return 5; // Default
        }

        public string GetDisplayMode()
        {
            return settings.TryGetValue("OpenOverlay.DisplayMode", out string value) ? value : "Text";
        }

        public string GetOverlayText()
        {
            return settings.TryGetValue("OpenOverlay.Text", out string value) ? value : "Loading Explorer...";
        }

        public Color GetBackgroundColor()
        {
            return ParseColor("OpenOverlay.BackgroundColor", Color.Black);
        }

        public Color GetTextColor()
        {
            return ParseColor("OpenOverlay.TextColor", Color.White);
        }

        public string GetImagePath()
        {
            return settings.TryGetValue("OpenOverlay.ImagePath", out string value) ? value : string.Empty;
        }

        public Color GetCloseButtonBackgroundColor()
        {
            return ParseColor("CloseButton.BackgroundColor", Color.DarkRed);
        }

        public Color GetCloseButtonTextColor()
        {
            return ParseColor("CloseButton.TextColor", Color.White);
        }

        public string GetCloseButtonText()
        {
            return settings.TryGetValue("CloseButton.Text", out string value) ? value : "Close Explorer";
        }

        public int GetCloseButtonX()
        {
            if (settings.TryGetValue("CloseButton.X", out string value) && int.TryParse(value, out int x))
            {
                return x;
            }
            return 0; // Default to left edge
        }

        public string GetCloseButtonY()
        {
            return settings.TryGetValue("CloseButton.Y", out string value) ? value : "auto";
        }

        public int GetCloseButtonWidth()
        {
            if (settings.TryGetValue("CloseButton.Width", out string value) && int.TryParse(value, out int width))
            {
                return width;
            }
            return 200; // Default width
        }

        public int GetCleanupDelay()
        {
            if (settings.TryGetValue("CloseButton.CleanupDelay", out string value) && int.TryParse(value, out int delay))
            {
                return delay;
            }
            return 5000; // Default 5 seconds
        }

        public List<ProcessCleanupInfo> GetProcessCleanupList()
        {
            var cleanupList = new List<ProcessCleanupInfo>();

            for (int i = 1; i <= 10; i++)
            {
                string processName = settings.TryGetValue($"ProcessCleanup.Process{i}", out string name) ? name : string.Empty;
                
                if (string.IsNullOrWhiteSpace(processName))
                    continue;

                int keepAlive = 0;
                if (settings.TryGetValue($"ProcessCleanup.Process{i}_KeepAlive", out string keepAliveStr))
                {
                    int.TryParse(keepAliveStr, out keepAlive);
                }

                int delay = 200;
                if (settings.TryGetValue($"ProcessCleanup.Process{i}_Delay", out string delayStr))
                {
                    int.TryParse(delayStr, out delay);
                }

                cleanupList.Add(new ProcessCleanupInfo
                {
                    ProcessName = processName,
                    KeepAlive = keepAlive,
                    DelayMs = delay
                });
            }

            return cleanupList;
        }

        public List<ProcessStartInfo> GetProcessStartList()
        {
            var startList = new List<ProcessStartInfo>();

            for (int i = 1; i <= 10; i++)
            {
                string filePath = settings.TryGetValue($"ProcessStart.Process{i}", out string path) ? path : string.Empty;
                
                if (string.IsNullOrWhiteSpace(filePath))
                    continue;

                int delay = 200;
                if (settings.TryGetValue($"ProcessStart.Process{i}_Delay", out string delayStr))
                {
                    int.TryParse(delayStr, out delay);
                }

                startList.Add(new ProcessStartInfo
                {
                    FilePath = filePath,
                    DelayMs = delay
                });
            }

            return startList;
        }

        public Color GetCloseOverlayBackgroundColor()
        {
            return ParseColor("CloseOverlay.BackgroundColor", Color.Black);
        }

        public Color GetCloseOverlayTextColor()
        {
            return ParseColor("CloseOverlay.TextColor", Color.White);
        }

        public string GetCloseOverlayText()
        {
            return settings.TryGetValue("CloseOverlay.Text", out string value) ? value : "Please Wait...";
        }

        public string GetStartupAppPath()
        {
            return settings.TryGetValue("StartupApp.AppPath", out string value) ? value : "explorer.exe";
        }

        public string GetStartupAppArguments()
        {
            return settings.TryGetValue("StartupApp.AppArguments", out string value) ? value : string.Empty;
        }

        public int GetStartupAppDelaySeconds()
        {
            if (settings.TryGetValue("StartupApp.DelaySeconds", out string value) && int.TryParse(value, out int delay))
            {
                return delay;
            }
            return 1; // Default 1 second
        }

        public bool GetOpenMaximizedExplorer()
        {
            if (settings.TryGetValue("StartupApp.OpenMaximizedExplorer", out string value))
            {
                return value.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
            return true; // Default true
        }

        private Color ParseColor(string key, Color defaultColor)
        {
            if (settings.TryGetValue(key, out string value))
            {
                var parts = value.Split(',');
                if (parts.Length == 3 &&
                    int.TryParse(parts[0].Trim(), out int r) &&
                    int.TryParse(parts[1].Trim(), out int g) &&
                    int.TryParse(parts[2].Trim(), out int b))
                {
                    return Color.FromArgb(r, g, b);
                }
            }
            return defaultColor;
        }
    }
}
