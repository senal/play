using System;
using System.Runtime.Serialization;

namespace Play.Identity.Service.Exceptions
{
    [Serializable]
    internal class UnknownUserException : Exception
    {
        private Guid userId;

        public UnknownUserException(Guid userId)
        : base($"Unknown user '{userId}'")
        {
            this.userId = userId;
        }
        public Guid UserId { get;}
   }
}