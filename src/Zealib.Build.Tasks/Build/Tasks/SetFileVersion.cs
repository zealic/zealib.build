using System;
using System.Collections;
using System.IO;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Vestris.ResourceLib;

namespace Zealib.Build.Tasks
{
    public class SetFileVersion : Task
    {
        [Required]
        public string TargetFile { get; set; }

        [Required]
        public ITaskItem Version { get; set; }

        public override bool Execute()
        {
            if (!File.Exists(TargetFile))
            {
                Log.LogError("Target file \"{0}\" dose not exist.", TargetFile);
                return false;
            }

            var versionResource = new VersionResource();
            versionResource.LoadFrom(TargetFile);
            var stringInfo = (StringFileInfo)versionResource["StringFileInfo"];
            var versionString = Version.ItemSpec;
            foreach (DictionaryEntry entry in Version.CloneCustomMetadata())
            {
                var name = (string)entry.Key;
                var value = (string)entry.Value;
                Log.LogMessage("{0}={1}", name, value);
                if (string.IsNullOrEmpty(value)) continue;
                stringInfo[name] = value + "\0";
            }
            versionResource.FileVersion = versionString;
            versionResource.ProductVersion = versionString;
            stringInfo["FileVersion"] = versionString + "\0";
            stringInfo["ProductVersion"] = versionString + "\0";
            versionResource.SaveTo(TargetFile);
            return true;
        }
    }
}
