using Baterija_59.Models;
using System;

namespace Baterija_59.Events
{
    public class SampleReceivedEventArgs : EventArgs
    {
        public EisMeta Meta { get; set; }
        public EisSample Sample { get; set; }

        public SampleReceivedEventArgs(EisMeta meta, EisSample sample)
        {
            Meta = meta;
            Sample = sample;
        }
    }
}