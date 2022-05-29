using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities.Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Test {
    [TestClass]
    public class NugetPackageToPushFinderTest {
        protected static TestTargetFolder PakledTarget = new(nameof(NugetPackageToPushFinderTest), "Pakled");
        protected static TestTargetFolder ChabTarget = new(nameof(NugetPackageToPushFinderTest), "Chab");
        protected static TestTargetFolder VishizhukelNetTarget = new(nameof(NugetPackageToPushFinderTest), "VishizhukelNet");
        private static IContainer Container, ContainerWithMockedPushedHeadTipShaRepository;
        protected static ITestTargetRunner TargetRunner;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            Container = new ContainerBuilder().UseGittyTestUtilities().UseProtch().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter()).Build();
            var containerBuilder = new ContainerBuilder().UseGittyTestUtilities().UseProtch().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter());
            var pushedHeadTipShaRepositoryMock = new Mock<IPushedHeadTipShaRepository>();
            pushedHeadTipShaRepositoryMock.Setup(r => r.GetAsync(It.IsAny<string>(), It.IsAny<IErrorsAndInfos>())).Returns(Task.FromResult(new List<string>()));
            containerBuilder.RegisterInstance(pushedHeadTipShaRepositoryMock.Object);
            ContainerWithMockedPushedHeadTipShaRepository = containerBuilder.Build();

            TargetRunner = Container.Resolve<ITestTargetRunner>();
        }

        [TestInitialize]
        public void Initialize() {
            PakledTarget.Delete();
            ChabTarget.Delete();
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledTarget.Delete();
            ChabTarget.Delete();
        }

        [TestMethod]
        public async Task CanFindNugetPackagesToPushForPakled() {
            var errorsAndInfos = new ErrorsAndInfos();

            var nugetFeed = await GetNugetFeedAsync(errorsAndInfos);

            CloneTarget(PakledTarget, errorsAndInfos);

            RunCakeScript(PakledTarget, true, errorsAndInfos);

            errorsAndInfos = new ErrorsAndInfos();
            var sut = Container.Resolve<INugetPackageToPushFinder>();
            var packageToPush = await sut.FindPackageToPushAsync(NugetFeed.AspenlaubLocalFeed, PakledTarget.Folder().ParentFolder().SubFolder(PakledTarget.SolutionId + @"Bin\Release"), PakledTarget.Folder(), PakledTarget.Folder().SubFolder("src").FullName + @"\" + PakledTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var source = await nugetFeed.UrlOrResolvedFolderAsync(Container.Resolve<IFolderResolver>(), errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(source, packageToPush.FeedUrl);
        }

        private static async Task<NugetFeed> GetNugetFeedAsync(IErrorsAndInfos errorsAndInfos) {
            var nugetFeedsSecret = new SecretNugetFeeds();
            var nugetFeeds = await Container.Resolve<ISecretRepository>().GetAsync(nugetFeedsSecret, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var nugetFeed = nugetFeeds.FirstOrDefault(f => f.Id == NugetFeed.AspenlaubLocalFeed);
            Assert.IsNotNull(nugetFeed);
            return nugetFeed;
        }

        [TestMethod]
        public async Task PackageForTheSameCommitIsNotPushed() {
            var errorsAndInfos = new ErrorsAndInfos();

            CloneTarget(PakledTarget, errorsAndInfos);

            var packages = await Container.Resolve<INugetFeedLister>().ListReleasedPackagesAsync(NugetFeed.AspenlaubLocalFeed, @"Aspenlaub.Net.GitHub.CSharp." + PakledTarget.SolutionId, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return; }
            if (!packages.Any()) { return; }

            var latestPackageVersion = packages.Max(p => p.Identity.Version.Version);
            var latestPackage = packages.First(p => p.Identity.Version.Version == latestPackageVersion);

            var headTipIdSha = Container.Resolve<IGitUtilities>().HeadTipIdSha(PakledTarget.Folder());
            if (!latestPackage.Tags.Contains(headTipIdSha)) {
                return; // $"No package has been pushed for {headTipIdSha} and {PakledTarget.SolutionId}, please run build.cake for this solution"
            }

            RunCakeScript(PakledTarget, false, errorsAndInfos);

            packages = await Container.Resolve<INugetFeedLister>().ListReleasedPackagesAsync(NugetFeed.AspenlaubLocalFeed, @"Aspenlaub.Net.GitHub.CSharp." + PakledTarget.SolutionId, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(latestPackageVersion, packages.Max(p => p.Identity.Version.Version));
        }

        private static void CloneTarget(ITestTargetFolder testTargetFolder, IErrorsAndInfos errorsAndInfos) {
            var gitUtilities = new GitUtilities();
            var url = "https://github.com/aspenlaub/" + testTargetFolder.SolutionId + ".git";
            gitUtilities.Clone(url, "master", testTargetFolder.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        private void RunCakeScript(ITestTargetFolder testTargetFolder, bool disableNugetPush, IErrorsAndInfos errorsAndInfos) {
            var projectLogic = Container.Resolve<IProjectLogic>();
            var projectFactory = Container.Resolve<IProjectFactory>();
            var solutionFileFullName = testTargetFolder.Folder().SubFolder("src").FullName + '\\' + testTargetFolder.SolutionId + ".sln";
            var projectErrorsAndInfos = new ErrorsAndInfos();
            Assert.IsTrue(projectLogic.DoAllConfigurationsHaveNuspecs(projectFactory.Load(solutionFileFullName, solutionFileFullName.Replace(".sln", ".csproj"), projectErrorsAndInfos)));

            var target = disableNugetPush ? "IgnoreOutdatedBuildCakePendingChangesAndDoNotPush" : "IgnoreOutdatedBuildCakePendingChanges";
            TargetRunner.RunBuildCakeScript(BuildCake.Standard, testTargetFolder, target, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [TestMethod]
        public async Task CanFindNugetPackagesToPushForChab() {
            var errorsAndInfos = new ErrorsAndInfos();
            var nugetFeed = await GetNugetFeedAsync(errorsAndInfos);

            CloneTarget(ChabTarget, errorsAndInfos);

            RunCakeScript(ChabTarget, true, errorsAndInfos);

            Assert.IsFalse(errorsAndInfos.Infos.Any(i => i.Contains("No test")));

            errorsAndInfos = new ErrorsAndInfos();
            var sut = Container.Resolve<INugetPackageToPushFinder>();
            var packageToPush = await sut.FindPackageToPushAsync(NugetFeed.AspenlaubLocalFeed, ChabTarget.Folder().ParentFolder().SubFolder(ChabTarget.SolutionId + @"Bin\Release"), ChabTarget.Folder(), ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var source = await nugetFeed.UrlOrResolvedFolderAsync(Container.Resolve<IFolderResolver>(), errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(source, packageToPush.FeedUrl);

            sut = ContainerWithMockedPushedHeadTipShaRepository.Resolve<INugetPackageToPushFinder>();
            packageToPush = await sut.FindPackageToPushAsync(NugetFeed.AspenlaubLocalFeed, ChabTarget.Folder().ParentFolder().SubFolder(ChabTarget.SolutionId + @"Bin\Release"), ChabTarget.Folder(), ChabTarget.Folder().SubFolder("src").FullName + @"\" + ChabTarget.SolutionId + ".sln", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            source = await nugetFeed.UrlOrResolvedFolderAsync(Container.Resolve<IFolderResolver>(), errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(source, packageToPush.FeedUrl);
        }

        [TestMethod]
        public async Task CanCheckForNugetPackagesToPushForVishizhukelNet() {
            var errorsAndInfos = new ErrorsAndInfos();

            var folderResolver = Container.Resolve<IFolderResolver>();
            var gitHubFolder = await folderResolver.ResolveAsync("$(GitHub)", errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());

            errorsAndInfos = new ErrorsAndInfos();
            var sut = Container.Resolve<INugetPackageToPushFinder>();
            var packageFolderWithBinaries = gitHubFolder.SubFolder(VishizhukelNetTarget.SolutionId + @"Bin\Release");
            if (!packageFolderWithBinaries.Exists()) { return; }

            var repositoryFolder = gitHubFolder.SubFolder(VishizhukelNetTarget.SolutionId);
            Assert.IsTrue(repositoryFolder.Exists(), $"Folder {repositoryFolder.FullName} does not exist");
            var solutionFileFullName = repositoryFolder.SubFolder("src").FullName + @"\" + VishizhukelNetTarget.SolutionId + ".sln";
            await sut.FindPackageToPushAsync(NugetFeed.AspenlaubLocalFeed, packageFolderWithBinaries, repositoryFolder, solutionFileFullName, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }
    }
}
