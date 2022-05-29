using System.Collections.Generic;
using System.IO;

// ReSharper disable UnusedMemberInSuper.Global

namespace Aspenlaub.Net.GitHub.CSharp.Fusion50.Interfaces {
    public interface IBinariesHelper {
        bool CanFilesOfEqualLengthBeTreatedEqual(FolderUpdateMethod folderUpdateMethod, string mainNamespace, IReadOnlyList<byte> sourceContents, IReadOnlyList<byte> destinationContents,
            FileInfo sourceFileInfo, bool hasSomethingBeenUpdated, FileSystemInfo destinationFileInfo, out string updateReason);

        bool IsBinary(string fileName);
    }
}