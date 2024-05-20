namespace MGrep.Test;

internal class MockFileFilter : IFileFilter
{
    private readonly IEnumerable<string> files;

    public MockFileFilter(IEnumerable<string> files)
    {
        this.files = files;
    }

    public void AddFilePatterns(IEnumerable<string> filePatterns)
    {
    }

    public IEnumerable<string> Execute() => files;
}