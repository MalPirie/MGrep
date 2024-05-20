using System;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MGrep;

public sealed class Options<T> where T : class, new()
{
    private readonly IFileSystem fileSystem;
    private readonly string sectionName;
    private readonly string fileName;
    private T? value;
    private readonly JsonSerializerOptions serializerOptions;

    public Options(string sectionName, string fileName) : this(sectionName, fileName, new FileSystem())
    { }

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

    public T Value => value ??= Load();

    public void Update(Action<T> applyChanges)
    {
        try
        {
            var documentObject = fileSystem.File.Exists(fileName)
                ? JsonNode.Parse(fileSystem.File.ReadAllText(fileName))?.AsObject() ?? throw new InvalidOperationException($"Cannot parse {fileName}")
                : new JsonObject();

            var sectionObject = (documentObject.AsObject().TryGetPropertyValue(sectionName, out var section)
                ? section.Deserialize<T>(serializerOptions) : null) ?? new T();

            applyChanges(sectionObject);
            value = sectionObject;

            var temporaryPath = fileSystem.Path.Combine(fileSystem.Path.GetDirectoryName(fileName)!, fileSystem.Path.GetRandomFileName());
            if (fileSystem.File.Exists(fileName))
            {
                fileSystem.File.Move(fileName, temporaryPath);
            }

            using var stream = fileSystem.File.OpenWrite(fileName);
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = true
            });
            documentObject[sectionName] = JsonSerializer.SerializeToNode(sectionObject, serializerOptions);
            documentObject.WriteTo(writer);
            fileSystem.File.Delete(temporaryPath);
        }
        catch
        {
            // Not a lot that can be done here, so suck it up.
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