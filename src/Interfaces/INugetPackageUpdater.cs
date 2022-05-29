using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

// ReSharper disable UnusedMemberInSuper.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface INugetPackageUpdater {
        Task<bool> AreThereNugetUpdateOpportunitiesAsync(IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos);
        Task<bool> AreThereNugetUpdateOpportunitiesForSolutionAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
        Task<YesNoInconclusive> UpdateNugetPackagesInRepositoryAsync(IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos);
        Task<YesNoInconclusive> UpdateNugetPackagesInSolutionAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos);
    }
}
