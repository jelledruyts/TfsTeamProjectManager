using System.Net.NetworkInformation;

namespace TeamProjectManager.Common.Infrastructure
{
    /// <summary>
    /// Provides network information.
    /// </summary>
    public static class Network
    {
        private static bool? isAvailable;

        static Network()
        {
            NetworkChange.NetworkAvailabilityChanged += (sender, e) => { isAvailable = null; };
        }

        /// <summary>
        /// Determines if a network connection is available.
        /// </summary>
        /// <returns><see langword="true"/> if a network connection is available; <see langword="false"/> otherwise.</returns>
        public static bool IsAvailable()
        {
            if (!isAvailable.HasValue)
            {
                // This is an expensive call so cache the result until the network availability changes.
                isAvailable = NetworkInterface.GetIsNetworkAvailable();
            }
            return isAvailable.Value;
        }
    }
}