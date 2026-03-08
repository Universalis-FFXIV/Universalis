using System;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath SpecsDirectory => RootDirectory / "clients" / "specs";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Test => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoRestore());
        });

    /// <summary>
    /// Exports OpenAPI specs for all three API versions to clients/specs/.
    /// Requires the devenv to be running (Redis, ScyllaDB, PostgreSQL) OR
    /// relies on UNIVERSALIS_SWAGGER_GEN=true skipping infra service setup.
    ///
    /// Run this when the API contracts change and commit the updated specs.
    /// The GitHub Actions publish-clients workflow consumes the committed specs.
    /// </summary>
    Target ExportSpecs => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            SpecsDirectory.CreateOrCleanDirectory();

            // Restore local dotnet tools (swashbuckle.aspnetcore.cli)
            DotNet("tool restore");

            var appDll = ArtifactsDirectory / "Universalis.Application.dll";

            // Set the swagger-gen flag so Startup skips infra service registrations
            // that require live connections (Redis, ScyllaDB, PostgreSQL, Mogboard).
            // DOTNET_ROLL_FORWARD=Major lets the swashbuckle.aspnetcore.cli tool (which
            // targets net7.0) run under .NET 8+ without needing .NET 7 installed.
            Environment.SetEnvironmentVariable("UNIVERSALIS_SWAGGER_GEN", "true");
            Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", "Major");
            try
            {
                foreach (var version in new[] { "v1", "v2", "v3" })
                {
                    var outputPath = SpecsDirectory / $"{version}.json";
                    DotNet($"swagger tofile --output {outputPath} {appDll} {version}");
                    Serilog.Log.Information("Exported spec: {Path}", outputPath);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("UNIVERSALIS_SWAGGER_GEN", null);
                Environment.SetEnvironmentVariable("DOTNET_ROLL_FORWARD", null);
            }
        });
}