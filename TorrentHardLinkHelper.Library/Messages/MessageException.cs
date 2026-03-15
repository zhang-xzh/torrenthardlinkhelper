using System;
using System.Runtime.Serialization;

namespace TorrentHardLinkHelper.Messages;

public class MessageException : Exception
{
    public MessageException()
    {
    }


    public MessageException(string message)
        : base(message)
    {
    }


    public MessageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }


    public MessageException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}