using System;
using System.Runtime.Serialization;

namespace Play.Identity.Service.Exceptions
{
    [Serializable]
    internal class InsufficientFundsException : Exception
    {
        private Guid userId;
        private decimal gil;
        public InsufficientFundsException(Guid userId, decimal gilToDebit) 
        : base($"Not enough gil to debit {gilToDebit} from user {userId}")
        {
        }

        public Guid UserId { get; }
        public decimal GilToDebit {get;}
    }
}