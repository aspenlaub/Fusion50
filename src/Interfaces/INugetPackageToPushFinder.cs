using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface INugetPackageToPushFinder {
        Task<IPackageToPush> FindPackageToPushAsync(string nugetFeedId, IFolder packageFolderWithBinaries, IFolder repositoryFolder, string solutionFileFullName, IErrorsAndInfos errorsAndInfos);
    }
}
