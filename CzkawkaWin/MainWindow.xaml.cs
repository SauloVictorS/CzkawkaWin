using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FFMpegCore;
using CzkawkaWin.Events;
using CzkawkaWin.Views;

namespace CzkawkaWin
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _videoTimer;
        private bool _isDraggingSeekBar = false;
        private TimeSpan _videoDuration = TimeSpan.Zero;
        private List<DuplicateGroup>? _currentResults;
        private string? _currentJsonFilePath;

        // Application version from assembly
        public string AppVersion => GetType().Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        
        // Commands for keyboard shortcuts
        public ICommand OpenJsonCommand { get; }
        public ICommand ExportJsonCommand { get; }
        public ICommand ClearResultsCommand { get; }
        public ICommand StartScanCommand { get; }
        public ICommand StopScanCommand { get; }

        public MainWindow()
        {
            // Initialize commands before InitializeComponent
            OpenJsonCommand = new RelayCommand(_ => LoadJsonReport());
            ExportJsonCommand = new RelayCommand(_ => MenuExportJson_Click(this, new RoutedEventArgs()), 
                _ => _currentResults != null && _currentResults.Count > 0);
            ClearResultsCommand = new RelayCommand(_ => MenuClearResults_Click(this, new RoutedEventArgs()),
                _ => _currentResults != null && _currentResults.Count > 0);
            StartScanCommand = new RelayCommand(_ => ExecuteStartScan());
            StopScanCommand = new RelayCommand(_ => ScannerTabControl?.StopCurrentScan(),
                _ => ScannerTabControl?.IsScanning == true);
            
            InitializeComponent();
            DataContext = this; // Set DataContext to enable version binding

            // Configure FFmpeg binary path (ffmpeg folder in executable directory)
            var ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg");
            GlobalFFOptions.Configure(options => options.BinaryFolder = ffmpegPath);


            // Timer to update video position
            _videoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _videoTimer.Tick += VideoTimer_Tick;
            
            // Connect FiltersTab to ScannerTab
            ScannerTabControl.SetFiltersTab(FiltersTabControl);
            
            // Subscribe to Scanner events
            ScannerTabControl.ScanStarted += OnScanStarted;
            ScannerTabControl.ScanCompleted += OnScanCompleted;
            
            // Start on Scanner tab by default
            MainTabControl.SelectedIndex = 0;
        }

        // ============ SCANNER EVENT HANDLER ============
        
        private void OnScanStarted(object? sender, EventArgs e)
        {
            MenuStopScanItem.IsEnabled = true;
            TxtStatusBar.Text = "Scanning for duplicate files...";
        }
        
        private void OnScanCompleted(object? sender, ScanCompletedEventArgs e)
        {
            MenuStopScanItem.IsEnabled = false;
            
            if (e.Success && !string.IsNullOrEmpty(e.JsonContent))
            {
                _currentJsonFilePath = e.JsonFilePath;
                
                if (e.IsEmpty)
                {
                    TxtStatus.Text = "No duplicates found";
                    TxtStatusBar.Text = $"Scan completed in {FormatDuration(e.Duration)} - No duplicate files found.";
                    MessageBox.Show(
                        "Scan completed successfully, but no duplicate files were found.",
                        "Scan Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    // Load results from scan
                    LoadResultsFromJson(e.JsonContent);
                    
                    // Switch to Results tab (index 2: Scanner=0, Filters=1, Results=2)
                    MainTabControl.SelectedIndex = 2;
                    
                    TxtStatus.Text = $"{_currentResults?.Count ?? 0} groups";
                    TxtStatusBar.Text = $"Scan completed in {FormatDuration(e.Duration)}. Found {_currentResults?.Count ?? 0} duplicate groups.";
                }
            }
            else if (e.Cancelled)
            {
                TxtStatus.Text = "Cancelled";
                TxtStatusBar.Text = "Scan was cancelled by user.";
            }
            else if (!e.Success)
            {
                TxtStatus.Text = "Error";
                TxtStatusBar.Text = $"Scan failed: {e.ErrorMessage}";
                MessageBox.Show(
                    $"Scan failed:\n\n{e.ErrorMessage}",
                    "Scan Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private void LoadResultsFromJson(string jsonContent)
        {
            try
            {
                var groups = ParseJsonContent(jsonContent);
                
                _currentResults = groups;
                GroupsList.ItemsSource = groups;
                
                TxtGroupCount.Text = $"({groups.Count})";
                MenuExportJsonItem.IsEnabled = groups.Count > 0;
                MenuClearResultsItem.IsEnabled = groups.Count > 0;
                EmptyGroupsState.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading results: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Parse JSON content supporting both Czkawka CLI and legacy formats.
        /// CLI format: Dictionary&lt;size, List&lt;List&lt;FileItem&gt;&gt;&gt;
        /// Legacy format: Dictionary&lt;key, List&lt;FileItem&gt;&gt;
        /// </summary>
        private static List<DuplicateGroup> ParseJsonContent(string jsonContent)
        {
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            
            // Detect format by checking the structure of the first value
            bool isCliFormat = false;
            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array && property.Value.GetArrayLength() > 0)
                {
                    var firstElement = property.Value[0];
                    // CLI format has nested arrays: [[{...}, {...}]]
                    // Legacy format has direct objects: [{...}, {...}]
                    isCliFormat = firstElement.ValueKind == JsonValueKind.Array;
                    break;
                }
            }
            
            return isCliFormat 
                ? ProcessCliFormat(jsonContent) 
                : ProcessLegacyFormat(jsonContent);
        }
        
        /// <summary>
        /// Process Czkawka CLI format: Dictionary&lt;size, List&lt;List&lt;FileItem&gt;&gt;&gt;
        /// </summary>
        private static List<DuplicateGroup> ProcessCliFormat(string jsonContent)
        {
            var rawData = JsonSerializer.Deserialize<Dictionary<string, List<List<FileItem>>>>(jsonContent);
            var resultList = new List<DuplicateGroup>();

            if (rawData != null)
            {
                foreach (var kvp in rawData)
                {
                    // Each size key can have multiple groups of duplicates
                    foreach (var duplicateGroup in kvp.Value)
                    {
                        if (duplicateGroup.Count > 0)
                        {
                            long size = duplicateGroup[0].Size;

                            resultList.Add(new DuplicateGroup
                            {
                                SizeBytes = size,
                                Count = duplicateGroup.Count,
                                Items = duplicateGroup
                            });
                        }
                    }
                }
            }

            resultList.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
            return resultList;
        }
        
        /// <summary>
        /// Process legacy/GUI format: Dictionary&lt;key, List&lt;FileItem&gt;&gt;
        /// </summary>
        private static List<DuplicateGroup> ProcessLegacyFormat(string jsonContent)
        {
            var rawData = JsonSerializer.Deserialize<Dictionary<string, List<FileItem>>>(jsonContent);
            var resultList = new List<DuplicateGroup>();

            if (rawData != null)
            {
                foreach (var kvp in rawData)
                {
                    if (kvp.Value.Count > 0)
                    {
                        long size = kvp.Value[0].Size;

                        resultList.Add(new DuplicateGroup
                        {
                            SizeBytes = size,
                            Count = kvp.Value.Count,
                            Items = kvp.Value
                        });
                    }
                }
            }

            resultList.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
            return resultList;
        }
        
        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
                return duration.ToString(@"h\:mm\:ss");
            if (duration.TotalMinutes >= 1)
                return duration.ToString(@"m\:ss\.f");
            return $"{duration.TotalSeconds:F1}s";
        }

        // ============ MENU HANDLERS ============
        
        private void MenuOpenJson_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Results tab and load JSON (index 2: Scanner=0, Filters=1, Results=2)
            MainTabControl.SelectedIndex = 2;
            LoadJsonReport();
        }
        
        private void MenuExportJson_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResults == null || _currentResults.Count == 0)
            {
                MessageBox.Show("No results to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var saveDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                Title = "Export Results",
                FileName = $"czkawka_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };
            
            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // If we have a current JSON file, just copy it
                    if (!string.IsNullOrEmpty(_currentJsonFilePath) && File.Exists(_currentJsonFilePath))
                    {
                        File.Copy(_currentJsonFilePath, saveDialog.FileName, overwrite: true);
                    }
                    else
                    {
                        // Rebuild JSON from current results
                        // Use Czkawka format: Dictionary<size, List<List<FileItem>>>
                        var exportData = new Dictionary<string, List<List<FileItem>>>();
                        foreach (var group in _currentResults)
                        {
                            string sizeKey = group.SizeBytes.ToString();
                            if (!exportData.ContainsKey(sizeKey))
                            {
                                exportData[sizeKey] = new List<List<FileItem>>();
                            }
                            exportData[sizeKey].Add(group.Items);
                        }
                        
                        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(saveDialog.FileName, json);
                    }
                    
                    TxtStatusBar.Text = $"Exported to: {Path.GetFileName(saveDialog.FileName)}";
                    MessageBox.Show($"Results exported successfully!\n\n{saveDialog.FileName}", 
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void MenuClearResults_Click(object sender, RoutedEventArgs e)
        {
            if (_currentResults == null || _currentResults.Count == 0)
            {
                MessageBox.Show("No results to clear.", "Clear Results", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                "Clear all results? This cannot be undone.",
                "Clear Results",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                // Clear results
                _currentResults = null;
                _currentJsonFilePath = null;
                GroupsList.ItemsSource = null;
                FilesList.ItemsSource = null;
                
                // Clear preview
                ClearPreview();
                
                // Show empty states
                EmptyGroupsState.Visibility = Visibility.Visible;
                EmptyFilesState.Visibility = Visibility.Visible;
                EmptyPreviewState.Visibility = Visibility.Visible;
                FileInfoPanel.Visibility = Visibility.Collapsed;
                
                // Update UI
                TxtGroupCount.Text = "";
                MenuExportJsonItem.IsEnabled = false;
                MenuClearResultsItem.IsEnabled = false;
                TxtStatus.Text = "Ready";
                TxtStatusBar.Text = "Results cleared. Load a JSON report or start a new scan.";
            }
        }
        
        private void MenuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void MenuNewScan_Click(object sender, RoutedEventArgs e)
        {
            // Switch to Scanner tab
            MainTabControl.SelectedIndex = 0;
        }
        
        private void MenuStopScan_Click(object sender, RoutedEventArgs e)
        {
            ScannerTabControl.StopCurrentScan();
        }
        
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow(AppVersion)
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }
        
        private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source != MainTabControl) return;
            
            // Update menu items state based on active tab (Results tab is now index 2)
            bool isResultsTab = MainTabControl.SelectedIndex == 2;
            bool hasResults = _currentResults != null && _currentResults.Count > 0;
            
            MenuExportJsonItem.IsEnabled = isResultsTab && hasResults;
            MenuClearResultsItem.IsEnabled = isResultsTab && hasResults;
        }

        // ============ JSON LOADING ============

        private async void BtnLoadJson_Click(object sender, RoutedEventArgs e)
        {
            LoadJsonReport();
        }
        
        private async void LoadJsonReport()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Czkawka Report"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtStatus.Text = "Loading...";
                TxtStatusBar.Text = "Loading and processing file... (Please wait)";

                try
                {
                    string path = openFileDialog.FileName;
                    _currentJsonFilePath = path;

                    // Run heavy processing on background thread to keep UI responsive
                    List<DuplicateGroup> groups = await Task.Run(() => LoadAndProcessData(path));

                    // Store results for export
                    _currentResults = groups;
                    
                    // Bind result to left panel list
                    GroupsList.ItemsSource = groups;

                    TxtStatus.Text = $"{groups.Count} groups";
                    TxtStatusBar.Text = $"Loaded {groups.Count} duplicate groups. Select a group to view files.";
                    TxtGroupCount.Text = $"({groups.Count})";
                    
                    // Enable menu items
                    MenuExportJsonItem.IsEnabled = groups.Count > 0;
                    MenuClearResultsItem.IsEnabled = groups.Count > 0;
                    
                    // Hide empty state when data is loaded
                    EmptyGroupsState.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtStatus.Text = "Error";
                    TxtStatusBar.Text = "Error loading file.";
                }
            }
        }

        // Optimized method to read and transform data (supports both CLI and legacy formats)
        private static List<DuplicateGroup> LoadAndProcessData(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return ParseJsonContent(json);
        }

        // ============ THUMBNAIL GENERATION ============

        // When a group is selected, generate thumbnails for all files in that group
        private async void GroupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GroupsList.SelectedItem is DuplicateGroup group)
            {
                // Hide empty state for files panel
                EmptyFilesState.Visibility = Visibility.Collapsed;
                TxtStatusBar.Text = $"Group selected: {group.Count} duplicate files ({group.HumanSize} each)";
                
                await GenerateThumbnailsForGroupAsync(group);
            }
            else
            {
                EmptyFilesState.Visibility = Visibility.Visible;
            }
        }

        // Generate thumbnails for all video/image files in a group
        private async Task GenerateThumbnailsForGroupAsync(DuplicateGroup group)
        {
            foreach (var fileItem in group.Items)
            {
                if (fileItem.Thumbnail != null) continue; // Already has thumbnail

                if (!File.Exists(fileItem.Path)) continue;

                try
                {
                    if (fileItem.IsImage)
                    {
                        await LoadImageThumbnailAsync(fileItem);
                    }
                    else if (fileItem.IsVideo)
                    {
                        await LoadVideoThumbnailAsync(fileItem);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error generating thumbnail for {fileItem.Path}: {ex.Message}");
                }
            }
        }

        // Load image thumbnail (scaled down for performance)
        private async Task LoadImageThumbnailAsync(FileItem fileItem)
        {
            await Task.Run(() =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.DecodePixelWidth = 50; // Small thumbnail
                        bitmap.UriSource = new Uri(fileItem.Path);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        fileItem.Thumbnail = bitmap;
                    });
                }
                catch { /* Ignore thumbnail errors */ }
            });
        }

        // Load video thumbnail using FFmpeg
        private async Task LoadVideoThumbnailAsync(FileItem fileItem)
        {
            try
            {
                // Create temp folder for thumbnails
                var tempFolder = Path.Combine(Path.GetTempPath(), "CzkawkaWin_Thumbnails");
                Directory.CreateDirectory(tempFolder);

                var thumbnailPath = Path.Combine(tempFolder, $"{Guid.NewGuid():N}.png");

                // Get video duration for capture position
                var mediaInfo = await FFProbe.AnalyseAsync(fileItem.Path);
                var duration = mediaInfo?.Duration ?? TimeSpan.Zero;
                var captureTime = duration.TotalSeconds > 2 ? TimeSpan.FromSeconds(duration.TotalSeconds / 4) : TimeSpan.Zero;

                // Store video metadata
                if (mediaInfo?.PrimaryVideoStream != null)
                {
                    var video = mediaInfo.PrimaryVideoStream;
                    fileItem.VideoInfo = $"📹 {video.Width}x{video.Height} | {video.CodecName?.ToUpper()} | {video.FrameRate:F1} fps | {FormatTime(duration)}";
                }

                // Generate thumbnail frame
                await FFMpegArguments
                    .FromFileInput(fileItem.Path, verifyExists: true, options => options.Seek(captureTime))
                    .OutputToFile(thumbnailPath, overwrite: true, options =>
                    {
                        options.WithFrameOutputCount(1).WithCustomArgument("-vf scale=50:-1");
                    })
                    .ProcessAsynchronously();

                // Load thumbnail to UI
                if (File.Exists(thumbnailPath))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(thumbnailPath);
                        bitmap.EndInit();
                        bitmap.Freeze();

                        fileItem.Thumbnail = bitmap;
                    });

                    // Clean up temp file
                    try { File.Delete(thumbnailPath); } catch { }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error generating video thumbnail: {ex.Message}");
            }
        }

        // ============ FILE PREVIEW ============

        private async void FilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesList.SelectedItem is not FileItem fileItem)
            {
                ClearPreview();
                return;
            }

            string filePath = fileItem.Path;

            // Update file info panel
            TxtFilePath.Text = filePath;
            TxtFileSize.Text = $"{fileItem.Size:N0} bytes ({fileItem.HumanSize})";
            TxtFileDate.Text = fileItem.HumanDate.ToString("MM/dd/yyyy HH:mm:ss");
            FileInfoPanel.Visibility = Visibility.Visible;
            
            // Hide empty preview state
            EmptyPreviewState.Visibility = Visibility.Collapsed;
            TxtStatusBar.Text = $"Viewing: {fileItem.FileName}";

            // Show video metadata if available
            if (!string.IsNullOrEmpty(fileItem.VideoInfo))
            {
                TxtVideoInfo.Text = fileItem.VideoInfo;
                TxtVideoInfo.Visibility = Visibility.Visible;
            }
            else
            {
                TxtVideoInfo.Visibility = Visibility.Collapsed;
            }

            if (!File.Exists(filePath))
            {
                ShowNoPreview("File not found");
                return;
            }

            // Detect file type and load preview
            if (fileItem.IsImage)
            {
                await LoadImagePreviewAsync(filePath);
            }
            else if (fileItem.IsVideo)
            {
                LoadVideoPreview(filePath, fileItem);
            }
            else
            {
                ShowNoPreview("Preview not available for this file type");
            }
        }

        private async Task LoadImagePreviewAsync(string filePath)
        {
            HideAllPreviews();

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(filePath);
                bitmap.EndInit();
                bitmap.Freeze();

                ImagePreview.Source = bitmap;
                ImagePreview.Visibility = Visibility.Visible;
                TxtNoPreview.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowNoPreview($"❌ Error loading image: {ex.Message}");
            }
        }

        private void LoadVideoPreview(string filePath, FileItem fileItem)
        {
            HideAllPreviews();

            try
            {
                VideoPreview.Source = new Uri(filePath);
                VideoPreview.Visibility = Visibility.Visible;
                VideoControls.Visibility = Visibility.Visible;
                TxtNoPreview.Visibility = Visibility.Collapsed;

                // Don't auto-play - user controls playback
                VideoPreview.Stop();

                // Load video metadata if not already loaded
                if (string.IsNullOrEmpty(fileItem.VideoInfo))
                {
                    _ = LoadVideoMetadataAsync(fileItem);
                }
            }
            catch (Exception ex)
            {
                ShowNoPreview($"Error loading video: {ex.Message}");
            }
        }

        private async Task LoadVideoMetadataAsync(FileItem fileItem)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(fileItem.Path);

                if (mediaInfo?.PrimaryVideoStream != null)
                {
                    var video = mediaInfo.PrimaryVideoStream;
                    var duration = mediaInfo.Duration;

                    fileItem.VideoInfo = $"📹 {video.Width}x{video.Height} | {video.CodecName?.ToUpper()} | {video.FrameRate:F1} fps | {FormatTime(duration)}";

                    await Dispatcher.InvokeAsync(() =>
                    {
                        TxtVideoInfo.Text = fileItem.VideoInfo;
                        TxtVideoInfo.Visibility = Visibility.Visible;
                    });
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    TxtVideoInfo.Text = $"Could not load metadata: {ex.Message}";
                    TxtVideoInfo.Visibility = Visibility.Visible;
                });
            }
        }

        // ============ OPEN FILE LOCATION ============

        private void FilesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FilesList.SelectedItem is FileItem fileItem)
            {
                OpenFolderAndSelectFile(fileItem.Path);
            }
        }

        private static void OpenFolderAndSelectFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Open Explorer and select the file
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                else
                {
                    // If file doesn't exist, try to open parent folder
                    var folder = Path.GetDirectoryName(filePath);
                    if (Directory.Exists(folder))
                    {
                        Process.Start("explorer.exe", folder);
                    }
                    else
                    {
                        MessageBox.Show("Folder not found.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ============ PREVIEW HELPERS ============

        private void ShowNoPreview(string message)
        {
            HideAllPreviews();
            EmptyPreviewState.Visibility = Visibility.Collapsed;
            TxtNoPreview.Text = message;
            TxtNoPreview.Visibility = Visibility.Visible;
        }

        private void HideAllPreviews()
        {
            ImagePreview.Source = null;
            ImagePreview.Visibility = Visibility.Collapsed;

            _videoTimer?.Stop();
            VideoPreview.Stop();
            VideoPreview.Source = null;
            VideoPreview.Visibility = Visibility.Collapsed;
            VideoControls.Visibility = Visibility.Collapsed;
        }

        private void ClearPreview()
        {
            // Stop and clear video
            _videoTimer?.Stop();
            VideoPreview.Stop();
            VideoPreview.Source = null;
            VideoPreview.Visibility = Visibility.Collapsed;
            VideoControls.Visibility = Visibility.Collapsed;
            VideoSeekBar.Value = 0;
            TxtVideoTime.Text = "00:00 / 00:00";
            TxtVideoInfo.Visibility = Visibility.Collapsed;
            _videoDuration = TimeSpan.Zero;

            // Clear image
            ImagePreview.Source = null;
            ImagePreview.Visibility = Visibility.Collapsed;

            // Hide file info panel and show empty state
            FileInfoPanel.Visibility = Visibility.Collapsed;
            TxtNoPreview.Visibility = Visibility.Collapsed;
            EmptyPreviewState.Visibility = Visibility.Visible;
        }

        // ============ VIDEO CONTROLS ============

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            VideoPreview.Play();
            _videoTimer?.Start();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            VideoPreview.Pause();
            _videoTimer?.Stop();
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            VideoPreview.Stop();
            _videoTimer?.Stop();
            VideoSeekBar.Value = 0;
            TxtVideoTime.Text = $"00:00 / {FormatTime(_videoDuration)}";
        }

        private void VideoPreview_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (VideoPreview.NaturalDuration.HasTimeSpan)
            {
                _videoDuration = VideoPreview.NaturalDuration.TimeSpan;
                VideoSeekBar.Maximum = _videoDuration.TotalSeconds;
                TxtVideoTime.Text = $"00:00 / {FormatTime(_videoDuration)}";
            }
        }

        private void VideoPreview_MediaEnded(object sender, RoutedEventArgs e)
        {
            VideoPreview.Stop();
            _videoTimer?.Stop();
            VideoSeekBar.Value = 0;
            TxtVideoTime.Text = $"00:00 / {FormatTime(_videoDuration)}";
        }

        private void VideoTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isDraggingSeekBar && VideoPreview.NaturalDuration.HasTimeSpan)
            {
                VideoSeekBar.Value = VideoPreview.Position.TotalSeconds;
                TxtVideoTime.Text = $"{FormatTime(VideoPreview.Position)} / {FormatTime(_videoDuration)}";
            }
        }

        private void VideoSeekBar_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = true;
        }

        private void VideoSeekBar_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeekBar = false;
            VideoPreview.Position = TimeSpan.FromSeconds(VideoSeekBar.Value);
        }

        // ============ UTILITY METHODS ============
        
        private void ExecuteStartScan()
        {
            // Switch to Scanner tab and trigger scan
            MainTabControl.SelectedIndex = 0;
            // Note: The actual scan start is handled by the ScannerTab
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return time.ToString(@"hh\:mm\:ss");
            return time.ToString(@"mm\:ss");
        }
    }
    
    /// <summary>
    /// Simple ICommand implementation for keyboard shortcuts
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}