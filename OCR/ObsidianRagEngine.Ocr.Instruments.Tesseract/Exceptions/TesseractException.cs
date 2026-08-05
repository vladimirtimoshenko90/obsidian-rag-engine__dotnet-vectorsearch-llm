namespace ObsidianRagEngine.Ocr.Instruments.Tesseract.Exceptions;

public class TesseractException : Exception
{
    public int? ExitCode { get; }
    public string? Signal { get; }
    public string? Stderr { get; }

    public TesseractException(string message, int? exitCode = null, string? signal = null, string? stderr = null)
        : base(BuildMessage(message, stderr))
    {
        ExitCode = exitCode;
        Signal = signal;
        Stderr = stderr;
    }

    private static string BuildMessage(string message, string? stderr) =>
        string.IsNullOrWhiteSpace(stderr) ? message : $"{message} Stderr: {stderr.Trim()}";
}
