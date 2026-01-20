using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CzkawkaWin.Events;
using CzkawkaWin.Models;
using CzkawkaWin.Services;
using Microsoft.Win32;

namespace CzkawkaWin.Views
{
    /// <summary>
    /// Scanner tab for configuring and executing duplicate file scans.
    /// </summary>
    public partial class ScannerTab : UserControl
    {
        // Services
        private readonly CzkawkaService _czkawkaService;
        private readonly ConfigurationService _configService;
        
        // State
        private CancellationTokenSource? _scanCts;
        private Stopwatch? _scanStopwatch;
        private bool _isScanning;
        
        // Data
        private readonly ObservableCollection<string> _directories;
        
        // Reference to FiltersTab (set by MainWindow)
        private FiltersTab? _filtersTab;

        /// <summary>
        /// Event raised when a scan completes.
        /// </summary>
        public event EventHandler<ScanCompletedEventArgs>? ScanCompleted;
        
        /// <summary>
        /// Event raised when a scan starts.
        /// </summary>
        public event EventHandler? ScanStarted;
        
        /// <summary>
        /// Gets whether a scan is currently running.
        /// </summary>
        public bool IsScanning => _isScanning;
        
        /// <summary>
        /// Sets the reference to the FiltersTab for accessing filter values.
        /// </summary>
        public void SetFiltersTab(FiltersTab filtersTab)
        {
            _filtersTab = filtersTab;
        }

        public ScannerTab()
        {
            InitializeComponent();
            
            // Initialize services
            _czkawkaService = new CzkawkaService();
            _configService = new ConfigurationService();
            
            // Initialize collections
            _directories = new ObservableCollection<string>();
            DirectoriesList.ItemsSource = _directories;
            
            // Subscribe to service events
            _czkawkaService.OutputReceived += OnOutputReceived;
            _czkawkaService.ErrorReceived += OnErrorReceived;
            _czkawkaService.ProcessExited += OnProcessExited;
            
            // Update directories visibility
            UpdateDirectoriesVisibility();
            
            // Check Czkawka CLI availability
            CheckCzkawkaAvailability();
            
            // Load profiles
            LoadProfiles();
            
            // Load last used configuration
            _ = LoadLastConfigurationAsync();
        }

        #region Initialization

        private void CheckCzkawkaAvailability()
        {
            if (_czkawkaService.IsAvailable)
            {
                TxtCzkawkaStatus.Text = "✓ Czkawka CLI Ready";
                TxtCzkawkaStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                BtnStartScan.IsEnabled = true;
            }
            else
            {
                TxtCzkawkaStatus.Text = "✗ Czkawka CLI Not Found";
                TxtCzkawkaStatus.Foreground = System.Windows.Media.Brushes.Tomato;
                BtnStartScan.IsEnabled = false;
                AppendLog($"[ERROR] Czkawka CLI not found at: {_czkawkaService.ExecutablePath}");
                AppendLog("Please ensure czkawka_cli.exe is in the application directory.");
            }
        }

        private void LoadProfiles()
        {
            CmbProfiles.Items.Clear();
            CmbProfiles.Items.Add(new ComboBoxItem { Content = "Default", IsSelected = true });
            
            foreach (var profile in _configService.GetSavedProfiles())
            {
                CmbProfiles.Items.Add(new ComboBoxItem { Content = profile });
            }
        }

        private async Task LoadLastConfigurationAsync()
        {
            var config = await _configService.LoadLastUsedConfigurationAsync();
            if (config != null)
            {
                ApplyConfiguration(config);
                AppendLog("[INFO] Loaded last used configuration.");
            }
        }

        #endregion

        #region Directory Management

        private void BtnAddDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Directory to Scan",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var folder in dialog.FolderNames)
                {
                    if (!_directories.Contains(folder))
                    {
                        _directories.Add(folder);
                        AppendLog($"[+] Added: {folder}");
                    }
                }
                UpdateDirectoriesVisibility();
            }
        }

        private void BtnRemoveDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (DirectoriesList.SelectedItem is string selected)
            {
                _directories.Remove(selected);
                AppendLog($"[-] Removed: {selected}");
                UpdateDirectoriesVisibility();
            }
        }


        private void DirectoriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnRemoveDirectory.IsEnabled = DirectoriesList.SelectedItem != null;
        }

        private void UpdateDirectoriesVisibility()
        {
            var hasDirectories = _directories.Count > 0;
            TxtNoDirectories.Visibility = hasDirectories ? Visibility.Collapsed : Visibility.Visible;
            DirectoriesList.Visibility = hasDirectories ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion


        #region Options

        private void CmbSearchMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Show/hide hash type based on search method
            bool isHashMethod = CmbSearchMethod.SelectedIndex == 2;
            if (TxtHashTypeLabel != null)
            {
                TxtHashTypeLabel.Visibility = isHashMethod ? Visibility.Visible : Visibility.Collapsed;
                CmbHashType.Visibility = isHashMethod ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion

        #region Configuration Management

        private ScanConfiguration BuildConfiguration()
        {
            var config = new ScanConfiguration
            {
                SearchDirectories = _directories.ToList(),
                Method = (SearchMethod)CmbSearchMethod.SelectedIndex,
                HashAlgorithm = (HashType)CmbHashType.SelectedIndex,
                Recursive = ChkRecursive.IsChecked ?? true,
                UseCache = ChkUseCache.IsChecked ?? true,
                CaseSensitive = ChkCaseSensitive.IsChecked ?? false,
                AllowHardLinks = ChkAllowHardLinks.IsChecked ?? false
            };
            
            // Get filter values from FiltersTab
            if (_filtersTab != null)
            {
                _filtersTab.ApplyToConfiguration(config);
            }
            
            return config;
        }

        private void ApplyConfiguration(ScanConfiguration config)
        {
            // Directories
            _directories.Clear();
            foreach (var dir in config.SearchDirectories)
            {
                _directories.Add(dir);
            }
            UpdateDirectoriesVisibility();
            
            
            // Search options
            CmbSearchMethod.SelectedIndex = (int)config.Method;
            CmbHashType.SelectedIndex = (int)config.HashAlgorithm;
            ChkRecursive.IsChecked = config.Recursive;
            ChkUseCache.IsChecked = config.UseCache;
            ChkCaseSensitive.IsChecked = config.CaseSensitive;
            ChkAllowHardLinks.IsChecked = config.AllowHardLinks;
            
            // Apply filter values to FiltersTab
            _filtersTab?.ApplyConfiguration(config);
        }

        #endregion



        #region Profile Management

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is ComboBoxItem selected)
            {
                var profileName = selected.Content?.ToString();
                     
                if (BtnDeleteProfile != null)
                {
                    BtnDeleteProfile.IsEnabled = profileName != "Default";
                }
                
                if (profileName != "Default")
                {
                    _ = LoadProfileAsync(profileName!);
                }
            }
        }

        private async Task LoadProfileAsync(string profileName)
        {
            var config = await _configService.LoadConfigurationAsync(profileName);
            if (config != null)
            {
                ApplyConfiguration(config);
                AppendLog($"[INFO] Loaded profile: {profileName}");
            }
        }

        private async void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Save Profile", "Enter profile name:");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
            {
                var profileName = dialog.InputText.Trim();
                
                if (profileName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Cannot use 'Default' as profile name.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                var config = BuildConfiguration();
                await _configService.SaveConfigurationAsync(config, profileName);
                
                LoadProfiles();
                
                // Select the new profile
                for (int i = 0; i < CmbProfiles.Items.Count; i++)
                {
                    if (CmbProfiles.Items[i] is ComboBoxItem item && 
                        item.Content?.ToString() == profileName)
                    {
                        CmbProfiles.SelectedIndex = i;
                        break;
                    }
                }
                
                AppendLog($"[SAVE] Saved profile: {profileName}");
            }
        }

        private async void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is ComboBoxItem selected)
            {
                var profileName = selected.Content?.ToString();
                if (profileName == "Default") return;
                
                var result = MessageBox.Show(
                    $"Delete profile '{profileName}'?",
                    "Delete Profile",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _configService.DeleteProfileAsync(profileName!);
                    LoadProfiles();
                    AppendLog($"[DELETE] Deleted profile: {profileName}");
                }
            }
        }

        #endregion

        #region Scan Execution

        private async void BtnStartScan_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;
            
            // Validate
            if (_directories.Count == 0)
            {
                MessageBox.Show("Please add at least one directory to scan.", 
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Build and validate configuration
            var config = BuildConfiguration();
            if (!config.IsValid())
            {
                MessageBox.Show(config.GetValidationError(), 
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Save as last used
            await _configService.SaveLastUsedConfigurationAsync(config);
            
            // Start scan
            _isScanning = true;
            _scanCts = new CancellationTokenSource();
            _scanStopwatch = Stopwatch.StartNew();
            
            SetScanningState(true);
            ClearLog();
            
            // Notify listeners that scan started
            ScanStarted?.Invoke(this, EventArgs.Empty);
            
            AppendLog("[START] Starting duplicate file scan...");
            AppendLog($"[DIR] Directories: {string.Join(", ", config.SearchDirectories)}");
            AppendLog($"[CFG] Method: {config.Method}, Hash: {config.HashAlgorithm}");
            AppendLog($"[FILTER] Size filter: {config.MinimalFileSize / 1024} KB - {(config.MaximalFileSize < long.MaxValue ? $"{config.MaximalFileSize / 1024} KB" : "No limit")}");
            if (config.AllowedExtensions.Count > 0)
            {
                AppendLog($"[EXT] Extensions: {string.Join(", ", config.AllowedExtensions)}");
            }
            AppendLog("========================================");
            
            
            try
            {
                var result = await _czkawkaService.ExecuteScanAsync(config, _scanCts.Token);
                
                _scanStopwatch.Stop();
                var duration = _scanStopwatch.Elapsed;
                
                AppendLog("========================================");
                
                if (result.IsSuccess)
                {
                    AppendLog($"[OK] Scan completed in {FormatDuration(duration)}");
                    
                    if (result.IsEmpty)
                    {
                        AppendLog("[INFO] No duplicates found.");
                        ScanCompleted?.Invoke(this, ScanCompletedEventArgs.Successful(
                            result.JsonContent!, result.JsonFilePath!, duration, isEmpty: true));
                    }
                    else
                    {
                        AppendLog($"[SAVE] Results saved to: {result.JsonFilePath}");
                        ScanCompleted?.Invoke(this, ScanCompletedEventArgs.Successful(
                            result.JsonContent!, result.JsonFilePath!, duration));
                    }
                }
                else if (result.IsCancelled)
                {
                    AppendLog($"[STOP] Scan cancelled after {FormatDuration(duration)}");
                    ScanCompleted?.Invoke(this, ScanCompletedEventArgs.CancelledByUser(duration));
                }
                else
                {
                    AppendLog($"[ERROR] Scan failed: {result.ErrorMessage}");
                    ScanCompleted?.Invoke(this, ScanCompletedEventArgs.Failed(
                        result.ErrorMessage!, duration));
                }
            }
            catch (Exception ex)
            {
                _scanStopwatch?.Stop();
                AppendLog($"[ERROR] Error: {ex.Message}");
                ScanCompleted?.Invoke(this, ScanCompletedEventArgs.Failed(
                    ex.Message, _scanStopwatch?.Elapsed ?? TimeSpan.Zero));
            }
            finally
            {
                _isScanning = false;
                _scanCts?.Dispose();
                _scanCts = null;
                SetScanningState(false);
            }
        }

        private void BtnStopScan_Click(object sender, RoutedEventArgs e)
        {
            StopCurrentScan();
        }
        
        /// <summary>
        /// Stops the currently running scan. Can be called externally.
        /// </summary>
        public void StopCurrentScan()
        {
            if (_scanCts != null && !_scanCts.IsCancellationRequested)
            {
                AppendLog("[STOP] Stopping scan...");
                _scanCts.Cancel();
                _czkawkaService.StopScan();
            }
        }

        private void SetScanningState(bool isScanning)
        {
            BtnStartScan.IsEnabled = !isScanning;
            BtnStopScan.IsEnabled = isScanning;
            BtnAddDirectory_Click_IsEnabled(!isScanning);
            BtnRemoveDirectory.IsEnabled = !isScanning && DirectoriesList.SelectedItem != null;
            
            // Options panels
            CmbSearchMethod.IsEnabled = !isScanning;
            CmbHashType.IsEnabled = !isScanning;
            ChkRecursive.IsEnabled = !isScanning;
            ChkUseCache.IsEnabled = !isScanning;
            ChkCaseSensitive.IsEnabled = !isScanning;
            ChkAllowHardLinks.IsEnabled = !isScanning;
            
            // Profiles
            CmbProfiles.IsEnabled = !isScanning;
            
            // Progress
            ScanProgressBar.IsIndeterminate = isScanning;
            TxtScanStatus.Text = isScanning ? "Scanning..." : "Ready";
        }

        private void BtnAddDirectory_Click_IsEnabled(bool enabled)
        {
            // Find the Add Folder button by iterating - it doesn't have x:Name in parent scope
            // This is handled by the general IsEnabled on parent panels
        }

        #endregion

        #region Service Event Handlers

        private void OnOutputReceived(object? sender, string output)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AppendLog(output);
            });
        }

        private void OnErrorReceived(object? sender, string error)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AppendLog($"[WARN] {error}");
            });
        }

        private void OnProcessExited(object? sender, int exitCode)
        {
            Dispatcher.BeginInvoke(() =>
            {
                AppendLog($"[INFO] Process exited with code: {exitCode}");
            });
        }

        #endregion

        #region Log Management

        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logLine = $"[{timestamp}] {message}\n";
            
            TxtOutputLog.AppendText(logLine);
            LogScrollViewer.ScrollToEnd();
        }

        private void ClearLog()
        {
            TxtOutputLog.Clear();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtOutputLog.Text = "Log cleared. Ready to scan.";
        }

        #endregion

        #region Utilities

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return duration.ToString(@"h\:mm\:ss");
            if (duration.TotalMinutes >= 1)
                return duration.ToString(@"m\:ss");
            return $"{duration.TotalSeconds:F1}s";
        }

        #endregion
    }

    /// <summary>
    /// Simple input dialog for profile names.
    /// </summary>
    public class InputDialog : Window
    {
        private readonly TextBox _textBox;
        
        public string InputText => _textBox.Text;
        
        public InputDialog(string title, string prompt)
        {
            Title = title;
            Width = 350;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.White;
            
            var grid = new Grid { Margin = new Thickness(15) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var label = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 8) };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);
            
            _textBox = new TextBox { Padding = new Thickness(5) };
            Grid.SetRow(_textBox, 1);
            grid.Children.Add(_textBox);
            
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);
            
            var okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            okButton.Click += (s, e) => { DialogResult = true; Close(); };
            buttonPanel.Children.Add(okButton);
            
            var cancelButton = new Button { Content = "Cancel", Width = 75, IsCancel = true };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
            buttonPanel.Children.Add(cancelButton);
            
            grid.Children.Add(buttonPanel);
            Content = grid;
            
            Loaded += (s, e) => _textBox.Focus();
        }
    }
}
