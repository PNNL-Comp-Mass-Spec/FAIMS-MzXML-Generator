using System.Collections.Generic;
using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    public static class ByteVariables
    {
        public static int byteDepth = 0;
        public static Encoding encoder = Encoding.ASCII;
        public static List<Index> scanOffsets = new List<Index>();
        public static int currentScan = 1;
    }
}
