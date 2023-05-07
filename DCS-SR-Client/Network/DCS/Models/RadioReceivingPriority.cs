namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Network.DCS.Models;

public class RadioReceivingPriority
{
    public bool Decryptable;
    public byte Encryption;
    public double Frequency;
    public float LineOfSightLoss;
    public short Modulation;
    public double ReceivingPowerLossPercent;
    public DCSRadioInformation ReceivingRadio;

    public RadioReceivingState ReceivingState;
}