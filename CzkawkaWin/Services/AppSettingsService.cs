using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CzkawkaWin.Models;

namespace CzkawkaWin.Services
{
    /// <summary>
    /// Service for persisting and loading application settings.
    /// </summary>
    public class AppSettingsService
    {
        private readonly string _settingsPath;
        private AppSettings? _cachedSettings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Creates a new instance of AppSettingsService.
        /// </summary>
        public AppSettingsService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CzkawkaWin");
            
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");
        }

        /// <summary>
        /// Loads the application settings.
        /// </summary>
        /// <returns>The loaded settings, or default settings if none exist.</returns>
        public async Task<AppSettings> LoadAsync()
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            if (!File.Exists(_settingsPath))
            {
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }

            try
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                return _cachedSettings;
            }
            catch (Exception)
            {
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Loads the application settings synchronously.
        /// </summary>
        public AppSettings Load()
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            if (!File.Exists(_settingsPath))
            {
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                return _cachedSettings;
            }
            catch (Exception)
            {
                _cachedSettings = new AppSettings();
                return _cachedSettings;
            }
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="settings">The settings to save.</param>
        public async Task SaveAsync(AppSettings settings)
        {
            _cachedSettings = settings;
            
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
        }

        /// <summary>
        /// Saves the application settings synchronously.
        /// </summary>
        public void Save(AppSettings settings)
        {
            _cachedSettings = settings;
            
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }

        /// <summary>
        /// Gets the current cached settings or loads them if not cached.
        /// </summary>
        public AppSettings GetCurrent()
        {
            return _cachedSettings ?? Load();
        }

        /// <summary>
        /// Updates a single setting value and saves.
        /// </summary>
        public async Task UpdateSettingAsync(Action<AppSettings> updateAction)
        {
            var settings = await LoadAsync();
            updateAction(settings);
            await SaveAsync(settings);
        }
    }
}
