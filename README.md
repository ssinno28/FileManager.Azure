# FileManager.Azure

![CI](https://github.com/ssinno28/FileManager.Azure/workflows/CI/badge.svg)

`Install-Package FileManager.Azure`

A service that provides all necessary functionality to maintain your azure storage account as well as helps to create hierarchical paths for organizational purposes. You will need to add the file manager service via DI:

```c#
  services.AddFileManager(Configuration);  
```

There are a few claims (that currently have defaults), that will be useful when trying to restrict access to certain paths:

* RootFolder - default is "/"
* BlobContainerSizeLimit - default is 500TB^2

The FileManager gets injected as a Func, since every instance could be working with a separate container:

```c# 
private readonly Func<string, IFileManager> _fileManagerFactory:

public TestClass(Func<string, IFileManager> fileManagerFactory) {
	_fileManagerFactory = fileManagerFactory;
	
	var fileManager = _fileManagerFactory("myContainerName");
}
```

Make sure to add two settings to the appsettings.json file. First and most importantly is the `StorageConnStr` property and the other is a boolean property called `TakeSnapshots`. If TakeSnapshots is set to true then it will take a snapshot of a file whenever it is either replaced or deleted.

For more information check out this blog article: https://www.sammisinno.com/azure/simplifing-access-to-azure-blob-storage

In order to run the tests you have to install the azurite npm package and run this command line

`azurite-blob -s --blobPort 7777`