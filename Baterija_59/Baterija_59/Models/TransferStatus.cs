using System.Runtime.Serialization;

namespace Baterija_59.Models
{
    [DataContract]
    public enum TransferStatus
    {
        [EnumMember]
        IN_PROGRESS = 0,

        [EnumMember]
        COMPLETED = 1,

        [EnumMember]
        FAILED = 2
    }
}
