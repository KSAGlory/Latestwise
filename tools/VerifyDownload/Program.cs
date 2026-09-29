using System.Security.Cryptography;
using System.Text.RegularExpressions;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: VerifyDownload <download> <download.sha256>");
    return 2;
}

try
{
    string downloadPath = Path.GetFullPath(args[0]);
    string checksumPath = Path.GetFullPath(args[1]);

    if (!File.Exists(downloadPath) || !File.Exists(checksumPath))
    {
        Console.Error.WriteLine("Both the download and its .sha256 file must exist.");
        return 2;
    }

    string[] lines = File.ReadAllLines(checksumPath)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray();

    if (lines.Length != 1)
    {
        Console.Error.WriteLine("The .sha256 file must contain exactly one checksum entry.");
        return 2;
    }

    Match entry = Regex.Match(lines[0], @"^([0-9a-fA-F]{64})  (.+)$");
    if (!entry.Success)
    {
        Console.Error.WriteLine("The .sha256 file has an invalid format.");
        return 2;
    }

    string downloadName = Path.GetFileName(downloadPath);
    if (!string.Equals(entry.Groups[2].Value, downloadName, StringComparison.Ordinal))
    {
        Console.Error.WriteLine("The checksum belongs to a different download.");
        return 2;
    }

    using FileStream stream = File.OpenRead(downloadPath);
    string actual = Convert.ToHexString(SHA256.HashData(stream));

    if (!string.Equals(actual, entry.Groups[1].Value, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine($"Mismatch: {downloadName} does not match the published checksum.");
        return 1;
    }

    Console.WriteLine($"Match: {downloadName} matches the published checksum.");
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
{
    Console.Error.WriteLine($"Could not verify download: {error.Message}");
    return 2;
}
