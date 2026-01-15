# 🔍 CzkawkaWin - Duplicate File Finder

A high-performance Windows desktop application for visualizing and managing duplicate files found by [Czkawka](https://github.com/qarmin/czkawka). Built with WPF and .NET 10, featuring video preview with FFmpeg integration.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?logo=windows)
![Version](https://img.shields.io/badge/version-0.0.1--alpha-orange)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

- **📂 JSON Report Viewer**: Load and parse Czkawka duplicate file scan results
- **🖼️ Image Preview**: View image files directly in the application
- **🎬 Video Preview**: Play video files with built-in media player controls
- **🎥 FFmpeg Integration**: Generate thumbnails and extract metadata for videos
- **⚡ High Performance**: Virtualized lists for handling large datasets
- **📊 Smart Grouping**: Organize duplicates by file size with file counts
- **🔎 Detailed Information**: View file size, date, video resolution, codec, and frame rate
- **📁 Quick Access**: Double-click any file to open its location in Windows Explorer
- **🎨 Modern Dark UI**: Clean, professional interface with empty states and status feedback

## 📸 Screenshots

### Empty State
Clean interface when no data is loaded, with helpful instructions for getting started.
<img width="1086" height="643" alt="image" src="https://github.com/user-attachments/assets/6b3ae135-213d-42b9-9315-eedbd7aba3df" />

### Groups and Files View
Browse duplicate groups sorted by file size, with thumbnails for images and videos.
<img width="1086" height="643" alt="image" src="https://github.com/user-attachments/assets/0e0894b4-d5e5-485f-a803-d32ee62a970b" />

### Video Preview
Watch videos directly in the app with playback controls and metadata display.
<img width="1293" height="776" alt="image" src="https://github.com/user-attachments/assets/56e5e681-1695-415b-88c7-d53e50bd949e" />

## 🚀 Getting Started

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 10 Runtime** or SDK
- **FFmpeg** binaries (included in releases)

### Installation

#### Option 1: Download Release
1. Download the latest release from the [Releases](https://github.com/yourusername/CzkawkaWin/releases) page
2. Extract the ZIP file
3. Run `CzkawkaWin.exe`

#### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/CzkawkaWin.git
cd CzkawkaWin

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project CzkawkaWin
```

### FFmpeg Setup

The application requires FFmpeg for video thumbnail generation and metadata extraction:

1. Download FFmpeg binaries from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract and place `ffmpeg.exe` and `ffprobe.exe` in the `ffmpeg/` folder next to the executable:
   ```
   CzkawkaWin/
   ├── CzkawkaWin.exe
   └── ffmpeg/
       ├── ffmpeg.exe
       └── ffprobe.exe
   ```

**Note**: Release builds include FFmpeg binaries automatically.

## 📖 Usage

1. **Generate Duplicate Report**
   - Use [Czkawka](https://github.com/qarmin/czkawka) to scan for duplicates
   - Export results as JSON

2. **Load in CzkawkaWin**
   - Click **"📂 Load JSON Report"**
   - Select your Czkawka JSON file
   - Browse duplicate groups in the left panel

3. **Review Files**
   - Click a group to see all duplicate files
   - Select a file to preview (images/videos)
   - Double-click to open the file location

4. **Video Controls**
   - Play/Pause/Stop controls for video files
   - Seek bar for navigation
   - Displays video metadata (resolution, codec, fps)

## 🛠️ Built With

- **[.NET 10](https://dotnet.microsoft.com/)** - Modern cross-platform framework
- **[WPF](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)** - Windows Presentation Foundation
- **[FFMpegCore](https://github.com/rosenbjerg/FFMpegCore)** - FFmpeg wrapper for .NET
- **[System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)** - High-performance JSON parsing

## 📁 Project Structure

```
CzkawkaWin/
├── CzkawkaWin/
│   ├── MainWindow.xaml          # Main UI layout
│   ├── MainWindow.xaml.cs       # UI logic and event handlers
│   ├── FileItem.cs              # File model with metadata
│   ├── DuplicateGroup.cs        # Group model for duplicates
│   ├── App.xaml                 # Application resources
│   └── CzkawkaWin.csproj        # Project configuration
├── ffmpeg/                      # FFmpeg binaries (runtime)
└── README.md                    # This file
```

## 🎯 Roadmap

- [ ] Full integration with Czkawka scanning
- [ ] File deletion with safety confirmations
- [ ] Compare files side-by-side

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Czkawka](https://github.com/qarmin/czkawka) - The powerful duplicate file finder this app complements
- [FFmpeg](https://ffmpeg.org/) - Multimedia framework for video processing
- [FFMpegCore](https://github.com/rosenbjerg/FFMpegCore) - Excellent .NET wrapper for FFmpeg


---

**Note**: CzkawkaWin is a viewer/manager for Czkawka results. You still need [Czkawka](https://github.com/qarmin/czkawka) to scan for duplicates.
