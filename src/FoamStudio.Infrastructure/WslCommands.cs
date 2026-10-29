using FoamStudio.Core;

namespace FoamStudio.Infrastructure;

public static class WslCommands
{
    // ArgumentList handles Windows quoting. Bash values require separate POSIX quoting.
    public static string Quote(string value)
    {
        if (value.Contains('\0')) throw new ArgumentException("A shell value cannot contain NUL.", nameof(value));
        return "'" + value.Replace("'", "'\"'\"'") + "'";
    }

    public static Command Bash(string distribution, string script) =>
        new("wsl.exe", ["--distribution", distribution, "--exec", "bash", "--noprofile", "--norc", "-c", script.Replace("\r\n", "\n")]);

    public static Command ListDistributions() => new("wsl.exe", ["--list", "--quiet"], true);
    public static Command ListVersions() => new("wsl.exe", ["--list", "--verbose"], true);
}
