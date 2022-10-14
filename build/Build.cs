using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using System.Linq;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.PushNuget);

    private const string ProjectName = "DarkHtmlViewer";

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    readonly Configuration Configuration = Configuration.Release;

    [Parameter] readonly string NugetApiUrl = "https://api.nuget.org/v3/index.json";
    [Parameter] readonly string NugetApiKey;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution.GetProject(ProjectName)));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution.GetProject(ProjectName))
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s => s
              .SetProject(Solution.GetProject(ProjectName))
              .SetConfiguration(Configuration)
              .EnableNoBuild()
              .EnableNoRestore()
              .SetOutputDirectory(ArtifactsDirectory));
        });

    Target PushNuget => _ => _
       .DependsOn(Pack)
       .Requires(() => NugetApiUrl)
       .Requires(() => NugetApiKey)
       .Executes(() =>
       {
           GlobFiles(ArtifactsDirectory, "*.nupkg")
              .Where(x => !string.IsNullOrEmpty(x) && !x.EndsWith("symbols.nupkg"))
              .ForEach(x =>
              {
                  DotNetNuGetPush(s => s
                      .SetTargetPath(x)
                      .SetSource(NugetApiUrl)
                      .SetApiKey(NugetApiKey)
                  );
              });
       });
}
