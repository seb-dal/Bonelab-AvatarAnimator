using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using System.Runtime.CompilerServices;
using System.Diagnostics;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
// For code line in stacktrace
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]

[assembly: AssemblyTitle(AvatarAnimator.BuildInfo.Name)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(AvatarAnimator.BuildInfo.Company)]
[assembly: AssemblyProduct(AvatarAnimator.BuildInfo.Name)]
[assembly: AssemblyCopyright("Created by " + AvatarAnimator.BuildInfo.Author)]
[assembly: AssemblyTrademark(AvatarAnimator.BuildInfo.Company)]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

[assembly: AssemblyVersion(AvatarAnimator.BuildInfo.Version)]
[assembly: AssemblyFileVersion(AvatarAnimator.BuildInfo.Version)]
[assembly: NeutralResourcesLanguage("en")]

[assembly: MelonColor(AssemblyVar.ColorA, AssemblyVar.ColorR, AssemblyVar.ColorG, AssemblyVar.ColorB)]
[assembly: MelonInfo(typeof(AvatarAnimator.Core), AvatarAnimator.BuildInfo.Name, AvatarAnimator.BuildInfo.Version, AvatarAnimator.BuildInfo.Author, AvatarAnimator.BuildInfo.DownloadLink)]
[assembly: MelonOptionalDependencies(new string[] { "LabFusion" })]

public static class AssemblyVar
{
    public const int ColorA = (int)((AvatarAnimator.BuildInfo.ConsoleColorARGB & 0xff000000) >> 24);
    public const int ColorR = (int)((AvatarAnimator.BuildInfo.ConsoleColorARGB & 0x00ff0000) >> 16);
    public const int ColorG = (int)((AvatarAnimator.BuildInfo.ConsoleColorARGB & 0x0000ff00) >> 8);
    public const int ColorB = (int)((AvatarAnimator.BuildInfo.ConsoleColorARGB & 0x000000ff) >> 0);
}