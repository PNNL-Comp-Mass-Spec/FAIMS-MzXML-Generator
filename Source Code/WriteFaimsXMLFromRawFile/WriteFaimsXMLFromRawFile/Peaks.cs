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
            var offset = new string(' ', spaceOffset);

            stringBuilder.AppendFormat(offset + "<peaks precision=\"{0}\"", Precision).AppendLine();
            stringBuilder.AppendFormat(offset + " byteOrder=\"{0}\"", ByteOrder).AppendLine();
            stringBuilder.AppendFormat(offset + " contentType=\"{0}\"", ContentType).AppendLine();
            stringBuilder.AppendFormat(offset + " compressionType=\"{0}\"", CompressionType).AppendLine();
            stringBuilder.AppendFormat(offset + " compressedLen=\"{0}\">{1}</peaks>", CompressedLen, EncodedData);

            return stringBuilder.ToString();
        }
    }
}
