using System.Collections.Generic;

namespace TOCTransfomer
{
    internal class OutputRow
    {
        public static readonly IReadOnlyDictionary<string, string> OUTPUT_TYPES = new Dictionary<string, string>()
        {
            {"Name", "utDynChar" },
            {"PosLongitude", "utDouble" },
            {"PosLatitude", "utDouble" },
            {"PosErrorDirection", "utDouble" },
            {"PosErrorLambda1", "utDouble" },
            {"PosErrorLambda2", "utDouble" },
            {"Power", "utDouble" },
            {"IsDirected", "utUTInt" },
            {"Direction", "utUSInt" },
            {"UniqueId", "utULInt" },
            {"BaseIndex", "utUSInt" },
            {"Channel", "utULInt" },
            {"FreqOff", "utUSInt" },
            {"MNC", "utUSInt" },
            {"MCC", "utUSInt" },
            {"LAC", "utUSInt" },
            {"LAC_Hex", "utDynChar" },
            {"CELL_NE_ID", "utDynChar" },
            {"SITE_BS_TYPE", "utDynChar" },
            {"ANTENNA_SYSTEM", "utDynChar" }
        };

        public string Name { get; set; }
        public float PosLongitude { get; set; }
        public float PosLatitude { get; set; }
        public float PosErrorDirection { get; set; } = 0;
        public float PosErrorLambda1 { get; set; } = 0;
        public float PosErrorLambda2 { get; set; } = 0;
        public float? Power { get; set; }
        public int IsDirected { get; set; }
        public int Direction { get; set; }
        public int UniqueId { get; set; } = 0;
        public int BaseIndex { get; set; } = 3;
        public int Channel { get; set; }
        public int FreqOff { get; set; } = 3;
        public int MNC { get; set; } = 1001;
        public int MCC { get; set; } = 262;
        public int LAC { get; set; }
        public string LAC_Hex { get; set; }
        public string CELL_NE_ID { get; set; }
        public string SITE_BS_TYPE { get; set; }
        public string ANTENNA_SYSTEM { get; set; }
    }
}
