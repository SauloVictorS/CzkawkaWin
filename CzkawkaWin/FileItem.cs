using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media.Imaging;

namespace CzkawkaWin
{
    public class FileItem : INotifyPropertyChanged
    {
        private BitmapImage? _thumbnail;

        [JsonPropertyName("path")]
        public string Path { get; set; } = string.Empty;

        [JsonPropertyName("modified_date")]
        public long ModifiedDate { get; set; } // Unix Timestamp from JSON (seconds)

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        // Thumbnail for video/image files (loaded dynamically)
        [JsonIgnore]
        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        // Video metadata (loaded dynamically)
        [JsonIgnore]
        public string? VideoInfo { get; set; }

        // Helper property to convert date for display (Unix timestamp in seconds)
        public DateTime HumanDate => DateTimeOffset.FromUnixTimeSeconds(ModifiedDate).LocalDateTime;

        // File name without full path
        public string FileName => System.IO.Path.GetFileName(Path);

        // File extension
        public string Extension => System.IO.Path.GetExtension(Path).ToLowerInvariant();

        // Formatted size (KB, MB, GB)
        public string HumanSize => FormatBytes(Size);

        // Check if file is an image
        public bool IsImage
        {
            get
            {
                string[] imageExtensions = [".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".tif", ".webp", ".ico"];
                return Array.Exists(imageExtensions, ext => ext == Extension);
            }
        }

        // Check if file is a video
        public bool IsVideo
        {
            get
            {
                string[] videoExtensions = [".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"];
                return Array.Exists(videoExtensions, ext => ext == Extension);
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = ["B", "KB", "MB", "GB", "TB"];
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
