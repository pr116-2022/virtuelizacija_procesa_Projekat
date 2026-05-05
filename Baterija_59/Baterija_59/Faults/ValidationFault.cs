using System.Runtime.Serialization;

namespace Baterija_59.Faults
{
    [DataContract]
    public class ValidationFault
    {
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string FieldName { get; set; }

        public ValidationFault()
        {
        }

        public ValidationFault(string message, string fieldName)
        {
            Message = message;
            FieldName = fieldName;
        }
    }
}