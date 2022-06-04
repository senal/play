using System;
using System.Runtime.Serialization;

namespace Play.Inventory.Service.Exceptions
{
    [Serializable]
    internal class UnknownItemException : Exception
    {

        public UnknownItemException(Guid itemId)
        :base($"Unknown ItemId {itemId}")
        {
            this.ItemId = itemId;
        }
        
        public Guid ItemId { get;}
        
        
    }
}