using System.Diagnostics;
using System.Runtime.InteropServices;

namespace KioskShellHelper;

public partial class OverlayForm : Form
{
    private SettingsManager settings;
    private Button closeButton;
    private Label textLabel;
    private PictureBox imageBox;
    private System.Windows.Forms.Timer delayTimer;
    private System.Windows.Forms.Timer positionTimer;
    private int remainingSeconds;
    private string buttonYValue = "auto";
    private bool isFirstPositionUpdate = true;

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_MAXIMIZE = 3;

    public OverlayForm()
    {
        InitializeComponent();
        settings = new SettingsManager();
        InitializeOverlay();
        StartExplorerAndDelay();
    }

    private void InitializeOverlay()
    {
        // Make form fullscreen
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;
        this.BackColor = settings.GetBackgroundColor();

        // Create display based on mode
        if (settings.GetDisplayMode().Equals("Image", StringComparison.OrdinalIgnoreCase))
        {
            InitializeImageDisplay();
        }
        else
        {
            InitializeTextDisplay();
        }

        // Create close button
        InitializeCloseButton();
    }

    private void InitializeTextDisplay()
    {
        textLabel = new Label
        {
            Text = settings.GetOverlayText(),
            ForeColor = settings.GetTextColor(),
            Font = new Font("Arial", 48, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(textLabel);
    }

    private void InitializeImageDisplay()
    {
        imageBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.StretchImage
        };

        string imagePath = settings.GetImagePath();
        if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                imageBox.Image = Image.FromFile(imagePath);
            }
            catch
            {
                // If image loading fails, fallback to text
                InitializeTextDisplay();
                return;
            }
        }
        else
        {
            // No valid image, fallback to text
            InitializeTextDisplay();
            return;
        }

        this.Controls.Add(imageBox);
    }

    private void InitializeCloseButton()
    {
        int taskbarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
        if (taskbarHeight == 0) taskbarHeight = 40; // Default if taskbar hidden

        int buttonWidth = settings.GetCloseButtonWidth();
        int buttonX = settings.GetCloseButtonX();

        buttonYValue = settings.GetCloseButtonY();

        closeButton = new Button
        {
            Text = settings.GetCloseButtonText(),
            BackColor = settings.GetCloseButtonBackgroundColor(),
            ForeColor = settings.GetCloseButtonTextColor(),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Arial", 12, FontStyle.Bold),
            Width = buttonWidth,
            Height = taskbarHeight,
            Left = buttonX
        };

        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.Click += CloseButton_Click;
        this.Controls.Add(closeButton);
        closeButton.BringToFront();

        // Initial position calculation
        UpdateButtonPosition();

        // Setup timer to recalculate position every 15 seconds
        positionTimer = new System.Windows.Forms.Timer
        {
            Interval = 15000 // 15 seconds
        };
        positionTimer.Tick += PositionTimer_Tick;
        positionTimer.Start();
    }

    private void PositionTimer_Tick(object sender, EventArgs e)
    {
        UpdateButtonPosition();
    }

    private void UpdateButtonPosition()
    {
        if (closeButton == null) return;

        int taskbarHeight = Screen.PrimaryScreen.Bounds.Height - Screen.PrimaryScreen.WorkingArea.Height;
        if (taskbarHeight == 0) taskbarHeight = 40; // Default if taskbar hidden

        // Update button height to match taskbar
        closeButton.Height = taskbarHeight;

        int buttonY;

        // On first update, use fixed position from bottom
        if (isFirstPositionUpdate)
        {
            buttonY = Screen.PrimaryScreen.Bounds.Height - 150;
            isFirstPositionUpdate = false;
        }
        else
        {
            // After first update, use configured position logic
            if (buttonYValue.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                // Position just above taskbar
                buttonY = Screen.PrimaryScreen.WorkingArea.Height - taskbarHeight;
            }
            else if (int.TryParse(buttonYValue, out int customY))
            {
                buttonY = customY;
            }
            else
            {
                // Default to just above taskbar
                buttonY = Screen.PrimaryScreen.WorkingArea.Height - taskbarHeight;
            }
        }

        closeButton.Top = buttonY;
    }

    private void StartExplorerAndDelay()
    {
        // Launch configured startup app
        try
        {
            string appPath = settings.GetStartupAppPath();
            string appArgs = settings.GetStartupAppArguments();

            // Validate that the file exists (only for full paths, not system commands)
            if (Path.IsPathRooted(appPath) && !File.Exists(appPath))
            {
                MessageBox.Show($"Startup application not found: {appPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true
            };

            // Only set working directory for full paths
            if (Path.IsPathRooted(appPath))
            {
                startInfo.WorkingDirectory = Path.GetDirectoryName(appPath) ?? Environment.CurrentDirectory;
            }

            if (!string.IsNullOrWhiteSpace(appArgs))
            {
                startInfo.Arguments = appArgs;
            }

            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Setup delay timer
        remainingSeconds = settings.GetStartupAppDelaySeconds();
        delayTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000 // 1 second
        };
        delayTimer.Tick += DelayTimer_Tick;
        delayTimer.Start();
    }

    private void DelayTimer_Tick(object sender, EventArgs e)
    {
        remainingSeconds--;

        if (remainingSeconds <= 0)
        {
            delayTimer?.Stop();
            
            // Only open maximized explorer if enabled
            if (settings.GetOpenMaximizedExplorer())
            {
                OpenMaximizedExplorer();
            }
            
            TransitionToPersistentButton();
        }
    }

    private void TransitionToPersistentButton()
    {
        // Hide overlay content
        if (textLabel != null)
        {
            textLabel.Visible = false;
        }
        if (imageBox != null)
        {
            imageBox.Visible = false;
        }

        // Make form transparent and click-through except for the button
        this.BackColor = Color.Lime;
        this.TransparencyKey = Color.Lime;
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;

        // Keep close button visible and ensure it's on top
        if (closeButton != null)
        {
            closeButton.BringToFront();
        }
    }

    private void OpenMaximizedExplorer()
    {
        try
        {
            // Open a new explorer window
            Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                UseShellExecute = true
            });

            // Wait a moment for window to appear
            System.Threading.Thread.Sleep(500);

            // Find and maximize the window
            IntPtr hwnd = FindWindow("CabinetWClass", null);
            if (hwnd != IntPtr.Zero)
            {
                ShowWindow(hwnd, SW_MAXIMIZE);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open Explorer window: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CloseButton_Click(object sender, EventArgs e)
    {
        // Build dynamic confirmation message
        var processList = settings.GetProcessCleanupList();
        string processNames = processList.Count > 0 
            ? string.Join(", ", processList.Select(p => p.ProcessName))
            : "configured processes";
        
        var result = MessageBox.Show(
            $"This will clean up the following processes: {processNames}\n\nAre you sure?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result == DialogResult.Yes)
        {
            // Stop position timer
            positionTimer?.Stop();
            positionTimer?.Dispose();

            // Show close overlay first
            ShowCloseOverlay();
            
            // Force update and wait for overlay to render
            Application.DoEvents();
            System.Threading.Thread.Sleep(100);

            // Start cleanup process in background
            Task.Run(() =>
            {
                // Wait configured delay before killing processes
                int cleanupDelay = settings.GetCleanupDelay();
                System.Threading.Thread.Sleep(cleanupDelay);
                
                CleanupConfiguredProcesses();
                
                // Close the overlay on UI thread before starting new processes
                this.Invoke((Action)(() => 
                {
                    this.Close();
                }));
                
                // Wait a moment for overlay to close
                System.Threading.Thread.Sleep(200);
                
                // Now start the new processes
                StartConfiguredProcesses();
                
                // Exit application
                Application.Exit();
            });
        }
    }

    private void ShowCloseOverlay()
    {
        // Hide existing overlay content
        if (textLabel != null)
        {
            textLabel.Visible = false;
        }
        if (imageBox != null)
        {
            imageBox.Visible = false;
        }
        if (closeButton != null)
        {
            closeButton.Visible = false;
        }

        // Reset form to fullscreen opaque overlay
        this.BackColor = settings.GetCloseOverlayBackgroundColor();
        this.TransparencyKey = Color.Empty;
        this.FormBorderStyle = FormBorderStyle.None;
        this.WindowState = FormWindowState.Maximized;
        this.TopMost = true;

        // Show close overlay text
        var closeOverlayLabel = new Label
        {
            Text = settings.GetCloseOverlayText(),
            ForeColor = settings.GetCloseOverlayTextColor(),
            Font = new Font("Arial", 48, FontStyle.Bold),
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        this.Controls.Add(closeOverlayLabel);
        closeOverlayLabel.BringToFront();

        this.Refresh();
    }

    private void CleanupConfiguredProcesses()
    {
        try
        {
            var processList = settings.GetProcessCleanupList();

            foreach (var processInfo in processList)
            {
                if (string.IsNullOrWhiteSpace(processInfo.ProcessName))
                    continue;

                try
                {
                    // Strip .exe extension if present (Process.GetProcessesByName doesn't use extensions)
                    string processName = processInfo.ProcessName.Trim();
                    if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        processName = processName.Substring(0, processName.Length - 4);
                    }

                    // Get all processes and filter by name (case-insensitive)
                    var allProcesses = Process.GetProcesses();
                    var matchingProcesses = allProcesses.Where(p =>
                    {
                        try
                        {
                            // Match against process name
                            if (p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                                return true;
                            
                            // Also try matching against MainModule filename (for cases like "Windows Explorer")
                            if (p.MainModule?.ModuleName != null && p.MainModule.ModuleName.Equals(processName + ".exe", StringComparison.OrdinalIgnoreCase))
                                return true;
                            
                            // Try matching the configured name with spaces replaced by process name format
                            string processNameNoSpaces = processName.Replace(" ", "");
                            if (p.ProcessName.Equals(processNameNoSpaces, StringComparison.OrdinalIgnoreCase))
                                return true;
                        }
                        catch { }
                        return false;
                    }).ToArray();

                    var processes = matchingProcesses;
                    
                    // Sort by start time (oldest first)
                    var sortedProcesses = processes.OrderBy(p => 
                    {
                        try { return p.StartTime; }
                        catch { return DateTime.MinValue; }
                    }).ToArray();
                    
                    // If keepAlive is 0, kill all instances
                    // If keepAlive is 1, keep the first (oldest) and kill the rest, etc.
                    int toKill = sortedProcesses.Length - processInfo.KeepAlive;
                    
                    if (toKill > 0)
                    {
                        // Kill from the end (newest processes first), keeping the oldest ones
                        for (int i = sortedProcesses.Length - 1; i >= processInfo.KeepAlive; i--)
                        {
                            try
                            {
                                KillProcessTree(sortedProcesses[i].Id);
                            }
                            catch { }
                        }
                    }

                    // Wait the configured delay before processing next item
                    if (processInfo.DelayMs > 0)
                    {
                        System.Threading.Thread.Sleep(processInfo.DelayMs);
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during process cleanup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void KillProcessTree(int pid)
    {
        try
        {
            // Use taskkill to force kill the process tree
            var process = new Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/PID {pid} /T /F",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            process.Start();
            process.WaitForExit(5000);
        }
        catch { }
    }

    private void StartConfiguredProcesses()
    {
        try
        {
            var processList = settings.GetProcessStartList();

            foreach (var processInfo in processList)
            {
                if (string.IsNullOrWhiteSpace(processInfo.FilePath))
                    continue;

                try
                {
                    // Check if file exists
                    if (!File.Exists(processInfo.FilePath))
                    {
                        MessageBox.Show($"File not found: {processInfo.FilePath}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        continue;
                    }

                    // Start the process
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = processInfo.FilePath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.GetDirectoryName(processInfo.FilePath) ?? Environment.CurrentDirectory
                    };

                    Process.Start(startInfo);

                    // Wait the configured delay before processing next item
                    if (processInfo.DelayMs > 0)
                    {
                        System.Threading.Thread.Sleep(processInfo.DelayMs);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to start {processInfo.FilePath}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during process startup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
