using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.ArrayList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.List;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Queue;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Stack;
using VisualAlgoritmi_Studio.Visualization;
using VisualAlgoritmi_Studio.Views.Dialogs;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class AnimationPlaybackViewModel : ViewModelBase, IDisposable
    {
        public ICommand GoHomeCommand { get; }
        public ICommand AdvanceStepCommand { get; }
        public ICommand ReverseStepCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ScreenshotCommand { get; }

        private readonly MainWindowViewModel _main;
        private readonly VisualizerCanvasBase _visualizerCanvas;
        private readonly VisualizedDataStructure _visualizedDataStructure;
        private bool _disposed;

        public VisualizerCanvasBase VisualizerCanvas => _visualizerCanvas;

        public string SelectedDataStructureName => GetDataStructureDisplayName(_visualizedDataStructure);

        public string CurrentStepValueText =>
            $" {_visualizerCanvas.CurrentStep + 1} / {_visualizerCanvas.StepCount}";

        public string CurrentOperationsText =>
            _visualizerCanvas.GetOperationsAtCurrentStep();

        public string OperationsPrefixText =>
            _visualizerCanvas.GetOperationCountAtCurrentStep() == 1 ? "Операция:" : "Операции:";

        public string CanvasZoomText => $"Мащаб: {_visualizerCanvas.ZoomPercentage:F0}%";

        public string CanvasOffsetText
        {
            get
            {
                var (x, y) = _visualizerCanvas.GetOffsetFromCenter();
                return $"Отместване: {x}, {y}";
            }
        }

        private static string GetDataStructureDisplayName(VisualizedDataStructure dataStructure)
        {
            return dataStructure switch
            {
                VisualizedDataStructure.ArrayList => "ArrayList<T>",
                VisualizedDataStructure.List => "List<T>",
                VisualizedDataStructure.LinkedList => "LinkedList<T>",
                VisualizedDataStructure.Queue => "Queue<T>",
                VisualizedDataStructure.Stack => "Stack<T>",
                _ => dataStructure.ToString()
            };
        }

        private void OnCanvasViewChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanvasZoomText));
            OnPropertyChanged(nameof(CanvasOffsetText));
        }

        private void NotifyStepChanged()
        {
            OnPropertyChanged(nameof(CurrentStepValueText));
            OnPropertyChanged(nameof(CurrentOperationsText));
            OnPropertyChanged(nameof(OperationsPrefixText));
        }

        public AnimationPlaybackViewModel(MainWindowViewModel main, string filePath)
        {
            _main = main;

            string content = File.ReadAllText(filePath);

            var parsedContent = ParseFileContent(content);

            if (parsedContent == null)
            {
                throw new InvalidOperationException("Invalid file content.");
            }

            (VisualizedDataStructure dataStructure, string body) = parsedContent.Value;
            _visualizedDataStructure = dataStructure;

            switch (dataStructure)
            {
                case VisualizedDataStructure.ArrayList:
                    _visualizerCanvas = new ArrayListCanvas();
                    break;

                case VisualizedDataStructure.List:
                    _visualizerCanvas = new ListCanvas();
                    break;

                case VisualizedDataStructure.LinkedList:
                    _visualizerCanvas = new LinkedListCanvas();
                    break;

                case VisualizedDataStructure.Queue:
                    _visualizerCanvas = new QueueCanvas();
                    break;

                case VisualizedDataStructure.Stack:
                    _visualizerCanvas = new StackCanvas();
                    break;

                default:
                    throw new NotSupportedException($"Visualization for {dataStructure} is not supported.");
            }

            if (!string.IsNullOrEmpty(body))
            {
                CanvasOpLogger? logger = CanvasOpLoggerIO.Deserialize(body);

                if (logger != null) 
                {
                    _visualizerCanvas.LoadLoggers([logger]);
                    _visualizerCanvas.ResetSteps();
                }
            }
            
            GoHomeCommand = new AsyncRelayCommand(async () =>
            {
                var result = await MessageBox.ShowAsync(
                    "Потвърждение",
                    "Наистина ли искате да се върнете на началния екран?",
                    MessageBoxButtons.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    Dispose();
                    _main.CurrentViewModel = new HomeViewModel(_main);
                }
            });

            AdvanceStepCommand = new RelayCommand(() => { _visualizerCanvas.StepForward(); NotifyStepChanged(); });
            ReverseStepCommand = new RelayCommand(() => { _visualizerCanvas.StepBack(); NotifyStepChanged(); });
            RestartCommand = new RelayCommand(() => { _visualizerCanvas.ResetSteps(); NotifyStepChanged(); });
            ScreenshotCommand = new AsyncRelayCommand(TakeScreenshotAsync);

            _visualizerCanvas.ViewChanged += OnCanvasViewChanged;
        }

        private static (VisualizedDataStructure dataStructure, string body)? ParseFileContent(string content)
        {
            // Normalize line endings for cross-platform compatibility
            content = content.Replace("\r\n", "\n");

            int newLineIndex = content.IndexOf('\n');

            if (newLineIndex == -1)
            {
                return null;
            }

            string header = content.Substring(0, newLineIndex);
            string body = content.Substring(newLineIndex + 1);

            const string prefix = "DataStructure: ";

            if (!header.StartsWith(prefix))
            {
                return null;
            }

            string dataStructureString = header.Substring(prefix.Length);
            if (!Enum.TryParse(dataStructureString, out VisualizedDataStructure dataStructure))
            {
                return null;
            }

            return (dataStructure, body);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _visualizerCanvas.ViewChanged -= OnCanvasViewChanged;
            _visualizerCanvas?.Dispose();
            _disposed = true;
        }

        private async System.Threading.Tasks.Task TakeScreenshotAsync()
        {
            Window? mainWindow =
                (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

            if (mainWindow is null)
            {
                return;
            }

            IStorageProvider storage = mainWindow.StorageProvider;

            if (!storage.CanSave)
            {
                return;
            }

            string suggestedFileName = $"visualizer-screenshot-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

            IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Запази екранна снимка",
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "png",
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG Image")
                    {
                        Patterns = ["*.png"],
                        AppleUniformTypeIdentifiers = ["public.png"],
                        MimeTypes = ["image/png"]
                    }
                ]
            });

            if (file is null)
            {
                return;
            }

            await _visualizerCanvas.TakeScreenshotAsync(file.Path.LocalPath);
        }
    }
}
