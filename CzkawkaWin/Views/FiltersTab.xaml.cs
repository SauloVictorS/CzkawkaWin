using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CzkawkaWin.Models;

namespace CzkawkaWin.Views
{
    /// <summary>
    /// Filters tab for configuring file extension, size, and exclusion filters.
    /// </summary>
    public partial class FiltersTab : UserControl
    {
        // Extension macros (from Czkawka CLI)
        private static readonly string[] ImageExtensions = { "jpg", "jpeg", "kra", "gif", "png", "bmp", "tiff", "hdr", "svg", "webp" };
        private static readonly string[] VideoExtensions = { "mp4", "flv", "mkv", "webm", "vob", "ogv", "gifv", "avi", "mov", "wmv", "mpg", "m4v", "m4p", "mpeg", "3gp" };
        private static readonly string[] MusicExtensions = { "mp3", "flac", "ogg", "tta", "wma", "wav", "aac", "m4a" };
        private static readonly string[] TextExtensions = { "txt", "doc", "docx", "odt", "rtf", "pdf", "xls", "xlsx" };
        
        // Default excluded items pattern
        private const string DefaultExcludedItems = @"*\.git\*,*\node_modules\*,*\lost+found\*,*:\windows\*,*:\$RECYCLE.BIN\*,*:\$SysReset\*,*:\System Volume Information\*,*:\OneDriveTemp\*,*:\hiberfil.sys,*:\pagefile.sys,*:\swapfile.sys";

        public FiltersTab()
        {
            InitializeComponent();
            
            // Set default excluded items
            TxtExcludedItems.Text = DefaultExcludedItems;
        }

        #region Public Properties for Filter Values

        /// <summary>
        /// Gets the list of allowed extensions.
        /// </summary>
        public List<string> AllowedExtensions
        {
            get
            {
                return TxtCustomExtensions.Text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.TrimStart('.').ToLowerInvariant())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();
            }
            set
            {
                TxtCustomExtensions.Text = string.Join(" ", value);
                UpdateMacroCheckboxes(value);
            }
        }

        /// <summary>
        /// Gets the list of excluded items (patterns).
        /// </summary>
        public List<string> ExcludedItems
        {
            get
            {
                return TxtExcludedItems.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }
            set => TxtExcludedItems.Text = string.Join(",", value);
        }

        /// <summary>
        /// Gets the list of excluded extensions.
        /// </summary>
        public List<string> ExcludedExtensions
        {
            get
            {
                return TxtExcludedExtensions.Text
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.TrimStart('.').ToLowerInvariant())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();
            }
            set => TxtExcludedExtensions.Text = string.Join(" ", value);
        }

        /// <summary>
        /// Gets the minimal file size in bytes.
        /// </summary>
        public long MinimalFileSize => ParseFileSize(TxtMinSize.Text, CmbMinSizeUnit.SelectedIndex);

        /// <summary>
        /// Gets the maximal file size in bytes.
        /// </summary>
        public long MaximalFileSize
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TxtMaxSize.Text))
                    return long.MaxValue;
                return ParseFileSize(TxtMaxSize.Text, CmbMaxSizeUnit.SelectedIndex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies filter configuration from a ScanConfiguration object.
        /// </summary>
        public void ApplyConfiguration(ScanConfiguration config)
        {
            // Allowed extensions
            TxtCustomExtensions.Text = string.Join(" ", config.AllowedExtensions);
            UpdateMacroCheckboxes(config.AllowedExtensions);
            
            // Excluded items
            TxtExcludedItems.Text = string.Join(",", config.ExcludedItems);
            
            // Excluded extensions
            TxtExcludedExtensions.Text = string.Join(" ", config.ExcludedExtensions);
            
            // File sizes
            var (minValue, minUnit) = ConvertToDisplaySize(config.MinimalFileSize);
            TxtMinSize.Text = minValue.ToString();
            CmbMinSizeUnit.SelectedIndex = minUnit;
            
            if (config.MaximalFileSize < long.MaxValue)
            {
                var (maxValue, maxUnit) = ConvertToDisplaySize(config.MaximalFileSize);
                TxtMaxSize.Text = maxValue.ToString();
                CmbMaxSizeUnit.SelectedIndex = maxUnit;
            }
            else
            {
                TxtMaxSize.Text = "";
            }
        }

        /// <summary>
        /// Applies filter values to a ScanConfiguration object.
        /// </summary>
        public void ApplyToConfiguration(ScanConfiguration config)
        {
            config.AllowedExtensions = AllowedExtensions;
            config.ExcludedItems = ExcludedItems;
            config.ExcludedExtensions = ExcludedExtensions;
            config.MinimalFileSize = MinimalFileSize;
            config.MaximalFileSize = MaximalFileSize;
        }

        #endregion

        #region Event Handlers

        private void MacroButton_Changed(object sender, RoutedEventArgs e)
        {
            UpdateExtensionsFromMacros();
        }

        private void BtnClearExtensions_Click(object sender, RoutedEventArgs e)
        {
            TxtCustomExtensions.Text = "";
            ChkMacroImage.IsChecked = false;
            ChkMacroVideo.IsChecked = false;
            ChkMacroMusic.IsChecked = false;
            ChkMacroText.IsChecked = false;
        }

        private void BtnRestoreExcludedDefaults_Click(object sender, RoutedEventArgs e)
        {
            TxtExcludedItems.Text = DefaultExcludedItems;
        }

        #endregion

        #region Private Methods

        private void UpdateExtensionsFromMacros()
        {
            var extensions = new List<string>();
            
            if (ChkMacroImage.IsChecked == true)
                extensions.AddRange(ImageExtensions);
            if (ChkMacroVideo.IsChecked == true)
                extensions.AddRange(VideoExtensions);
            if (ChkMacroMusic.IsChecked == true)
                extensions.AddRange(MusicExtensions);
            if (ChkMacroText.IsChecked == true)
                extensions.AddRange(TextExtensions);
            
            // Add any custom extensions already in the textbox that aren't from macros
            var existingCustom = TxtCustomExtensions.Text
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(ext => !ImageExtensions.Contains(ext) && 
                              !VideoExtensions.Contains(ext) && 
                              !MusicExtensions.Contains(ext) && 
                              !TextExtensions.Contains(ext));
            
            extensions.AddRange(existingCustom);
            
            TxtCustomExtensions.Text = string.Join(" ", extensions.Distinct());
        }

        private void UpdateMacroCheckboxes(List<string> extensions)
        {
            ChkMacroImage.IsChecked = ImageExtensions.All(e => extensions.Contains(e));
            ChkMacroVideo.IsChecked = VideoExtensions.All(e => extensions.Contains(e));
            ChkMacroMusic.IsChecked = MusicExtensions.All(e => extensions.Contains(e));
            ChkMacroText.IsChecked = TextExtensions.All(e => extensions.Contains(e));
        }

        private static long ParseFileSize(string value, int unitIndex)
        {
            if (!long.TryParse(value, out long size))
                return 8192; // Default 8 KB
            
            return unitIndex switch
            {
                0 => size * 1024,           // KB
                1 => size * 1024 * 1024,    // MB
                2 => size * 1024 * 1024 * 1024, // GB
                _ => size * 1024
            };
        }

        private static (long value, int unit) ConvertToDisplaySize(long bytes)
        {
            if (bytes >= 1024 * 1024 * 1024)
                return (bytes / (1024 * 1024 * 1024), 2); // GB
            if (bytes >= 1024 * 1024)
                return (bytes / (1024 * 1024), 1); // MB
            return (bytes / 1024, 0); // KB
        }

        #endregion
    }
}
