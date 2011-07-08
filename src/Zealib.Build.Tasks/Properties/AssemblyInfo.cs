using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !UNIT_TEST
[assembly: AssemblyTitle(ThisAssembly.Title)]
#else
[assembly: AssemblyTitle(ThisAssembly.Title + ".Tests")]
#endif
[assembly: AssemblyCompany(ThisAssembly.Company)]
[assembly: AssemblyProduct(ThisAssembly.Product)]
[assembly: AssemblyCopyright(ThisAssembly.Copyright)]
[assembly: AssemblyDescription(ThisAssembly.Description)]
[assembly: AssemblyConfiguration(ThisAssembly.Configuration)]
[assembly: AssemblyVersion(ThisAssembly.Version)]
[assembly: AssemblyFileVersion(ThisAssembly.FileVersion)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
#if !UNIT_TEST
[assembly: InternalsVisibleTo(ThisAssembly.Title + ".Tests")]

partial class ThisAssembly
{
    public const string Company = "Zealic";
    public const string Product = "Zealib.Build";
    public const string Copyright = "Copyright © 2011 Zealic, All right reserved.";
    public const string Description = "";
    public const string Configuration = "";
    public const string Version =
        VersionMajor + "." + VersionMinor + "." +
        VersionBuild + "." + VersionRevision;
    public const string FileVersion = Version;

#if !BUILD_TYPE_PRODUCTION
    public const string VersionMajor = "0";
    public const string VersionMinor = "0";
    public const string VersionBuild = "0";
    public const string VersionRevision = "0";
    public const string VersionIdentity = "0000000000000000000000000000000000000000";
    public const string VersionType = "DEVELOPMENT";
    public const string Timestamp = "";
#endif
}
#endif
