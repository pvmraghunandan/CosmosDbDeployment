Param(  
	[Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=0)][ValidateNotNullOrEmpty()]
	[string]$ResourceGroupName,
    [Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=1)][ValidateNotNullOrEmpty()]
	[string]$CosmosDbAccountName,
	[Parameter(Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true, Position=2)][ValidateNotNullOrEmpty()]
	[string]$Environment
     )
$keys = Invoke-AzureRmResourceAction -Action listKeys -ResourceType "Microsoft.DocumentDb/databaseAccounts" -ApiVersion "2015-04-08" -ResourceGroupName $ResourceGroupName -Name $cosmosDbAccountName -Force
$cosmosDbConnectionString = "AccountEndpoint=https://"+$CosmosDbAccountName+".documents.azure.com:443/;AccountKey="+$keys.primaryMasterKey +";"
try
	{
		$DocumentDBToolPath = $PSScriptRoot
		$Path = $DocumentDBToolPath + "\Content\ConfigurationSettings_" + $Environment + ".json"
		if ([System.IO.File]::Exists($Path))
		{
			$DocumentDBConfigPath = $Path
		}
		else
		{
			$DocumentDBConfigPath = Join-Path $DocumentDBToolPath "Content\ConfigurationSettings.json" -Resolve -ErrorAction Stop
		}
        
		$DocumentDBTool = Join-Path $DocumentDBToolPath "CosmosDb.Deployment.Console.exe " -Resolve
		$command = $DocumentDBTool + "setupDocumentDb --ShouldUpdate"
		$arguments += (" --DocumentDbConnectionString="  + "`"" + $cosmosDbConnectionString + "`"")
		$arguments += (" --ConfigurationFilePath=" + "`"" + $DocumentDBConfigPath + "`"")
        
		$runcommand = "$command $arguments"
		cmd /c $runcommand
		if ($LASTEXITCODE -ne 0)
		{
			throw " Exception while running DocumentDBTool "
		}
	}
	catch
	{
		Write-Error " DocumentDB Deployment Failed due to Exception"
		Write-Error $_.Exception.Message
		Write-Error " ErrorStack: $Error[0]"
        exit 1
	}