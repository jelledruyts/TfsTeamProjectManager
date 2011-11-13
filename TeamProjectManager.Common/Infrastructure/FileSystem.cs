using System.Runtime.InteropServices;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides helper methods for files.
    /// </summary>
    public static class FileSystem
    {
        #region Constants

        private const string BlockedFileAlternateDataStreamName = "Zone.Identifier";
        private const char AlternateDataStreamSeparator = ':';
        private const int MaxPathLength = 256;
        private const string LongPathPrefix = @"\\?\";

        #endregion

        #region Blocked Files

        /// <summary>
        /// Determines if the specified file has been blocked by Windows because it comes from an untrusted source (e.g. it was downloaded).
        /// </summary>
        /// <param name="fileName">The full path of the file to verify.</param>
        /// <returns><see langword="true"/> if the file has been blocked by Windows, <see langword="false"/> otherwise.</returns>
        public static bool IsBlockedFile(string fileName)
        {
            var adsPath = BuildStreamPath(fileName, BlockedFileAlternateDataStreamName);
            bool exists = -1 != NativeMethods.GetFileAttributes(adsPath);
            return exists;
        }

        /// <summary>
        /// Unblocks the specified file if it has been blocked by Windows because it comes from an untrusted source (e.g. it was downloaded).
        /// </summary>
        /// <param name="fileName">The full path of the file to unblock.</param>
        /// <returns><see langword="true"/> if the file was unblocked, <see langword="false"/> otherwise (e.g. if the file did not exist or was not blocked).</returns>
        public static bool UnblockFile(string fileName)
        {
            var adsPath = BuildStreamPath(fileName, BlockedFileAlternateDataStreamName);
            return NativeMethods.DeleteFile(adsPath);
        }

        private static string BuildStreamPath(string fileName, string streamName)
        {
            string result = fileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                if (result.Length == 1)
                {
                    result = ".\\" + result;
                }
                result += AlternateDataStreamSeparator + streamName + AlternateDataStreamSeparator + "$DATA";
                if (result.Length >= MaxPathLength)
                {
                    result = LongPathPrefix + result;
                }
            }
            return result;
        }

        private sealed class NativeMethods
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern int GetFileAttributes(string fileName);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteFile(string name);
        }

        #endregion
    }
}