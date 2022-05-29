using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities {
    public class ManuallyUpdatedPackage : IManuallyUpdatedPackage {
        [Key, XmlAttribute("id")]
        public string Id { get; set; }
    }
}
