using System.Collections.Generic;
using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    public class ByteVariables
    {
        public int ByteDepth { get; set; }
        public Encoding Encoder { get; }
        public List<Index> ScanOffsets { get; set; }
        public int CurrentScan { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ByteVariables() : this(Encoding.ASCII)
        {
        }

        /// <summary>
        /// Constructor that accepts an encoder
        /// </summary>
        public ByteVariables(Encoding encoder)
        {
            Encoder = encoder;
            ScanOffsets = new List<Index>();
            Reset();
        }

        public void Reset()
        {
           ByteDepth = 0;
           ScanOffsets.Clear();
           CurrentScan = 1;
        }
}
}
