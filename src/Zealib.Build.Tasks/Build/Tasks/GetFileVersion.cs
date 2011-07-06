using System;
using System.Globalization;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Vestris.ResourceLib;

namespace Zealib.Build.Tasks
{
    public class GetFileVersion : Task
    {
        [Required]
        public string TargetFile { get; set; }

        public string Language { get; set; }

        [Output]
        public ITaskItem Version { get; private set; }

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
            var langCode = 0;
            if (!string.IsNullOrEmpty(Language))
                langCode = CultureInfo.GetCultureInfo(Language).LCID;
            if (!File.Exists(TargetFile))
            {
                Log.LogError("Target file \"{0}\" dose not exist.", TargetFile);
                return false;
            }

            var versionResource = new VersionResource
            {
                Language = (ushort)langCode
            };
            versionResource.LoadFrom(TargetFile);
            var versionStr = versionResource.FileVersion;
            var fileVersion = new Version(versionStr);
            var stringInfo = (StringFileInfo)versionResource["StringFileInfo"];

            VersionMajor = fileVersion.Major;
            VersionMinor = fileVersion.Minor;
            VersionBuild = fileVersion.Build;
            VersionRevision = fileVersion.Revision;

            Version = new TaskItem(versionStr);
            foreach (var e in stringInfo.Default.Strings)
                Version.SetMetadata(e.Key, e.Value.StringValue);
            return true;
        }
    }
}
