using System.Runtime.Serialization;

namespace Baterija_59.Faults
{
    [DataContract]
    public class DataFormatFault
    {
            [DataMember]
            public string Message { get; set; }

            [DataMember]
            public string FileName { get; set; }

            [DataMember]
            public int RowIndex { get; set; }

            public DataFormatFault()
            {
            }

            public DataFormatFault(string message, string fileName, int rowIndex)
            {
                Message = message;
                FileName = fileName;
                RowIndex = rowIndex;
            }
        }
}
