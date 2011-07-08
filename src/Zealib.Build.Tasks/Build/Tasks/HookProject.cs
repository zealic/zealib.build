using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Zealib.Properties;

namespace Zealib.Build.Tasks
{
    public class HookProject : Task
    {
        [Required]
        public ITaskItem[] Projects { get; set; }

        [Output]
        public ITaskItem[] HookedProjects { get; private set; }

        [Output]
        public string ProjectBeforeFile { get; set; }

        [Output]
        public string ProjectAfterFile { get; set; }

        public string[] DefineConstants { get; set; }

        [Output]
        public string[] SourceFiles { get; set; }

        public override bool Execute()
        {
            if (Projects == null) throw new ArgumentException("Projects can not be empty.");

            if (InitializeHook() &&
                InitializeDefineConstants() &&
                InitializeSourceFiles())
            {
                var list = new List<ITaskItem>();
                foreach (var project in Projects)
                {
                    ITaskItem result;
                    if (!ProcessProject(project, out result))
                        return false;
                    list.Add(result);
                }
                HookedProjects = list.ToArray();
                return true;
            }
            return false;
        }

        private bool InitializeHook()
        {
            var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            if (string.IsNullOrEmpty(ProjectBeforeFile))
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                ProjectBeforeFile = Path.Combine(dir, "ProjectBeforeFile.xml");
                File.WriteAllText(ProjectBeforeFile, Resources.HookProject_Before);
            }
            if (string.IsNullOrEmpty(ProjectAfterFile))
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                ProjectAfterFile = Path.Combine(dir, "ProjectAfterFile.xml");
                File.WriteAllText(ProjectAfterFile, Resources.HookProject_After);
            }

            if (!File.Exists(ProjectBeforeFile))
            {
                Log.LogError("ProjectBeforeFile \"{0}\" not exist.", ProjectBeforeFile);
                return false;
            }
            if (!File.Exists(ProjectAfterFile))
            {
                Log.LogError("ProjectAfterFile \"{0}\" not exist.", ProjectAfterFile);
                return false;
            }

            return true;
        }

        private bool InitializeDefineConstants()
        {
            if (DefineConstants == null) DefineConstants = new string[0];
            var constants = new HashSet<string>(DefineConstants)
            {
                "HOOK_PROJECT"
            };
            DefineConstants = constants.ToArray();
            return true;
        }

        private bool InitializeSourceFiles()
        {
            if (SourceFiles == null) SourceFiles = new string[0];
            bool haveError = false;
            foreach (var file in SourceFiles.Where(file => !File.Exists(file)))
            {
                haveError = true;
                Log.LogError("Source file \"{0}\" not exist.", file);
            }
            return !haveError;
        }

        private bool ProcessProject(ITaskItem project, out ITaskItem result)
        {
            result = new TaskItem(project);
            project.CopyMetadataTo(project);
            Dictionary<string, string> properties;
            if (!ParseProperties(project.GetMetadata("Properties"), out properties))
                return false;

            string parentCustomBeforeTargets, parentCustomAfterTargets;
            if (properties.TryGetValue("CustomBeforeMicrosoftCommonTargets", out parentCustomBeforeTargets))
            {
                properties["HookProject-Parent-CustomBeforeMicrosoftCommonTargets"] = Escape(parentCustomBeforeTargets);
            }
            if (properties.TryGetValue("HookProject-Parent-CustomAfterMicrosoftCommonTargets", out parentCustomAfterTargets))
            {
                properties["HookProject-Parent-CustomAfterMicrosoftCommonTargets"] = Escape(parentCustomAfterTargets);
            }

            properties["CustomBeforeMicrosoftCommonTargets"] = Escape(ProjectBeforeFile);
            properties["CustomAfterMicrosoftCommonTargets"] = Escape(ProjectAfterFile);
            properties["HookProject-DefineConstants"] = Escape(string.Join(";", DefineConstants));
            properties["HookProject-SourceFiles"] = Escape(string.Join(";", SourceFiles));

            result.SetMetadata("Properties", PropertiesToString(properties));
            return true;
        }

        private bool ParseProperties(string properties, out Dictionary<string, string> result)
        {
            result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(properties)) return true;

            string lastName = null;
            foreach (var str in properties.Split(';'))
            {
                var index = str.IndexOf("=");
                if (index != -1)
                {
                    var name = str.Substring(0, index).Trim();
                    var value = Escape(str.Substring(index + 1).Trim());
                    if (name.Length == 0)
                    {
                        Log.LogError("Properties contain invalid property name.");
                        return false;
                    }

                    if (result.ContainsKey(name))
                        result[name] = value;
                    else
                        result.Add(name, value);

                    lastName = name;
                }
                else if (lastName != null)
                {
                    result[lastName] = string.Concat(result[lastName], ";", Escape(str).Trim());
                }
                else
                {
                    Log.LogError("Properties contain invalid property name.");
                    return false;
                }
            }

            return true;
        }

        private static string PropertiesToString(Dictionary<string, string> properties)
        {
            var propertiesArray = properties
                .Select(e => string.Concat(e.Key, "=", e.Value))
                .ToList()
                .ToArray();

            return string.Join(";", propertiesArray);
        }

        private static string Escape(string str)
        {
            return ProjectCollection.Escape(str);
        }
    }
}