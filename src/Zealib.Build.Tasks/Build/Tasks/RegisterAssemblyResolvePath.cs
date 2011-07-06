using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Zealib.Build.Tasks
{
    public class RegisterAssemblyResolvePath : Task
    {
        [Required]
        public string BinPath { get; set; }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(BinPath) ||
                !Directory.Exists(BinPath))
            {
                Log.LogError("BinPath directory \"{0}\" dose not exist.", BinPath);
                return false;
            }
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
            return true;
        }

        private Assembly ResolveAssembly(object sender, ResolveEventArgs e)
        {
            return (from file in Directory.EnumerateFiles(BinPath, "*.dll")
                    let name = AssemblyName.GetAssemblyName(file)
                    where name.FullName == e.Name
                    select Assembly.LoadFrom(file)).FirstOrDefault();
        }
    }
}
