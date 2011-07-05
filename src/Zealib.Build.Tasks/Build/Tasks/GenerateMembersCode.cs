using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace Zealib.Build.Tasks
{
    public class GenerateMembersCode : Task
    {
        private enum ClassMemberTypes
        {
            Constant = 0,
            Field,
            Property
        }

        private static readonly string[] AllowClassModifiers = 
        {
            "public", "internal", "protected", "private",
            "partial", "static", "sealed", "abstract"
        };

        [Required]
        public ITaskItem[] CodeFiles { get; set; }

        public bool ForceOverwrite { get; set; }

        public override bool Execute()
        {
            return CodeFiles.All(GenerateCodeFile);
        }

        private bool GetConditionalConstants(ITaskItem item, out string[] constants)
        {
            constants = new string[0];
            var constantsStr = item.GetMetadata("Code-ConditionalConstants");
            if (!string.IsNullOrEmpty(constantsStr))
                constants = constantsStr.Split(';');
            var invalidConstants = constants.Where(m => !Regex.IsMatch(m, @"[a-zA-Z][a-zA-Z0-9_]*"));
            if (invalidConstants.Count() > 0)
            {
                foreach (var constant in invalidConstants)
                {
                    Log.LogError("Invalid conditional constant name \"{0}\".", constant);
                }
                return false;
            }

            return true;
        }

        private bool GetClassName(ITaskItem item, out string className)
        {
            className = null;
            var classNameStr = item.GetMetadata("Code-ClassName");
            if (string.IsNullOrEmpty(classNameStr))
            {
                Log.LogError("Code-ClassName metadata is required.", className);
                return false;
            }
            if (!Regex.IsMatch(classNameStr, @"[a-zA-Z][a-zA-Z0-9_]*"))
            {
                Log.LogError("Invalid class name \"{0}\".", className);
                return false;
            }

            className = classNameStr;
            return true;
        }

        private bool GetClassModifiers(ITaskItem item, out string[] modifiers)
        {
            modifiers = null;
            var classModifiers = new HashSet<string>();
            var classModifiersStr = item.GetMetadata("Code-ClassModifiers");
            if (!string.IsNullOrEmpty(classModifiersStr))
                foreach (var modifier in classModifiersStr.Split(';'))
                {
                    classModifiers.Add(modifier);
                }

            var invalidModifiers = classModifiers.Where(m => !AllowClassModifiers.Contains(m));
            if (invalidModifiers.Count() > 0)
            {
                foreach (var modifier in invalidModifiers)
                {
                    Log.LogError("\"{0}\" is invalid class modifier.", modifier);
                }
                return false;
            }

            modifiers = classModifiers.ToArray();
            return true;
        }

        private bool GetClassMemberType(ITaskItem item, out ClassMemberTypes type)
        {
            var memberTypeStr = item.GetMetadata("Code-ClassMemberType");
            if (string.IsNullOrEmpty(memberTypeStr))
                memberTypeStr = ClassMemberTypes.Constant.ToString();
            if (!Enum.TryParse(memberTypeStr, true, out type))
            {
                Log.LogError("Invalid Code-ClassMemberType \"{0}\", value must be in \"{1}\".", memberTypeStr, string.Join(", ", Enum.GetNames(typeof(ClassMemberTypes))));
                return false;
            }
            return true;
        }

        private bool GenerateCodeFile(ITaskItem item)
        {
            var codeFile = item.ItemSpec;
            string[] conditionalConstants;
            string className;
            string[] classModifiers;
            ClassMemberTypes classMemberType;
            if (GetConditionalConstants(item, out conditionalConstants) &&
                GetClassName(item, out className) &&
                GetClassModifiers(item, out classModifiers) &&
                GetClassMemberType(item, out classMemberType))
            {
                if (File.Exists(codeFile) && !ForceOverwrite)
                {
                    Log.LogError("Code file \"{0}\" already exist.", codeFile);
                    return false;
                }
                using (var writer = File.CreateText(codeFile))
                {
                    var dict = item.CloneCustomMetadata();
                    if (conditionalConstants.Length > 0)
                        writer.WriteLine(string.Format("#if {0}", string.Join(" && ", conditionalConstants)));
                    if (classModifiers.Length > 0)
                        writer.Write(string.Format("{0} ", string.Join(" ", classModifiers)));
                    writer.WriteLine(string.Format("class {0}", className));
                    writer.WriteLine("{");
                    foreach (DictionaryEntry e in dict)
                    {
                        var name = e.Key.ToString();
                        if (name.Contains("-")) continue;
                        WriteMember(writer, classMemberType, name, e.Value.ToString());
                    }
                    writer.WriteLine("}");
                    if (conditionalConstants.Length > 0)
                        writer.WriteLine("#endif");
                }

                return true;
            }

            return false;
        }

        private static void WriteMember(TextWriter writer, ClassMemberTypes classMemberType, string name, string value)
        {
            switch (classMemberType)
            {
                case ClassMemberTypes.Constant:
                    writer.WriteLine("    public const string {0} = \"{1}\";", name, value);
                    break;
                case ClassMemberTypes.Property:
                    writer.WriteLine("    public string {0} {{ get {{ return \"{1}\"; }}", name, value);
                    break;
                case ClassMemberTypes.Field:
                    writer.WriteLine("    public readonly string {0} = \"{1}\";", name, value);
                    break;
                default:
                    throw new NotSupportedException(string.Format("Not supported ClassMemberType \"{0}\".", classMemberType));
            }
        }

    }
}
