using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;
using VisualAlgoritmi_Studio.Controls.Editor;
using VisualAlgoritmi_Studio.ProjectCore;
using VisualAlgoritmi_Studio.RoslynCore;
using VisualAlgoritmi_Studio.Visualization;
using VisualAlgoritmi_Studio.Config;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;
using VisualAlgoritmi_Studio.Controls.Canvas.Registry;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.List;
using System.IO;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using VisualAlgoritmi_Studio.Models;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.ArrayList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.LinkedList;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Queue;
using VisualAlgoritmi_Studio.Controls.Canvas.Structures.Stack;
using VisualAlgoritmi_Studio.Controls.Console;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using VisualAlgoritmi_Studio.Views.Dialogs;
using System.Linq;

namespace VisualAlgoritmi_Studio.ViewModels
{
    internal class VisualizationViewModel : ViewModelBase, IDisposable
    {
        public readonly string InitialCode;

        public ICommand GoHomeCommand { get; }
        public ICommand SaveCodeCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand CommentLinesCommand { get; }
        public ICommand UncommentLinesCommand { get; }
        public ICommand RunCodeCommand { get; }
        public ICommand ReverseStepCommand { get; }
        public ICommand AdvanceStepCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ShowEditorOnlyCommand { get; }
        public ICommand ShowNormalCommand { get; }
        public ICommand ShowCanvasOnlyCommand { get; }
        public ICommand ShowErrorListCommand { get; }
        public ICommand ShowOutputCommand { get; }
        public ICommand ExportAnimationCommand { get; }
        public ICommand ResetViewCommand { get; }
        public ICommand NavigateToErrorCommand { get; }
        public ICommand CopyErrorMessageCommand { get; }
        public ICommand ImportCodeCommand { get; }
        public ICommand OpenLocationCommand { get; }
        public ICommand ExportCodeCommand { get; }
        public ICommand LoadExampleCodeCommand { get; }

        private readonly MainWindowViewModel _main;
        private readonly ProjectManager _projectManager;
        private readonly Settings _settings;
        private IClipboard? _clipboard;
        private readonly VisualizedDataStructure _visualizedDataStructure;

        private readonly DataStructureMetadata _dataStructureMetadata;
        private readonly VisualizerCanvasBase _visualizerCanvas;
        private readonly ConsoleRedirectWriter _consoleRedirectWriter;
        private readonly ConsoleRedirectReader _consoleRedirectReader;

        private Compiler? _compiler;
        private CodeEditor? _codeEditor;
        private ConsoleControl? _consoleControl;
        private IStorageProvider? _storageProvider;
        private bool _disposed;
        private bool _isExecuting;
        private LayoutMode _layoutMode = LayoutMode.Normal;
        private BottomPanelMode _bottomPanelMode = BottomPanelMode.ErrorList;
        private string _executionStatusText = "Не се изпълнява";
        private DispatcherTimer? _executionTimer;
        private DateTime _executionStartTime;
        private bool _hasUnsavedChanges;

        public ObservableCollection<EditorError> Errors { get; } = new();

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            private set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public LayoutMode CurrentLayout
        {
            get => _layoutMode;
            set
            {
                if (SetProperty(ref _layoutMode, value))
                {
                    OnPropertyChanged(nameof(EditorColumnWidth));
                    OnPropertyChanged(nameof(CanvasColumnWidth));
                    OnPropertyChanged(nameof(IsEditorAreaVisible));
                    OnPropertyChanged(nameof(IsCanvasAreaVisible));
                }
            }
        }

        public BottomPanelMode CurrentBottomPanel
        {
            get => _bottomPanelMode;
            set
            {
                if (SetProperty(ref _bottomPanelMode, value))
                {
                    OnPropertyChanged(nameof(IsErrorListVisible));
                    OnPropertyChanged(nameof(IsOutputVisible));
                    OnPropertyChanged(nameof(IsErrorListSelected));
                    OnPropertyChanged(nameof(IsOutputSelected));
                }
            }
        }

        public GridLength EditorColumnWidth =>
            _layoutMode == LayoutMode.CanvasOnly
                ? GridLength.Auto
                : new GridLength(1, GridUnitType.Star);

        public GridLength CanvasColumnWidth =>
            _layoutMode == LayoutMode.EditorOnly
                ? new GridLength(0)
                : new GridLength(1, GridUnitType.Star);

        public bool IsEditorAreaVisible => _layoutMode != LayoutMode.CanvasOnly;
        public bool IsCanvasAreaVisible => _layoutMode != LayoutMode.EditorOnly;
        public bool IsErrorListVisible => _bottomPanelMode == BottomPanelMode.ErrorList;
        public bool IsOutputVisible => _bottomPanelMode == BottomPanelMode.Output;
        public bool IsErrorListSelected => _bottomPanelMode == BottomPanelMode.ErrorList;
        public bool IsOutputSelected => _bottomPanelMode == BottomPanelMode.Output;

        public string RunButtonText => "Изпълни кода";
        public string RunButtonIcon => "/Assets/Icons/run-code.svg";
        public string SelectedDataStructureName => GetDataStructureDisplayName(_visualizedDataStructure);

        public string ExecutionStatusText
        {
            get => _executionStatusText;
            private set => SetProperty(ref _executionStatusText, value);
        }

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

        public VisualizerCanvasBase VisualizerCanvas
        {
            get => _visualizerCanvas;
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

        public VisualizationViewModel(MainWindowViewModel main,
            ProjectManager projectManager,
            VisualizedDataStructure visualizedDataStructure,
            Settings settings)
        {
            _main = main;
            _projectManager = projectManager;
            _settings = settings;
            _visualizedDataStructure = visualizedDataStructure;

            InitialCode = projectManager.GetUserCode();

            _dataStructureMetadata = DataStructureMetadataFactory.Create(visualizedDataStructure);

            switch (visualizedDataStructure)
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
                    throw new NotSupportedException($"Visualization for {visualizedDataStructure} is not supported.");
            }

            _consoleRedirectWriter = new ConsoleRedirectWriter();

            _consoleRedirectReader = new ConsoleRedirectReader();

            GoHomeCommand = new AsyncRelayCommand(async () =>
            {
                string message = HasUnsavedChanges
                    ? "Имате незапазени промени. Наистина ли искате да се върнете на началния екран?"
                    : "Наистина ли искате да се върнете на началния екран?";

                var result = await MessageBox.ShowAsync(
                    "Потвърждение",
                    message,
                    MessageBoxButtons.YesNo,
                    HasUnsavedChanges ? MessageBoxIcon.Warning : MessageBoxIcon.None);

                if (result == MessageBoxResult.Yes)
                {
                    Dispose();
                    _main.CurrentViewModel = new HomeViewModel(_main);
                }
            });

            SaveCodeCommand = new RelayCommand(() =>
            {
                if (_codeEditor == null)
                {
                    return;
                }
                
                SaveCode();
            });

            UndoCommand = new RelayCommand(() =>
            {
                if (_codeEditor == null)
                {
                    return;
                }

                _codeEditor.UndoChange();
            });

            RedoCommand = new RelayCommand(() =>
            {
                if (_codeEditor == null)
                {
                    return;
                }

                _codeEditor.RedoChange();
            });

            CommentLinesCommand = new RelayCommand(() =>
            {
                if (_codeEditor == null)
                {
                    return;
                }

                _codeEditor.CommentOutSelectedLines();
            });

            UncommentLinesCommand = new RelayCommand(() =>
            {
                if (_codeEditor == null)
                {
                    return;
                }

                _codeEditor.UncommentSelectedLines();
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

            RunCodeCommand = new AsyncRelayCommand(RunCode);

            ShowEditorOnlyCommand = new RelayCommand(() => CurrentLayout = LayoutMode.EditorOnly);
            ShowNormalCommand = new RelayCommand(() => CurrentLayout = LayoutMode.Normal);
            ShowCanvasOnlyCommand = new RelayCommand(() => CurrentLayout = LayoutMode.CanvasOnly);
            ShowErrorListCommand = new RelayCommand(() => CurrentBottomPanel = BottomPanelMode.ErrorList);
            ShowOutputCommand = new RelayCommand(() => CurrentBottomPanel = BottomPanelMode.Output);

            ExportAnimationCommand = new AsyncRelayCommand(ExportAnimation);

            ResetViewCommand = new RelayCommand(() =>
            {
                _visualizerCanvas.ResetView(); 
            });

            NavigateToErrorCommand = new RelayCommand<EditorError>(error =>
            {
                if (_codeEditor == null || error == null)
                {
                    return;
                }

                _codeEditor.SetCaretPosition(error.Line - 1, error.Column - 1);
            });

            CopyErrorMessageCommand = new AsyncRelayCommand<EditorError>(async error =>
            {
                if (_clipboard == null || error == null)
                {
                    return;
                }

                string text = $"{error.Code}  {error.Message}  (Ред {error.Line}, Колона {error.Column})";
                await _clipboard.SetTextAsync(text);
            });

            ImportCodeCommand = new AsyncRelayCommand(ImportCode);
            OpenLocationCommand = new RelayCommand(OpenLocation);
            ExportCodeCommand = new AsyncRelayCommand(ExportCode);
            LoadExampleCodeCommand = new AsyncRelayCommand(LoadExampleCode);

            _visualizerCanvas.ViewChanged += OnCanvasViewChanged;
        }

        private void SaveCode()
        {
            if (_codeEditor == null)
            {
                return;
            }

            _projectManager.SaveUserCode(_codeEditor.GetCode());
            HasUnsavedChanges = false;
        }

        private async Task ImportCode()
        {
            if (_codeEditor == null || _storageProvider == null)
            {
                return;
            }

            IReadOnlyList<IStorageFile> files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Импортирай код",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("C# Source File") { Patterns = ["*.cs"] }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            await using Stream stream = await files[0].OpenReadAsync();
            using StreamReader reader = new(stream);
            string code = await reader.ReadToEndAsync();

            await _codeEditor.SetCode(code);
        }

        private void OpenLocation()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _projectManager.ProjectRootPath,
                UseShellExecute = true
            });
        }

        private async Task LoadExampleCode()
        {
            if (_codeEditor == null)
            {
                return;
            }

            MessageBoxResult result;

            if (_codeEditor.GetCodeLength() == 0)
            {
                result = await MessageBox.ShowAsync(
                    "Зареди примерен код",
                    "Наистина ли искате да заредите примерния код?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.None);
            }
            else
            {
                result = await MessageBox.ShowAsync(
                    "Зареди примерен код",
                    "Зареждането на примерния код ще замени текущото съдържание на редактора. Наистина ли искате да продължите?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
            }

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            string exampleCode = _projectManager.GetExampleCode();
            await _codeEditor.SetCode(exampleCode);
        }

        private async Task ExportCode()
        {
            if (_codeEditor == null || _storageProvider == null)
            {
                return;
            }

            IStorageFile? file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Експортирай код",
                DefaultExtension = "cs",
                FileTypeChoices =
                [
                    new FilePickerFileType("C# Source File") { Patterns = ["*.cs"] }
                ],
                SuggestedFileName = "UserCode.cs"
            });

            if (file == null)
            {
                return;
            }

            string code = _codeEditor.GetCode();

            await using Stream stream = await file.OpenWriteAsync();
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(code);
        }

        private async Task RunCode()
        {
            if (_compiler == null || _codeEditor == null)
            {
                return;
            }

            if (_isExecuting)
            {
                return;
            }

            _isExecuting = true;

            SaveCode();

            CurrentBottomPanel = BottomPanelMode.Output;
            Dispatcher.UIThread.Post(() =>
            {
                _consoleControl?.FocusConsole();
            });

            _consoleControl?.Clear();

            VisualDataStructuresRegister.BeginExecution(_dataStructureMetadata.ReplacementTypeRuntimeType);

            VisualDataStructuresRegister.RegisterCanvas(_visualizerCanvas);

            VisualDataStructuresRegister.CloseCanvasRegistration();

            _executionStartTime = DateTime.UtcNow;
            ExecutionStatusText = "Изпълнява се… (0 сек)";
            StartExecutionTimer();

            try
            {
                var result = await _compiler.CompileAndRun();

                StopExecutionTimer();

                double elapsed = (DateTime.UtcNow - _executionStartTime).TotalSeconds;

                if (result.FailureMessage != null || Errors.Count > 0)
                {
                    CurrentBottomPanel = BottomPanelMode.ErrorList;
                }

                if (result.UserException is { } ex)
                {
                    ExecutionStatusText = $"Завърши за {FormatElapsed(elapsed)} с изключение";

                    string locationInfo = GetExceptionLocation(ex);

                    string messageContent = 
                        $"{ex.GetType().Name}: {ex.Message}{Environment.NewLine}" +
                        $"{Environment.NewLine}" + 
                        $"Stack Trace:{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}" +
                        Environment.NewLine + 
                        $"{locationInfo}";

                    await MessageBox.ShowAsync("Грешка при изпълнение",
                        messageContent,
                        MessageBoxButtons.OkCopy,
                        MessageBoxIcon.Critical);
                }
                else
                {
                    ExecutionStatusText = $"Завърши за {FormatElapsed(elapsed)}";
                }
            }
            catch (Exception ex)
            {
                StopExecutionTimer();
                double elapsed = (DateTime.UtcNow - _executionStartTime).TotalSeconds;
                ExecutionStatusText = $"Грешка в хоста ({FormatElapsed(elapsed)})";

                // Since this is a system/host error, we want more technical detail 
                // than just Row/Column, because the error isn't in the user's script.
                string technicalDetails = ex.StackTrace ?? "Няма налична следа (Stack Trace).";

                await MessageBox.ShowAsync("Системна грешка",
                    $"Възникна неочаквана грешка в средата за изпълнение:{Environment.NewLine}{Environment.NewLine}" +
                    $"{ex.GetType().Name}: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                    $"Технически детайли:{Environment.NewLine}{technicalDetails}",
                    MessageBoxButtons.OkCopy,
                    MessageBoxIcon.Critical);
            }
            finally
            {
                StopExecutionTimer();

                VisualDataStructuresRegister.EndExecution();

                foreach (var canvas in VisualDataStructuresRegister.GetRegisteredCanvases())
                {
                    canvas.OnExecutionEnded();
                    canvas.ResetSteps();
                }

                NotifyStepChanged();

                _isExecuting = false;
            }
        }

        private static string FormatElapsed(double totalSeconds)
        {
            int whole = (int)totalSeconds;
            int minutes = whole / 60;
            int seconds = whole % 60;
            return minutes > 0 ? $"{minutes} мин {seconds} сек" : $"{seconds} сек";
        }

        private void StartExecutionTimer()
        {
            _executionTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _executionTimer.Tick += (_, _) =>
            {
                double elapsed = (DateTime.UtcNow - _executionStartTime).TotalSeconds;
                ExecutionStatusText = $"Изпълнява се… ({FormatElapsed(elapsed)})";
            };
            _executionTimer.Start();
        }

        private void StopExecutionTimer()
        {
            _executionTimer?.Stop();
            _executionTimer = null;
        }

        private void NotifyStepChanged()
        {
            OnPropertyChanged(nameof(CurrentStepValueText));
            OnPropertyChanged(nameof(CurrentOperationsText));
            OnPropertyChanged(nameof(OperationsPrefixText));
        }

        private static string GetExceptionLocation(Exception ex)
        {
            var st = new StackTrace(ex, true);
            var frames = st.GetFrames();

            // Find the first frame that actually has a file name (usually Code.cs)
            var userFrame = frames?.FirstOrDefault(f => !string.IsNullOrEmpty(f.GetFileName()));

            if (userFrame != null)
            {
                int lineNumber = userFrame.GetFileLineNumber();
                int columnNumber = userFrame.GetFileColumnNumber();

                // OFFSET ADJUSTMENT:
                // Since the Rewrite() method adds 2 using directives at the top, 
                // the reported line is 2 lines higher than what the user sees in the editor.
                int offset = 2; 
                int adjustedLine = Math.Max(1, lineNumber - offset);

                return $"Местоположение: Ред {adjustedLine}, Колона {columnNumber}";
            }

            return "Местоположение: Не са налични детайли";
        }
        private void OnCanvasViewChanged(object? sender, EventArgs e)
        {
            OnPropertyChanged(nameof(CanvasZoomText));
            OnPropertyChanged(nameof(CanvasOffsetText));
        }

        private async Task ExportAnimation()
        {
            if (_storageProvider == null)
            {
                return;
            }

            if (!_visualizerCanvas.HasExecuted)
            {
                await MessageBox.ShowAsync(
                    "Няма налична анимация",
                    "Моля, изпълнете кода, преди да експортирате анимацията.",
                    MessageBoxButtons.Ok,
                    MessageBoxIcon.Warning);
                return;
            }

            if (_visualizerCanvas.StepCount == 0)
            {
                var confirmResult = await MessageBox.ShowAsync(
                    "Празна анимация",
                    "Анимацията не съдържа стъпки. Сигурни ли сте, че искате да я експортирате?",
                    MessageBoxButtons.YesCancel,
                    MessageBoxIcon.Warning);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            string dataStructureName = _visualizedDataStructure.ToString();
            string suggestedFileName = $"{dataStructureName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.vaanim";

            IStorageFile? file = await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Експортирай анимация",
                DefaultExtension = "vaanim",
                FileTypeChoices =
                [
                    new FilePickerFileType("Visual Algoritmi Animation") { Patterns = ["*.vaanim"] }
                ],
                SuggestedFileName = suggestedFileName
            });

            if (file == null)
            {
                return;
            }

            string body = _visualizerCanvas.SerializeAnimation();
            string header = $"DataStructure: {dataStructureName}{Environment.NewLine}";
            string content = header + body;

            await using Stream stream = await file.OpenWriteAsync();
            await using StreamWriter writer = new(stream);
            await writer.WriteAsync(content);
        }

        public void AttachStorageProvider(IStorageProvider storageProvider)
        {
            _storageProvider = storageProvider;
        }

        public void AttachClipboard(IClipboard clipboard)
        {
            _clipboard = clipboard;
        }

        public async Task AttachCodeEditor(CodeEditor codeEditor)
        {
            _codeEditor = codeEditor;

            _compiler = new Compiler(codeEditor.CodeAnalysisSession,
                _consoleRedirectWriter,
                _consoleRedirectReader,
                _dataStructureMetadata);

            await codeEditor.SetCode(InitialCode);
            codeEditor.DiagnosticsUpdated += DiagnosticsUpdated;
            codeEditor.CodeContentChanged += OnCodeContentChanged;
        }

        private void OnCodeContentChanged(object? sender, EventArgs e)
        {
            HasUnsavedChanges = true;
        }

        public void AttachConsoleControl(ConsoleControl consoleControl)
        {
            _consoleControl = consoleControl;
            consoleControl.SetWriter(_consoleRedirectWriter);
            consoleControl.SetReader(_consoleRedirectReader);
        }

        private void DiagnosticsUpdated(List<Diagnostic> diagnostics)
        {
            Errors.Clear();

            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity != DiagnosticSeverity.Error)
                {
                    continue;
                }

                var span = diagnostic.Location.GetLineSpan();

                int line = span.StartLinePosition.Line + 1;
                int column = span.StartLinePosition.Character + 1;

                Errors.Add(new EditorError(
                    diagnostic.Id,
                    diagnostic.GetMessage(),
                    line,
                    column
                ));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopExecutionTimer();
            _consoleRedirectReader.CancelPendingRead();

            if (_codeEditor != null)
            {
                _codeEditor.DiagnosticsUpdated -= DiagnosticsUpdated;
                _codeEditor.CodeContentChanged -= OnCodeContentChanged;
            }

            _visualizerCanvas.ViewChanged -= OnCanvasViewChanged;
            _visualizerCanvas?.Dispose();

            _disposed = true;
        }
    }

    internal enum LayoutMode 
    { 
        EditorOnly,
        Normal,
        CanvasOnly
    }

    internal enum BottomPanelMode 
    { 
        ErrorList,
        Output
    }
}
