namespace CzkawkaWin.Models
{
    /// <summary>
    /// Application-wide settings that persist between sessions.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// The last profile used for scanning.
        /// </summary>
        public string LastUsedProfile { get; set; } = "Default";
        
        /// <summary>
        /// Whether to remember the last scan configuration.
        /// </summary>
        public bool RememberLastScan { get; set; } = true;
        
        /// <summary>
        /// Default path for exporting JSON results.
        /// </summary>
        public string DefaultExportPath { get; set; } = "";
        
        /// <summary>
        /// Maximum number of lines to keep in the log output.
        /// </summary>
        public int MaxLogLines { get; set; } = 1000;
        
        /// <summary>
        /// Whether to automatically switch to Results tab after scan.
        /// </summary>
        public bool AutoSwitchToResults { get; set; } = true;
        
        /// <summary>
        /// Last selected tab index (0 = Scanner, 1 = Results).
        /// </summary>
        public int LastSelectedTab { get; set; } = 0;
        
        /// <summary>
        /// Window width.
        /// </summary>
        public double WindowWidth { get; set; } = 1100;
        
        /// <summary>
        /// Window height.
        /// </summary>
        public double WindowHeight { get; set; } = 650;
        
        /// <summary>
        /// Window state (Normal, Maximized, Minimized).
        /// </summary>
        public string WindowState { get; set; } = "Normal";
    }
}
