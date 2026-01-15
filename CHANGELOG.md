# Changelog

All notable changes to CzkawkaWin will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## [Unreleased]

### Planned Features
- File deletion capabilities
- Side-by-side file comparison
- Audio file preview
- Export filtered results
- Drag-and-drop JSON loading
- Multiple file selection
- Keyboard shortcuts
- Settings panel
- Custom thumbnail cache management
- Batch operations
- Search and filter functionality

---

