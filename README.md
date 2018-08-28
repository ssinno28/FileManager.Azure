# Azure.BlobFileManager

[![Build status](https://ci.appveyor.com/api/projects/status/r46wrckv1gnaw8vg/branch/master?svg=true)](https://ci.appveyor.com/project/ssinno28/azure-blobfilemanager/branch/master)

`Install-Package FileManager.Azure`

A service that treats azure blog storage as a traditional file system and helps to create paths based on directory references for organizational purposes.

There are a few claims (that currently have defaults), that will be useful when trying to restrict access to certain paths:

 * BlobContainer - default is filemanager
* RootFolder - default is "/"
* BlobContainerSizeLimit - default is 500TB^2
