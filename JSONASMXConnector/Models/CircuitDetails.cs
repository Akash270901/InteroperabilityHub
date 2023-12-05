using System.Collections.Generic;
using System.ComponentModel;
using System;

namespace JSONASMXConnector.Models
{
    [Serializable]
    public class CircuitDetails
    {
        public StringSplitOptions ProductType;

        public int? CustomerId;
        public string CRMReference;

        public int? AEndNNIId, ShadowVlanNNIId;
        public string AEndPostcode, BEndPostcode;

        public List<PortAndBandwidth> PortAndBandwidths;

        public List<int> TermLengthInMonths;

        public int? AEndFloor, BEndFloor;
        public int? AEndPoPId, BEndPoPId;

        public List<LocationIdentifier> AEndLocationIdentifiers, BEndLocationIdentifiers;

        [DefaultValue(false)]
        public bool AEndExcludeNeosOnnet, BEndExcludeNeosOnnet;
        [DefaultValue(false)]
        public bool IsDiverse, IsManagedDIA;
        [DefaultValue(true)]
        public bool InstallAmortisation;

        public List<string> ChosenAccessTypes;

        public List<string> AdditionalServices;

        public List<string> CloudConnectOptions;
    }

    public class PortAndBandwidth
    {
        public string AEndPort, BEndPort;
        public string Bandwidth;
        internal int? BandwidthInt { get => int.TryParse(Bandwidth, out int b) ? (int?)b : null; }
    }
    public class LocationIdentifier
    {
        public string Identifier;
        public string Descriptor;
    }
}