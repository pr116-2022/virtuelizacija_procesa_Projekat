using System.Runtime.Serialization;

namespace Baterija_59.Faults
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember]
        public string Message { get; set; }

        public DataFormatFault()
        {
        }

        public DataFormatFault(string message)
        {
            Message = message;
        }
    }
}
