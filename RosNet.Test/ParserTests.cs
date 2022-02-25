using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using RosNet.MessageGeneration;

using Xunit;

namespace RosNet.Test;

using Messages = List<List<MessageGeneration.Field>>;
public class TokenizerTests
{
    public static string GetTestPath(string relativePath)
    {
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var dirPath = Path.GetDirectoryName(codeBasePath)!;
        return Path.Combine(dirPath, "TestMessages", relativePath);
    }

    private static MessageGeneration.Field Field(string type, string identifier, string? value = null)
    {
        var split = type.Split('/', 2);
        var package = split.Length == 2 ? split.First() : null;
        return value != null 
                ? new ConstField(identifier, split.Last(), value, Enumerable.Empty<string>(), Enumerable.Empty<string>()) 
                : new MessageGeneration.Field(identifier, split.Last(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), package);
    }

    private static MessageGeneration.Field ArrayField(string type, string identifier, uint? length)
    {
        var split = type.Split('/', 2);
        var package = split.Length == 2 ? split.First() : null;
        return new ArrayField(identifier, split.Last(), Enumerable.Empty<string>(), Enumerable.Empty<string>(), package, length);
    }


    // Lost typing is required from XUnit's side
    public static IEnumerable<object[]> TokenizerTestData => new List<object[]> {
        new object[] { "basic.msg", new Messages {
            new() {
                Field("uint", "number"),
            },
        } },
        new object[] { "multiline.msg", new Messages {
            new() {
                Field("std_msgs/Header", "header"),
                Field("float", "FL_vel"),
                Field("float", "FR_vel"),
                Field("float", "RL_vel"),
                Field("float", "RR_vel"),
            },
        } },
        new object[] { "constants.msg", new Messages {
            new() {
                Field("int", "X", "123"),
                Field("int", "Y","-123"),
                Field("string", "FOO", "\"foo\""),
                Field("string", "EXAMPLE", "\"\"#comments\" are ignored, and leading and trailing whitespace removed\""),
            },
        } },
        new object[] { "service.srv", new Messages {
            new() {
                Field("uint", "number"),
            },
            new() {
                Field("uint", "number"),
            },
        } },
        new object[] { "array.msg", new Messages {
            new() {
                ArrayField("uint", "fixedLength", 10),
                ArrayField("uint", "flexi", null),
            },
        } },
        new object[] { "empty.msg", new Messages {
            new(),
        } },
        new object[] { "emptyStringConst.msg", new Messages {
            new() {
                Field("string", "haha", "\"\""),
            },
        } },
        new object[] { "commented.msg", new Messages {
            new() {
                Field("uint", "seq"),
                Field("Time", "stamp"),
                Field("string", "frame_id"),
            },
        } },
    };

    [Theory]
    [MemberData(nameof(TokenizerTestData))]
    internal void TestTokenizerWorksAsExpected(string messageFile, IEnumerable<IEnumerable<MessageGeneration.Field>> expected)
    {
        var test = File.ReadAllText(GetTestPath(messageFile));

        // explicitly ignore comments
        var result = MessageParser.Parse(test);

        Assert.Equal(expected, result, new MessageComparer());
    }

    [Theory]
    [InlineData("bad.msg")]
    [InlineData("whitespaceBetween=.msg")]
    [InlineData("incomplete.msg")]
    [InlineData("badSeparator.msg")]
    [InlineData("goodAndBad.msg")]
    [InlineData("hangingArray.msg")]
    [InlineData("emptyIntConst.msg")]
    public void TestParseFails(string badFile)
    {
        var msg = File.ReadAllText(GetTestPath(badFile));

        // ToList required to evaluate the `IEnumerable`
        var test = () => MessageParser.Parse(msg).ToList();

        Assert.Throws<MessageParserException>(test);
    }
}

internal class FieldComparer : IEqualityComparer<MessageGeneration.Field>
{
    /// We want to ignore the position-specifiers (and comments) that comes with Sprache's position-awareness, makes test-data noisy
    public bool Equals(MessageGeneration.Field? x, MessageGeneration.Field? y)
    {
        var xn = x?.Name;
        var yn = y?.Name;
        var xt = x?.Type + x?.Package;
        var yt = y?.Type + y?.Package;
        return (x, y) switch
        {
            (ConstField { ConstantDeclaration: var xv }, ConstField { ConstantDeclaration: var yv }) => xt == yt && xv == yv && xn == yn,
            (ConstField, MessageGeneration.Field) or (MessageGeneration.Field, ConstField) => false,
            (
                ArrayField { ArraySize: var xas },
                ArrayField { ArraySize: var yas }
            ) => xt == yt && xas == yas && xn == yn,
            (MessageGeneration.Field, MessageGeneration.Field) => xt == yt && xn == yn,
            _ => x == y,
        };
    }

    public int GetHashCode([DisallowNull] MessageGeneration.Field obj) => obj.GetHashCode();
}

internal class MessageComparer : IEqualityComparer<IEnumerable<MessageGeneration.Field>>
{
    public bool Equals(IEnumerable<MessageGeneration.Field>? x, IEnumerable<MessageGeneration.Field>? y) => x is not null && y is not null && x.SequenceEqual(y, new FieldComparer());

    public int GetHashCode([DisallowNull] IEnumerable<MessageGeneration.Field> obj) => obj.GetHashCode();
}
