using Newtonsoft.Json;

namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network;

public class LatLngPosition
{
    public double alt;
    public double lat;
    public double lng;
    public bool IsValid()
    {
        return alt != 0 &&
               lat != 0
               && lng != 0;
    }
}