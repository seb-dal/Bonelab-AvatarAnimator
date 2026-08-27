using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using MelonLoader;
using System.Runtime.CompilerServices;
using System.Diagnostics;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
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

[assembly: MelonInfo(typeof(AvatarAnimator.Core), AvatarAnimator.BuildInfo.Name, AvatarAnimator.BuildInfo.Version, AvatarAnimator.BuildInfo.Author, AvatarAnimator.BuildInfo.DownloadLink)]
[assembly: MelonOptionalDependencies(new string[] { "LabFusion" })]
