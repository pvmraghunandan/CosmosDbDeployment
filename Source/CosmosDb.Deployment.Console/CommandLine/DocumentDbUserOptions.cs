// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Console
{
    using CommandLine;
    using CommandLine.Text;
    using CosmosDb.Deployment.Core;

    /// <summary>
    /// The Document DB User Options
    /// </summary>
    [Verb("setupUser", HelpText = "Run the tool in user registration mode")]
    public class DocumentDbUserOptions : DocumentDbManagerVerbOptionsBase
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        [Option("DatabaseName", HelpText = "Database name is required", Required = true)]
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        [Option("UserName", HelpText = "User Name is required",  Required = true)]
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the resource link.
        /// </summary>
        /// <value>
        /// The resource link.
        /// </value>
        [Option("resourceLink", HelpText = "ResourceLink is required", Required = true)]
        public string ResourceLink { get; set; }

        /// <summary>
        /// Gets or sets the permission mode.
        /// </summary>
        /// <value>
        /// The permission mode.
        /// </value>
        [Option("permissionMode", HelpText = "Permission Mode is required", Required = true)]
        public PermissionMode PermissionMode { get; set; }
    }
}