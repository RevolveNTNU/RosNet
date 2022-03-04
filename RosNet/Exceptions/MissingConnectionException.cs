using System;
using RosNet.DataModel;

namespace RosNet.Exceptions;

/// <summary>
/// Exception for when there are messages in the bag without corresponding connection
/// </summary>
/// <param name="message">Message which explains the cause</param>
/// <param name="messages">List of the messages without the corresponding connection and the message data as bytearray</param>
public class MissingConnectionException : Exception
{
    public List<(Message, byte[])>> Messages { get; }

    public MissingConnectionException(string message, List<(Message, byte[])>>  messages) : base(message)
    {
        Messages = messages;
    }
}
