using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Test {
    [TestClass]
    public class NugetPackageUpdaterTest {
        private static readonly TestTargetFolder PakledConsumerCoreTarget = new(nameof(NugetPackageUpdaterTest), "PakledConsumerCore");
        private const string PakledConsumerCoreHeadTipSha = "a1e7e4ce2906ce52ff48e7b102bd4d4522d66c97"; // Before PakledCore update
        private const string PakledCoreVersion = "2.0.610.1192"; // Before PakledCore update
        private static IContainer Container;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context) {
            Container = new ContainerBuilder().UseGittyTestUtilities().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter()).Build();
        }

        [TestInitialize]
        public void Initialize() {
            PakledConsumerCoreTarget.Delete();
            var gitUtilities = Container.Resolve<IGitUtilities>();
            var url = "https://github.com/aspenlaub/" + PakledConsumerCoreTarget.SolutionId + ".git";
            var errorsAndInfos = new ErrorsAndInfos();
            gitUtilities.Clone(url, "master", PakledConsumerCoreTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        }

        [TestCleanup]
        public void TestCleanup() {
            PakledConsumerCoreTarget.Delete();
        }

        [TestMethod]
        public async Task CanIdentifyNugetPackageOpportunity() {
            var gitUtilities = Container.Resolve<IGitUtilities>();
            var errorsAndInfos = new ErrorsAndInfos();
            gitUtilities.Reset(PakledConsumerCoreTarget.Folder(), PakledConsumerCoreHeadTipSha, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var yesNo = await NugetUpdateOpportunitiesAsync(errorsAndInfos);
            Assert.IsTrue(yesNo);
            Assert.IsTrue(errorsAndInfos.Infos.Any(i => i.Contains($"package PakledCore from {PakledCoreVersion}")));
        }

        [TestMethod]
        public async Task CanUpdateNugetPackagesWithCsProjAndConfigChanges() {
            var gitUtilities = Container.Resolve<IGitUtilities>();
            var errorsAndInfos = new ErrorsAndInfos();
            gitUtilities.Reset(PakledConsumerCoreTarget.Folder(), PakledConsumerCoreHeadTipSha, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            var packageConfigsScanner = Container.Resolve<IPackageConfigsScanner>();
            var dependencyErrorsAndInfos = new ErrorsAndInfos();
            var dependencyIdsAndVersions = await packageConfigsScanner.DependencyIdsAndVersionsAsync(PakledConsumerCoreTarget.Folder().SubFolder("src").FullName, true, false, dependencyErrorsAndInfos);
            MakeCsProjAndConfigChange();
            var yesNoInconclusive = await UpdateNugetPackagesAsync();
            Assert.IsTrue(yesNoInconclusive.YesNo);
            Assert.IsFalse(yesNoInconclusive.Inconclusive);
            yesNoInconclusive.YesNo = await NugetUpdateOpportunitiesAsync(errorsAndInfos);
            Assert.IsFalse(yesNoInconclusive.YesNo);
            var dependencyIdsAndVersionsAfterUpdate = await packageConfigsScanner.DependencyIdsAndVersionsAsync(PakledConsumerCoreTarget.Folder().SubFolder("src").FullName, true, false, dependencyErrorsAndInfos);
            Assert.AreEqual(dependencyIdsAndVersions.Count, dependencyIdsAndVersionsAfterUpdate.Count,
                $"Project had {dependencyIdsAndVersions.Count} package/-s before update, {dependencyIdsAndVersionsAfterUpdate.Count} afterwards");
            Assert.IsTrue(dependencyIdsAndVersions.All(i => dependencyIdsAndVersionsAfterUpdate.ContainsKey(i.Key)), "Package id/-s have changed");
            Assert.IsTrue(dependencyIdsAndVersions.Any(i => dependencyIdsAndVersionsAfterUpdate[i.Key].ToString() != i.Value.ToString()), "No package update was made");
        }

        [TestMethod]
        public async Task CanDetermineThatThereIsNoNugetPackageToUpdateWithCsProjAndConfigChanges() {
            var yesNoInconclusive = await UpdateNugetPackagesAsync();
            if (yesNoInconclusive.YesNo) { return; }

            MakeCsProjAndConfigChange();
            yesNoInconclusive = await UpdateNugetPackagesAsync();
            Assert.IsFalse(yesNoInconclusive.YesNo);
            Assert.IsFalse(yesNoInconclusive.Inconclusive);
        }

        [TestMethod]
        public async Task ErrorWhenAskedToUpdateNugetPackagesWithCsChange() {
            MakeCsChange();
            var yesNoInconclusive = await UpdateNugetPackagesAsync();
            Assert.IsFalse(yesNoInconclusive.YesNo);
            Assert.IsTrue(yesNoInconclusive.Inconclusive);
        }

        private async Task<bool> NugetUpdateOpportunitiesAsync(IErrorsAndInfos errorsAndInfos) {
            var sut = Container.Resolve<INugetPackageUpdater>();
            var yesNo = await sut.AreThereNugetUpdateOpportunitiesAsync(PakledConsumerCoreTarget.Folder(), errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            return yesNo;
        }

        private async Task<YesNoInconclusive> UpdateNugetPackagesAsync() {
            var sut = Container.Resolve<INugetPackageUpdater>();
            var errorsAndInfos = new ErrorsAndInfos();
            var yesNoInconclusive = await sut.UpdateNugetPackagesInRepositoryAsync(PakledConsumerCoreTarget.Folder(), errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
            return yesNoInconclusive;
        }

        private void MakeCsChange() {
            File.WriteAllText(PakledConsumerCoreTarget.FullName() + @"\src\Cs.cs", "Cs.cs");
        }

        private void MakeCsProjAndConfigChange() {
            File.WriteAllText(PakledConsumerCoreTarget.FullName() + @"\src\CsProj.csproj", "CsProj.csproj");
            File.WriteAllText(PakledConsumerCoreTarget.FullName() + @"\src\Config.config", "Config.config");
        }
    }
}
