// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentDbManagerVerbOptionsBase.cs" company="Microsoft"> 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Console
{
    using CommandLine;

    /// <summary>
    /// The document DB manager verb options base class
    /// </summary>
    public class DocumentDbManagerVerbOptionsBase
    {
        /// <summary>
        /// Gets or sets the document database connection string.
        /// </summary>
        /// <value>
        /// The document database connection string.
        /// </value>
        [Option("DocumentDbConnectionString", HelpText = "Connection string for the document DB", Required = true)]
        public string DocumentDbConnectionString { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [should update].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [should update]; otherwise, <c>false</c>.
        /// </value>
        [Option("ShouldUpdate", HelpText = "Updates document db resources", Required = false)]
        public bool ShouldUpdate { get; set; }

        /// <summary>
        /// Gets or sets the configuration file path.
        /// </summary>
        /// <value>
        /// The configuration file path.
        /// </value>
        [Option("ConfigurationFilePath", HelpText = "Configuration file path is required", Required = true)]
        public string ConfigurationFilePath { get; set; }
    }
}