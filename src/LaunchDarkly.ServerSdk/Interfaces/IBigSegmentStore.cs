﻿using System;

namespace LaunchDarkly.Sdk.Server.Interfaces
{
    /// <summary>
    /// Interface for a read-only data store that allows querying of user membership in big segments.
    /// </summary>
    /// <remarks>
    /// "Big segments" are a specific type of user segments. For more information, read the LaunchDarkly
    /// documentation about user segments: https://docs.launchdarkly.com/home/users
    /// </remarks>
    public interface IBigSegmentStore : IDisposable
    {
        /// <summary>
        /// Queries the store for a snapshot of the current segment state for a specific user.
        /// </summary>
        /// <remarks>
        /// The userHash is a base64-encoded string produced by hashing the user key as defined by
        /// the big segments specification; the store implementation does not need to know the details
        /// of how this is done, because it deals only with already-hashed keys, but the string can be
        /// assumed to only contain characters that are valid in base64.
        /// </remarks>
        /// <param name="userHash">the hashed user identifier</param>
        /// <returns>the user's segment membership state; may be null if no such user exists</returns>
        BigSegmentStoreTypes.IMembership GetMembership(string userHash);

        /// <summary>
        /// Returns information about the overall state of the store.
        /// </summary>
        /// <remarks>
        /// This method will be called only when the SDK needs the latest state, so it should not be cached.
        /// </remarks>
        /// <returns>the store metadata</returns>
        BigSegmentStoreTypes.StoreMetadata GetMetadata();
    }
}
