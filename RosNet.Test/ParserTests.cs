using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using RosNet.MessageGeneration;

using Xunit;

namespace RosNet.Test;

using Messages = List<List<MessageGeneration.Field>>;
public class ParserTests
{
    public static string GetTestPath(string relativePath)
    {
        var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
        var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
        var dirPath = Path.GetDirectoryName(codeBasePath)!;
        return Path.Combine(dirPath, "TestMessages", relativePath);
    }

    private static MessageGeneration.Field Field(string type, string identifier, string? value = null, IEnumerable<string>? initialComments = null, string? trailingComment = null)
    {
        var split = type.Split('/', 2);
        var package = split.Length == 2 ? split.First() : null;
        var ic = initialComments ?? Enumerable.Empty<string>();
        return value != null
                ? new Constant(identifier, split.Last(), value, ic, trailingComment)
                : new MessageGeneration.Field(identifier, split.Last(), ic, trailingComment, package);
    }

    private static MessageGeneration.Field ArrayField(string type, string identifier, uint? length)
    {
        var split = type.Split('/', 2);
        var package = split.Length == 2 ? split.First() : null;
        return new ArrayField(identifier, split.Last(), Enumerable.Empty<string>(), null, package, length);
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
                Field("uint", "seq", null, new List<string>(){"Standard metadata for higher-level flow data types", "sequence ID: consecutively increasing ID"}),
                Field("Time", "stamp", null, new List<string>() {
                        "Two-integer timestamp that is expressed as:",
                        " * stamp.secs: seconds (stamp_secs) since epoch",
                        " * stamp.nsecs: nanoseconds since stamp_secs",
                        " time-handling sugar is provided by the client library"}),
                Field("string", "frame_id", null, new List<string>(){"Frame this data is associated with"}, " this is the frame_id"),
            },
        } },
        new object[] { "headerAfterConst.msg", new Messages {
            new() {
                Field("sbyte", "DEBUG", "1"),
                Field("sbyte", "INFO", "2"),
                Field("sbyte", "WARN", "4"),
                Field("sbyte", "ERROR", "8"),
                Field("sbyte", "FATAL", "16"),
                Field("std_msgs/Header", "header"),
                Field("sbyte", "level"),
                ArrayField("string", "topics", null),
            },
        } },
    };

    [Theory]
    [MemberData(nameof(TokenizerTestData))]
    internal void TestParserWorksAsExpected(string messageFile, IEnumerable<IEnumerable<MessageGeneration.Field>> expected)
    {
        var test = File.ReadAllText(GetTestPath(messageFile));

        var result = MessageParser.ParseFile(test);

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
        var test = () => MessageParser.ParseFile(msg).Select(Enumerable.ToList).ToList();

        Assert.Throws<MessageParserException>(test);
    }
}

internal class FieldComparer : IEqualityComparer<MessageGeneration.Field>
{
    public bool Equals(MessageGeneration.Field? x, MessageGeneration.Field? y)
    {
        if (x is null || y is null)
        {
            return false;
        }
        var nameEqual = x.Name == y.Name;
        var typeEqual = (x.Type + x.Package) == (y.Type + y.Package);
        var lcEq = x.LeadingComments.SequenceEqual(y.LeadingComments);
        var ltEq = x.TrailingComment == y.TrailingComment;
        return typeEqual && nameEqual && lcEq && ltEq && (x, y) switch
        {
            (Constant { Value: var xv }, Constant { Value: var yv }) => xv == yv,
            (ArrayField { ArraySize: var xas }, ArrayField { ArraySize: var yas }) => xas == yas,
            _ when x.GetType() != y.GetType() => false,
            _ => true,
        };
    }

    public int GetHashCode([DisallowNull] MessageGeneration.Field obj) => obj.GetHashCode();
}

internal class MessageComparer : IEqualityComparer<IEnumerable<MessageGeneration.Field>>
{
    public bool Equals(IEnumerable<MessageGeneration.Field>? x, IEnumerable<MessageGeneration.Field>? y) => x is not null && y is not null && x.SequenceEqual(y, new FieldComparer());

    public int GetHashCode([DisallowNull] IEnumerable<MessageGeneration.Field> obj) => obj.GetHashCode();
}
