namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network.EventMessages;

public class VOIPStatusMessage
{
    public VOIPStatusMessage(bool con)
    {
        Connected = con;
    }

    public bool Connected { get; }
}