using System;

namespace CzkawkaWin.Events
{
    /// <summary>
    /// Event arguments for when a scan operation completes.
    /// </summary>
    public class ScanCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the scan completed successfully.
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// Whether the scan was cancelled by the user.
        /// </summary>
        public bool Cancelled { get; init; }
        
        /// <summary>
        /// Whether the scan completed but found no duplicates.
        /// </summary>
        public bool IsEmpty { get; init; }
        
        /// <summary>
        /// The JSON content of the scan results.
        /// </summary>
        public string? JsonContent { get; init; }
        
        /// <summary>
        /// The file path where the JSON results were saved.
        /// </summary>
        public string? JsonFilePath { get; init; }
        
        /// <summary>
        /// Error message if the scan failed.
        /// </summary>
        public string? ErrorMessage { get; init; }
        
        /// <summary>
        /// Duration of the scan operation.
        /// </summary>
        public TimeSpan Duration { get; init; }

        /// <summary>
        /// Creates event args for a successful scan.
        /// </summary>
        public static ScanCompletedEventArgs Successful(
            string jsonContent, 
            string jsonFilePath, 
            TimeSpan duration,
            bool isEmpty = false)
        {
            return new ScanCompletedEventArgs
            {
                Success = true,
                JsonContent = jsonContent,
                JsonFilePath = jsonFilePath,
                Duration = duration,
                IsEmpty = isEmpty
            };
        }

        /// <summary>
        /// Creates event args for a failed scan.
        /// </summary>
        public static ScanCompletedEventArgs Failed(string errorMessage, TimeSpan duration)
        {
            return new ScanCompletedEventArgs
            {
                Success = false,
                ErrorMessage = errorMessage,
                Duration = duration
            };
        }

        /// <summary>
        /// Creates event args for a cancelled scan.
        /// </summary>
        public static ScanCompletedEventArgs CancelledByUser(TimeSpan duration)
        {
            return new ScanCompletedEventArgs
            {
                Cancelled = true,
                Duration = duration
            };
        }
    }
}
