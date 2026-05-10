using Baterija_59.Models;
using System;

namespace Baterija_59.Events
{
    public class TransferEventArgs : EventArgs
    {
        public EisMeta Meta { get; set; }

        public TransferEventArgs(EisMeta meta)
        {
            Meta = meta;
        }
    }
}