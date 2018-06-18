using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WriteFaimsXMLFromRawFile
{
    public class Index
    {
        public int scanNumber;
        public int byteDepth;

        public Index(int scanNumber, int byteDepth)
        {
            this.scanNumber = scanNumber;
            this.byteDepth = byteDepth;
        }
    }
}
