using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Entities;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Entities;
using Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Components {
    public class NugetPackageUpdater : INugetPackageUpdater {
        private readonly IGitUtilities GitUtilities;
        private readonly IProcessRunner ProcessRunner;
        private readonly INugetFeedLister NugetFeedLister;
        private readonly ISecretRepository SecretRepository;
        private readonly IPackageConfigsScanner PackageConfigsScanner;
        private readonly ISimpleLogger SimpleLogger;

        private readonly IList<string> EndingsThatAllowReset = new List<string> { "csproj", "config" };

        public NugetPackageUpdater(IGitUtilities gitUtilities, IProcessRunner processRunner, INugetFeedLister nugetFeedLister, ISecretRepository secretRepository, IPackageConfigsScanner packageConfigsScanner, ISimpleLogger simpleLogger) {
            GitUtilities = gitUtilities;
            ProcessRunner = processRunner;
            NugetFeedLister = nugetFeedLister;
            SecretRepository = secretRepository;
            PackageConfigsScanner = packageConfigsScanner;
            SimpleLogger = simpleLogger;
        }

        public async Task<YesNoInconclusive> UpdateNugetPackagesInRepositoryAsync(IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos) {
            using (SimpleLogger.BeginScope(SimpleLoggingScopeId.Create(nameof(UpdateNugetPackagesInRepositoryAsync), Guid.NewGuid().ToString()))) {
                SimpleLogger.LogInformation("Determining files with uncommitted changes");
                var yesNoInconclusive = new YesNoInconclusive();
                var files = GitUtilities.FilesWithUncommittedChanges(repositoryFolder);
                yesNoInconclusive.Inconclusive = files.Any(f => EndingsThatAllowReset.All(e => !f.EndsWith("." + e, StringComparison.InvariantCultureIgnoreCase)));
                yesNoInconclusive.YesNo = false;
                if (yesNoInconclusive.Inconclusive) {
                    errorsAndInfos.Infos.Add("Not all files allow a reset");
                    SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                    return yesNoInconclusive;
                }

                SimpleLogger.LogInformation("Resetting repository");
                GitUtilities.Reset(repositoryFolder, GitUtilities.HeadTipIdSha(repositoryFolder), errorsAndInfos);
                if (errorsAndInfos.AnyErrors()) {
                    errorsAndInfos.Infos.Add("Could not reset");
                    SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                    return yesNoInconclusive;
                }

                SimpleLogger.LogInformation("Searching for project files");
                var projectFileFullNames = Directory.GetFiles(repositoryFolder.SubFolder("src").FullName, "*.csproj", SearchOption.AllDirectories).ToList();
                if (!projectFileFullNames.Any()) {
                    errorsAndInfos.Infos.Add("No project files found");
                    SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                    return yesNoInconclusive;
                }

                foreach (var projectFileFullName in projectFileFullNames) {
                    SimpleLogger.LogInformation($"Analyzing project file {projectFileFullName}");
                    var projectErrorsAndInfos = new ErrorsAndInfos();
                    if (!await UpdateNugetPackagesForProjectAsync(projectFileFullName, yesNoInconclusive.YesNo, projectErrorsAndInfos)) {
                        continue;
                    }

                    yesNoInconclusive.YesNo = true;
                }

                if (yesNoInconclusive.YesNo) {
                    errorsAndInfos.Infos.Add("No project was updated");
                    SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                    return yesNoInconclusive;
                }

                SimpleLogger.LogInformation("Resetting repository");
                GitUtilities.Reset(repositoryFolder, GitUtilities.HeadTipIdSha(repositoryFolder), errorsAndInfos);
                SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                return yesNoInconclusive;
            }
        }

        public async Task<YesNoInconclusive> UpdateNugetPackagesInSolutionAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos) {
            using (SimpleLogger.BeginScope(SimpleLoggingScopeId.Create(nameof(UpdateNugetPackagesInSolutionAsync), Guid.NewGuid().ToString()))) {
                SimpleLogger.LogInformation("Searching for project files");
                var yesNoInconclusive = new YesNoInconclusive();
                var projectFileFullNames = Directory.GetFiles(solutionFolder.FullName, "*.csproj", SearchOption.AllDirectories).ToList();
                if (!projectFileFullNames.Any()) {
                    SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                    return yesNoInconclusive;
                }

                foreach (var projectFileFullName in projectFileFullNames) {
                    SimpleLogger.LogInformation($"Analyzing project file {projectFileFullName}");
                    yesNoInconclusive.YesNo = await UpdateNugetPackagesForProjectAsync(projectFileFullName, yesNoInconclusive.YesNo, errorsAndInfos);
                }

                SimpleLogger.LogInformation($"Returning {yesNoInconclusive}");
                return yesNoInconclusive;
            }
        }

        private async Task<bool> UpdateNugetPackagesForProjectAsync(string projectFileFullName, bool yesNo, IErrorsAndInfos errorsAndInfos) {
            using (SimpleLogger.BeginScope(SimpleLoggingScopeId.Create(nameof(UpdateNugetPackagesForProjectAsync), Guid.NewGuid().ToString()))) {
                SimpleLogger.LogInformation("Retrieving dependency ids and versions");
                var dependencyErrorsAndInfos = new ErrorsAndInfos();
                var dependencyIdsAndVersions =
                    await PackageConfigsScanner.DependencyIdsAndVersionsAsync(projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\')), true, true, dependencyErrorsAndInfos);

                SimpleLogger.LogInformation("Retrieving manually updated packages");
                var secret = new SecretManuallyUpdatedPackages();
                var manuallyUpdatedPackages = await SecretRepository.GetAsync(secret, errorsAndInfos);
                if (errorsAndInfos.AnyErrors()) {
                    SimpleLogger.LogInformation("Returning false");
                    return false;
                }

                foreach (var id in dependencyIdsAndVersions.Select(dependencyIdsAndVersion => dependencyIdsAndVersion.Key).Where(id => manuallyUpdatedPackages.All(p => p.Id != id))) {
                    SimpleLogger.LogInformation($"Updating dependency {id}");
                    var projectFileFolder = new Folder(projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\')));
                    ProcessRunner.RunProcess("dotnet", "remove " + projectFileFullName + " package " + id, projectFileFolder, errorsAndInfos);
                    ProcessRunner.RunProcess("dotnet", "add " + projectFileFullName + " package " + id, projectFileFolder, errorsAndInfos);
                }

                SimpleLogger.LogInformation("Retrieving dependency ids and versions once more");
                var dependencyIdsAndVersionsAfterUpdate =
                    await PackageConfigsScanner.DependencyIdsAndVersionsAsync(projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\')), true, true, dependencyErrorsAndInfos);

                SimpleLogger.LogInformation("Determining differences");
                foreach (var dependencyIdsAndVersion in dependencyIdsAndVersionsAfterUpdate) {
                    var id = dependencyIdsAndVersion.Key;
                    var version = dependencyIdsAndVersion.Value;
                    yesNo = yesNo || !dependencyIdsAndVersions.ContainsKey(id) || version != dependencyIdsAndVersions[id];
                }

                SimpleLogger.LogInformation($"Returning {yesNo}");
                return yesNo;
            }
        }

        public async Task<bool> AreThereNugetUpdateOpportunitiesAsync(IFolder repositoryFolder, IErrorsAndInfos errorsAndInfos) {
            return await AreThereNugetUpdateOpportunitiesForSolutionAsync(repositoryFolder.SubFolder("src"), errorsAndInfos);
        }

        public async Task<bool> AreThereNugetUpdateOpportunitiesForSolutionAsync(IFolder solutionFolder, IErrorsAndInfos errorsAndInfos) {
            var projectFileFullNames = Directory.GetFiles(solutionFolder.FullName, "*.csproj", SearchOption.AllDirectories).ToList();
            if (!projectFileFullNames.Any()) {
                return false;
            }

            var nugetFeedsSecret = new SecretNugetFeeds();
            var nugetFeeds = await SecretRepository.GetAsync(nugetFeedsSecret, errorsAndInfos);
            if (errorsAndInfos.Errors.Any()) { return false; }

            var feedIds = nugetFeeds.Select(f => f.Id).ToList();
            foreach (var projectFileFullName in projectFileFullNames) {
                if (await AreThereNugetUpdateOpportunitiesForProjectAsync(projectFileFullName, feedIds, errorsAndInfos)) {
                    return !errorsAndInfos.AnyErrors();
                }
            }

            return false;
        }

        private async Task<bool> AreThereNugetUpdateOpportunitiesForProjectAsync(string projectFileFullName, IList<string> nugetFeedIds, IErrorsAndInfos errorsAndInfos) {
            var dependencyErrorsAndInfos = new ErrorsAndInfos();
            var dependencyIdsAndVersions = await PackageConfigsScanner.DependencyIdsAndVersionsAsync(projectFileFullName.Substring(0, projectFileFullName.LastIndexOf('\\')), true, true, dependencyErrorsAndInfos);

            var secret = new SecretManuallyUpdatedPackages();
            var manuallyUpdatedPackages = await SecretRepository.GetAsync(secret, errorsAndInfos);
            if (errorsAndInfos.AnyErrors()) { return false; }

            var yesNo = false;
            foreach (var dependencyIdsAndVersion in dependencyIdsAndVersions) {
                var id = dependencyIdsAndVersion.Key;
                if (manuallyUpdatedPackages.Any(p => p.Id == id)) { continue; }

                IList<IPackageSearchMetadata> remotePackages = null;
                foreach (var nugetFeedId in nugetFeedIds) {
                    var listingErrorsAndInfos = new ErrorsAndInfos();
                    remotePackages = await NugetFeedLister.ListReleasedPackagesAsync(nugetFeedId, id, listingErrorsAndInfos);
                    if (listingErrorsAndInfos.AnyErrors()) {
                        continue;
                    }
                    if (remotePackages.Any()) {
                        break;
                    }
                }

                if (remotePackages?.Any() != true) {
                    continue;
                }

                if (!Version.TryParse(dependencyIdsAndVersion.Value, out var version)) {
                    continue;
                }

                var latestRemotePackageVersion = remotePackages.Max(p => p.Identity.Version.Version);
                if (latestRemotePackageVersion <= version || latestRemotePackageVersion?.ToString().StartsWith(version.ToString()) == true) {
                    continue;
                }

                errorsAndInfos.Infos.Add(string.Format(Properties.Resources.CanUpdatePackageFromTo, id, version, latestRemotePackageVersion));
                yesNo = true;
            }

            return yesNo;
        }
    }
}
