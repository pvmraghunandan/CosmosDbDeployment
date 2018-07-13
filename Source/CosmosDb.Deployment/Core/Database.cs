// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// The Databases
    /// </summary>
    public class Database
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collections.
        /// </summary>
        /// <value>
        /// The collections.
        /// </value>
        public IEnumerable<Collection> Collections { get; set; }

        /// <summary>
        /// Gets or sets the users.
        /// </summary>
        /// <value>
        /// The users.
        /// </value>
        public IEnumerable<User> Users { get; set; }
    }
}