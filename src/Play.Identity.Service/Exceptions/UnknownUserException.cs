using System;
using System.Runtime.Serialization;

namespace Play.Identity.Service.Exceptions;

[Serializable]
internal class UnknownUserException : Exception
{
    public Guid UserId { get; init; }


    public UnknownUserException(Guid userId) : base($"'")
    {
        this.UserId = userId;
    }
}