using System;
using System.Collections.Generic;

namespace JSONASMXConnector.Models
{
    public class Quotation
    {
        public int QuotationId;
        public DateTime DateCreated;
        public DateTime? DateOrdered; //orders no longer on LQ, so, always null

        public CircuitDetails CircuitDetails;

        public List<Item> AdditionalProperties;

        public List<QuotationLine> QuoteOptions;

        public string ErrorMessage;
    }

    public class QuotationLine
    {
        public CircuitDetails CircuitDetails;

        public int QuotationId;
        public int QuotationLineId;

        public DateTime DateCreated;

        public decimal? AEndMainLinkDistance, BEndMainLinkDistance;

        public string AEndAccessType, BEndAccessType;
        public string AEndProductName, BEndProductName, OnNetProductName;

        public string NNIEthernetBearerReference;
        public string AEndPoPPostcode, BEndPoPPostcode, AEndPoPNodeReference, BEndPoPNodeReference;

        public decimal SetupPrice, AnnualPrice;

        public List<string> AdditionalServices;

        public List<Item> AdditionalProperties;

        public string ErrorMessage;
    }


    public class Item
    {
        public string Name, Value;
        public Item(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public Item()
        {

        }
    }
}