# FileManager.Azure

![CI](https://github.com/ssinno28/FileManager.Azure/workflows/CI/badge.svg)

`Install-Package FileManager.Azure`

A service that provides all necessary functionality to maintain your azure storage account as well as helps to create hierarchical paths for organizational purposes. You will need to add the file manager service via DI:

```
  services.AddScoped<IFileManagerService, FileManagerService>();  
```

There are a few claims (that currently have defaults), that will be useful when trying to restrict access to certain paths:

* BlobContainer - default is filemanager
* RootFolder - default is "/"
* BlobContainerSizeLimit - default is 500TB^2

This package comes with an options object that should be wired up at startup like so:

```c#
 services.AddOptions();
 services.Configure<StorageOptions>(Configuration);
```

There are two properties you'll have to configure, first and most importantly is the `StorageConnStr` property and the other is a boolean property called `TakeSnapshots`. If TakeSnapshots is set to true then it will take a snapshot of a file whenever it is either replaced or deleted.
