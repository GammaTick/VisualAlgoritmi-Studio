using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Canvas.Operations;
using VisualAlgoritmi_Studio.Controls.Canvas.Canvases;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using VisualAlgoritmi_Studio.Controls.Canvas.Operations;
using VisualAlgoritmi_Studio.Views.Dialogs;
using VisualAlgoritmi_Studio.Visualization;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class AnimationPlaybackViewModel : ViewModelBase
    {
        public ICommand GoHomeCommand { get; }
        public ICommand AdvanceStepCommand { get; }
        public ICommand ReverseStepCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ScreenshotCommand { get; }

        private readonly MainWindowViewModel _main;
        private readonly VisualizerCanvasBase _visualizerCanvas;
        private readonly VisualizedDataStructure _visualizedDataStructure;

        public VisualizerCanvasBase VisualizerCanvas => _visualizerCanvas;

        public string SelectedDataStructureName => GetDataStructureDisplayName(_visualizedDataStructure);

        public string CurrentStepValueText =>
          $" {(_visualizerCanvas.CurrentStep + 1).ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ")} / {_visualizerCanvas.StepCount.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ")}";

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

        public static async Task<AnimationPlaybackViewModel?> CreateAsync(MainWindowViewModel main, string filePath)
        {
            var result = await ParseFileAsync(filePath);

            return result == null
                ? null
                : new AnimationPlaybackViewModel(main, result.Value.Item1, result.Value.Item2);
        }

        private AnimationPlaybackViewModel(MainWindowViewModel main, VisualizerCanvasBase visualizerCanvas, VisualizedDataStructure visualizedDataStructure)
        {
            _main = main;

            _visualizerCanvas = visualizerCanvas;
            _visualizedDataStructure = visualizedDataStructure;

            GoHomeCommand = new AsyncRelayCommand(async () =>
            {
                var result = await MessageBox.ShowAsync(
                    "Потвърждение",
                    "Наистина ли искате да се върнете на началния екран?",
                    MessageBoxButtons.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    _main.CurrentViewModel = new HomeViewModel(_main);
                }
            });

            AdvanceStepCommand = new RelayCommand(() => 
            { 
                _visualizerCanvas.StepForward();
                NotifyStepChanged();
            });

            ReverseStepCommand = new RelayCommand(() => 
            { 
                _visualizerCanvas.StepBack();
                NotifyStepChanged();
            });

            RestartCommand = new RelayCommand(() => 
            {
                _visualizerCanvas.ResetSteps(); 
                NotifyStepChanged();
            });

            ScreenshotCommand = new AsyncRelayCommand(TakeScreenshotAsync);

            _visualizerCanvas.ViewChanged += OnCanvasViewChanged;
        }

        public void ResetCanvasView()
        {
            _visualizerCanvas.ResetView();

            OnPropertyChanged(nameof(CanvasZoomText));
            OnPropertyChanged(nameof(CanvasOffsetText));
        }

        private static async Task<(VisualizerCanvasBase, VisualizedDataStructure)?> ParseFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    throw new ArgumentException("Пътят към файла е празен.");
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Файлът не съществува.", filePath);
                }

                string content = await File.ReadAllTextAsync(filePath);

                var result = ParseFileContent(content);

                if (result is ParseResult.Failure failure)
                {
                    throw new InvalidDataException(failure.Reason);
                }

                if (result is not ParseResult.Success success)
                {
                    throw new InvalidDataException("Невалиден формат на файла: неизвестен резултат от парсването.");
                }

                var visualizerCanvas = CreateVisualizerCanvas(success.DataStructure);

                string body = success.Body;

                if (!string.IsNullOrWhiteSpace(body))
                {
                    CanvasTimeline? canvasTimeline = CanvasTimelineSerializer.Deserialize(body);

                    if (canvasTimeline == null)
                    {
                        throw new InvalidDataException("Файлът съдържа невалидни данни за анимацията.");
                    }

                    visualizerCanvas.LoadTimelineAndResetView(canvasTimeline);
                    visualizerCanvas.ResetSteps();
                }

                return (visualizerCanvas, success.DataStructure);
            }
            catch (Exception ex)
            {
                await MessageBox.ShowAsync(
                    "Грешка",
                    $"Неуспешно зареждане на анимация: {ex.Message}",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Error);

                return null;
            }
        }

        private static VisualizerCanvasBase CreateVisualizerCanvas(VisualizedDataStructure dataStructure)
        {
            return dataStructure switch
            {
                VisualizedDataStructure.ArrayList => new ArrayListCanvas(),
                VisualizedDataStructure.List => new ListCanvas(),
                VisualizedDataStructure.LinkedList => new LinkedListCanvas(),
                VisualizedDataStructure.Queue => new QueueCanvas(),
                VisualizedDataStructure.Stack => new StackCanvas(),
                _ => throw new NotSupportedException($"Visualization for {dataStructure} is not supported.")
            };
        }

        private static ParseResult ParseFileContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ParseResult.Failure("Файлът е празен.");
            }

            content = content.ReplaceLineEndings("\n");
            string[] lines = content.Split('\n');

            if (!lines[0].StartsWith("HeaderLines:"))
            {
                return ParseOldFormat(content);
            }

            string headerLinesText = lines[0]["HeaderLines:".Length..].Trim();

            if (!int.TryParse(headerLinesText, out int headerLines))
            {
                return new ParseResult.Failure("Невалидна стойност за 'HeaderLines'.");
            }

            if (headerLines < 1 || headerLines > lines.Length)
            {
                return new ParseResult.Failure("Невалиден брой header редове.");
            }

            var headers = new Dictionary<string, string>();

            for (int i = 1; i < headerLines; i++)
            {
                string line = lines[i];
                int separatorIndex = line.IndexOf(':');

                if (separatorIndex <= 0)
                {
                    return new ParseResult.Failure($"Невалиден header ред: '{line}'.");
                }

                string key = line[..separatorIndex].Trim();
                string value = line[(separatorIndex + 1)..].Trim();

                headers[key] = value;
            }

            if (!headers.TryGetValue("DataStructure", out string? dataStructureString))
            {
                return new ParseResult.Failure("Липсва поле 'DataStructure'.");
            }

            if (!Enum.TryParse(dataStructureString, out VisualizedDataStructure dataStructure))
            {
                return new ParseResult.Failure(
                    $"'{dataStructureString}' не е валидна стойност за {nameof(VisualizedDataStructure)}. " +
                    $"Очаквани стойности: {string.Join(", ", Enum.GetNames<VisualizedDataStructure>())}");
            }

            string body = string.Join('\n', lines[headerLines..]);

            return new ParseResult.Success(dataStructure, body);
        }

        private static ParseResult ParseOldFormat(string content)
        {
            int newLineIndex = content.IndexOf('\n');

            if (newLineIndex < 0)
            {
                return new ParseResult.Failure(
                    "Съдържанието не съдържа нов ред — липсва разделител между заглавната част и тялото.");
            }

            string header = content[..newLineIndex];
            string body = content[(newLineIndex + 1)..];

            if (!header.StartsWith("DataStructure:", StringComparison.Ordinal))
            {
                return new ParseResult.Failure(
                    $"Заглавната част '{header}' не започва с очаквания префикс 'DataStructure:'.");
            }

            string dataStructureString = header["DataStructure:".Length..].Trim();

            if (!Enum.TryParse(dataStructureString, out VisualizedDataStructure dataStructure))
            {
                return new ParseResult.Failure(
                    $"'{dataStructureString}' не е валидна стойност за {nameof(VisualizedDataStructure)}. " +
                    $"Очаквани стойности: {string.Join(", ", Enum.GetNames<VisualizedDataStructure>())}");
            }

            return new ParseResult.Success(dataStructure, body);
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

        private async Task TakeScreenshotAsync()
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

        private sealed record ParsedFileContent(
            VisualizedDataStructure DataStructure,
            string Body
        );

        private abstract record ParseResult
        {
            public sealed record Success(VisualizedDataStructure DataStructure, string Body) : ParseResult;
            public sealed record Failure(string Reason) : ParseResult;
        }
    }
}
