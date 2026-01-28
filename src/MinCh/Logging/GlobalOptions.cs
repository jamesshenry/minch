namespace MinCh.Logging;

public record GlobalOptions(VerbosityLevel Verbosity);

public enum VerbosityLevel
{
    Quiet,
    Minimal,
    Normal,
    Detailed,
    Verbose,
}
