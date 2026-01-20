using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CzkawkaWin.Models
{
    /// <summary>
    /// Configuration for a duplicate file scan operation.
    /// Maps to Czkawka CLI arguments.
    /// </summary>
    public class ScanConfiguration
    {
        // ============ DIRECTORIES ============
        
        /// <summary>
        /// List of directories to search for duplicates (absolute paths).
        /// These directories are NOT set as reference folders.
        /// CLI: -d, --directories
        /// </summary>
        public List<string> SearchDirectories { get; set; } = new();
        
        /// <summary>
        /// List of directories to exclude from search (absolute paths).
        /// CLI: -e, --excluded-directories
        /// </summary>
        public List<string> ExcludedDirectories { get; set; } = new();
        
        /// <summary>
        /// List of reference directories (absolute paths).
        /// Files in these directories will not appear in results but will be used for comparison.
        /// CLI: -r, --reference-directories
        /// </summary>
        public List<string> ReferenceDirectories { get; set; } = new();
        
        /// <summary>
        /// List of excluded items with wildcard support.
        /// CLI: -E, --excluded-items
        /// </summary>
        public List<string> ExcludedItems { get; set; } = new();

        // ============ EXTENSIONS ============
        
        /// <summary>
        /// List of allowed file extensions (without dot).
        /// If empty, all extensions are allowed.
        /// CLI: -x, --allowed-extensions
        /// </summary>
        public List<string> AllowedExtensions { get; set; } = new();
        
        /// <summary>
        /// List of excluded file extensions (without dot).
        /// CLI: -P, --excluded-extensions
        /// </summary>
        public List<string> ExcludedExtensions { get; set; } = new();

        // ============ FILE SIZE FILTERS ============
        
        /// <summary>
        /// Minimum file size in bytes to include in scan.
        /// CLI: -m, --minimal-file-size (default: 8192)
        /// </summary>
        public long MinimalFileSize { get; set; } = 8192;
        
        /// <summary>
        /// Maximum file size in bytes to include in scan.
        /// CLI: -i, --maximal-file-size (default: max long)
        /// </summary>
        public long MaximalFileSize { get; set; } = long.MaxValue;

        // ============ SEARCH METHOD ============
        
        /// <summary>
        /// Method used to find duplicates.
        /// CLI: -s, --search-method
        /// </summary>
        public SearchMethod Method { get; set; } = SearchMethod.Hash;
        
        /// <summary>
        /// Hash algorithm to use when Method is Hash.
        /// CLI: -t, --hash-type
        /// </summary>
        public HashType HashAlgorithm { get; set; } = HashType.BLAKE3;

        // ============ PERFORMANCE ============
        
        /// <summary>
        /// Number of threads to use. 0 = use all available.
        /// CLI: -T, --thread-number (default: 0)
        /// </summary>
        public int ThreadNumber { get; set; } = 0;
        
        /// <summary>
        /// Whether to use the file hash cache.
        /// CLI: -H, --disable-cache (inverted)
        /// </summary>
        public bool UseCache { get; set; } = true;
        
        /// <summary>
        /// Whether to use prehash cache for faster scanning.
        /// CLI: -u, --use-prehash-cache
        /// </summary>
        public bool UsePrehashCache { get; set; } = false;
        
        /// <summary>
        /// Minimum size of cached files in bytes.
        /// CLI: -c, --minimal-cached-file-size (default: 257144)
        /// </summary>
        public long MinimalCachedFileSize { get; set; } = 257144;
        
        /// <summary>
        /// Minimum size of prehash cached files in bytes.
        /// CLI: -Z, --minimal-prehash-cache-file-size (default: 257144)
        /// </summary>
        public long MinimalPrehashCacheFileSize { get; set; } = 257144;

        // ============ OPTIONS ============
        
        /// <summary>
        /// Whether to search subdirectories recursively.
        /// CLI: -R, --not-recursive (inverted)
        /// </summary>
        public bool Recursive { get; set; } = true;
        
        /// <summary>
        /// Whether to use case-sensitive name comparison.
        /// CLI: -l, --case-sensitive-name-comparison
        /// </summary>
        public bool CaseSensitive { get; set; } = false;
        
        /// <summary>
        /// Whether to include hard links in the scan.
        /// CLI: -L, --allow-hard-links
        /// </summary>
        public bool AllowHardLinks { get; set; } = false;

        // ============ OUTPUT ============
        
        /// <summary>
        /// Custom output JSON file path. If null, a temp file will be used.
        /// </summary>
        public string? OutputJsonPath { get; set; }

        // ============ VALIDATION ============
        
        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns>True if configuration is valid.</returns>
        public bool IsValid()
        {
            if (SearchDirectories.Count == 0)
                return false;
            
            // Check if at least one search directory exists
            return SearchDirectories.Any(Directory.Exists);
        }
        
        /// <summary>
        /// Gets a human-readable validation error message.
        /// </summary>
        /// <returns>Error message or empty string if valid.</returns>
        public string GetValidationError()
        {
            if (SearchDirectories.Count == 0)
                return "At least one search directory is required.";
            
            var missingDirs = SearchDirectories.Where(d => !Directory.Exists(d)).ToList();
            if (missingDirs.Count == SearchDirectories.Count)
                return "None of the specified search directories exist.";
            
            if (missingDirs.Count > 0)
                return $"Some directories do not exist: {string.Join(", ", missingDirs)}";
            
            if (MinimalFileSize < 0)
                return "Minimal file size cannot be negative.";
            
            if (MaximalFileSize < MinimalFileSize)
                return "Maximal file size must be greater than minimal file size.";
            
            return string.Empty;
        }

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        public ScanConfiguration Clone()
        {
            return new ScanConfiguration
            {
                SearchDirectories = new List<string>(SearchDirectories),
                ExcludedDirectories = new List<string>(ExcludedDirectories),
                ReferenceDirectories = new List<string>(ReferenceDirectories),
                ExcludedItems = new List<string>(ExcludedItems),
                AllowedExtensions = new List<string>(AllowedExtensions),
                ExcludedExtensions = new List<string>(ExcludedExtensions),
                MinimalFileSize = MinimalFileSize,
                MaximalFileSize = MaximalFileSize,
                Method = Method,
                HashAlgorithm = HashAlgorithm,
                ThreadNumber = ThreadNumber,
                UseCache = UseCache,
                UsePrehashCache = UsePrehashCache,
                MinimalCachedFileSize = MinimalCachedFileSize,
                MinimalPrehashCacheFileSize = MinimalPrehashCacheFileSize,
                Recursive = Recursive,
                CaseSensitive = CaseSensitive,
                AllowHardLinks = AllowHardLinks,
                OutputJsonPath = OutputJsonPath
            };
        }
    }

    /// <summary>
    /// Method used to find duplicate files.
    /// </summary>
    public enum SearchMethod
    {
        /// <summary>
        /// Compare by file name only. Fast but rarely useful.
        /// </summary>
        Name,
        
        /// <summary>
        /// Compare by file size. Fast but not accurate.
        /// </summary>
        Size,
        
        /// <summary>
        /// Compare by file hash. Slowest but most accurate.
        /// </summary>
        Hash
    }

    /// <summary>
    /// Hash algorithm for file comparison.
    /// </summary>
    public enum HashType
    {
        /// <summary>
        /// BLAKE3 hash algorithm. Fast and secure.
        /// </summary>
        BLAKE3,
        
        /// <summary>
        /// CRC32 checksum. Very fast but less collision-resistant.
        /// </summary>
        CRC32,
        
        /// <summary>
        /// XXH3 hash algorithm. Extremely fast.
        /// </summary>
        XXH3
    }
}
