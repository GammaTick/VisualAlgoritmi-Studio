using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Config;
using VisualAlgoritmi_Studio.ProjectCore;
using VisualAlgoritmi_Studio.RoslynCore;
using VisualAlgoritmi_Studio.Views.Dialogs;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class HomeViewModel : ViewModelBase, IDisposable
    {
        private readonly MainWindowViewModel _main;
        private readonly Settings _settings;
        private readonly Timer _pathCheckTimer;

        public ICommand NewProjectCommand { get; }
        public ICommand LoadProjectCommand { get; }

        public ObservableCollection<RecentProjectCard> RecentProjects { get; }

        public bool HasRecentProjects => RecentProjects.Count > 0;
        public bool HasNoRecentProjects => RecentProjects.Count == 0;

        public HomeViewModel(MainWindowViewModel main)
        {
            _main = main;
            _settings = App.Settings;
            _settings.CleanInvalidRecentProjects();
            _settings.Save();

            RecentProjects = [];
            RecentProjects.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasRecentProjects));
                OnPropertyChanged(nameof(HasNoRecentProjects));
            };

            NewProjectCommand = new RelayCommand(() =>
                _main.CurrentViewModel = new NewProjectViewModel(_main, _settings)
            );

            LoadProjectCommand = new AsyncRelayCommand<string>(LoadProjectFromDirectory);

            LoadRecentProjects();

            _pathCheckTimer = new Timer(_ => CheckProjectPathsExist(), null,
                TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        private void CheckProjectPathsExist()
        {
            var missing = RecentProjects
                .Where(card => !Directory.Exists(card.ProjectPath))
                .ToList();

            if (missing.Count == 0)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var card in missing)
                    RemoveProjectFromList(card.ProjectPath);
            });
        }

        private void LoadRecentProjects()
        {
            RecentProjects.Clear();

            if (_settings.RecentProjectPaths == null || _settings.RecentProjectPaths.Count == 0)
            {
                return;
            }

            var projectCards = _settings.RecentProjectPaths
                .Where(Directory.Exists)
                .Select(projectPath =>
                {
                    try
                    {
                        string configPath = Path.Combine(projectPath, ProjectManager.ProjectConfigFileName);

                        if (!File.Exists(configPath))
                        {
                            return null;
                        }

                        string json = File.ReadAllText(configPath);
                        var config = JsonSerializer.Deserialize<ProjectManager.ProjectConfig>(json);

                        if (config == null)
                        {
                            return null;
                        }

                        return new RecentProjectCard(
                            config.ProjectName,
                            projectPath,
                            config.LastTimeOpened,
                            (VisualizedDataStructure)config.ProjectType,
                            ConfirmAndRemoveProjectFromList
                        );
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Failed to load project at {projectPath}: {ex.Message}");
                        return null;
                    }
                })
                .Where(card => card != null)
                .Cast<RecentProjectCard>()
                .OrderByDescending(card => card.LastOpened)
                .ToList();

            foreach (var card in projectCards)
            {
                RecentProjects.Add(card);
            }
        }

        private async Task ConfirmAndRemoveProjectFromList(string path)
        {
            var result = await MessageBox.ShowAsync(
                "Премахване на проект",
                "Наистина ли искате да премахнете този проект от списъка?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.None);

            if (result != MessageBoxResult.Yes)
                return;

            RemoveProjectFromList(path);
        }

        private void RemoveProjectFromList(string path)
        {
            var card = RecentProjects.FirstOrDefault(c => c.ProjectPath == path);

            if (card != null) 
            {
                RecentProjects.Remove(card);
            }

            _settings.RecentProjectPaths.RemoveAll(p =>
                string.Equals(
                    p.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                    StringComparison.OrdinalIgnoreCase));

            _settings.Save();
        }

        private async Task LoadProjectFromDirectory(string? projectRootPath)
        {
            if (string.IsNullOrEmpty(projectRootPath))
            {
                return;
            }

            string configPath = Path.Combine(projectRootPath, ProjectManager.ProjectConfigFileName);

            await LoadProjectFromConfig(configPath);
        }

        public async Task LoadProjectFromConfig(string projectConfigFilePath)
        {
            if (string.IsNullOrEmpty(projectConfigFilePath) || !File.Exists(projectConfigFilePath))
            {
                return;
            }

            try
            {
                ProjectManager projectManager = await ProjectManager.LoadProjectFromConfig(projectConfigFilePath);

                _main.CurrentViewModel = new VisualizationViewModel(_main, projectManager, projectManager.VisualizedDataStructure, _settings);

                string projectRootPath = projectManager.ProjectRootPath;

                _settings.AddRecentProject(projectRootPath);
                _settings.Save();
            }
            catch (Exception ex)
            {
                await MessageBox.ShowAsync("Грешка", $"Неуспешно зареждане на проект: {ex.Message}",
                    MessageBoxButtons.Ok, MessageBoxIcon.Critical);
            }
        }

        public void Dispose()
        {
            _pathCheckTimer.Dispose();
        }

        public async Task LoadAnimationFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                _main.CurrentViewModel = new AnimationPlaybackViewModel(_main, filePath);
            }
            catch (Exception ex)
            {
                await MessageBox.ShowAsync("Грешка", $"Неуспешно отваряне на анимация: {ex.Message}",
                    MessageBoxButtons.Ok, MessageBoxIcon.Critical);
            }
        }

        public class RecentProjectCard
        {
            public string ProjectName { get; }
            public string ProjectPath { get; }
            public DateTime LastOpened { get; }
            public string LastOpenedFormatted { get; }
            public string DataStructureType { get; }
            public ICommand RemoveFromListCommand { get; }
            public ICommand OpenProjectLocationCommand { get; }

            public RecentProjectCard(string projectName, string projectPath, DateTime lastOpened, VisualizedDataStructure visualizedDataStructure, Func<string, Task> removeFromList)
            {
                ProjectName = projectName;
                ProjectPath = projectPath;
                LastOpened = lastOpened;
                LastOpenedFormatted = lastOpened.ToString("dd.MM.yyyy г. HH:mm");
                DataStructureType = visualizedDataStructure.ToString();

                RemoveFromListCommand = new AsyncRelayCommand(() => removeFromList(projectPath));
                OpenProjectLocationCommand = new RelayCommand(() =>
                {
                    if (Directory.Exists(projectPath))
                        Process.Start(new ProcessStartInfo { FileName = projectPath, UseShellExecute = true });
                });
            }
        }
    }
}
