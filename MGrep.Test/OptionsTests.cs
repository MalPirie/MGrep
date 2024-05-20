using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Shouldly;

namespace MGrep.Test;

public class OptionsTests
{
    [Fact]
    public void GivenNoFileWhenLoadingThenReturnsDefaults()
    {
        var expected = new TestOptions
        {
            Text = null,
            Number = 0,
            Truth = false
        };
        var fileSystem = new MockFileSystem();
        var options = new Options<TestOptions>("Test", "test.settings", fileSystem);

        options.Value.ShouldBeEquivalentTo(expected);
    }

    [Fact]
    public void GivenFileWhenLoadingThenReturnsOptions()
    {
        var expected = new TestOptions
        {
            Text = "Testing",
            Number = 123,
            Truth = true
        };
        var root = AppContext.BaseDirectory;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            {
                Path.Combine(AppContext.BaseDirectory, "test.settings"),
                new MockFileData("{\"Test\":{\"Text\":\"Testing\",\"Number\":123,\"Truth\":true}}")
            }
        }, root);
        var options = new Options<TestOptions>("Test", "test.settings", fileSystem);

        options.Value.ShouldBeEquivalentTo(expected);
    }


    [Fact]
    public void GivenFileWithInvalidValuesWhenLoadingThenThrowsException()
    {
        var root = AppContext.BaseDirectory;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            {
                Path.Combine(AppContext.BaseDirectory, "test.settings"),
                new MockFileData("{\"Test\":{\"Text\":\"Testing\",\"Number\":\"Big\",\"Truth\":true}}")
            }
        }, root);

        var options = new Options<TestOptions>("Test", "test.settings", fileSystem);

        Should.Throw<JsonException>(() => options.Value.Number);
    }

    [Fact]
    public void GivenOptionsWhenUpdatingThenChangesFile()
    {
        var expected = """
                       {
                         "Test": {
                           "Text": "Testing",
                           "Number": 123,
                           "Truth": true
                         }
                       }
                       """;
        var root = AppContext.BaseDirectory;
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(), root);
        var options = new Options<TestOptions>("Test", "test.settings", fileSystem);

        options.Update(o =>
        {
            o.Text = "Testing";
            o.Number = 123;
            o.Truth = true;
        });

        var file = fileSystem.AllFiles.First(file => file.EndsWith("test.settings"));
        fileSystem.File.ReadAllText(file).ShouldBe(expected);
    }

    private class TestOptions
    {
        public string? Text { get; set; }
        public int Number { get; set; }
        public bool Truth { get; set; }
    }
}