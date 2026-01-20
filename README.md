# 🔍 CzkawkaWin - Duplicate File Finder

A high-performance Windows desktop application for finding and managing duplicate files. Features an **integrated scanner** powered by [Czkawka CLI](https://github.com/qarmin/czkawka) and a results viewer with video preview support using FFmpeg. Built with WPF and .NET 10.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-Windows-0078D4?logo=windows)
![Version](https://img.shields.io/badge/version-1.0.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

## ✨ Features

### 🔍 Integrated Scanner
- **Direct Czkawka Integration**: Run duplicate scans directly from the UI
- **Multiple Search Methods**: Name, Size, or Hash-based scanning
- **Hash Algorithms**: BLAKE3 (secure), CRC32 (fast), XXH3 (extremely fast)
- **File Filters**: Extension macros (IMAGE, VIDEO, MUSIC, TEXT) and custom extensions
- **Size Filters**: Min/max file size configuration
- **Profile System**: Save and load scan configurations
- **Real-time Output**: Live log of scan progress

### 📊 Results Viewer
- **📂 JSON Report Viewer**: Load and parse Czkawka duplicate file scan results
- **🖼️ Image Preview**: View image files directly in the application
- **🎬 Video Preview**: Play video files with built-in media player controls
- **🎥 FFmpeg Integration**: Generate thumbnails and extract metadata for videos
- **⚡ High Performance**: Virtualized lists for handling large datasets
- **📊 Smart Grouping**: Organize duplicates by file size with file counts
- **🔎 Detailed Information**: View file size, date, video resolution, codec, and frame rate
- **📁 Quick Access**: Double-click any file to open its location in Windows Explorer
- **💾 Export Results**: Export scan results to JSON format

### ⌨️ Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `F5` | Start/Focus Scanner |
| `Escape` | Stop Scan |
| `Ctrl+O` | Open JSON Report |
| `Ctrl+S` | Export Results |
| `Ctrl+Shift+C` | Clear Results |

### 🎨 User Experience
- **Modern Dark UI**: Clean, professional interface
- **Two-Tab Layout**: Separate Scanner and Results views
- **Empty States**: Helpful instructions when no data is loaded
- **Status Feedback**: Real-time status bar updates
- **Input Validation**: Visual feedback for invalid inputs

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
1. Download the latest release from the [Releases](https://github.com/SauloVictorS/CzkawkaWin/releases) page
2. Extract the ZIP file
3. Run `CzkawkaWin.exe`

#### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/SauloVictorS/CzkawkaWin.git
cd CzkawkaWin

# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Run the application
dotnet run --project CzkawkaWin
```

### Czkawka CLI Setup

The application requires Czkawka CLI for scanning functionality:

1. Download `czkawka_cli.exe` from [Czkawka Releases](https://github.com/qarmin/czkawka/releases)
2. Place it in the application directory:
   ```
   CzkawkaWin/
   ├── CzkawkaWin.exe
   ├── czkawka_cli.exe    # Required for scanning
   └── ffmpeg/
       ├── ffmpeg.exe
       └── ffprobe.exe
   ```

### FFmpeg Setup

The application requires FFmpeg for video thumbnail generation and metadata extraction:

1. Download FFmpeg binaries from [ffmpeg.org](https://ffmpeg.org/download.html)
2. Extract and place `ffmpeg.exe` and `ffprobe.exe` in the `ffmpeg/` folder next to the executable

**Note**: Release builds include both Czkawka CLI and FFmpeg binaries automatically.

## 📖 Usage

### Integrated Scanner (Recommended)
1. **Configure Scan**
   - Go to the **Scanner** tab
   - Add directories to scan using **"+ Add Folder"**
   - Select search method (Name/Size/Hash)
   - Configure filters (extensions, file size)

2. **Run Scan**
   - Click **"Start Scan"** or press **F5**
   - Monitor progress in the output log
   - Results automatically load in the Results tab

3. **Save Profiles**
   - Save your scan configuration as a profile for reuse
   - Profiles persist between sessions

### Manual JSON Loading
1. **Generate Duplicate Report**
   - Use [Czkawka](https://github.com/qarmin/czkawka) to scan for duplicates
   - Export results as JSON

2. **Load in CzkawkaWin**
   - Go to **File → Open JSON Report** (Ctrl+O)
   - Select your Czkawka JSON file

### Review Results
- Click a group to see all duplicate files
- Select a file to preview (images/videos)
- Double-click to open the file location
- Export results via **File → Export JSON** (Ctrl+S)

### Video Controls
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
│   ├── Models/
│   │   ├── ScanConfiguration.cs    # Scan settings model
│   │   ├── AppSettings.cs          # Application settings
│   │   ├── FileItem.cs             # File model with metadata
│   │   └── DuplicateGroup.cs       # Group model for duplicates
│   ├── Services/
│   │   ├── CzkawkaService.cs       # Czkawka CLI integration
│   │   ├── ConfigurationService.cs # Profile persistence
│   │   └── AppSettingsService.cs   # App settings persistence
│   ├── Views/
│   │   └── ScannerTab.xaml(.cs)    # Scanner UI
│   ├── Events/
│   │   └── ScanCompletedEventArgs.cs
│   ├── MainWindow.xaml(.cs)        # Main window with tabs
│   ├── App.xaml                    # Application resources
│   └── CzkawkaWin.csproj           # Project configuration
├── ffmpeg/                         # FFmpeg binaries (runtime)
├── czkawka_cli.exe                 # Czkawka CLI (runtime)
└── README.md                       # This file
```

## 🎯 Roadmap

- [x] Full integration with Czkawka scanning
- [x] Profile system for scan configurations
- [x] Export results to JSON
- [ ] File deletion with safety confirmations
- [ ] Compare files side-by-side
- [ ] Settings panel for app preferences

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

**Note**: CzkawkaWin includes an integrated scanner powered by Czkawka CLI. You can also load existing JSON reports from [Czkawka](https://github.com/qarmin/czkawka) scans.
