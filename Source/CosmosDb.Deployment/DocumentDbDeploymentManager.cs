// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using CosmosDb.Deployment.Core;
    using CosmosDb.Deployment.Interfaces;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;
    using NLog;
    using IndexingMode = Core.IndexingMode;
    using PermissionMode = Core.PermissionMode;
    using User = Core.User;

    /// <summary>
    /// Deployment Manager for Document Database
    /// </summary>
    /// <seealso cref="Microsoft.Azure.DocumentDb.Deployment.Interfaces.IDocumentDbDeploymentManager" />
    public class DocumentDbDeploymentManager : IDocumentDbDeploymentManager
    {
        /// <summary>
        /// The connection string
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// The retry count
        /// </summary>
        private readonly int retryCount;

        /// <summary>
        /// The retry interval
        /// </summary>
        private readonly int retryInterval;

        /// <summary>
        /// The logger
        /// </summary>
        private readonly Logger logger;

        /// <summary>
        /// The client
        /// </summary>
        private DocumentClient client;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentDbDeploymentManager"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="retryCountOnThrottling">The retry count on throttling.</param>
        /// <param name="retryIntervalInSeconds">The retry interval in seconds.</param>
        public DocumentDbDeploymentManager(string connectionString, int retryCountOnThrottling = 9, int retryIntervalInSeconds = 30)
        {
            this.connectionString = connectionString;
            this.retryCount = retryCountOnThrottling;
            this.retryInterval = retryIntervalInSeconds;
            this.logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Configures the document database.
        /// </summary>
        /// <param name="documentDbConfig">The document database configuration.</param>
        /// <returns>The Task that configures document database</returns>
        public Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig)
        {
            return this.ConfigureDocumentDb(documentDbConfig, true);
        }

        /// <summary>
        /// Configures the document database.
        /// </summary>
        /// <param name="documentDbConfig">The document database configuration.</param>
        /// <param name="allowUpdate">if set to <c>true</c> [allow update].</param>
        /// <returns>
        /// The Result
        /// </returns>
        /// <exception cref="ArgumentNullException">Document database config</exception>
        public async Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig, bool allowUpdate)
        {
            if (documentDbConfig == null)
            {
                this.logger.Error("Invalid Document Db Config");
                throw new ArgumentNullException(nameof(documentDbConfig));    
            }

            await this.InitializeDocumentClientAsync().ConfigureAwait(false);

            foreach (var db in documentDbConfig.Databases)
            {
                var database = await this.CreateDatabaseAsync(db.Name).ConfigureAwait(false);
                if (database != null)
                {
                    foreach (var coll in db.Collections)
                    {
                        var collection = await this.CreateCollectionAsync(database, coll, allowUpdate).ConfigureAwait(false);
                        if (collection != null)
                        {
                            foreach (var item in coll.StoredProcedures)
                            {
                                await this.ImportStoreProcedureAsync(collection, item, allowUpdate).ConfigureAwait(false);
                            }
                        }
                    }

                    foreach (var user in db.Users)
                    {
                        await this.CreateUserAsync(database, user, allowUpdate).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the user permission asynchronous.
        /// </summary>
        /// <param name="resourceLink">The resource link.</param>
        /// <param name="permissionMode">The permission mode.</param>
        /// <param name="database">The database.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="resourcePartitionKey">The resource partition key.</param>
        /// <returns>
        /// The Resource Response
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// resourceLink
        /// or
        /// database
        /// or
        /// userName
        /// </exception>
        public async Task<Permission> CreateUserPermissionAsync(string resourceLink, PermissionMode permissionMode, string database, string userName, string resourcePartitionKey = null)
        {
            if (string.IsNullOrWhiteSpace(resourceLink))
            {
                this.logger.Error("Invalid Resource Link");
                throw new ArgumentNullException(nameof(resourceLink));
            }

            if (string.IsNullOrWhiteSpace(database))
            {
                this.logger.Error("Invalid Database Name");
                throw new ArgumentNullException(nameof(database));
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                this.logger.Error("Invalid User");
                throw new ArgumentNullException(nameof(userName));
            }

            await this.InitializeDocumentClientAsync().ConfigureAwait(false);
            ResourceResponse<Permission> permission;
            try
            {
                var db = this.client.CreateDatabaseQuery().Where(x => x.Id == database).AsEnumerable().SingleOrDefault();
                if (db == null)
                {
                    this.logger.Error("Database {0} Not found", database);
                    return null;
                }

                this.logger.Info("Checking whether {0} user exists or not", userName);
                var existingUser = this.client.CreateUserQuery(db.UsersLink).Where(x => x.Id == userName).AsEnumerable().SingleOrDefault();
                if (existingUser == null)
                {
                    this.logger.Error("User {0} doesn't exist in database {1} ", userName, database);
                    return null;
                }

                this.logger.Info("Checking whether permission exists or not");
                var permissionQuery = this.client.CreatePermissionQuery(existingUser.PermissionsLink).Where(x => x.ResourceLink == resourceLink).AsDocumentQuery();
                var permissionList = await permissionQuery.ExecuteNextAsync<Permission>().ConfigureAwait(false);
                var existingPermission = permissionList.FirstOrDefault();
                var documentDbPermission = this.GetPermissionMode(permissionMode);

                // Create if Permission is empty
                if (existingPermission == null)
                {
                    this.logger.Info("creating permission");
                    var permissiondata = new Permission
                    {
                        Id = Guid.NewGuid().ToString(),
                        PermissionMode = documentDbPermission,
                        ResourceLink = resourceLink,
                    };
                    if (!string.IsNullOrEmpty(resourcePartitionKey))
                    {
                        this.logger.Info("Setting up resource partition key");
                        permissiondata.ResourcePartitionKey = new PartitionKey(resourcePartitionKey);
                    }

                    permission = await this.client.CreatePermissionAsync(existingUser.PermissionsLink, permissiondata).ConfigureAwait(false);
                    this.logger.Info("Permission successfully created");
                }
                else if (permissionList.Any(t => t.PermissionMode == documentDbPermission))
                {
                    this.logger.Info("Permission already exists for resouce id {0} for user {1} in database {2}", resourceLink, userName, database);
                    return existingPermission;
                }
                else
                {
                    this.logger.Info("Permission already exists for resouce id {0} for user {1} in database {2}. Replacing with new permision mode {3}", resourceLink, userName, database, documentDbPermission);
                    existingPermission.PermissionMode = documentDbPermission;
                    permission = await this.client.ReplacePermissionAsync(existingPermission).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                this.logger.Error(string.Concat("Exception occured: ", exception.Message));
                throw;
            }

            if (permission != null)
            {
                return permission.Resource;
            }

            return null;
        }

        /// <summary>
        /// Gets the permissions.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="userName">Name of the user.</param>
        /// <returns>
        /// User's Permissions
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// database
        /// or
        /// userName
        /// </exception>
        public async Task<IList<Permission>> GetPermissions(string database, string userName)
        {
            if (string.IsNullOrWhiteSpace(database))
            {
                this.logger.Error("Invalid Database Name");
                throw new ArgumentNullException(nameof(database));
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                this.logger.Error("Invalid User");
                throw new ArgumentNullException(nameof(userName));
            }

            await this.InitializeDocumentClientAsync().ConfigureAwait(false);
            var permsFeed = await this.client.ReadPermissionFeedAsync(UriFactory.CreateUserUri(database, userName)).ConfigureAwait(false);
            return permsFeed.ToList();
        }

        #region private non static Methods

        /// <summary>
        /// Validates the partition key.
        /// </summary>
        /// <param name="destinationPartitionKey">The destination partition key.</param>
        /// <param name="sourcePartitionKey">The source partition key.</param>
        /// <returns>The boolean</returns>
        private static bool ValidatePartitionKey(PartitionKeyDefinition destinationPartitionKey, PartitionKeyDefinition sourcePartitionKey)
        {
            return sourcePartitionKey.Paths.SequenceEqual(destinationPartitionKey.Paths);
        }

        /// <summary>
        /// Creates the database if not exists.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <returns>The Resource Response for database</returns>
        private async Task<Microsoft.Azure.Documents.Database> CreateDatabaseAsync(string databaseName)
        {
            ResourceResponse<Microsoft.Azure.Documents.Database> response = null;
            try
            {
                this.logger.Info("Checking whether {0} database exists or not", databaseName);
                var db = this.client.CreateDatabaseQuery().Where(x => x.Id == databaseName).AsEnumerable().SingleOrDefault();
                if (db == null)
                {
                    this.logger.Info("{0} Database doesn't exist, Creating it", databaseName);
                    response = await this.client.CreateDatabaseAsync(new Microsoft.Azure.Documents.Database() { Id = databaseName }).ConfigureAwait(false);
                    this.logger.Info("{0} Database is created", databaseName);
                }
                else
                {
                    this.logger.Info("{0} database exists.", databaseName);
                    response = new ResourceResponse<Microsoft.Azure.Documents.Database>(db);
                }
            }
            catch (Exception exception)
            {
                this.logger.Error("An exception occured during {0} database creation with error :{1} ", databaseName, exception.Message);
                throw;
            }

            return response;
        }

        /// <summary>
        /// Creates the collection.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="collection">The collection</param>
        /// <param name="allowUpdate">if set to <c>true</c> [allow updates].</param>
        /// <returns>
        /// The Resource Response for collection
        /// </returns>
        /// <exception cref="InvalidOperationException">Document collection partition key cannot be changed.</exception>
        private async Task<DocumentCollection> CreateCollectionAsync(Microsoft.Azure.Documents.Database db, Collection collection, bool allowUpdate)
        {
            ResourceResponse<DocumentCollection> response = null;
            this.logger.Info("Checking whether {0} collection exists or not", collection.Name);
            var existingColl = this.client.CreateDocumentCollectionQuery(db.SelfLink).Where(x => x.Id == collection.Name).AsEnumerable().SingleOrDefault();
            this.logger.Info("Initializing {0} collection", collection.Name);
            var documentCollection = new DocumentCollection
            {
                Id = collection.Name,
                DefaultTimeToLive = collection.Ttl
            };

            // Setting up Partition Key
            if (collection.Partitioned && !string.IsNullOrEmpty(collection.PartitionKey))
            {
                this.logger.Info("Setting up Partition Key for the {0} collection", collection.Name);
                documentCollection.PartitionKey = new PartitionKeyDefinition();
                documentCollection.PartitionKey.Paths.Add(collection.PartitionKey);
            }

            // Excluded Paths
            foreach (var path in collection.ExcludedPaths)
            {
                this.logger.Info("Setting up excluded paths for the {0} collection", collection.Name);
                var excludedPaths = new ExcludedPath() { Path = path };
                documentCollection.IndexingPolicy.ExcludedPaths.Add(excludedPaths);
            }

            // Included Paths 
            foreach (var path in collection.IncludedPaths)
            {
                this.logger.Info("Setting up included path {0} for the {1} collection", path, collection.Name);
                var includedPaths = new IncludedPath() { Path = path };
                documentCollection.IndexingPolicy.IncludedPaths.Add(includedPaths);
            }

            // Range Index Path
            foreach (var path in collection.RangeIndexIncludedPaths)
            {
                this.logger.Info("Setting up range included path {0} for the {1} collection", path, collection.Name);
                var includedPaths = new IncludedPath() { Path = path, Indexes = new System.Collections.ObjectModel.Collection<Index> { new RangeIndex(DataType.String) { Precision = -1 } } };
                documentCollection.IndexingPolicy.IncludedPaths.Add(includedPaths);
            }

            documentCollection.IndexingPolicy.IndexingMode = this.GetIndexingMode(collection.IndexingMode);
            if (existingColl == null)
            {
                this.logger.Info("{0} Collection doesn't exist, Creating it", collection.Name);
                response = await this.client.CreateDocumentCollectionAsync(db.SelfLink, documentCollection, new RequestOptions { OfferThroughput = collection.ResourceUnits }).ConfigureAwait(false);
            }
            else if (allowUpdate)
            {
                this.logger.Info("{0} Collection exists, Updating the collection", collection.Name);

                var validationResult = ValidatePartitionKey(existingColl.PartitionKey, documentCollection.PartitionKey);
                if (validationResult)
                {
                    existingColl.PartitionKey = documentCollection.PartitionKey;
                }
                else
                {
                    throw new InvalidOperationException("Document collection partition key cannot be changed.");
                }

                existingColl.IndexingPolicy = documentCollection.IndexingPolicy;
                response = await this.client.ReplaceDocumentCollectionAsync(existingColl).ConfigureAwait(false);

                this.logger.Info("Updating throughput of the collection");
                var offer = this.client.CreateOfferQuery().Where(t => t.ResourceLink == existingColl.SelfLink).AsEnumerable().SingleOrDefault();
                if (offer != null)
                {
                    offer = new OfferV2(offer, collection.ResourceUnits);
                    await this.client.ReplaceOfferAsync(offer).ConfigureAwait(false);
                }
            }
            else
            {
                this.logger.Info("{0} Collection exists. Skipping update", collection.Name);
                response = new ResourceResponse<DocumentCollection>(existingColl);
            }

            if (response != null)
            {
                return response.Resource;
            }

            return null;
        }

        /// <summary>
        /// Create users the specified database.
        /// </summary>
        /// <param name="db">The database.</param>
        /// <param name="dbuser">Name of the user.</param>
        /// <param name="allowUpdate">if set to <c>true</c> [allow updates].</param>
        /// <returns>
        /// The Resource Response
        /// </returns>
        private async Task<Microsoft.Azure.Documents.User> CreateUserAsync(Microsoft.Azure.Documents.Database db, User dbuser, bool allowUpdate)
        {
            ResourceResponse<Microsoft.Azure.Documents.User> response = null;
            try
            {
                this.logger.Info("Checking whether {0} user exists or not", dbuser.Name);
                var existingUser = this.client.CreateUserQuery(db.UsersLink).Where(x => x.Id == dbuser.Name).AsEnumerable().SingleOrDefault();
                if (existingUser == null)
                {
                    this.logger.Info("{0} User doesn't exist. Creating it", dbuser.Name);
                    response = await this.client.CreateUserAsync(db.SelfLink, new Microsoft.Azure.Documents.User() { Id = dbuser.Name }).ConfigureAwait(false);
                }
                else if (allowUpdate)
                {
                    this.logger.Info("{0} User exists, Updating it", dbuser.Name);
                    response = await this.client.ReplaceUserAsync(existingUser).ConfigureAwait(false);
                }
                else
                {
                    this.logger.Info("{0} user exists. Skipping update", dbuser.Name);
                    response = new ResourceResponse<Microsoft.Azure.Documents.User>(existingUser);
                }
            }
            catch (Exception exception)
            {
                this.logger.Error("An exception occured in {0} user creation/update with the error :{1} ", dbuser.Name, exception.StackTrace);
                throw;
            }

            if (response != null)
            {
                return response.Resource;
            }

            return null;
        }

        /// <summary>
        /// Imports the store procedure.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <param name="scriptFilePath">The script file path.</param>
        /// <param name="allowUpdate">if set to <c>true</c> [allow update].</param>
        /// <returns>
        /// The Resource Response
        /// </returns>
        /// <exception cref="ArgumentException">Invalid File Path for the stored procedure</exception>
        private async Task<StoredProcedure> ImportStoreProcedureAsync(DocumentCollection collection, string scriptFilePath, bool allowUpdate)
        {
            ResourceResponse<StoredProcedure> response = null;
            try
            {
                string scriptPath = Path.GetFileNameWithoutExtension(scriptFilePath);

                if (scriptPath == null || !File.Exists(scriptPath))
                {
                    throw new ArgumentException("Invalid File Path for the stored procedure");
                }

                var sproc = new StoredProcedure
                {
                    Id = scriptPath,
                    Body = File.ReadAllText(scriptFilePath)
                };
                this.logger.Info("Checking whether {0} stored procedure exists or not", sproc.Id);
                var storeproc = this.client.CreateStoredProcedureQuery(collection.StoredProceduresLink).Where(x => x.Id == sproc.Id).AsEnumerable().SingleOrDefault();
                if (storeproc == null)
                {
                    this.logger.Info("{0} Stored procedure doesn't exist, Creating new one", sproc.Id);
                    response = await this.client.CreateStoredProcedureAsync(collection.SelfLink, sproc).ConfigureAwait(false);
                }
                else if (allowUpdate && collection.PartitionKey == null)
                {
                    this.logger.Info("{0} Stored procedure exists, updating it", sproc.Id);
                    storeproc.Body = sproc.Body;
                    response = await this.client.ReplaceStoredProcedureAsync(storeproc).ConfigureAwait(false);
                }
                else
                {
                    this.logger.Info("{0} Stored procedure exists and {1} collection is partitioned hence deleting stored procedure.", storeproc.Id, collection.Id);
                    var isDeleted = await this.client.DeleteStoredProcedureAsync(storeproc.SelfLink);
                    if (isDeleted.StatusCode == HttpStatusCode.NoContent)
                    {
                        this.logger.Info("Creating {0} store procedure.", sproc.Id);
                        response = await this.client.CreateStoredProcedureAsync(collection.SelfLink, sproc).ConfigureAwait(false);
                    }
                    else
                    {
                        this.logger.Error("The {0} store procedure deletion fails.", sproc.Id);
                    }
                }
            }
            catch (Exception exception)
            {
                this.logger.Error("An exception occurred in store procedure creation from path {0} with the message: {1}", scriptFilePath, exception.Message);
                throw;
            }

            return response;
        }

        /// <summary>
        /// Initializes the document client.
        /// </summary>
        /// <returns>Task that denotes opening client</returns>
        private Task InitializeDocumentClientAsync()
        {
            if (this.client == null)
            {
                var documentDbContext = new DocumentDbContext();
                this.client = documentDbContext.GetDocumentClient(this.connectionString, this.retryCount, this.retryInterval);
            }

            return this.client.OpenAsync();
        }

        /// <summary>
        /// Gets the indexing mode.
        /// </summary>
        /// <param name="indexingMode">The indexing mode.</param>
        /// <returns>Indexing Mode</returns>
        private Microsoft.Azure.Documents.IndexingMode GetIndexingMode(Core.IndexingMode indexingMode)
        {
            switch (indexingMode)
            {
                case IndexingMode.None:
                    return Microsoft.Azure.Documents.IndexingMode.None;
                case IndexingMode.Consistent:
                    return Microsoft.Azure.Documents.IndexingMode.Consistent;
                case IndexingMode.Lazy:
                    return Microsoft.Azure.Documents.IndexingMode.Lazy;
                default:
                    return Microsoft.Azure.Documents.IndexingMode.Consistent;
            }
        }

        /// <summary>
        /// Gets the permission mode.
        /// </summary>
        /// <param name="permissionMode">The permission mode.</param>
        /// <returns>Permission Mode</returns>
        private Microsoft.Azure.Documents.PermissionMode GetPermissionMode(PermissionMode permissionMode)
        {
            switch (permissionMode)
            {
                case PermissionMode.All:
                    return Microsoft.Azure.Documents.PermissionMode.All;
                case PermissionMode.Read:
                    return Microsoft.Azure.Documents.PermissionMode.Read;
                default:
                    return Microsoft.Azure.Documents.PermissionMode.Read;
            }
        }

        #endregion
    }
}