using System;

namespace Example.NServiceBus.Messages
{
    public class DataResponseMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
    }
}
