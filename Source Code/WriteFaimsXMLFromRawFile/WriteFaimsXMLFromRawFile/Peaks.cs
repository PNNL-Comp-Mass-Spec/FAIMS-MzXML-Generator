using System.Text;

namespace WriteFaimsXMLFromRawFile
{
    internal class Peaks
    {
        public int Precision { get; set; }
        public string ByteOrder { get; set; }
        public string ContentType { get; set; }
        public string CompressionType { get; set; }
        public int CompressedLen { get; set; }
        public string EncodedData { get; set; }

        public Peaks(int precision, string byteOrder, string contentType, string compressionType, int compressedLen, string encodedData)
        {
            Precision = precision;
            ByteOrder = byteOrder;
            ContentType = contentType;
            CompressionType = compressionType;
            CompressedLen = compressedLen;
            EncodedData = encodedData;
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
            stringBuilder.Append(offset + " compressedLen=\"" + compressedLen + "\"" + ">" + encodedData + "</peaks>");

            return stringBuilder.ToString();
        }
    }
}
