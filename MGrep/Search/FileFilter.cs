using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace MGrep;

/// <summary>
/// Resolves a set of file-pattern strings into a concrete list of absolute file paths
/// rooted at a given folder.
/// </summary>
/// <remarks>
/// Patterns prefixed with <c>-</c> are treated as exclusions.
/// When <paramref name="globbing"/> is <see langword="false"/> (simple mode), patterns must be
/// plain file-name globs (no directory separator) and are automatically prefixed with
/// <c>**/</c> when <paramref name="includeSubfolders"/> is <see langword="true"/>.
/// When <paramref name="globbing"/> is <see langword="true"/>, full glob syntax is accepted and
/// applied as-is by <see cref="Matcher"/>.
/// </remarks>
public interface IFileFilter
{
    /// <summary>Registers one or more include/exclude patterns.</summary>
    void AddFilePatterns(IEnumerable<string> filePatterns);

    /// <summary>Executes the filter and returns the matching absolute file paths.</summary>
    IEnumerable<string> Execute();
}

/// <inheritdoc cref="IFileFilter"/>
public class FileFilter : IFileFilter
{
    private readonly Matcher matcher = new();
    private readonly string rootFolder;
    private readonly bool globbing;
    private readonly bool includeSubfolders;
    private bool addedIncludes;

    /// <param name="rootFolder">The directory to search.</param>
    /// <param name="globbing">
    ///   When <see langword="true"/> full glob paths are accepted;
    ///   when <see langword="false"/> only plain file-name patterns are allowed.
    /// </param>
    /// <param name="includeSubfolders">
    ///   When <see langword="true"/> and not using full globbing, patterns are prefixed
    ///   with <c>**/</c> to recurse into sub-directories.
    /// </param>
    public FileFilter(string rootFolder, bool globbing, bool includeSubfolders)
    {
        this.rootFolder = rootFolder;
        this.globbing = globbing;
        this.includeSubfolders = includeSubfolders;
    }

    /// <inheritdoc/>
    public void AddFilePatterns(IEnumerable<string> filePatterns)
    {
        foreach (var filePattern in filePatterns)
        {
            if (filePattern[0] == '-')
            {
                matcher.AddExclude(ValidateFilePattern(filePattern[1..]));
            }
            else
            {
                matcher.AddInclude(ValidateFilePattern(filePattern));
                addedIncludes = true;
            }
        }
    }

    /// <inheritdoc/>
    /// <exception cref="DirectoryNotFoundException">
    ///   Thrown when <see cref="rootFolder"/> does not exist.
    /// </exception>
    public IEnumerable<string> Execute()
    {
        if (!Directory.Exists(rootFolder))
        {
            throw new DirectoryNotFoundException($"Folder {rootFolder} not found");
        }

        if (!addedIncludes)
        {
            matcher.AddInclude(includeSubfolders ? "**/*" : "*");
        }

        var directoryInfo = new DirectoryInfo(rootFolder);
        var result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));
        return result.Files
            .Select(fileMatch => Path.GetFullPath(Path.Combine(directoryInfo.FullName, fileMatch.Path)))
            .ToArray();
    }

    /// <summary>
    /// Validates and normalises a single pattern for use with <see cref="Matcher"/>.
    /// In simple (non-globbing) mode the pattern must be a plain file name; the recursive
    /// prefix <c>**/</c> is added automatically when sub-folder traversal is enabled.
    /// </summary>
    private string ValidateFilePattern(string filePattern)
    {
        if (globbing)
        {
            var directoryName = Path.GetDirectoryName(filePattern);
            if (Path.GetFileName(filePattern).Any(c => Path.GetInvalidFileNameChars().Contains(c) && c != '*') ||
                (directoryName != null && directoryName.Any(c => Path.GetInvalidPathChars().Contains(c) && c != '*')))
            {
                throw new InvalidOperationException($"File pattern, {filePattern}, contains invalid characters");
            }

            return filePattern;
        }

        if (Path.GetFileName(filePattern) != filePattern)
        {
            throw new InvalidOperationException($"File pattern, {filePattern}, must not contain a path");
        }

        if (filePattern.Any(c => Path.GetInvalidFileNameChars().Contains(c) && c != '*'))
        {
            throw new InvalidOperationException($"File pattern, {filePattern}, contains invalid characters");
        }

        return includeSubfolders ? "**/" + filePattern : filePattern;
    }
}
