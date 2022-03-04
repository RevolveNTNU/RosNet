using System;

namespace RosNet.Exceptions;

/// <summary>
/// Exception for when there are required header fields that are missing
/// </summary>
/// <param name="message">Message which explains the cause</param>
/// <param name="missingHeaderFields">List of the missing header fields</param>
public class MissingHeaderFieldException : Exception
{
    public List<string> MissingHeaderFields { get; }

    public MissingHeaderFieldException(string message, List<string> missingHeaderFields) : base(message)
    {
        MissingHeaderFields = missingHeaderFields;
    }
}
