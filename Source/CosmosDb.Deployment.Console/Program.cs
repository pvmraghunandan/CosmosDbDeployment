// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft"> 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Console
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using CommandLine;
    using CosmosDb.Deployment.Core;
    using Newtonsoft.Json;
    using NLog;

    /// <summary>
    /// Program class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The integer</returns>
        public static int Main(string[] args)
        {
            try
            {
                var verbOptions = ParseCommandLine(args);
                if (verbOptions == null)
                {
                    return -1;
                }

                RunDocumentDbDeploymentAsync(verbOptions).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// Parses the command line.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <returns>The Verb Options</returns>
        private static object ParseCommandLine(string[] args)
        {
            object verbOptions = null;
            try
            {
                Parser.Default.ParseArguments<DocumentDbVerbOptions, DocumentDbUserOptions>(args).WithParsed<DocumentDbUserOptions>(opts => verbOptions = opts).WithParsed<DocumentDbVerbOptions>(opts => verbOptions = opts);
            }
            catch (Exception exception)
            {
                Logger.Error(exception.Message);
                throw;
            }

            return verbOptions;
        }

        /// <summary>
        /// Documents the database account set up.
        /// </summary>
        /// <param name="verbOptions">The verb options.</param>
        /// <returns>The Task</returns>
        /// <exception cref="System.Exception">Initialization of Document DB Client Failed</exception>
        private static async Task RunDocumentDbDeploymentAsync(object verbOptions)
        {
            var verbOptionsBase = verbOptions as DocumentDbManagerVerbOptionsBase;
            if (verbOptionsBase != null)
            {
                var connectionString = verbOptionsBase.DocumentDbConnectionString;
                var documentDbManager = new DocumentDbDeploymentManager(connectionString);

                // isOverWrite is a optional parameter and default value is false
                if (verbOptionsBase is DocumentDbVerbOptions)
                {
                    var docDbOptions = (DocumentDbVerbOptions)verbOptionsBase;
                    DocumentDbConfig documentDbConfig;
                    try
                    {
                        var configurationstring = File.ReadAllText(Path.GetFullPath(docDbOptions.ConfigurationFilePath));
                        documentDbConfig = JsonConvert.DeserializeObject<DocumentDbConfig>(configurationstring);
                        if (documentDbConfig != null && documentDbConfig.ValidateModel())
                        {
                            Logger.Info("Validated Configuration. Proceeding with Deployment");
                        }
                        else
                        {
                            throw new ArgumentException("Configuration validation failed");
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Error("An exception occurred while retrieving Client with the message: {0}", exception.Message);
                        return;
                    }

                    await documentDbManager.ConfigureDocumentDb(documentDbConfig, verbOptionsBase.ShouldUpdate).ConfigureAwait(false);
                }

                if (verbOptionsBase is DocumentDbUserOptions)
                {
                    var userOption = (DocumentDbUserOptions)verbOptionsBase;
                    await documentDbManager.CreateUserPermissionAsync(userOption.ResourceLink, userOption.PermissionMode, userOption.DatabaseName, userOption.UserName).ConfigureAwait(false);
                }

                Logger.Info("Document DB Deployment is completed...\n");
            }
        }
    }
}