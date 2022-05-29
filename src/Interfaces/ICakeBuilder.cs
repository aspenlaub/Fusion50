using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface ICakeBuilder {
        bool Build(string solutionFileName, bool debug, string tempFolderName, IErrorsAndInfos errorsAndInfos);
    }
}