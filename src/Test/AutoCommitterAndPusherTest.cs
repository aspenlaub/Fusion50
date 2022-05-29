using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Components;
using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Test {
    [TestClass]
    public class AutoCommitterAndPusherTest {

        [TestMethod]
        public void CanConstructAutoCommitterAndPusher() {
            var container = new ContainerBuilder().UseGittyTestUtilities().UseFusionNuclideProtchAndGitty(new DummyCsArgumentPrompter()).Build();
            Assert.IsNotNull(container.Resolve<IAutoCommitterAndPusher>());
        }
    }
}
