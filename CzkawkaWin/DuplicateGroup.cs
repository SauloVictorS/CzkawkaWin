using System.Collections.Generic;
using System.Linq;

namespace CzkawkaWin
{
    public class DuplicateGroup
    {
        public long SizeBytes { get; set; }
        public int Count { get; set; }
        public List<FileItem> Items { get; set; } = [];

        // Formatted size (KB, MB, GB)
        public string HumanSize => FormatBytes(SizeBytes);

        // List of file names for display
        public string FileNames => Items != null 
            ? string.Join("\n", Items.Select(f => $"  📄 {f.FileName}"))
            : string.Empty;

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
    }
}
