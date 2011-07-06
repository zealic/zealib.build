using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Zealib.Build.Tasks
{
    public class GetFileVersion : Task
    {
        [Required]
        public string TargetFile { get; set; }

        [Output]
        public string Version { get; private set; }

        [Output]
        public int VersionMajor { get; private set; }

        [Output]
        public int VersionMinor { get; private set; }

        [Output]
        public int VersionBuild { get; private set; }

        [Output]
        public int VersionRevision { get; private set; }

        public override bool Execute()
        {
            if (!File.Exists(TargetFile))
            {
                Log.LogError("Target file \"{0}\" dose not exist.", TargetFile);
                return false;
            }

            var vi = FileVersionInfo.GetVersionInfo(TargetFile);
            VersionMajor = vi.FileMajorPart;
            VersionMinor = vi.FileMinorPart;
            VersionBuild = vi.FileBuildPart;
            VersionRevision = vi.FilePrivatePart;
            Version = string.Format("{0}.{1}.{2}.{3}",
              vi.FileMajorPart, vi.FileMinorPart,
              vi.FileBuildPart, vi.FilePrivatePart);
            return true;
        }
    }
}
