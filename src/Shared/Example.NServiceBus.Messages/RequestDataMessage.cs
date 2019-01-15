using System;
using System.Collections.Generic;
using System.Text;

namespace Example.NServiceBus.Messages
{
    public class RequestDataMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
    }
}
