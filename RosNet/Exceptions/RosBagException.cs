using System;

namespace RosNet.Exceptions;

/// <summary>
/// Exception for when RosBag is corrupt or missing fields
/// </summary>
public class RosBagException : Exception
{
    public RosBagException(string message) : base(message) { }
}
