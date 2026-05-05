using System.Runtime.Serialization;

namespace Baterija_59.Models
{
    [DataContract]
    public class EisMeta
    {

        [DataMember]
        public string BatteryId { get; set; }

        [DataMember]
        public string TestId { get; set; }

        [DataMember]
        public int SocPercent { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public int TotalRows { get; set; }
    }
}
