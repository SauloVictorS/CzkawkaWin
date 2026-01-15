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

namespace CzkawkaWin
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _videoTimer;
        private bool _isDraggingSeekBar = false;
        private TimeSpan _videoDuration = TimeSpan.Zero;

        // Application version from assembly
        public string AppVersion => GetType().Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        public MainWindow()
        {
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
        }

        // ============ JSON LOADING ============

        private async void BtnLoadJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select Czkawka Report"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TxtStatus.Text = "Loading and processing... (Please wait)";

                // Disable button to prevent double clicks
                ((Button)sender).IsEnabled = false;

                try
                {
                    string path = openFileDialog.FileName;

                    // Run heavy processing on background thread to keep UI responsive
                    List<DuplicateGroup> groups = await Task.Run(() => LoadAndProcessData(path));

                    // Bind result to left panel list
                    GroupsList.ItemsSource = groups;

                    TxtStatus.Text = $"Done! {groups.Count} duplicate groups found.";
                    TxtStatusBar.Text = $"Loaded {groups.Count} groups. Select a group to view duplicate files.";
                    TxtGroupCount.Text = $"({groups.Count})";
                    
                    // Hide empty state when data is loaded
                    EmptyGroupsState.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtStatus.Text = "Error loading file.";
                }
                finally
                {
                    ((Button)sender).IsEnabled = true;
                }
            }
        }

        // Optimized method to read and transform data
        private static List<DuplicateGroup> LoadAndProcessData(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var rawData = JsonSerializer.Deserialize<Dictionary<string, List<FileItem>>>(json);

            var resultList = new List<DuplicateGroup>(rawData?.Count ?? 0);

            if (rawData != null)
            {
                foreach (var kvp in rawData)
                {
                    long size = kvp.Value.Count > 0 ? kvp.Value[0].Size : 0;

                    resultList.Add(new DuplicateGroup
                    {
                        SizeBytes = size,
                        Count = kvp.Value.Count,
                        Items = kvp.Value
                    });
                }
            }

            // Sort by size descending (largest files first)
            resultList.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));

            return resultList;
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

        private static string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return time.ToString(@"hh\:mm\:ss");
            return time.ToString(@"mm\:ss");
        }
    }
}