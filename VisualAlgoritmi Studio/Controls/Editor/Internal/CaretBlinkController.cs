using Avalonia.Threading;
using System;

namespace VisualAlgoritmi_Studio.Controls.Editor.Internal
{
    internal sealed class CaretBlinkController
    {
        private readonly Func<bool> _shouldBlink;

        private readonly DispatcherTimer _timer;
        private long _lastInputTimestamp;

        private const int BlinkSuppressMs = 700;

        public bool CaretVisible { get; private set; } = true;

        public event Action? BlinkStateChanged;

        public CaretBlinkController(TimeSpan interval, Func<bool> shouldBlink)
        {
            _shouldBlink = shouldBlink;

            _timer = new DispatcherTimer
            {
                Interval = interval
            };

            _timer.Tick += OnTick;
        }
        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void ResetBlink()
        {
            _lastInputTimestamp = Environment.TickCount64;

            if (!CaretVisible)
            {
                CaretVisible = true;
                BlinkStateChanged?.Invoke();
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (!_shouldBlink())
            {
                if (!CaretVisible)
                {
                    CaretVisible = true;
                    BlinkStateChanged?.Invoke();
                }

                return;
            }

            long now = Environment.TickCount64;

            if (now - _lastInputTimestamp < BlinkSuppressMs)
            {
                return;
            }

            CaretVisible = !CaretVisible;
            BlinkStateChanged?.Invoke();
        }
    }
}