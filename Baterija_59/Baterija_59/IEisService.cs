using Baterija_59.Faults;
using Baterija_59.Models;
using System.ServiceModel;

namespace Baterija_59
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface IEisService
    {
        [OperationContract(IsInitiating = true)]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse StartSession(EisMeta meta);

        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse PushSample(EisSample sample);

        [OperationContract(IsTerminating = true)]
        [FaultContract(typeof(ValidationFault))]
        [FaultContract(typeof(DataFormatFault))]
        AckResponse EndSession();
    }
}