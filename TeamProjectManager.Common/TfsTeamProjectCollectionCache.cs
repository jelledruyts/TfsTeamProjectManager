using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.Client;

namespace TeamProjectManager.Common
{
    /// <summary>
    /// Provides a cache for authenticated Team Project Collections.
    /// </summary>
    public static class TfsTeamProjectCollectionCache
    {
        private static IDictionary<Uri, TfsTeamProjectCollection> tfsTeamProjectCollectionCache = new Dictionary<Uri, TfsTeamProjectCollection>();

        /// <summary>
        /// Returns a Team Project Collection based on its URI. If it has already been retrieved before, the cached instance will be returned.
        /// </summary>
        /// <param name="uri">The URI of the Team Project Collection.</param>
        /// <returns>The Team Project Collection.</returns>
        public static TfsTeamProjectCollection GetTfsTeamProjectCollection(Uri uri)
        {
            lock (tfsTeamProjectCollectionCache)
            {
                if (!tfsTeamProjectCollectionCache.ContainsKey(uri))
                {
                    tfsTeamProjectCollectionCache[uri] = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(uri);
                }
                return tfsTeamProjectCollectionCache[uri];
            }
        }

        /// <summary>
        /// Disposes of all cached instances and clears the cache.
        /// </summary>
        internal static void ClearCache()
        {
            lock (tfsTeamProjectCollectionCache)
            {
                foreach (var tfsTeamProjectCollection in tfsTeamProjectCollectionCache.Values)
                {
                    tfsTeamProjectCollection.Dispose();
                }
                tfsTeamProjectCollectionCache.Clear();
            }
        }
    }
}