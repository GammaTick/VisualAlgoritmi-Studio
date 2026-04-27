using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using VisualAlgoritmi_Studio.Controls.Canvas.Core;

namespace VisualAlgoritmi_Studio.Controls.Canvas.Registry
{
    internal static class VisualDataStructuresRegister
    {
        private static readonly List<CanvasOpLogger> _loggers = [];
        private static readonly HashSet<VisualizerCanvasBase> _registeredCanvases = [];

        private static Type? _activeDataStructureType;
        private static bool _executionActive;
        private static bool _canRegisterCanvases;

        /// <summary>
        /// Begins user code execution.
        /// Clears previous results and opens registration.
        /// </summary>
        public static void BeginExecution(Type dataStructureType)
        {
            _loggers.Clear();
            _registeredCanvases.Clear();

            _activeDataStructureType = dataStructureType;
            _executionActive = true;
            _canRegisterCanvases = true;
        }

        /// <summary>
        /// Ends user code execution.
        /// Freezes results and closes all registration.
        /// Visualization may begin after this.
        /// </summary>
        public static void EndExecution()
        {
            _executionActive = false;
            _canRegisterCanvases = false;
            _activeDataStructureType = null;

            foreach (var canvas in _registeredCanvases)
            {
                canvas.InvalidateVisual();
            }
        }

        // ================================
        // Canvas registration
        // ================================
        public static bool RegisterCanvas(VisualizerCanvasBase canvas)
        {
            if (canvas is null)
            {
                throw new ArgumentNullException(nameof(canvas));
            }

            if (!_executionActive)
            {
                ThrowHelper.ThrowNoActiveExecution();
            }

            if (!_canRegisterCanvases)
            {
                return false;
            }

            _registeredCanvases.Add(canvas);
            return true;
        }

        public static void CloseCanvasRegistration()
        {
            _canRegisterCanvases = false;
        }

        // ================================
        // Data-structure registration
        // ================================
        public static CanvasOpLogger RegisterLogger(Type dataStructureType)
        {
            if (!_executionActive)
            {
                ThrowHelper.ThrowNoActiveExecution();
            }

            if (_activeDataStructureType != dataStructureType)
            {
                ThrowHelper.ThrowWrongDataStructureType(
                    dataStructureType,
                    _activeDataStructureType!
                );
            }

            CanvasOpLogger logger = new();
            _loggers.Add(logger);
            return logger;
        }

        // ================================
        // Visualization access
        // ================================
        public static IReadOnlyList<CanvasOpLogger>? GetLoggers(VisualizerCanvasBase canvas)
        {
            if (canvas is null)
            {
                ThrowHelper.ThrowCanvasNull();
            }

            if (_executionActive)
            {
                // Visualization is not allowed while execution is running
                return null;
            }

            if (!_registeredCanvases.Contains(canvas))
            {
                return null;
            }

            return _loggers;
        }

        public static IEnumerable<VisualizerCanvasBase> GetRegisteredCanvases()
        {
            return _registeredCanvases;
        }

        private static class ThrowHelper
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowNoActiveExecution()
            {
                throw new InvalidOperationException("No active execution.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowWrongDataStructureType(Type actual, Type expected)
            {
                throw new InvalidOperationException($"Cannot register {actual.Name} in an execution for {expected.Name}.");
            }

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowCanvasNull()
            {
                throw new ArgumentNullException("canvas");
            }
        }
    }
}