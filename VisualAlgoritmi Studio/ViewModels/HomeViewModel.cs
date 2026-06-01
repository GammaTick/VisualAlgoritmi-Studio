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
using VisualAlgoritmi_Studio.Models;
using VisualAlgoritmi_Studio.ProjectCore;
using VisualAlgoritmi_Studio.Views.Dialogs;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class HomeViewModel : ViewModelBase
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

            _pathCheckTimer = new Timer(_ => CheckProjectPathsExist(),
                null,
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(2));
        }

        private void LoadRecentProjects()
        {
            RecentProjects.Clear();

            if (_settings.RecentProjectPaths == null || _settings.RecentProjectPaths.Count == 0)
            {
                return;
            }

            var cards = _settings.RecentProjectPaths
                .Where(Directory.Exists)
                .Select(TryCreateRecentProjectCard)
                .OfType<RecentProjectCard>()
                .OrderByDescending(card => card.LastOpened);

            foreach (var card in cards)
            {
                RecentProjects.Add(card);
            }
        }

        private RecentProjectCard? TryCreateRecentProjectCard(string projectPath)
        {
            try
            {
                string configPath = Path.Combine(projectPath, ProjectIO.ProjectConfigFileName);

                if (!File.Exists(configPath))
                {
                    return null;
                }

                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ProjectIO.ProjectConfig>(json);

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
        }

        private async Task ConfirmAndRemoveProjectFromList(string path)
        {
            var result = await MessageBox.ShowAsync(
                "Премахване на проект",
                "Наистина ли искате да премахнете този проект от списъка?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.None);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            RemoveProjectFromList(path);
        }

        private void CheckProjectPathsExist()
        {
            var missing = RecentProjects
                .Where(card => !Directory.Exists(card.ProjectPath))
                .ToList();

            if (missing.Count == 0)
            {
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                foreach (var card in missing)
                {
                    RemoveProjectFromList(card.ProjectPath);
                }
            });
        }

        private void RemoveProjectFromList(string pathToRemove)
        {
            var card = RecentProjects.FirstOrDefault(c => c.ProjectPath == pathToRemove);

            if (card != null) 
            {
                RecentProjects.Remove(card);
            }

            _settings.RecentProjectPaths.RemoveAll(p => string.Equals(NormalizePath(p), NormalizePath(pathToRemove),
                StringComparison.OrdinalIgnoreCase));

            _settings.Save();
        }

        private static string NormalizePath(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private async Task LoadProjectFromDirectory(string? projectRootPath)
        {
            if (string.IsNullOrWhiteSpace(projectRootPath))
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    "Неуспешно зареждане на проект: пътят към проекта е празен.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return;
            }

            string configPath = Path.Combine(projectRootPath, ProjectIO.ProjectConfigFileName);

            await LoadProjectFromConfig(configPath);
        }

        public async Task LoadProjectFromConfig(string projectConfigFilePath)
        {
            if (string.IsNullOrWhiteSpace(projectConfigFilePath))
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    "Неуспешно зареждане на проект: пътят към конфигурационния файл е празен.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return;
            }

            if (!File.Exists(projectConfigFilePath))
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно зареждане на проект: конфигурационният файл не съществува.{Environment.NewLine}{Environment.NewLine}{projectConfigFilePath}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                Project projectManager = await ProjectIO.LoadProjectFromConfigAsync(projectConfigFilePath);

                _main.CurrentViewModel = new VisualizationViewModel(
                    _main,
                    projectManager,
                    projectManager.VisualizedDataStructure);

                string projectRootPath = projectManager.RootPath;

                _settings.AddRecentProject(projectRootPath);
                _settings.Save();
            }
            catch (Exception ex)
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно зареждане на проект: {ex.Message}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);
            }
        }

        public async Task LoadAnimationFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    "Неуспешно отваряне на анимация: пътят към файла е празен.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return;
            }

            if (!File.Exists(filePath))
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно отваряне на анимация: файлът не съществува.{Environment.NewLine}{Environment.NewLine}{filePath}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                AnimationPlaybackViewModel? vm = await AnimationPlaybackViewModel.CreateAsync(_main, filePath);

                if (vm is null)
                {
                    return;
                }

                _main.CurrentViewModel = vm;
            }
            catch (Exception ex)
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно отваряне на анимация: {ex.Message}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);
            }
        }
    }
}