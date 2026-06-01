using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ProjectCore
{
    internal static class ProjectIO
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public const string UserCodeFileName = "UserCode.cs";
        public const string ProjectConfigFileName = "project_config.vasproj";

        public static async Task<Project> CreateNewProjectAsync(
            string projectName,
            string projectPath,
            VisualizedDataStructure visualizedDataStructure,
            bool loadExampleCode = false)
        {
            string projectRootPath = Path.Combine(projectPath, projectName);
            string configPath = Path.Combine(projectRootPath, ProjectConfigFileName);
            string codeFilePath = Path.Combine(projectRootPath, UserCodeFileName);

            if (Directory.Exists(projectRootPath))
            {
                ThrowHelper.ThrowProjectAlreadyExists();
            }

            Directory.CreateDirectory(projectRootPath);

            await CreateProjectConfigFileAsync(configPath, projectName, visualizedDataStructure);

            await CreateUserCodeFileAsync(codeFilePath, visualizedDataStructure, loadExampleCode);

            return new Project(projectName, projectRootPath, visualizedDataStructure);
        }

        private static async Task CreateProjectConfigFileAsync(
            string configPath,
            string projectName,
            VisualizedDataStructure visualizedDataStructure)
        {
            ProjectConfig config = new()
            {
                ProjectName = projectName,
                UserCodeFile = UserCodeFileName,
                LastTimeOpened = DateTime.Now,
                ProjectType = (int)visualizedDataStructure
            };

            string json = JsonSerializer.Serialize(config, JsonOptions);

            await File.WriteAllTextAsync(configPath, json);
        }

        private static async Task CreateUserCodeFileAsync(
            string codeFilePath,
            VisualizedDataStructure visualizedDataStructure,
            bool loadExampleCode)
        {
            string templateFileName = GetTemplateFileName(visualizedDataStructure, loadExampleCode);

            string templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "ProjectCore",
                "Templates",
                templateFileName);

            string templateCode = await File.ReadAllTextAsync(templatePath);

            await File.WriteAllTextAsync(codeFilePath, templateCode);
        }

        private static string GetTemplateFileName(VisualizedDataStructure visualizedDataStructure, bool loadExampleCode)
        {
            if (!loadExampleCode)
            {
                return "ConsoleAppTemplate.txt";
            }

            string possibleFile = visualizedDataStructure + "ExampleUsageTemplate.txt";

            string possiblePath = Path.Combine(
                AppContext.BaseDirectory,
                "ProjectCore",
                "Templates",
                possibleFile);

            return File.Exists(possiblePath) ? possibleFile : "ConsoleAppTemplate.txt";
        }

        public static async Task<Project> LoadProjectFromConfigAsync(string projectConfigFilePath)
        {
            if (!File.Exists(projectConfigFilePath))
            {
                ThrowHelper.ThrowProjectConfigMissing(projectConfigFilePath);
            }

            string projectConfig = await File.ReadAllTextAsync(projectConfigFilePath);

            ProjectConfig? config = JsonSerializer.Deserialize<ProjectConfig>(projectConfig, JsonOptions);

            if (config == null || string.IsNullOrEmpty(config.UserCodeFile))
            {
                ThrowHelper.ThrowInvalidProjectConfig();
            }

            string projectRootPath = Path.GetDirectoryName(projectConfigFilePath)!;
            string userCodePath = Path.Combine(projectRootPath, config.UserCodeFile);

            if (!File.Exists(userCodePath))
            {
                ThrowHelper.ThrowEntryFileMissing(config.UserCodeFile);
            }

            VisualizedDataStructure dataStructure = (VisualizedDataStructure)config.ProjectType;

            Project projectManager = new(config.ProjectName, projectRootPath, dataStructure);

            config.LastTimeOpened = DateTime.Now;

            string updatedJson = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(projectConfigFilePath, updatedJson);

            return projectManager;
        }

        public static async Task<string> GetExampleCodeAsync(VisualizedDataStructure visualizedDataStructure)
        {
            string templateFileName = GetTemplateFileName(
                visualizedDataStructure,
                loadExampleCode: true);

            string templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "ProjectCore",
                "Templates",
                templateFileName);

            return await File.ReadAllTextAsync(templatePath);
        }

        internal sealed class ProjectConfig
        {
            public string ProjectName { get; init; } = string.Empty;

            public string UserCodeFile { get; init; } = string.Empty;

            public DateTime LastTimeOpened { get; set; } = DateTime.Now;

            public int ProjectType { get; set; } = (int)VisualizedDataStructure.List;
        }

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowProjectAlreadyExists()
            {
                throw new IOException("A project with the same name already exists.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowProjectConfigMissing(string path)
            {
                throw new IOException($"Project config file is missing: '{path}'.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowInvalidProjectConfig()
            {
                throw new IOException("Project configuration is invalid.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowEntryFileMissing(string entryFile)
            {
                throw new IOException($"Entry file '{entryFile}' does not exist.");
            }
        }
    }
}
