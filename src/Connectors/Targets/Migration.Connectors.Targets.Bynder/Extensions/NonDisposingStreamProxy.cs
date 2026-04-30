namespace Migration.Connectors.Targets.Bynder.Extensions;

internal class NonDisposingStreamProxy(Stream innerStream) : Stream
{
    public override bool CanRead => innerStream.CanRead;
    public override bool CanWrite => innerStream.CanWrite;
    public override bool CanSeek => innerStream.CanSeek;
    public override bool CanTimeout => innerStream.CanTimeout;
    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override int ReadTimeout
    {
        get => innerStream.ReadTimeout;
        set => innerStream.ReadTimeout = value;
    }

    public override int WriteTimeout
    {
        get => innerStream.WriteTimeout;
        set => innerStream.WriteTimeout = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return innerStream.Read(buffer, offset, count);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return innerStream.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        innerStream.Write(buffer, offset, count);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return innerStream.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return innerStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        innerStream.SetLength(value);
    }

    public override void Flush()
    {
        innerStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return innerStream.FlushAsync(cancellationToken);
    }

    public override void Close()
    {
        // Intentionally do nothing to prevent disposing the underlying stream.
    }

    protected override void Dispose(bool disposing)
    {
        // Intentionally do nothing to prevent disposing the underlying stream.
    }

    public override ValueTask DisposeAsync()
    {
        // Intentionally do nothing to prevent disposing the underlying stream.
        return ValueTask.CompletedTask;
    }
}
