// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Core
{
    using System;
    using System.Linq;
    using Microsoft.Azure.Documents.Client;
    using NLog;

    /// <summary>
    /// The Document DB Context
    /// </summary>
    public class DocumentDbContext
    {
        #region Global Variables 

        /// <summary>
        /// The logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The client
        /// </summary>
        private static DocumentClient client;

        #endregion

        /// <summary>
        /// Gets the document client.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="retryCountOnThrottling">The retry count on throttling.</param>
        /// <param name="retryIntervalInSeconds">The retry interval in seconds.</param>
        /// <returns>
        /// The DocumentClient
        /// </returns>
        /// <exception cref="Exception">Invalid Document Database Connection String</exception>
        /// <exception cref="System.Exception">Invalid Document Database Connection String</exception>
        public DocumentClient GetDocumentClient(string connectionString, int retryCountOnThrottling = 9, int retryIntervalInSeconds = 30)
        {
            try
            {
                Logger.Info("Creating document db client");
                var documentDbConnectionStringMembers = connectionString.Split(new[] { Constants.SemiColonPunctuation }, StringSplitOptions.None);
                var accountEndPoint = documentDbConnectionStringMembers.FirstOrDefault(x => x.Contains(Constants.DocumentDbAccountHostPrefix));
                var accountKey = documentDbConnectionStringMembers.FirstOrDefault(t => t.Contains(Constants.DocumentDbAccountKeyPrefix));
                if (string.IsNullOrWhiteSpace(accountEndPoint) || string.IsNullOrWhiteSpace(accountKey))
                {
                    throw new Exception("Invalid Document Db Connection String");
                }

                accountEndPoint = accountEndPoint.Replace(Constants.DocumentDbAccountHostPrefix, string.Empty);
                accountKey = accountKey.Replace(Constants.DocumentDbAccountKeyPrefix, string.Empty);

                // Get the Connection Policy
                Logger.Info("Setting up connection policy");
                var connectionPolicy = new ConnectionPolicy
                {
                    RetryOptions = new RetryOptions
                    {
                        MaxRetryWaitTimeInSeconds = retryCountOnThrottling,
                        MaxRetryAttemptsOnThrottledRequests = retryCountOnThrottling
                    },
                };
                client = new DocumentClient(new Uri(accountEndPoint), accountKey, connectionPolicy);

                Logger.Info("client created");
            }
            catch (Exception exception)
            {
                Logger.Error("An exception occurred while creating Client with the message: {0}", exception.Message);
            }

            return client;
        }
    }
}