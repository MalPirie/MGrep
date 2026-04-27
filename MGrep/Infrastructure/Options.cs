using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MGrep;

/// <summary>
/// Provides typed, section-scoped access to a shared JSON configuration file.
/// Each <see cref="Options{T}"/> instance owns one named section within the file.
/// Reads are lazy (first access only); writes are atomic (write-to-temp then replace).
/// </summary>
/// <typeparam name="T">
/// A plain POCO that represents the configuration section.
/// Must have a parameterless constructor and be JSON-serialisable.
/// </typeparam>
public sealed class Options<T> where T : class, new()
{
    private readonly IFileSystem fileSystem;
    private readonly string sectionName;
    private readonly string fileName;
    private T? value;
    private readonly JsonSerializerOptions serializerOptions;

    /// <param name="sectionName">The JSON property name that wraps this section's data.</param>
    /// <param name="fileName">
    ///   File name (not path) of the config file, resolved relative to
    ///   <see cref="AppContext.BaseDirectory"/>.
    /// </param>
    public Options(string sectionName, string fileName)
        : this(sectionName, fileName, new FileSystem())
    { }

    /// <summary>Overload that accepts an injectable <see cref="IFileSystem"/> for testing.</summary>
    public Options(string sectionName, string fileName, IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
        this.sectionName = sectionName;
        this.fileName = Path.Combine(AppContext.BaseDirectory, fileName);
        serializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <summary>Gets the current section value, loading from disk on first access.</summary>
    public T Value => value ??= Load();

    /// <summary>
    /// Applies <paramref name="applyChanges"/> to the section, then atomically saves the
    /// entire config file.  Failures are silently swallowed — the in-memory value is still
    /// updated.
    /// </summary>
    public void Update(Action<T> applyChanges)
    {
        try
        {
            var documentObject = fileSystem.File.Exists(fileName)
                ? JsonNode.Parse(fileSystem.File.ReadAllText(fileName))?.AsObject()
                    ?? throw new InvalidOperationException($"Cannot parse {fileName}")
                : new JsonObject();

            var sectionObject = (documentObject.AsObject().TryGetPropertyValue(sectionName, out var section)
                ? section.Deserialize<T>(serializerOptions) : null) ?? new T();

            applyChanges(sectionObject);
            value = sectionObject;

            // Write to a temp file first, then move — prevents corruption on crash.
            var temporaryPath = fileSystem.Path.Combine(
                fileSystem.Path.GetDirectoryName(fileName)!,
                fileSystem.Path.GetRandomFileName());

            var movedToTemp = false;
            if (fileSystem.File.Exists(fileName))
            {
                fileSystem.File.Move(fileName, temporaryPath);
                movedToTemp = true;
            }

            try
            {
                using var stream = fileSystem.File.OpenWrite(fileName);
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                documentObject[sectionName] = JsonSerializer.SerializeToNode(sectionObject, serializerOptions);
                documentObject.WriteTo(writer);
            }
            catch
            {
                // Write failed — restore the original file from the temp backup.
                if (movedToTemp && fileSystem.File.Exists(temporaryPath))
                {
                    fileSystem.File.Move(temporaryPath, fileName);
                }
                throw;
            }

            if (movedToTemp)
            {
                fileSystem.File.Delete(temporaryPath);
            }
        }
        catch
        {
            // Config writes are best-effort — not a lot that can be done here.
        }
    }

    private T Load()
    {
        if (!fileSystem.File.Exists(fileName))
        {
            return new T();
        }

        var documentObject = JsonNode.Parse(fileSystem.File.ReadAllText(fileName))?.AsObject()
                             ?? throw new InvalidOperationException($"Cannot parse {fileName}");

        return (documentObject.AsObject().TryGetPropertyValue(sectionName, out var section)
            ? section.Deserialize<T>(serializerOptions) : null) ?? new T();
    }
}
