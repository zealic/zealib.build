using System;
using System.IO;
using System.Xml;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using Vestris.ResourceLib;

namespace Zealib.Build.Tasks
{
    public class EmbedManifest : Task
    {
        [Required]
        public string TargetFile { get; set; }

        [Required]
        public string ManifestFile { get; private set; }

        public override bool Execute()
        {
            if (!File.Exists(TargetFile))
            {
                Log.LogError("Target file \"{0}\" dose not exist.", ManifestFile);
                return false;
            }
            if (!File.Exists(ManifestFile))
            {
                Log.LogError("Manifest file \"{0}\" dose not exist.", ManifestFile);
                return false;
            }
            var doc = new XmlDocument();
            doc.Load(ManifestFile);
            var manifestResource = new ManifestResource { Manifest = doc };
            manifestResource.SaveTo(TargetFile);
            return true;
        }
    }
}
