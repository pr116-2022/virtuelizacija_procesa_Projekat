using System.Runtime.Serialization;

namespace Baterija_59.Models
{
    [DataContract]
    public class AckResponse
    {
        [DataMember]
        public bool IsAck { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public TransferStatus Status { get; set; }

        public AckResponse()
        {

        }

        public AckResponse(bool isAck, string message, TransferStatus status)
        {
            IsAck = isAck;
            Message = message;
            Status = status;
        }

    }
}
