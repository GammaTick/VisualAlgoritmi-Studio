using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ProjectCore
{
    public class ProjectManager
    {
        public const string UserCodeFileName = "UserCode.cs";
        public const string ProjectConfigFileName = "project_config.vasproj";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly VisualizedDataStructure _visualizedDataStructure;
        private readonly string _projectName;
        private readonly string _projectRootPath;
        private bool _createdProject;

        public string ProjectName => _projectName;
        public string ProjectRootPath => _projectRootPath;
        public VisualizedDataStructure VisualizedDataStructure => _visualizedDataStructure;

        public ProjectManager(string projectName, string parentDirectory, VisualizedDataStructure visualizedDataStructure)
        {
            _projectName = projectName;
            _projectRootPath = Path.Combine(parentDirectory, projectName);
            _visualizedDataStructure = visualizedDataStructure;
        }

        public void CreateProject(bool loadExampleDataStructureCode)
        {
            if (_createdProject)
            {
                return;
            }

            if (Directory.Exists(_projectRootPath))
            {
                ThrowHelper.ThrowProjectAlreadyExists();
            }

            Directory.CreateDirectory(_projectRootPath);

            CreateProjectConfigFile();
            CreateUserCodeFile(loadExampleDataStructureCode);

            _createdProject = true;
        }

        private void CreateProjectConfigFile()
        {
            string configPath = Path.Combine(_projectRootPath, ProjectConfigFileName);

            ProjectConfig config = new()
            {
                ProjectName = _projectName,
                UserCodeFile = UserCodeFileName,
                LastTimeOpened = DateTime.Now,
                ProjectType = (int)_visualizedDataStructure
            };

            string json = JsonSerializer.Serialize(config, JsonOptions);

            File.WriteAllText(configPath, json);
        }

        public string GetExampleCode()
        {
            string prefix = _visualizedDataStructure.ToString();
            string possibleFile = prefix + "ExampleUsageTemplate.txt";

            string possiblePath = Path.Combine(
                AppContext.BaseDirectory,
                "ProjectCore",
                possibleFile);

            string templateFileName = File.Exists(possiblePath)
                ? possibleFile
                : "ConsoleAppTemplate.txt";

            string templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "ProjectCore",
                templateFileName);

            return File.ReadAllText(templatePath);
        }

        private void CreateUserCodeFile(bool loadExampleDataStructureCode)
        {
            string templateFileName;

            if (loadExampleDataStructureCode)
            {
                string prefix = _visualizedDataStructure.ToString();
                string possibleFile = prefix + "ExampleUsageTemplate.txt";

                string possiblePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "ProjectCore",
                    possibleFile);

                Trace.WriteLine(possiblePath);

                if (File.Exists(possiblePath))
                {
                    templateFileName = possibleFile;
                }
                else
                {
                    templateFileName = "ConsoleAppTemplate.txt";
                }
            }
            else
            {
                templateFileName = "ConsoleAppTemplate.txt";
            }

            string templatePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "ProjectCore",
                    templateFileName);

            string codeFilePath = Path.Combine(_projectRootPath, UserCodeFileName);

            string templateCode = File.ReadAllText(templatePath);

            File.WriteAllText(codeFilePath, templateCode);
        }

        public static async Task<ProjectManager> LoadProjectFromConfig(string projectConfigFilePath)
        {
            if (!File.Exists(projectConfigFilePath))
            {
                ThrowHelper.ThrowProjectConfigMissing(projectConfigFilePath);
            }

            string projectConfig = await File.ReadAllTextAsync(projectConfigFilePath);

            ProjectConfig? config = JsonSerializer.Deserialize<ProjectConfig>(projectConfig, JsonOptions);

            if (config == null || string.IsNullOrWhiteSpace(config.UserCodeFile))
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
            string projectName = Path.GetFileName(projectRootPath);
            string parentDirectoryPath = Directory.GetParent(projectRootPath)!.FullName;

            ProjectManager projectManager = new(projectName, parentDirectoryPath, dataStructure)
            {
                _createdProject = true
            };

            config.LastTimeOpened = DateTime.Now;

            string updatedJson = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(projectConfigFilePath, updatedJson);

            return projectManager;
        }

        public string GetUserCode()
        {
            string entryFilePath = Path.Combine(_projectRootPath, UserCodeFileName);

            if (!File.Exists(entryFilePath))
            {
                ThrowHelper.ThrowEntryFileMissing(UserCodeFileName);
            }

            return File.ReadAllText(entryFilePath);
        }

        public void SaveUserCode(string code)
        {
            string entryFilePath = Path.Combine(_projectRootPath, UserCodeFileName);

            if (!File.Exists(entryFilePath))
            {
                ThrowHelper.ThrowEntryFileMissing(UserCodeFileName);
            }

            File.WriteAllText(entryFilePath, code);
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
            public static void ThrowProjectFolderNotFound(string folderPath)
            {
                throw new DirectoryNotFoundException($"Project folder does not exist: '{folderPath}'.");
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
