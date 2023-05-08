using System;
using System.Collections.Generic;
using Ciribob.DCS.SimpleRadio.Standalone.Common;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Helpers;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Network;
using Newtonsoft.Json;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Network.DCS.Models;

public class DCSPlayerRadioInfo
{
    //HOTAS or IN COCKPIT controls
    public enum RadioSwitchControls
    {
        HOTAS = 0,
        IN_COCKPIT = 1
    }

    public enum SimultaneousTransmissionControl
    {
        ENABLED_INTERNAL_SRS_CONTROLS = 1,
        EXTERNAL_DCS_CONTROL = 0
    }

    [JsonIgnore]
    public static readonly uint
        UnitIdOffset = 100000000; // this is where non aircraft "Unit" Ids start from for satcom intercom

    [JsonNetworkIgnoreSerialization] public DCSAircraftCapabilities capabilities = new();

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public RadioSwitchControls control = RadioSwitchControls.HOTAS;

    public DCSTransponder iff = new();

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public bool inAircraft = false;

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public bool intercomHotMic = false; //if true switch to intercom and transmit

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public DCSLatLngPosition latLng = new();

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public string name = "";

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public volatile bool ptt;

    public DCSRadioInformation[] radios = new DCSRadioInformation[11]; //10 + intercom

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public int seat;

    [JsonNetworkIgnoreSerialization] public short selected;

    [JsonNetworkIgnoreSerialization] [JsonDCSIgnoreSerialization]
    public bool
        simultaneousTransmission; // Global toggle enabling simultaneous transmission on multiple radios, activated via the AWACS panel

    [JsonNetworkIgnoreSerialization] public SimultaneousTransmissionControl simultaneousTransmissionControl =
        SimultaneousTransmissionControl.EXTERNAL_DCS_CONTROL;

    public string unit = "";

    public uint unitId;

    public DCSPlayerRadioInfo()
    {
        for (var i = 0; i < 11; i++) radios[i] = new DCSRadioInformation();
    }

    [JsonIgnore] public long LastUpdate { get; set; }

    public void Reset()
    {
        name = "";
        latLng = new DCSLatLngPosition();
        ptt = false;
        selected = 0;
        unit = "";
        simultaneousTransmission = false;
        simultaneousTransmissionControl = SimultaneousTransmissionControl.EXTERNAL_DCS_CONTROL;
        LastUpdate = 0;
        seat = 0;

        for (var i = 0; i < 11; i++) radios[i] = new DCSRadioInformation();
    }

    // override object.Equals
    public override bool Equals(object compare)
    {
        try
        {
            if (compare == null || GetType() != compare.GetType()) return false;

            var compareRadio = compare as DCSPlayerRadioInfo;

            if (control != compareRadio.control) return false;
            //if (side != compareRadio.side)
            //{
            //    return false;
            //}
            if (!name.Equals(compareRadio.name)) return false;
            if (!unit.Equals(compareRadio.unit)) return false;

            if (unitId != compareRadio.unitId) return false;

            if (inAircraft != compareRadio.inAircraft) return false;

            if (iff == null || compareRadio.iff == null) return false;

            //check iff
            if (!iff.Equals(compareRadio.iff)) return false;

            for (var i = 0; i < radios.Length; i++)
            {
                var radio1 = radios[i];
                var radio2 = compareRadio.radios[i];

                if (radio1 != null && radio2 != null)
                    if (!radio1.Equals(radio2))
                        return false;
            }
        }
        catch
        {
            return false;
        }


        return true;
    }


    /*
     * Was Radio updated in the last 10 Seconds
     */

    public bool IsCurrent()
    {
        return LastUpdate > DateTime.Now.Ticks - 100000000;
    }

    public DCSPlayerRadioInfo DeepClone()
    {
        var clone = (DCSPlayerRadioInfo)MemberwiseClone();

        clone.iff = iff.Copy();
        //ignore position
        clone.radios = new DCSRadioInformation[11];

        for (var i = 0; i < 11; i++) clone.radios[i] = radios[i].Copy();

        return clone;
    }
    
    
    //TODO this is a duplicate function - we need to merge this and get rid of it
    public DCSRadioInformation CanHearTransmission(double frequency,
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

        DCSRadioInformation bestMatchingRadio = null;
        RadioReceivingState bestMatchingRadioState = null;
        var bestMatchingDecryptable = false;

        for (var i = 0; i < radios.Length; i++)
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
                if (RadioInfo.FreqCloseEnough(receivingRadio.freq, frequency)
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