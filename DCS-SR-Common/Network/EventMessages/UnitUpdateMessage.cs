namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network.EventMessages;

public class UnitUpdateMessage
{
    private SRClient _unitUpdate;

    public SRClient UnitUpdate
    {
        get => _unitUpdate;
        set
        {
            if (value == null)
            {
                _unitUpdate = null;
            }
            else
            {
                //TODO fix clone
                // var clone = value.DeepClone();
                // _unitUpdate = clone;
                _unitUpdate = value;
            }
        }
    }

    public bool FullUpdate { get; set; }
}