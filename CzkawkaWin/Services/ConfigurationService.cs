using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CzkawkaWin.Models;

namespace CzkawkaWin.Services
{
    /// <summary>
    /// Service for persisting and loading scan configurations.
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configDirectory;
        private const string DefaultConfigFileName = "default_config.json";
        private const string LastUsedConfigFileName = "last_used_config.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Gets the configuration directory path.
        /// </summary>
        public string ConfigDirectory => _configDirectory;

        /// <summary>
        /// Creates a new instance of ConfigurationService.
        /// </summary>
        public ConfigurationService()
        {
            _configDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CzkawkaWin",
                "Configs");

            // Ensure directory exists
            Directory.CreateDirectory(_configDirectory);
        }

        /// <summary>
        /// Saves a configuration to a file.
        /// </summary>
        /// <param name="config">Configuration to save</param>
        /// <param name="profileName">Profile name (without .json extension)</param>
        public async Task SaveConfigurationAsync(ScanConfiguration config, string? profileName = null)
        {
            var fileName = string.IsNullOrWhiteSpace(profileName) 
                ? DefaultConfigFileName 
                : SanitizeFileName(profileName) + ".json";
            
            var filePath = Path.Combine(_configDirectory, fileName);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Saves the current configuration as the last used configuration.
        /// </summary>
        public async Task SaveLastUsedConfigurationAsync(ScanConfiguration config)
        {
            var filePath = Path.Combine(_configDirectory, LastUsedConfigFileName);
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }

        /// <summary>
        /// Loads a configuration from a file.
        /// </summary>
        /// <param name="profileName">Profile name (without .json extension)</param>
        /// <returns>The loaded configuration or null if not found</returns>
        public async Task<ScanConfiguration?> LoadConfigurationAsync(string? profileName = null)
        {
            var fileName = string.IsNullOrWhiteSpace(profileName) 
                ? DefaultConfigFileName 
                : SanitizeFileName(profileName) + ".json";
            
            var filePath = Path.Combine(_configDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<ScanConfiguration>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Loads the last used configuration.
        /// </summary>
        public async Task<ScanConfiguration?> LoadLastUsedConfigurationAsync()
        {
            var filePath = Path.Combine(_configDirectory, LastUsedConfigFileName);

            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<ScanConfiguration>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a default configuration with sensible defaults.
        /// </summary>
        public ScanConfiguration GetDefaultConfiguration()
        {
            return new ScanConfiguration
            {
                Method = SearchMethod.Hash,
                HashAlgorithm = HashType.BLAKE3,
                MinimalFileSize = 8192,  // 8 KB
                MaximalFileSize = long.MaxValue,
                ThreadNumber = 0,  // Use all available
                UseCache = true,
                UsePrehashCache = false,
                Recursive = true,
                CaseSensitive = false,
                AllowHardLinks = false
            };
        }

        /// <summary>
        /// Gets a list of saved profile names.
        /// </summary>
        public List<string> GetSavedProfiles()
        {
            if (!Directory.Exists(_configDirectory))
            {
                return new List<string>();
            }

            return Directory.GetFiles(_configDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != null 
                    && name != Path.GetFileNameWithoutExtension(LastUsedConfigFileName)
                    && name != Path.GetFileNameWithoutExtension(DefaultConfigFileName))
                .Cast<string>()
                .OrderBy(name => name)
                .ToList();
        }

        /// <summary>
        /// Deletes a saved profile.
        /// </summary>
        /// <param name="profileName">Profile name to delete</param>
        public async Task DeleteProfileAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return;

            var fileName = SanitizeFileName(profileName) + ".json";
            var filePath = Path.Combine(_configDirectory, fileName);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }

        /// <summary>
        /// Checks if a profile exists.
        /// </summary>
        public bool ProfileExists(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
                return false;

            var fileName = SanitizeFileName(profileName) + ".json";
            var filePath = Path.Combine(_configDirectory, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Renames a profile.
        /// </summary>
        public async Task RenameProfileAsync(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return;

            var oldFileName = SanitizeFileName(oldName) + ".json";
            var newFileName = SanitizeFileName(newName) + ".json";
            
            var oldPath = Path.Combine(_configDirectory, oldFileName);
            var newPath = Path.Combine(_configDirectory, newFileName);

            if (File.Exists(oldPath) && !File.Exists(newPath))
            {
                await Task.Run(() => File.Move(oldPath, newPath));
            }
        }

        /// <summary>
        /// Sanitizes a file name by removing invalid characters.
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", name.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Trim();
        }
    }
}
