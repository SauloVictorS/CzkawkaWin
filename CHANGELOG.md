# Changelog

All notable changes to CzkawkaWin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


### Planned Features
- Compare files side-by-side
- Settings panel for app preferences
- Support for multiple languages

## [1.0.0] - 20-01-2026

### Added
- **Integrated Scanner**: Full Czkawka CLI integration for scanning directly from the UI
- **Scanner Tab**: New dedicated tab for configuring and running scans
- **Search Methods**: Support for Name, Size, and Hash-based duplicate detection
- **Hash Algorithms**: BLAKE3, CRC32, and XXH3 options
- **File Filters**: Extension macros (IMAGE, VIDEO, MUSIC, TEXT) and custom extensions
- **Size Filters**: Configurable minimum and maximum file size
- **Profile System**: Save, load, and delete scan configuration profiles
- **Real-time Logging**: Live output log during scan execution
- **Progress Indicator**: Visual feedback during scan operations
- **Clear Results**: Menu option to clear all results (Ctrl+Shift+C)
- **Export JSON**: Export scan results to JSON file (Ctrl+S)
- **Keyboard Shortcuts**: F5 (Start Scan), Escape (Stop), Ctrl+O (Open), Ctrl+S (Export)
- **Input Validation**: Real-time validation with visual feedback for file size inputs
- **Application Settings**: Model for persisting app preferences
- **File Deletion**: Option to delete selected files from the results.

### Changed
- Reorganized UI into tabbed interface (Scanner, Results)
- Updated main menu structure with Scan menu
- Improved status bar with contextual messages
- Enhanced project structure with Services and Models folders

### Technical Details
- CzkawkaService for process management and CLI integration
- ConfigurationService for profile persistence
- AppSettingsService for application settings
- ScanConfiguration model for scan parameters
- ScanCompletedEventArgs for scan result events
- RelayCommand implementation for keyboard shortcuts

## [0.0.1] - 15-01-2026

### Added
- Initial alpha release of CzkawkaWin
- JSON report loading and parsing from Czkawka
- Duplicate file grouping by size
- Three-panel layout (Groups, Files, Preview)
- Image preview with thumbnail generation
- Video preview with playback controls (Play, Pause, Stop)
- FFmpeg integration for video metadata and thumbnails
- Video metadata display (resolution, codec, frame rate, duration)
- Double-click to open file location in Explorer
- Empty states with helpful instructions
- Status bar with contextual feedback
- Dark theme UI
- Virtualized lists for high performance
- Real-time video seeking with slider
- Thumbnail generation for images and videos in file list
- Dynamic version display from assembly

### Technical Details
- Built with .NET 10 and WPF
- Uses FFMpegCore 5.4.0 for video processing
- System.Text.Json for high-performance JSON parsing
- MVVM-like architecture with data binding
- Asynchronous file operations to keep UI responsive

---

