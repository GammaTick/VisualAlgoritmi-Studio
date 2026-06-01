using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Config;
using VisualAlgoritmi_Studio.ProjectCore;
using VisualAlgoritmi_Studio.Views.Dialogs;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class NewProjectViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel _main;
        private readonly Settings _settings;

        private DataStructureCard? _selectedCard;
        private readonly DataStructureCard[] _allCards;
        private string _projectName = string.Empty;
        private string _projectParentDirectory = string.Empty;
        private string _searchText = string.Empty;
        private string _projectNameError = string.Empty;
        private string _projectLocationError = string.Empty;
        private string _dataStructureError = string.Empty;
        private bool _loadExampleCode;

        public ICommand BackToHomeCommand { get; }
        public ICommand CreateProjectCommand { get; }
        public ICommand SelectCardCommand { get; }

        public ObservableCollection<DataStructureCard> DataStructureCards { get; }

        public DataStructureCard? SelectedCard
        {
            get => _selectedCard;
            set
            {
                if (_selectedCard != value)
                {
                    if (_selectedCard != null)
                    {
                        _selectedCard.IsSelected = false;
                    }
                    
                    SetProperty(ref _selectedCard, value);
                    
                    if (_selectedCard != null)
                    {
                        _selectedCard.IsSelected = true;
                    }

                    ValidateDataStructure();
                }
            }
        }

        public string ProjectName
        {
            get => _projectName;
            set
            {
                if (SetProperty(ref _projectName, value))
                {
                    OnPropertyChanged(nameof(ProjectFullPath));
                    ValidateProjectName();
                }
            }
        }

        public string ProjectParentDirectory
        {
            get => _projectParentDirectory;
            set
            {
                if (SetProperty(ref _projectParentDirectory, value))
                {
                    OnPropertyChanged(nameof(ProjectFullPath));
                    ValidateProjectParentDirectory();
                    ValidateProjectName();
                }
            }
        }

        public string ProjectFullPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectParentDirectory) || string.IsNullOrWhiteSpace(ProjectName))
                {
                    return string.Empty;
                }

                return $"Проектът ще бъде създаден в: {System.IO.Path.Combine(ProjectParentDirectory, ProjectName)}";
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    ApplySearch();
                }
            }
        }

        public bool LoadExampleCode
        {
            get => _loadExampleCode;
            set => SetProperty(ref _loadExampleCode, value);
        }

        public string ProjectNameError
        {
            get => _projectNameError;
            set => SetProperty(ref _projectNameError, value);
        }

        public string ProjectLocationError
        {
            get => _projectLocationError;
            set => SetProperty(ref _projectLocationError, value);
        }

        public string DataStructureError
        {
            get => _dataStructureError;
            set => SetProperty(ref _dataStructureError, value);
        }

        public NewProjectViewModel(MainWindowViewModel main, Settings settings)
        {
            _main = main;
            _settings = settings;

            _allCards =
            [
                new DataStructureCard(
                    "ArrayList<T>",
                    "Динамичен масив с индексиран достъп и автоматично преоразмеряване, оптимизиран за чести добавяния и премахвания в края.",
                    VisualizedDataStructure.ArrayList
                ),

                new DataStructureCard(
                    "List<T>",
                    "Динамичен масив с индексиран достъп и автоматично преоразмеряване.",
                    VisualizedDataStructure.List
                ),

                new DataStructureCard(
                    "LinkedList<T>",
                    "Колекция от възли, всеки съдържащ стойност и препратка към следващия.",
                    VisualizedDataStructure.LinkedList
                ),

                new DataStructureCard(
                    "Queue<T>",
                    "Колекция от тип FIFO (ред), при която елементите се добавят отзад и се вземат отпред.",
                    VisualizedDataStructure.Queue
                ),

                new DataStructureCard(
                    "Stack<T>",
                    "Колекция от тип LIFO (стек), при която елементите се добавят и вземат от върха.",
                    VisualizedDataStructure.Stack
                )
            ];

            DataStructureCards = [];

            BackToHomeCommand = new RelayCommand(() =>
                _main.CurrentViewModel = new HomeViewModel(_main)
            );

            CreateProjectCommand = new AsyncRelayCommand(CreateProject);
            SelectCardCommand = new RelayCommand<DataStructureCard>(card => SelectedCard = card);

            ProjectParentDirectory = settings.DefaultProjectCreationPath;
            ProjectName = GetDefaultProjectName();

            ApplySearch();
        }

        private string GetDefaultProjectName()
        {
            string baseName = "VisualizationProject";

            if (!Directory.Exists(ProjectParentDirectory))
            {
                return baseName + "1";
            }

            HashSet<int> used = [];

            foreach (string dir in Directory.EnumerateDirectories(ProjectParentDirectory))
            {
                string name = Path.GetFileName(dir);

                if (!name.StartsWith(baseName))
                {
                    continue;
                }

                string suffix = name.Substring(baseName.Length);

                if (int.TryParse(suffix, out int n))
                {
                    if (n > 0)
                    {
                        used.Add(n);
                    }
                }
            }

            int candidate = 1;

            while (used.Contains(candidate))
            {
                candidate++;
            }

            return baseName + candidate;
        }

        private void ValidateProjectName()
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                ProjectNameError = "Името на проекта не трябва да е празно.";
                return;
            }

            if (ProjectFolderAlreadyExists())
            {
                ProjectNameError = "Проект с това име вече съществува.";
                return;
            }

            ProjectNameError = string.Empty;
        }

        private void ValidateProjectParentDirectory()
        {
            if (string.IsNullOrWhiteSpace(ProjectParentDirectory))
            {
                ProjectLocationError = "Местоположението на проекта не трябва да е празно.";
                return;
            }

            try
            {
                Path.GetFullPath(ProjectParentDirectory);
                ProjectLocationError = string.Empty;
            }
            catch
            {
                ProjectLocationError = "Невалиден път на местоположението.";
            }
        }

        private void ValidateDataStructure()
        {
            if (SelectedCard == null)
            {
                DataStructureError = "Моля, изберете структура от данни.";
            }
            else
            {
                DataStructureError = string.Empty;
            }
        }

        private async Task CreateProject()
        {
            if (!CanCreateProject())
            {
                return;
            }

            VisualizedDataStructure visualizedDataStructure = SelectedCard!.VisualizedDataStructure;

            if (!VisualizedDataStructureSupport.IsSupported(visualizedDataStructure))
            {
                await MessageBox.ShowAsync(
                    "Неподдържана структура",
                    $"Структурата '{visualizedDataStructure}' все още не се поддържа.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Warning);

                return;
            }

            Project? createdProject = null;

            try
            {
                createdProject = await ProjectIO.CreateNewProjectAsync(
                    ProjectName,
                    ProjectParentDirectory,
                    visualizedDataStructure,
                    LoadExampleCode);

                VisualizationViewModel visualizationViewModel = new(
                    _main,
                    createdProject,
                    visualizedDataStructure);

                string projectRootPath = createdProject.RootPath;

                if (!_settings.RecentProjectPaths.Contains(projectRootPath))
                {
                    _settings.RecentProjectPaths.Add(projectRootPath);
                    SettingsIO.Save(_settings);
                }

                _main.CurrentViewModel = visualizationViewModel;
            }
            catch (Exception ex)
            {
                if (createdProject is not null)
                {
                    TryDeleteCreatedProject(createdProject.RootPath);
                }

                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно създаване на проект: {ex.Message}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);
            }
        }

        private static void TryDeleteCreatedProject(string projectRootPath)
        {
            try
            {
                if (Directory.Exists(projectRootPath))
                {
                    Directory.Delete(projectRootPath, recursive: true);
                }
            }
            catch
            {
                // Do not throw here.
                // The original creation error is more important than cleanup failure.
            }
        }

        private bool CanCreateProject()
        {
            ValidateProjectName();
            ValidateProjectParentDirectory();
            ValidateDataStructure();

            return string.IsNullOrEmpty(ProjectNameError)
                && string.IsNullOrEmpty(ProjectLocationError)
                && string.IsNullOrEmpty(DataStructureError);
        }

        private bool ProjectFolderAlreadyExists()
        {
            foreach (string dir in Directory.EnumerateDirectories(ProjectParentDirectory))
            {
                string name = Path.GetFileName(dir);

                if (name.Equals(ProjectName))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplySearch()
        {
            DataStructureCards.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var card in _allCards)
                {
                    DataStructureCards.Add(card);
                }

                return;
            }

            string query = SearchText.Trim().ToLowerInvariant();

            foreach (var card in _allCards)
            {
                if (card.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase))
                {
                    DataStructureCards.Add(card);
                }
            }
        }

        public class DataStructureCard : ViewModelBase
        {
            private bool _isSelected;

            public string Title { get; }
            public string Description { get; }
            public VisualizedDataStructure VisualizedDataStructure { get; }

            public bool IsSelected
            {
                get => _isSelected;
                set => SetProperty(ref _isSelected, value);
            }

            public DataStructureCard(string title, string description, VisualizedDataStructure visualizedDataStructure)
            {
                Title = title;
                Description = description;
                VisualizedDataStructure = visualizedDataStructure;
            }
        }
    }
}