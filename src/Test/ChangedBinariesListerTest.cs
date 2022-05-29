using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Test {
    [TestClass]
    public class ChangedBinariesListerTest {
        private const string BeforeMajorPeghChangeHeadTipSha = "932cb235841ce7ab5afc80fcbc3220c4ae54933e";
        private const string PreviousPeghHeadTipIdSha = "6e314114c347c17776bdd8367cc5d0f1687a7775";
        private const string CurrentPeghHeadTipIdSha = "b09bf637ae6eb84e098c81da6281034ea685f307";
        private const string CurrentProuserHeadTipIdSha = "1719eb27a77904c88886a184ddfbcfeb4419000d";
        private const string PreviousProuserHeadTipIdSha = "7418e247ddee3caf5b841187df868a1ecbaab7bc";

        private readonly IContainer Container;

        public ChangedBinariesListerTest() {
            Container = new ContainerBuilder().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter()).Build();
        }

        [TestMethod]
        public void UnchangedPeghBinariesAreNotListed() {
            var sut = Container.Resolve<IChangedBinariesLister>();
            Assert.IsNotNull(sut);
            var errorsAndInfos = new ErrorsAndInfos();
            var changedBinaries = sut.ListChangedBinaries("Pegh", PreviousPeghHeadTipIdSha, CurrentPeghHeadTipIdSha, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.IsFalse(changedBinaries.Any());
        }

        [TestMethod]
        public void CanListChangedPeghBinaries() {
            var sut = Container.Resolve<IChangedBinariesLister>();
            Assert.IsNotNull(sut);
            var errorsAndInfos = new ErrorsAndInfos();
            var changedBinaries = sut.ListChangedBinaries("Pegh", BeforeMajorPeghChangeHeadTipSha, CurrentPeghHeadTipIdSha, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(3, changedBinaries.Count);
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Pegh.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Pegh.pdb"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Pegh.deps.json"));
        }

        [TestMethod, Ignore]
        public void CanListChangedProuserBinaries() {
            var sut = Container.Resolve<IChangedBinariesLister>();
            Assert.IsNotNull(sut);
            var errorsAndInfos = new ErrorsAndInfos();
            var changedBinaries = sut.ListChangedBinaries("Prouser", PreviousProuserHeadTipIdSha, CurrentProuserHeadTipIdSha, errorsAndInfos);
            Assert.IsFalse(errorsAndInfos.AnyErrors(), errorsAndInfos.ErrorsPlusRelevantInfos());
            Assert.AreEqual(9, changedBinaries.Count);
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Prouser.deps.json"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Prouser.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Aspenlaub.Net.GitHub.CSharp.Prouser.pdb"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Microsoft.Web.WebView2.Core.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Microsoft.Web.WebView2.WinForms.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == "Microsoft.Web.WebView2.Wpf.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == @"runtimes\win-arm64\native\WebView2Loader.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == @"runtimes\win-x64\native\WebView2Loader.dll"));
            Assert.IsTrue(changedBinaries.Any(c => c.FileName == @"runtimes\win-x86\native\WebView2Loader.dll"));
        }
    }
}
