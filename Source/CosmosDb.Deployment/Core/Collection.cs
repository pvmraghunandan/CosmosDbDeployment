// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Core
{
    /// <summary>
    /// The Collection
    /// </summary>
    public class Collection
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the resource units.
        /// </summary>
        /// <value>
        /// The resource units.
        /// </value>
        public int ResourceUnits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Collection"/> is partitioned.
        /// </summary>
        /// <value>
        ///   <c>true</c> if partitioned; otherwise, <c>false</c>.
        /// </value>
        public bool Partitioned { get; set; }

        /// <summary>
        /// Gets or sets the partition key.
        /// </summary>
        /// <value>
        /// The partition key.
        /// </value>
        public string PartitionKey { get; set; }

        /// <summary>
        /// Gets or sets the TTL.
        /// </summary>
        /// <value>
        /// The TTL.
        /// </value>
        public int Ttl { get; set; }

        /// <summary>
        /// Gets or sets the indexing mode.
        /// </summary>
        /// <value>
        /// The indexing mode.
        /// </value>
        public IndexingMode IndexingMode { get; set; }

        /// <summary>
        /// Gets or sets the included paths.
        /// </summary>
        /// <value>
        /// The included paths.
        /// </value>
        public string[] IncludedPaths { get; set; }

        /// <summary>
        /// Gets or sets the range index included paths.
        /// </summary>
        /// <value>
        /// The range index included paths.
        /// </value>
        public string[] RangeIndexIncludedPaths { get; set; }

        /// <summary>
        /// Gets or sets the excluded paths.
        /// </summary>
        /// <value>
        /// The excluded paths.
        /// </value>
        public string[] ExcludedPaths { get; set; }

        /// <summary>
        /// Gets or sets the stored procedures.
        /// </summary>
        /// <value>
        /// The stored procedures.
        /// </value>
        public string[] StoredProcedures { get; set; }
    }
}