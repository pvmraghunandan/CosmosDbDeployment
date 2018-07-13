# CosmosDbDeployment
Supporting Tool to provision cosmos db databases, collections, stored procedures, users and user permissions. This tool also provides reference implementation for retrieving user permissions which is used to communicate with document db instead of using Read or Write keys.

This library has two components.

*   Common Library for document db management.
*   Console Application

### Common Library

Common library that communicates with document db. It provides functionality to

*   Create Databases
*   Create Collections (Partitioned and Non-Partitioned)
*   Create Users
*   Configuring Indexing Policies
*   Configuring TTL
*   Create Stored Procedures
*   Create User Permissions
*   Retrieve User Access Token

#### Document Db Config

The input to the document db deployment follows common structure as shown below 

![](https://github.com/pvmraghunandan/CosmosDbDeployment/blob/master/img/structure.PNG)

The database collection can have one or more document collections and users. Each document collection has following properties

*   Name: Name of the collection
*   Resource Units: Resource Units to provision
*   Partitioned: Denotes whether collection is single partitioned or multi partitioned
*   PartitionKey: Applicable for Partitioned collection and denotes partition key
*   TTL: TTL Setting on collection. By default, TTL is disabled in document collection.
*   Indexing Mode: Supports twoindexing modes : Consistent = 1, Lazy = 2\. Default indexing mode is consistent
*   Excluded Paths: JPaths excluded from document indexing
*   Included Paths: JPaths included for document indexing
*	RangeIndex Included Paths : Paths for Range Index
*   Stored Procedures: List of Relative/Absolute Paths of Node JS Scripts

#### Interface Specification

Following are the methods supported and their functionality

| Method | Description |
|--------| ------------|
|Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig) | Creates or updates Document Db entities using document db config|
| Task ConfigureDocumentDb(DocumentDbConfig documentDbConfig, bool allowUpdate) | Creates or updates (if allowupdate is true) Document Db entities using document db config|
|Task CreateUserPermissionAsync(string resourceLink, Core.PermissionMode permissionMode, string database, string userName, string resourcePartitionKey = null)|	Creates Permission to user in database. Resource Partition Key is optional and should be specified if permission is applicable only for a specific partition|
|Task GetPermissions(string database, string userName)|	Gets the permissions used to access document db. It serves as token with expiry associated to it|

### Console Application

Console Application Provides reference Implementation of consuming common library for automating creation of document db entities. It maintains the configuration as JSON and loads it to send to common library.

Console application has two modes as explained below

*   Setup Document Db:

This mode is used to setup document db by reading the config file. The command to use is

setupDocumentDb --DocumentDbConnectionString="DocumentDbConnectionString" --ConfigurationFilePath="Path to configurationFile” –ShouldUpdate

Example Command is:

CosmosDb.Deployment.Console setupDocumentDb --DocumentDbConnectionString=" AccountEndpoint=[https://contoso.documents.azure.com:443/;AccountKey=RTYLJYUIJ65HJlDjLszvnqFsFGHXwM7u1npcaJaSiub8dEtSDoPvD2QZ5ZkjYim1XQQdjvldoGHTYr0cb3bw==](https://contoso.documents.azure.com:443/;AccountKey=RTYLJYUIJ65HJlDjLszvnqFsFGHXwM7u1npcaJaSiub8dEtSDoPvD2QZ5ZkjYim1XQQdjvldoGHTYr0cb3bw==);" --ConfigurationFilePath=" Content\ConfigurationSettings.json” –ShouldUpdate

*   Setup User:

This mode is used to setup user in document db account. The command to use is

setupUser --DocumentDbConnectionString="DocumentDbConnectinString" --DatabaseName="DbName" --UserName="UserName" --resourceLink="ResourceLink" --permissionMode="Mode" --ConfigurationFilePath="ConfigFilePath"

Example Command is

CosmosDb.Deployment.Console setupUser --DocumentDbConnectionString=" AccountEndpoint=[https://contoso.documents.azure.com:443/;AccountKey=RTYLJYUIJ65HJlDjLszvnqFsFGHXwM7u1npcaJaSiub8dEtSDoPvD2QZ5ZkjYim1XQQdjvldoGHTYr0cb3bw==](https://contoso.documents.azure.com:443/;AccountKey=RTYLJYUIJ65HJlDjLszvnqFsFGHXwM7u1npcaJaSiub8dEtSDoPvD2QZ5ZkjYim1XQQdjvldoGHTYr0cb3bw==);" --DatabaseName="contoso1" --UserName="user1" --resourceLink="/dbs/contoso1/colls/coll1" --permissionMode="All" --ConfigurationFilePath="Content\ConfigurationSettings.json"
