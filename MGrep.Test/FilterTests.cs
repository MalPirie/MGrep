using Shouldly;

namespace MGrep.Test;

public class FilterTests
{
    [Theory]
    [InlineData("Watch the flow", false)]
    [InlineData("Look at the pattern", true)]
    [InlineData("LOOK AT THE PATTERN", true)]
    public void Nothing(string line, bool expected)
    {
        var filter = new Filter(false, false, false, "pat");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("Watch the flow", false)]
    [InlineData("Look at the pattern", true)]
    [InlineData("LOOK AT THE PATTERN", false)]
    public void MatchCase(string line, bool expected)
    {
        var filter = new Filter(true, false, false, "pat");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pat", true)]
    [InlineData("PAT", true)]
    [InlineData("pat the cat", true)]
    [InlineData("the cat was pat", true)]
    [InlineData("Look at the pattern", false)]
    [InlineData("The pattern is pat", true)]
    public void MatchWholeWord(string line, bool expected)
    {
        var filter = new Filter(false, true, false, "pat");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pat", true)]
    [InlineData("PAT", false)]
    [InlineData("pat the cat", true)]
    [InlineData("the cat was pat", true)]
    [InlineData("Look at the pattern", false)]
    public void MatchCaseMatchWholeWorld(string line, bool expected)
    {
        var filter = new Filter(true, true, false, "pat");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pat", false)]
    [InlineData("pat the cat", false)]
    [InlineData("the cat was pats", true)]
    [InlineData("the cat was Pats", true)]
    [InlineData("Look at the pattern", true)]
    public void UseRegex(string line, bool expected)
    {
        var filter = new Filter(false, false, true, "pat[ts]");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pat", false)]
    [InlineData("pat the cat", false)]
    [InlineData("the cat was pats", true)]
    [InlineData("the cat was Pats", false)]
    [InlineData("Look at the pattern", true)]
    public void MatchCaseUseRegex(string line, bool expected)
    {
        var filter = new Filter(true, false, true, "pat[ts]");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pats", true)]
    [InlineData("PATS", true)]
    [InlineData("pat the cat", false)]
    [InlineData("the cat was pats", true)]
    [InlineData("the cat was Pats", true)]
    [InlineData("Look at the pattern", false)]
    public void MatchWholeWordUseRegex(string line, bool expected)
    {
        var filter = new Filter(false, true, true, "pat[ts]");
        ActAndAssert(filter, line, expected);
    }

    [Theory]
    [InlineData("pats", true)]
    [InlineData("PATS", false)]
    [InlineData("pat the cat", false)]
    [InlineData("the cat was pats", true)]
    [InlineData("the cat was Pats", false)]
    [InlineData("Look at the pattern", false)]
    public void MatchCaseMatchWholeWordUseRegex(string line, bool expected)
    {
        var filter = new Filter(true, true, true, "pat[ts]");
        ActAndAssert(filter, line, expected);
    }

    private void ActAndAssert(Filter filter, string line, bool expected)
    {
        var actual = filter.IsMatch(line);

        actual.ShouldBe(expected);
    }

}