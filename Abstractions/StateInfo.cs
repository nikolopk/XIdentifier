using System.Collections.Generic;

namespace Abstractions
{
    public class StateInfo
    {
        public enum TransmissionState
        {
            TransmitPublicKey,
            TransmitX,
            RequestX,
            TransmitB,
            TransmitY,
            TransmitR,
            TransmitN,
            Exit
        }

        public TransmissionState State { get; set; }
        public List<DataEntry> DataEntries { get; set; }
    }
}
