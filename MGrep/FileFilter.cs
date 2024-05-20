using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using DirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;

namespace MGrep;

public interface IFileFilter
{
    void AddFilePatterns(IEnumerable<string> filePatterns);
    IEnumerable<string> Execute();
}

public class FileFilter : IFileFilter
{
    private readonly Matcher matcher = new();
    private readonly string rootFolder;
    private readonly bool globbing;
    private readonly bool includeSubfolders;
    private bool addedIncludes;

    public FileFilter(string rootFolder, bool globbing, bool includeSubfolders)
    {
        this.rootFolder = rootFolder;
        this.globbing = globbing;
        this.includeSubfolders = includeSubfolders;
    }

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
        var x = result.Files.Select(fileMatch => Path.GetFullPath(Path.Combine(directoryInfo.FullName, fileMatch.Path)))
            .ToArray();
        return x;
    }

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