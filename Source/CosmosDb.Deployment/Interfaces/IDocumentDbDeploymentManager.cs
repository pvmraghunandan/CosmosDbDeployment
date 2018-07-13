// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CosmosDb.Deployment.Core;
    using Microsoft.Azure.Documents;

    /// <summary>
    /// Common Interface for Document database Deployment Manager
    /// </summary>
    public interface IDocumentDbDeploymentManager
    {
        /// <summary>
        /// Configures the document database.
        /// </summary>
        /// <param name="documentDbConfig">The document database configuration.</param>
        /// <returns>The Task</returns>
        Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig);

        /// <summary>
        /// Configures the document database.
        /// </summary>
        /// <param name="documentDbConfig">The document database configuration.</param>
        /// <param name="allowUpdate">if set to <c>true</c> [allow update].</param>
        /// <returns>The Task</returns>
        Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig, bool allowUpdate);

        /// <summary>
        /// Creates the user permission asynchronous.
        /// </summary>
        /// <param name="resourceLink">The resource link.</param>
        /// <param name="permissionMode">The permission mode.</param>
        /// <param name="database">The database.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="resourcePartitionKey">The resource partition key.</param>
        /// <returns>
        /// User's Permission
        /// </returns>
        Task<Permission> CreateUserPermissionAsync(string resourceLink, Core.PermissionMode permissionMode, string database, string userName, string resourcePartitionKey = null);

        /// <summary>
        /// Gets the permissions.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="userName">Name of the user.</param>
        /// <returns>
        /// User's Permissions
        /// </returns>
        Task<IList<Permission>> GetPermissions(string database, string userName);
    }
}