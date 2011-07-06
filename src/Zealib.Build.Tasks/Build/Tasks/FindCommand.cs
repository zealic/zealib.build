using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zealib.Build.Tasks
{
    public class FindCommand : Task
    {
        [Required]
        public string Name { get; set; }

        public string[] Paths { get; set; }

        public string[] PathExtensions { get; set; }

        public bool NoResultFailure { get; set; }

        [Output]
        public bool HasResult { get; set; }

        [Output]
        public ITaskItem FirstResult { get; set; }

        [Output]
        public ITaskItem LastResult { get; set; }

        [Output]
        public ITaskItem[] Results { get; set; }

        public override bool Execute()
        {
            if (Paths == null)
            {
                var pathVar = Environment.GetEnvironmentVariable("PATH");
                Paths = string.IsNullOrEmpty(pathVar)
                    ? new string[0]
                    : pathVar.Split(';');
            }
            if (PathExtensions == null)
            {
                var pathExtVar = Environment.GetEnvironmentVariable("PATHEXT");
                PathExtensions = string.IsNullOrEmpty(pathExtVar)
                    ? new string[0]
                    : pathExtVar.Split(';');
            }

            var result = new LinkedList<ITaskItem>();
            foreach (var currentPath in Paths)
                foreach (var currentExt in PathExtensions)
                {
                    if (!currentExt.StartsWith(".")) continue;
                    var file = Path.Combine(currentPath, Name + currentExt);
                    if (File.Exists(file)) result.AddLast(new TaskItem(file));
                }

            if (result.Count > 0)
            {
                HasResult = true;
                FirstResult = result.First.Value;
                LastResult = result.Last.Value;
                Results = result.ToArray();
            }
            else if (NoResultFailure)
            {
                Log.LogError("Can not found command \"{0}\".", Name);
                return false;
            }

            return true;
        }
    }
}