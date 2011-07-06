using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Zealib.Build.Tasks
{
    public class RegisterAssemblyResolvePath : Task
    {
        private static readonly string BinPathCollectionKey;

        static RegisterAssemblyResolvePath()
        {
            BinPathCollectionKey = string.Format("{0}.BinPathSet", typeof(RegisterAssemblyResolvePath).FullName);
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private static Stack<string> BinPathCollection
        {
            get
            {
                var domain = AppDomain.CurrentDomain;
                var list = domain.GetData(BinPathCollectionKey) as Stack<string>;
                if (list == null)
                {
                    list = new Stack<string>();
                    domain.SetData(BinPathCollectionKey, list);
                }

                return list;
            }
        }

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

            BinPathCollection.Push(BinPath);
            return true;
        }

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs e)
        {
            return (from binPath in BinPathCollection
                    from file in Directory.EnumerateFiles(binPath, "*.dll")
                    let name = AssemblyName.GetAssemblyName(file)
                    where name.FullName == e.Name
                    select AppDomain.CurrentDomain.Load(name)).FirstOrDefault();
        }
    }
}
