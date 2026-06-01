using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VisualAlgoritmi_Studio.ProjectCore;

namespace VisualAlgoritmi_Studio.Config
{
    public class Settings
    {
        public string DefaultProjectCreationPath { get; set; } = GetDefaultProjectCreationPath();
        public List<string> RecentProjectPaths { get; set; } = [];
        public bool StartWindowMaximized { get; set; }
        public double CodeEditorFontSize { get; set; } = 14;
        public bool DisplayExtraLinkedListInfo { get; set; } = true;

        private static string GetDefaultProjectCreationPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "VisualAlgoritmi Studio",
                "projects");
        }

        public void AddRecentProject(string projectRootPath)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                return;
            }

            string normalized = NormalizePath(projectRootPath);

            if (RecentProjectPaths
                .Select(NormalizePath)
                .Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            RecentProjectPaths.Add(normalized);
        }

        private static string NormalizePath(string path)
        {
            return Path
                .GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public void CleanInvalidRecentProjects()
        {
            for (int i = RecentProjectPaths.Count - 1; i >= 0; i--)
            {
                string configPath = Path.Combine(RecentProjectPaths[i], ProjectIO.ProjectConfigFileName);

                if (!File.Exists(configPath))
                {
                    RecentProjectPaths.RemoveAt(i);
                }
            }
        }

        public void Save()
        {
            SettingsIO.Save(this);
        }
    }
}
