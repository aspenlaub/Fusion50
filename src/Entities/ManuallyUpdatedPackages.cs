using System.Collections.Generic;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities {
    [XmlRoot("ManuallyUpdatedPackages")]
    public class ManuallyUpdatedPackages : List<ManuallyUpdatedPackage>, ISecretResult<ManuallyUpdatedPackages> {
        public ManuallyUpdatedPackages Clone() {
            var clone = new ManuallyUpdatedPackages();
            clone.AddRange(this);
            return clone;
        }
    }
}
