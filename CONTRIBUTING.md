# Contributing to CzkawkaWin

First off, thank you for considering contributing to CzkawkaWin! It's people like you that make this tool better for everyone.

## Code of Conduct

This project and everyone participating in it is governed by respect and professionalism. Please be kind and courteous to others.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues to avoid duplicates. When you create a bug report, include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples** (sample JSON files if possible)
- **Describe the behavior you observed** and what you expected
- **Include screenshots** if applicable
- **Mention your environment**: Windows version, .NET version

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- **Use a clear and descriptive title**
- **Provide a detailed description** of the suggested enhancement
- **Explain why this enhancement would be useful**
- **Provide examples** of how it would work

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the coding style** used throughout the project:
   - Use meaningful variable and method names
   - Add comments for complex logic
   - Follow C# naming conventions
   - Keep methods focused and concise

3. **Test your changes**:
   - Ensure the application builds without errors
   - Test with various JSON files
   - Verify video/image preview works
   - Check for memory leaks with large datasets

4. **Update documentation** if needed

5. **Write a clear commit message**:
   ```
   Add feature: Video playback speed control
   
   - Added speed control slider to video player
   - Supported speeds: 0.5x, 1x, 1.5x, 2x
   - Updated UI to accommodate new control
   ```

6. **Submit the pull request**

## Development Setup

### Prerequisites
- Visual Studio 2022 or later
- .NET 10 SDK
- FFmpeg binaries (place in `ffmpeg/` folder)

### Building
```bash
git clone https://github.com/yourusername/CzkawkaWin.git
cd CzkawkaWin
dotnet restore
dotnet build
```

### Running
```bash
dotnet run --project CzkawkaWin
```

## Project Structure

```
CzkawkaWin/
??? MainWindow.xaml          # UI layout
??? MainWindow.xaml.cs       # Business logic
??? FileItem.cs              # Data model for files
??? DuplicateGroup.cs        # Data model for groups
??? App.xaml                 # Application resources
```

## Coding Guidelines

### C# Style
- Use `var` for local variables when type is obvious
- Prefer `async/await` over `Task.ContinueWith`
- Use `null-coalescing` operators (`??`, `?.`)
- Follow Microsoft's C# coding conventions

### XAML Style
- Use data binding over code-behind when possible
- Keep XAML clean and organized
- Use resources for reusable styles
- Comment complex layouts

### Performance
- Use virtualization for large lists (`VirtualizingPanel.IsVirtualizing="True"`)
- Dispose of large resources (images, video streams)
- Use `async` for file I/O operations
- Cache thumbnails when possible

## Testing Checklist

Before submitting a PR, ensure:
- [ ] Application builds without warnings
- [ ] UI is responsive (no freezing)
- [ ] Video playback works smoothly
- [ ] Image preview loads correctly
- [ ] File double-click opens Explorer
- [ ] Large JSON files (1000+ groups) load efficiently
- [ ] No memory leaks during extended use

## Questions?

Feel free to open an issue with your question. We're here to help!

---

Thank you for contributing! ??
