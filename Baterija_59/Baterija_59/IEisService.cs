using Baterija_59.Faults;
using Baterija_59.Models;
using System.ServiceModel;

namespace Baterija_59
{
    [ServiceContract]
    public interface IEisService
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse StartSession(EisMeta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse PushSample(EisSample sample);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse EndSession();
    }
}
