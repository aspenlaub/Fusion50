using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities {
    public class PackageToPush : IPackageToPush {
        public string PackageFileFullName { get; set; }
        public string FeedUrl { get; set; }
        public string ApiKey { get; set; }
        public string Id { get; set; }
        public string Version { get; set; }
    }
}
