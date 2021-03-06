using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zealib.Build.Tasks
{
    public class PruneSolution : Task
    {
        [Required]
        public string[] Files { get; set; }

        [Required]
        public string[] ProjectNamePatterns { get; set; }

        [Required]
        public string[] ItemPatterns { get; set; }

        [Output]
        public string[] DestinationFiles { get; set; }

        public bool ForceOverwrite { get; set; }

        public override bool Execute()
        {
            if (Files == null) Files = new string[0];
            if (DestinationFiles == null) DestinationFiles = new string[0];
            if (ProjectNamePatterns == null) ProjectNamePatterns = new string[0];
            if (ItemPatterns == null) ItemPatterns = new string[0];

            if (DestinationFiles.Length == 0)
            {
                foreach (var file in Files)
                    RemoveProjectFromSolution(file, file);
                DestinationFiles = (string[])Files.Clone();
            }
            else if (Files.Length == DestinationFiles.Length)
            {
                for (var i = 0; i < Files.Length; i++)
                {
                    var file = Files[i];
                    var destFile = DestinationFiles[i];
                    RemoveProjectFromSolution(file, destFile);
                }
            }
            else
            {
                Log.LogError("DestinationFiles count must equals Files count or be empty.");
            }
            return true;
        }

        private void RemoveProjectFromSolution(string slnFile, string destFile)
        {
            if (slnFile == null) throw new ArgumentNullException("slnFile");
            if (destFile == null) throw new ArgumentNullException("destFile");
            if (!File.Exists(slnFile)) throw new ArgumentException(
                string.Format("Can not found file \"{0}\".", slnFile));
            if (!ForceOverwrite && File.Exists(destFile))
                throw new ArgumentException(string.Format
                    ("Destination file \"{0}\" already exists, "
                    + "You can use ForceOverwrite to force overwrite file.", destFile));

            var sln = new SolutionInfo(slnFile);
            foreach (var e in ProjectNamePatterns)
            {
                var pattern = e;
                sln.RemoveProjects(p => new Regex(pattern).IsMatch(p.ProjectName));
            }
            foreach (var e in ItemPatterns)
            {
                var pattern = e;
                sln.RemoveSolutionItems(m => new Regex(pattern).IsMatch(m));
            }
            File.WriteAllText(destFile, sln.ToString(), new UTF8Encoding(true, true));
        }
    }
}

namespace Zealib.Build
{
    internal class ProjectInfo
    {
        private const string REGEX_PATTERN =
          "^Project\\(" +
          "\"(?<ProjectType>[^\"]+)\"" + @"\)" + @"\s*=\s*" +
          "\"(?<ProjectName>[^\"]+)\"" + @"\s*,\s*" +
          "\"(?<ProjectPath>[^\"]+)\"" + @"\s*,\s*" +
          "\"(?<ProjectGuid>[^\"]+)\"\\s*$";
        private readonly static Regex s_Regex = new Regex(REGEX_PATTERN);

        public ProjectInfo(string info)
        {
            var match = s_Regex.Match(info);

            if (!match.Success)
            {
                throw new ArgumentException(
                    string.Format("Invalid project info \"{0}\".", info), "info");
            }
            ProjectType = match.Result("${ProjectType}");
            ProjectName = match.Result("${ProjectName}");
            ProjectPath = match.Result("${ProjectPath}");
            ProjectGuid = match.Result("${ProjectGuid}");
        }

        public string ProjectType { get; private set; }
        public string ProjectName { get; private set; }
        public string ProjectPath { get; private set; }
        public string ProjectGuid { get; private set; }
        public List<string> SolutionItems { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"\r\n",
              ProjectType, ProjectName, ProjectPath, ProjectGuid);
            if (SolutionItems != null && SolutionItems.Count > 0)
            {
                sb.AppendLine("\tProjectSection(SolutionItems) = preProject");
                foreach (var item in SolutionItems)
                {
                    sb.AppendLine("\t\t" + item);
                }
                sb.AppendLine("\tEndProjectSection");
            }
            sb.Append("EndProject");
            return sb.ToString();
        }

    }

    internal class SolutionInfo
    {
        private readonly List<object> m_Items = new List<object>();

        public SolutionInfo(string file)
        {
            var lines = File.ReadAllLines(file);

            ProjectInfo info = null;
            List<string> solutionItems = null;
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("Project("))
                {
                    info = new ProjectInfo(line);
                }
                else if (line.Trim() == "EndProject")
                {
                    m_Items.Add(info);
                    info = null;
                }
                else if (info != null)
                {
                    if (line.Trim().StartsWith("ProjectSection("))
                    {
                        solutionItems = new List<string>();
                    }
                    else if (line.Trim() == "EndProjectSection")
                    {
                        info.SolutionItems = solutionItems;
                        solutionItems = null;
                    }
                    else if (solutionItems != null)
                        solutionItems.Add(line.Trim());
                }
                else
                {
                    m_Items.Add(line);
                }
            }
        }

        public void RemoveProjects(Func<ProjectInfo, bool> condition)
        {
            var removeList = new List<object>();
            foreach (var item in m_Items)
            {
                var info = item as ProjectInfo;
                if (info == null) continue;
                if (condition(info))
                {
                    removeList.Add(info);
                    removeList.AddRange(m_Items.OfType<string>()
                        .Where(line => line.Contains(info.ProjectGuid)));
                }
            }

            foreach (var info in removeList) m_Items.Remove(info);
        }

        public void RemoveSolutionItems(Func<string, bool> condition)
        {
            foreach (var item in m_Items)
            {
                var info = item as ProjectInfo;
                if (info == null || info.SolutionItems == null) continue;
                var removeList = info.SolutionItems.Where(condition).ToList();

                foreach (var si in removeList) info.SolutionItems.Remove(si);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var o in m_Items)
            {
                sb.AppendLine(o.ToString());
            }
            return sb.ToString();
        }
    }
}