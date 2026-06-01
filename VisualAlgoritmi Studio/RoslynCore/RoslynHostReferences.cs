using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

namespace VisualAlgoritmi_Studio.RoslynCore
{
    internal static class RoslynHostReferences
    {
        private static readonly Lazy<List<MetadataReference>> CachedReferences =
            new(CreateDefaultReferences);

        public static List<MetadataReference> GetDefaultReferences()
        {
            return CachedReferences.Value;
        }

        private static List<MetadataReference> CreateDefaultReferences()
        {
            var references = new List<MetadataReference>();
            var addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddReferenceIfExists(string? path)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || !addedPaths.Add(path))
                {
                    return;
                }

                references.Add(MetadataReference.CreateFromFile(path));
            }

            if (TryGetReferencePackDirectory(out var referencePackDirectory))
            {
                foreach (var path in Directory.EnumerateFiles(referencePackDirectory!, "*.dll"))
                {
                    AddReferenceIfExists(path);
                }
            }
            else if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies &&
                     !string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
            {
                foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
                {
                    AddReferenceIfExists(path);
                }
            }
            else
            {
                var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                if (!string.IsNullOrWhiteSpace(assemblyPath))
                {
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Private.CoreLib.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Runtime.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Collections.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Console.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Linq.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Threading.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "System.Threading.Thread.dll"));
                    AddReferenceIfExists(Path.Combine(assemblyPath, "netstandard.dll"));
                }
            }

            AddReferenceIfExists(typeof(RoslynHost).Assembly.Location);

            AddReferenceIfExists(Path.Combine(
                AppContext.BaseDirectory,
                "VisualAlgoritmi.Runtime.dll"));

            return references;
        }

        private static bool TryGetReferencePackDirectory(out string? referencePackDirectory)
        {
            referencePackDirectory = null;

            var frameworkAttribute = typeof(RoslynHostReferences).Assembly
                .GetCustomAttribute<TargetFrameworkAttribute>();

            if (frameworkAttribute == null ||
                !TryParseTargetFrameworkVersion(frameworkAttribute.FrameworkName, out var targetVersion))
            {
                return false;
            }

            var targetFrameworkMoniker = $"net{targetVersion.Major}.{targetVersion.Minor}";

            foreach (var dotnetRoot in GetDotNetRoots())
            {
                var packsRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");

                if (!Directory.Exists(packsRoot))
                {
                    continue;
                }

                var bestMatchingPack = Directory.EnumerateDirectories(packsRoot)
                    .Select(path => new
                    {
                        Path = path,
                        Version = ParseVersion(Path.GetFileName(path))
                    })
                    .Where(entry => entry.Version != null &&
                                    entry.Version.Major == targetVersion.Major &&
                                    entry.Version.Minor == targetVersion.Minor)
                    .OrderByDescending(entry => entry.Version)
                    .FirstOrDefault();

                if (bestMatchingPack == null)
                {
                    continue;
                }

                var candidate = Path.Combine(bestMatchingPack.Path, "ref", targetFrameworkMoniker);

                if (!Directory.Exists(candidate))
                {
                    continue;
                }

                referencePackDirectory = candidate;
                return true;
            }

            return false;
        }

        private static IEnumerable<string> GetDotNetRoots()
        {
            var roots = new[]
            {
                Environment.GetEnvironmentVariable("DOTNET_ROOT"),
                Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet")
            };

            return roots
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path!)
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryParseTargetFrameworkVersion(string frameworkName, out Version version)
        {
            const string versionPrefix = "Version=v";
            var versionPrefixIndex = frameworkName.IndexOf(versionPrefix, StringComparison.OrdinalIgnoreCase);

            if (versionPrefixIndex >= 0)
            {
                var versionText = frameworkName.Substring(versionPrefixIndex + versionPrefix.Length);

                if (Version.TryParse(versionText, out var parsedVersion))
                {
                    version = parsedVersion;
                    return true;
                }
            }

            version = new Version(0, 0);
            return false;
        }

        private static Version? ParseVersion(string? value)
        {
            return Version.TryParse(value, out var version)
                ? version
                : null;
        }
    }
}
