using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface IChangedBinariesLister {
        IList<BinaryToUpdate> ListChangedBinaries(string repositoryId, string previousHeadTipIdSha, string currentHeadTipIdSha, IErrorsAndInfos errorsAndInfos);
    }
}
