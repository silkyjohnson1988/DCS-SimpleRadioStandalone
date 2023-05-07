namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network;

public class Radio
{
    public enum Modulation
    {
        AM = 0,
        FM = 1,
        INTERCOM = 2,
        DISABLED = 3,
        HAVEQUICK = 4,
        SATCOM = 5,
        MIDS = 6
    }
    
    public bool enc; // encryption enabled
    public byte encKey;
    public double freq = 1;
    public double secFreq = 1;
    public Modulation modulation = Modulation.DISABLED;

}