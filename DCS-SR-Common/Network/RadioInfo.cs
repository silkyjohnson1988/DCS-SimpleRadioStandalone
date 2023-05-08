using System;
using System.Collections.Generic;

namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Network;

public class RadioInfo
{
    public long LastUpdate { get; set; }

    public List<Radio> radios { get; set; } = new List<Radio>();

    public uint unitId { get; set; }

    public Transponder iff { get; set; }

    public string unit { get; set; }
    
    
    //comparing doubles is risky - check that we're close enough to hear (within 100hz)
    public static bool FreqCloseEnough(double freq1, double freq2)
    {
        var diff = Math.Abs(freq1 - freq2);

        return diff < 500;
    }

    public Radio CanHearTransmission(double frequency,
        Radio.Modulation modulation,
        byte encryptionKey,
        bool strictEncryption,
        uint sendingUnitId,
        List<int> blockedRadios,
        out RadioReceivingState receivingState,
        out bool decryptable)
    {
        //    if (!IsCurrent())
        //     {
        //         receivingState = null;
        //        decryptable = false;
        //       return null;
        //   }

        Radio bestMatchingRadio = null;
        RadioReceivingState bestMatchingRadioState = null;
        var bestMatchingDecryptable = false;

        for (var i = 0; i < radios.Count; i++)
        {
            var receivingRadio = radios[i];

            if (receivingRadio != null)
            {
                //handle INTERCOM Modulation is 2
                if (receivingRadio.modulation == Radio.Modulation.INTERCOM &&
                    modulation == Radio.Modulation.INTERCOM)
                {
                    if (unitId > 0 && sendingUnitId > 0
                                   && unitId == sendingUnitId)
                    {
                        receivingState = new RadioReceivingState
                        {
                            IsSecondary = false,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        decryptable = true;
                        return receivingRadio;
                    }

                    decryptable = false;
                    receivingState = null;
                    return null;
                }

                if (modulation == Radio.Modulation.DISABLED
                    || receivingRadio.modulation == Radio.Modulation.DISABLED)
                    continue;

                //within 1khz
                if (FreqCloseEnough(receivingRadio.freq, frequency)
                    && receivingRadio.modulation == modulation
                    && receivingRadio.freq > 10000)
                {
                    var isDecryptable = (receivingRadio.enc ? receivingRadio.encKey : 0) == encryptionKey ||
                                        (!strictEncryption && encryptionKey == 0);

                    if (isDecryptable && !blockedRadios.Contains(i))
                    {
                        receivingState = new RadioReceivingState
                        {
                            IsSecondary = false,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        decryptable = true;
                        return receivingRadio;
                    }

                    bestMatchingRadio = receivingRadio;
                    bestMatchingRadioState = new RadioReceivingState
                    {
                        IsSecondary = false,
                        LastReceviedAt = DateTime.Now.Ticks,
                        ReceivedOn = i
                    };
                    bestMatchingDecryptable = isDecryptable;
                }

                if (receivingRadio.secFreq == frequency
                    && receivingRadio.secFreq > 10000)
                {
                    if ((receivingRadio.enc ? receivingRadio.encKey : 0) == encryptionKey ||
                        (!strictEncryption && encryptionKey == 0))
                    {
                        receivingState = new RadioReceivingState
                        {
                            IsSecondary = true,
                            LastReceviedAt = DateTime.Now.Ticks,
                            ReceivedOn = i
                        };
                        decryptable = true;
                        return receivingRadio;
                    }

                    bestMatchingRadio = receivingRadio;
                    bestMatchingRadioState = new RadioReceivingState
                    {
                        IsSecondary = true,
                        LastReceviedAt = DateTime.Now.Ticks,
                        ReceivedOn = i
                    };
                }
            }
        }

        decryptable = bestMatchingDecryptable;
        receivingState = bestMatchingRadioState;
        return bestMatchingRadio;
    }

}