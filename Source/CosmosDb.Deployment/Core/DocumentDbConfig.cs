// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Core
{
    using System.Collections.Generic;
    using System.Linq;

    using NLog;

    /// <summary>
    /// The Document DB Config
    /// </summary>
    public class DocumentDbConfig
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Gets or sets the databases.
        /// </summary>
        /// <value>
        /// The databases.
        /// </value>
        public IEnumerable<Database> Databases { get; set; }

        /// <summary>
        /// Validates the model.
        /// </summary>
        /// <returns>Result of Validation</returns>
        public bool ValidateModel()
        {
            var results = true;
            foreach (var coll in this.Databases.SelectMany(db => db.Collections))
            {
                if (coll.Partitioned && string.IsNullOrEmpty(coll.PartitionKey))
                {
                    results = false;
                    Logger.Info("Partition Key is mandatory for Partitioned Collection {0}", coll.Name);
                }

                if (!coll.Partitioned && !string.IsNullOrEmpty(coll.PartitionKey))
                {
                    results = false;
                    Logger.Info("Partition Key is specified for Non Partitioned Collection {0}", coll.Name);
                }

                // As per latest Release of Document Db
                if (coll.Partitioned && coll.ResourceUnits < 2500)
                {
                    Logger.Warn("{0} is Partitioned. Partitioning Collection less than 2500 RUs will be deployed as single partitioned collection", coll.Name);
                }
            }

            if (this.Databases.GroupBy(x => x.Name).Any(c => c.Count() > 1))
            {
                results = false;
                Logger.Error("Duplicate databases not permitted.");
            }

            if (this.Databases.SelectMany(x => x.Collections).GroupBy(x => x.Name).Any(c => c.Count() > 1))
            {
                results = false;
                Logger.Error("Duplicate collections not permitted.");
            }

            if (this.Databases.SelectMany(x => x.Users).GroupBy(x => x.Name).Any(c => c.Count() > 1))
            {
                results = false;
                Logger.Error("Duplicate users not permitted.");
            }

            return results;
        }
    }
}