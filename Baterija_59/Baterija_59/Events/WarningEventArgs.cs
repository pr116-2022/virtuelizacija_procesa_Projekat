using Baterija_59.Models;
using System;

namespace Baterija_59.Events
{
    public class WarningEventArgs : EventArgs
    {
        public EisMeta Meta { get; set; }
        public EisSample Sample { get; set; }
        public string WarningType { get; set; }
        public string Message { get; set; }

        public WarningEventArgs(EisMeta meta, EisSample sample, string warningType, string message)
        {
            Meta = meta;
            Sample = sample;
            WarningType = warningType;
            Message = message;
        }
    }
}