namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network.EventMessages;

public class SRClientUpdateMessage
{
    public SRClientUpdateMessage(SRClient srClient, bool connected = true)
    {
        SrClient = srClient;
        Connected = connected;
    }

    public SRClient SrClient { get; }
    public bool Connected { get; }
}