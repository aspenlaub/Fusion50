using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Entities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Components {
    public class CakeBuilder : ICakeBuilder {
        private readonly IDotNetCakeRunner CakeRunner;

        public CakeBuilder(IDotNetCakeRunner cakeRunner) {
            CakeRunner = cakeRunner;
        }

        public bool Build(string solutionFileName, bool debug, string tempFolderName, IErrorsAndInfos errorsAndInfos) {
            var config = debug ? "Debug" : "Release";
            var cakeScript = new List<string> {
                "Task(\"Build\")",
                ".Does(() => {",
                $"MSBuild(@\"{solutionFileName}\", settings ",
                "=> settings",
                $".SetConfiguration(\"{config}\")",
                ".SetVerbosity(Verbosity.Minimal)",
                ".WithProperty(\"Platform\", \"Any CPU\")",
                tempFolderName == "" ? "" : $".WithProperty(\"OutDir\", @\"{tempFolderName}\")",
                ");",
                "});",
                "RunTarget(\"Build\");"
            };
            var folder = new Folder(Path.GetTempPath()).SubFolder("AspenlaubTemp").SubFolder(nameof(CakeBuilder));
            folder.CreateIfNecessary();
            var cakeFileName = folder.FullName + @"\" + "build-" + Guid.NewGuid() + ".cake";
            File.WriteAllText(cakeFileName, string.Join("\r\n", cakeScript));
            CakeRunner.CallCake(cakeFileName, errorsAndInfos);
            if (!errorsAndInfos.Errors.Any(e => e.Contains("The file is locked"))) {
                errorsAndInfos.Infos.Where(e => e.Contains("The file is locked")).ToList().ForEach(e => errorsAndInfos.Errors.Add(e));
            }
            return !errorsAndInfos.Errors.Any();
        }
    }
}
