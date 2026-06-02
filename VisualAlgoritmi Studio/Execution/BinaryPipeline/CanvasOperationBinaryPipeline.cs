using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace VisualAlgoritmi_Studio.Execution.BinaryPipeline;

public sealed class CanvasOperationBinaryPipeline : IAsyncDisposable
{
    private NamedPipeServerStream? _pipe;

    public bool IsOpen => _pipe != null;

    public string Open()
    {
        if (_pipe is not null)
        {
            throw new InvalidOperationException("Binary pipeline is already open.");
        }

        string pipeName = OperatingSystem.IsWindows()
            ? $"VisualAlgoritmi_{Guid.NewGuid():N}"
            : $"VA_{Random.Shared.Next(int.MaxValue):X}";

        _pipe = new NamedPipeServerStream(
            pipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            transmissionMode: PipeTransmissionMode.Byte,
            options: PipeOptions.Asynchronous);

        return pipeName;
    }

    public async Task<MemoryStream> CaptureToMemoryStreamAsync()
    {
        if (_pipe is null)
        {
            throw new InvalidOperationException("Binary pipeline is not open.");
        }

        await _pipe.WaitForConnectionAsync();

        MemoryStream buffer = new();

        await _pipe.CopyToAsync(buffer);

        buffer.Position = 0;

        return buffer;
    }

    public async ValueTask DisposeAsync()
    {
        if (_pipe is null)
        {
            return;
        }

        await _pipe.DisposeAsync();
        _pipe = null;
    }
}