using System;

namespace RosNet.Exceptions;

/// <summary>
/// Exception for when datatype is not defined as primitivetype or in messagedefinition
/// </summary>
/// <param name="message">Message which explains the cause</param>
/// <param name="dataType">The datatype which was found to be invalid</param>
public class InvalidDataTypeException : Exception
{
    public string DataType { get; }

    public InvalidDataTypeException(string message, string dataType) : base(message) { }
}
