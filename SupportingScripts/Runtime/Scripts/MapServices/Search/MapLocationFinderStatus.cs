// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity.Search
{
    /// <summary>
    /// Represents the status of a completed MapLocationFinder request.
    /// </summary>
    public enum MapLocationFinderStatus
    {
        /// <summary>
        /// The request completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// The request was cancelled by the user.
        /// </summary>
        Cancel,

        /// <summary>
        /// A fatal parsing error has occured while processing the response.
        /// </summary>
        BadResponse,

        /// <summary>
        /// The credentials provided for the request were not valid.
        /// </summary>
        InvalidCredentials,

        /// <summary>
        /// A network failure has occured while processing the request.
        /// </summary>
        NetworkFailure,

        /// <summary>
        /// A server error has occured while processing the request.
        /// </summary>
        ServerError,

        /// <summary>
        /// An unknown error has occured while processing the request.
        /// </summary>
        UnknownError,

        /// <summary>
        /// The request succeeded but the response was empty.
        /// </summary>
        EmptyResponse
    }
}
