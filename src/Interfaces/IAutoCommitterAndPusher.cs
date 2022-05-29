using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMember.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface IAutoCommitterAndPusher {
        Task AutoCommitAndPushSingleCakeFileAsync(string nugetFeedId, IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos);
        Task AutoCommitAndPushSingleCakeFileIfNecessaryAsync(string nugetFeedId, IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos);
        Task AutoCommitAndPushPackageUpdates(string nugetFeedId, IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos);
    }
}
