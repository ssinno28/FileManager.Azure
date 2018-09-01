using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using FileManager.Azure.Dictionary;
using FileManager.Azure.Dtos;
using FileManager.Azure.Helpers;
using FileManager.Azure.Interfaces;
using FileManager.Azure.Models;
using FileManager.Azure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace FileManager.Azure.Tests
{
    [TestFixture]
    public class StorageTests
    {
        private IFileManagerService _fileManagerService;
        private readonly List<string> _tempFiles = new List<string>();

        [OneTimeSetUp]
        public void FixtureSetUp()
        {
            StorageEmulator.Start();
        }

        [SetUp]
        public void Init()
        {
            var loggingFactoryMock = new Mock<ILoggerFactory>();

            var fakeIdentity = new GenericIdentity("User");
            fakeIdentity.AddClaim(new Claim("BlobContainer", "test-user-container"));
            var principal = new GenericPrincipal(fakeIdentity, null);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.SetupGet(x => x.HttpContext.User).Returns(() => principal);

            var configMock = new Mock<IOptions<StorageOptions>>();
            configMock.SetupGet(x => x.Value).Returns(() => new StorageOptions
            {
                StorageConnStr = "UseDevelopmentStorage=true;DevelopmentStorageProxyUri=http://127.0.0.1"
            });

            _fileManagerService = new FileManagerService(configMock.Object, httpContextAccessorMock.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            foreach (var tempFile in _tempFiles)
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }

            var container = await _fileManagerService.GetContainer();
            await container.DeleteIfExistsAsync();
        }

        [Test]
        public async Task Test_Add_File()
        {
            string tempFile = CreateTempFile();
            var uploadedBytes = File.ReadAllBytes(tempFile);
            
            string name = Path.GetFileNameWithoutExtension(tempFile);
            string path = $"/temp/{name}.tmp";

            await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
            Assert.IsTrue(await _fileManagerService.FileExists(path));
        }

        [Test]
        public async Task Test_Delete_File()
        {
            string tempFile = CreateTempFile();
            var uploadedBytes = File.ReadAllBytes(tempFile);
            
            string name = Path.GetFileNameWithoutExtension(tempFile);
            string path = $"/temp/{name}.tmp";

            await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
            Assert.IsTrue(await _fileManagerService.FileExists(path));

            await _fileManagerService.DeleteFile(path);
            Assert.IsFalse(await _fileManagerService.FileExists(path));
        }

        [Test]
        public async Task Test_Delete_Folder()
        {
            var files = GetFiles();

            foreach (var file in files)
            {
                var uploadedBytes = File.ReadAllBytes(file);

                string name = Path.GetFileNameWithoutExtension(file);
                string path = $"/temp/{name}.tmp";

                await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
                Assert.IsTrue(await _fileManagerService.FileExists(path));
            }

            await _fileManagerService.DeleteFile("temp/");
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                string path = $"/temp/{name}.tmp";
                Assert.IsFalse(await _fileManagerService.FileExists(path));
            }
        }

        [Test]
        public async Task Test_Get_Folder_Files()
        {
            var tempFiles = GetFiles();

            foreach (var file in tempFiles)
            {
                var uploadedBytes = File.ReadAllBytes(file);

                string name = Path.GetFileNameWithoutExtension(file);
                string path = $"/temp/{name}.tmp";

                await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
                Assert.IsTrue(await _fileManagerService.FileExists(path));
            }

            var files = await _fileManagerService.GetFolderFiles("temp/");
            Assert.AreEqual(files.Count(), 5);
        }

        [Test]
        public async Task Test_Rename_Folder()
        {
            var tempFiles = GetFiles();

            foreach (var file in tempFiles)
            {
                var uploadedBytes = File.ReadAllBytes(file);

                string name = Path.GetFileNameWithoutExtension(file);
                string path = $"/temp/{name}.tmp";

                await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
                Assert.IsTrue(await _fileManagerService.FileExists(path));
            }

            await _fileManagerService.RenameFolder(new BlobDto
            {
                Path = "temp/",
                BlobType = BlobType.Folder,
                Name = "temp"
            }, "temp2");

            var files = await _fileManagerService.GetFolderFiles("temp2");
            Assert.AreEqual(files.Count(), 5);
        }

        [Test]
        public async Task Test_Move_Folder()
        {
            var tempFiles = GetFiles();

            foreach (var file in tempFiles)
            {
                var uploadedBytes = File.ReadAllBytes(file);

                string name = Path.GetFileNameWithoutExtension(file);
                string path = $"/temp/{name}.tmp";

                await _fileManagerService.AddFile(path, "application/pdf", name, uploadedBytes);
                Assert.IsTrue(await _fileManagerService.FileExists(path));
            }

            await _fileManagerService.MoveFolder(new BlobDto
            {
                Path = "temp/",
                BlobType = BlobType.Folder,
                Name = "temp"
            }, "temp2/");

            var files = await _fileManagerService.GetFolderFiles("temp2/temp");
            Assert.AreEqual(5, files.Count());
        }

        [Test]
        public async Task Test_Get_Child_Folders()
        {
            for (var i = 0; i < 5; i++)
            {
                await _fileManagerService.AddFolder("temp/", $"folder{i}");
            }

            var files = await _fileManagerService.GetChildFolders("temp/");
            Assert.AreEqual(files.Count(), 5);
        }

        private string CreateTempFile()
        {
            string tempFile = Path.GetTempFileName();

            Random random = new Random();
            var uploadedBytes = new byte[128];
            random.NextBytes(uploadedBytes);

            File.WriteAllBytes(tempFile, uploadedBytes);

            return tempFile;
        }

        private List<string> GetFiles()
        {
            var files = new List<string>();
            for (var i = 0; i < 5; i++)
            {
                files.Add(CreateTempFile());
            }

            _tempFiles.AddRange(files);
            return files;
        }
    }
}