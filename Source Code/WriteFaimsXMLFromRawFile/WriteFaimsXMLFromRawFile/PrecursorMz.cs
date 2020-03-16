
namespace WriteFaimsXMLFromRawFile
{
    class PrecursorMz
    {
        public double precursorIntensity;
        public string activationMethod;
        public double precursorMz;

        public PrecursorMz(double precursorIntensity, string activationMethod, double precursorMz)
        {
            this.precursorIntensity = precursorIntensity;
            this.activationMethod = activationMethod;
            this.precursorMz = precursorMz;
        }

        public string ToXML()
        {
            return "    <precursorMz precursorIntensity=\"" + precursorIntensity + "\" activationMethod=\"" + activationMethod + "\" >" + precursorMz + "</precursorMz>";
        }
    }
}
