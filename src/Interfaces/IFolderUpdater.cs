using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMemberInSuper.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface IFolderUpdater {
        void UpdateFolder(IFolder sourceFolder, IFolder destinationFolder, FolderUpdateMethod folderUpdateMethod, IErrorsAndInfos errorsAndInfos);
        void UpdateFolder(IFolder sourceFolder, IFolder destinationFolder, FolderUpdateMethod folderUpdateMethod, string mainNamespace, IErrorsAndInfos errorsAndInfos);
        Task UpdateFolderAsync(string repositoryId, string sourceHeadTipIdSha, IFolder sourceFolder, string destinationHeadTipIdSha, IFolder destinationFolder,
            bool forRelease, bool createAndPushPackages, string nugetFeedId, IErrorsAndInfos errorsAndInfos);
    }

    public enum FolderUpdateMethod {
        AssembliesButNotIfOnlySlightlyChanged = 1, AssembliesEvenIfOnlySlightlyChanged = 2
    }
}
