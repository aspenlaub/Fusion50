using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using IFolderUpdater = Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces.IFolderUpdater;
// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Test {
    [Binding]
    public class FolderUpdaterSteps {
        protected IFolder SourceFolder, SourceSubFolder, DestinationFolder;
        protected string EmptySourceFileFullName, EmptyDestinationFileFullName;
        protected string EmptySourceFileInSubFolderFullName, EmptyDestinationFileInSubFolderFullName;
        protected IDictionary<int, string> SourceFiles, DestinationFiles;
        protected IDictionary<string, DateTime> LastWriteTimeBeforeUpdate;
        protected IFolderUpdater Sut;
        protected FileStream LockFileStream;
        protected ErrorsAndInfos UpdateFolderErrorsAndInfos;

        public FolderUpdaterSteps() {
            SourceFolder = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").SubFolder(nameof(FolderUpdaterSteps)).SubFolder("Source");
            DestinationFolder = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").SubFolder(nameof(FolderUpdaterSteps)).SubFolder("Destination");
            EmptySourceFileFullName = SourceFolder.FullName + @"\empty.txt";
            EmptyDestinationFileFullName = DestinationFolder.FullName + @"\empty.txt";
            SourceSubFolder = SourceFolder.SubFolder("Subb");
            EmptySourceFileInSubFolderFullName = SourceSubFolder.FullName + @"\empty.txt";
            EmptyDestinationFileInSubFolderFullName = DestinationFolder.SubFolder("Subb").FullName + @"\empty.txt";
            SourceFiles = new Dictionary<int, string>();
            DestinationFiles = new Dictionary<int, string>();
            UpdateFolderErrorsAndInfos = new ErrorsAndInfos();
            var container = new ContainerBuilder().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter()).Build();
            Sut = container.Resolve<IFolderUpdater>();
        }

        [AfterScenario("FolderUpdater")]
        public void CleanUp() {
            LockFileStream?.Dispose();
            if (SourceFolder.Exists()) {
                Directory.Delete(SourceFolder.FullName, true);
            }
            if (DestinationFolder.Exists()) {
                Directory.Delete(DestinationFolder.FullName, true);
            }
        }

        [Given(@"I have an empty source and an empty destination folder beneath the user's temp folder")]
        public void GivenIHaveAnEmptySourceAndAnEmptyDestinationFolderBeneathTheUserSTempFolder() {
            CleanUp();
            Directory.CreateDirectory(SourceFolder.FullName);
            Directory.CreateDirectory(DestinationFolder.FullName);
        }

        [Given(@"The destination folder does not exist")]
        public void GivenTheDestinationFolderDoesNotExist() {
            if (!DestinationFolder.Exists()) { return; }

            Directory.Delete(DestinationFolder.FullName, true);
        }

        [Given(@"I place an empty file into the source folder")]
        public void GivenIPlaceAnEmptyFileIntoTheSourceFolder() {
            File.WriteAllText(EmptySourceFileFullName, "");
        }

        [Given(@"I place an empty file into a sub folder of the source folder")]
        public void GivenIPlaceAnEmptyFileIntoASubFolderOfTheSourceFolder() {
            Directory.CreateDirectory(SourceSubFolder.FullName);
            File.WriteAllText(EmptySourceFileInSubFolderFullName, "");
        }

        [Given(@"I place a (.*) kilobyte file into the source folder")]
        public void GivenIPlaceAKilobyteFileIntoTheSourceFolder(int p0) {
            var bytes = new List<byte>();
            byte b = 0;
            for (var i = 0; i < p0 * 1000; i++) {
                bytes.Add(b);
                b = (byte)((b + 1) % 256);
            }

            if (!SourceFiles.ContainsKey(p0)) {
                SourceFiles[p0] = SourceFolder.FullName + @"\" + p0 + "kilobytes.dll";
            }
            File.WriteAllBytes(SourceFiles[p0], bytes.ToArray());
        }

        [Given(@"I place a (.*) kilobyte file with (.*) differences into the destination folder")]
        public void GivenIPlaceAKilobyteFileWithDifferencesIntoTheDestinationFolder(int p0, int p1) {
            var bytes = new List<byte>();
            byte b = 0;
            for (var i = 0; i < p0 * 1000; i++) {
                if (p1 == 0 || i % 10 != 0) {
                    bytes.Add(b);
                } else {
                    bytes.Add((byte)((b + 3) % 256));
                    p1 --;
                }
                b = (byte)((b + 1) % 256);
            }

            if (!DestinationFiles.ContainsKey(p0)) {
                DestinationFiles[p0] = DestinationFolder.FullName + @"\" + p0 + "kilobytes.dll";
            }
            File.WriteAllBytes(DestinationFiles[p0], bytes.ToArray());
        }

        [Given(@"I lock the (.*) kilobyte file")]
        public void GivenILockTheKilobyteFile(int p0) {
            LockFileStream = new FileStream(DestinationFiles[p0], FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        [Given(@"I wait two seconds")]
        public void GivenIWaitTwoSeconds() {
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [When(@"I update the destination folder")]
        public void WhenIUpdateTheDestinationFolder() {
            LastWriteTimeBeforeUpdate = new Dictionary<string, DateTime>();
            if (DestinationFolder.Exists()) {
                foreach (var fileName in Directory.GetFiles(DestinationFolder.FullName, "*.*")) {
                    LastWriteTimeBeforeUpdate[fileName] = File.GetLastWriteTime(fileName);
                }
            }

            Sut.UpdateFolder(SourceFolder, DestinationFolder, FolderUpdateMethod.AssembliesButNotIfOnlySlightlyChanged, UpdateFolderErrorsAndInfos);
        }

        [When(@"I overwrite the empty file in the source folder")]
        public void WhenIOverwriteTheEmptyFileInTheSourceFolder() {
            File.WriteAllText(EmptySourceFileFullName, "");
        }

        [When(@"I wait two seconds")]
        public void WhenIWaitTwoSeconds() {
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        [Then(@"the destination folder exists")]
        public void ThenTheDestinationFolderExists() {
            Assert.IsTrue(DestinationFolder.Exists());
        }

        [Then(@"the empty file is present in the destination folder")]
        public void ThenTheEmptyFileIsPresentInTheDestinationFolder() {
            File.Exists(EmptyDestinationFileFullName);
        }

        [Then(@"the last write times of the empty source and the empty destination files are identical")]
        public void ThenTheLastWriteTimesOfTheEmptySourceAndTheEmptyDestinationFilesIsIdentical() {
            Assert.AreEqual(File.GetLastWriteTime(EmptySourceFileFullName), File.GetLastWriteTime(EmptyDestinationFileFullName));
        }

        [Then(@"the empty file is present in a sub folder of the destination folder")]
        public void ThenTheEmptyFileIsPresentInASubFolderOfTheDestinationFolder() {
            File.Exists(EmptyDestinationFileInSubFolderFullName);
        }

        [Then(@"the last write times of the empty source and the empty destination files in the sub folders are identical")]
        public void ThenTheLastWriteTimesOfTheEmptySourceAndTheEmptyDestinationFilesInTheSubFoldersAreIdentical() {
            Assert.AreEqual(File.GetLastWriteTime(EmptySourceFileInSubFolderFullName), File.GetLastWriteTime(EmptyDestinationFileInSubFolderFullName));
        }

        [Then(@"the last write time of the empty destination file is unchanged")]
        public void ThenTheLastWriteTimeOfTheEmptyDestinationFileIsUnchanged() {
            Assert.AreEqual(LastWriteTimeBeforeUpdate[EmptyDestinationFileFullName], File.GetLastWriteTime(EmptyDestinationFileFullName));
        }

        [Then(@"the (.*) kilobyte destination file is identical to the (.*) kilobyte source file")]
        public void ThenTheKilobyteDestinationFileIsIdenticalToTheKilobyteSourceFile(int p0, int p1) {
            Assert.IsTrue(File.Exists(SourceFiles[p1]));
            Assert.IsTrue(File.Exists(DestinationFiles[p0]));
            Assert.IsTrue(File.ReadAllBytes(SourceFiles[p1]).SequenceEqual(File.ReadAllBytes(DestinationFiles[p0])));
        }

        [Then(@"the last write times of the (.*) kilobyte source and the (.*) kilobyte destination files are identical")]
        public void ThenTheLastWriteTimesOfTheKilobyteSourceAndTheKilobyteDestinationFilesAreIdentical(int p0, int p1) {
            Assert.IsTrue(File.Exists(SourceFiles[p0]));
            Assert.IsTrue(File.Exists(DestinationFiles[p1]));
            Assert.AreEqual(File.GetLastWriteTime(SourceFiles[p0]), File.GetLastWriteTime(DestinationFiles[p1]));
        }

        [Then(@"the last write time of the (.*) kilobyte destination file is unchanged")]
        public void ThenTheLastWriteTimeOfTheKilobyteDestinationFileIsUnchanged(int p0) {
            Assert.AreEqual(LastWriteTimeBeforeUpdate[DestinationFiles[p0]], File.GetLastWriteTime(DestinationFiles[p0]));
        }

        [Then(@"no errors occurred during the folder update")]
        public void ThenNoErrorsOccurredDuringTheFolderUpdate() {
            Assert.IsFalse(UpdateFolderErrorsAndInfos.Errors.Any(), UpdateFolderErrorsAndInfos.ErrorsToString());
        }

        [Then(@"folder update failed because the (.*) kilobyte destination file could not be renamed")]
        public void ThenFolderUpdateFailedBecauseTheKilobyteDestinationFileCouldNotBeRenamed(int p0) {
            var expectedError = "Failed to rename \"" + p0 + "kilobytes.dll\" into \"~1~" + p0 + "kilobytes.dll\"";
            Assert.IsTrue(UpdateFolderErrorsAndInfos.Errors.Contains(expectedError));
        }
    }
}