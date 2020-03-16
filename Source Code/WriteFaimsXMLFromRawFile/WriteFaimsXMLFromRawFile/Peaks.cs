using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    class Peaks
    {
        public int precision;
        public string byteOrder;
        public string contentType;
        public string compressionType;
        public int compressedLen;
        public string encodedData;

        public Peaks(int precision, string byteOrder, string contentType, string compressionType, int compressedLen, string encodedData)
        {
            this.precision = precision;
            this.byteOrder = byteOrder;
            this.contentType = contentType;
            this.compressionType = compressionType;
            this.compressedLen = compressedLen;
            this.encodedData = encodedData;
        }

        public string ToXML(int spaceOffset)
        {
            var stringBuilder = new StringBuilder();
            // define spacer offset
            var offset = "";
            for (var i = 0; i < spaceOffset; i++)
            {
                offset += " ";
            }

            stringBuilder.AppendLine(offset + "<peaks precision=\"" + precision + "\"");
            stringBuilder.AppendLine(offset + " byteOrder=\"" + byteOrder + "\"");
            stringBuilder.AppendLine(offset + " contentType=\"" + contentType + "\"");
            stringBuilder.AppendLine(offset + " compressionType=\"" + compressionType + "\"");
            stringBuilder.Append(offset + " compressedLen=\"" + compressedLen + "\"" + " >" + encodedData + "</peaks>");

            return stringBuilder.ToString();
        }
    }
}
