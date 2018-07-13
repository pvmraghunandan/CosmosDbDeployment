// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace CosmosDb.Deployment.Console
{
    using global::CommandLine;

    /// <summary>
    /// The Document DB Verb Options
    /// </summary>
    [Verb("setupDocumentDb", HelpText = "Run the tool to setup document db")]
    public class DocumentDbVerbOptions : DocumentDbManagerVerbOptionsBase
    {
    }
}
