using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities {
    public class SecretManuallyUpdatedPackages : ISecret<ManuallyUpdatedPackages> {
        private ManuallyUpdatedPackages DefaultManuallyUpdatedPackages;
        public ManuallyUpdatedPackages DefaultValue => DefaultManuallyUpdatedPackages ??= new ManuallyUpdatedPackages { new() { Id = "LibGit2Sharp" } };

        public string Guid => "7D7E7553-288F-4D05-B22E-715ECD3EACF5";
    }
}
