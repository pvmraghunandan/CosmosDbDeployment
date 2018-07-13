// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Core
{
    /// <summary>
    /// Specifies the indexing mode for a collection
    /// </summary>
    public enum IndexingMode
    {
        /// <summary>
        /// The none
        /// </summary>
        None = 0,

        /// <summary>
        /// The consistent
        /// </summary>
        Consistent = 1,

        /// <summary>
        /// The lazy
        /// </summary>
        Lazy = 2        
    }
}