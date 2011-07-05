using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public string ProjectBeforeFile { get; set; }

        public string ProjectAfterFile { get; set; }

        public string[] DefineConstants { get; set; }

        [Output]
        public string[] SourceFiles { get; set; }

        [Output]
        public string AttachCodeFile { get; set; }

        public ITaskItem[] AttachClasses { get; set; }

        private static string Escape(string str)
        {
            return ProjectCollection.Escape(str);
        }

        public override bool Execute()
        {
            if (Projects == null) throw new ArgumentException("Projects can not be empty.");
            if (string.IsNullOrEmpty(ProjectBeforeFile))
            {
                ProjectBeforeFile = Path.GetTempFileName();
                File.WriteAllText(ProjectBeforeFile, Resources.HookProject_Before);
            }
            if (string.IsNullOrEmpty(ProjectAfterFile))
            {
                ProjectAfterFile = Path.GetTempFileName();
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
            if (AttachClasses == null) AttachClasses = new ITaskItem[0];


            if (InitializeDefineConstants() &&
                InitializeSourceFiles())
            {
                HookedProjects = Projects.Select(ProcessProject).ToArray();
                return true;
            }
            return false;
        }

        private ITaskItem ProcessProject(ITaskItem project)
        {
            var item = new TaskItem(project);
            project.CopyMetadataTo(item);
            var sb = new StringBuilder();
            var oldProperties = project.GetMetadata("Properties");
            if (!string.IsNullOrEmpty(oldProperties))
                sb.AppendFormat("{0};", oldProperties);
            sb.AppendFormat("CustomBeforeMicrosoftCommonTargets={0}", Escape(ProjectBeforeFile));
            sb.AppendFormat(";CustomAfterMicrosoftCommonTargets={0}", Escape(ProjectAfterFile));
            sb.AppendFormat(";HookProject-DefineConstants={0}", Escape(string.Join(";", DefineConstants)));
            sb.AppendFormat(";HookProject-SourceFiles={0}", Escape(string.Join(";", SourceFiles)));
            item.SetMetadata("Properties", sb.ToString());

            return item;
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
    }
}