using System.Net.NetworkInformation;

namespace TeamProjectManager.Common.Infrastructure
{
    public static class Network
    {
        private static bool? isAvailable;

        static Network()
        {
            NetworkChange.NetworkAvailabilityChanged += (sender, e) => { isAvailable = null; };
        }

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