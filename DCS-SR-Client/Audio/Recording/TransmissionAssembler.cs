//using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Recording
//{
//    internal class TransmissionAssembler
//    {
//        private readonly Dictionary<string, ClientTransmissionBuffer> _clientAudioBuffers;
//        private float[] _sampleRemainders;

//        public TransmissionAssembler()
//        {
//            _clientAudioBuffers = new Dictionary<string, ClientTransmissionBuffer>();
//            _sampleRemainders = new float[48000 * 2];
//        }

//        private float[] SplitRemainder(float[] sample)
//        {
//            (float[], float[]) splitArrays = AudioManipulationHelper.SplitSampleByTime(48000 * 2, sample);
//            float[] fullLengthRemainder = new float[48000 * 2];
//            splitArrays.Item2.CopyTo(fullLengthRemainder, 0);
//            _sampleRemainders = AudioManipulationHelper.MixSamplesClipped(_sampleRemainders, fullLengthRemainder, 48000 * 2);

//            return splitArrays.Item1;
//        }

//        public void AddTransmission(DeJitteredTransmission audio)
//        {
//            string guid = audio.OriginalClientGuid;

//            if (!_clientAudioBuffers.ContainsKey(guid))
//            {
//                _clientAudioBuffers.Add(guid, new ClientTransmissionBuffer());
//            }

//            _clientAudioBuffers[guid].AddSample(audio);
//        }

//        public float[] GetAssembledSample()
//        {
//            float[] finalFloatArray = new float[48000 * 2];
//            _sampleRemainders.CopyTo(finalFloatArray, 0);
//            _sampleRemainders = new float[48000 * 2];

//            foreach (var sample in _clientAudioBuffers.Values)
//            {
//                float[] clientPCM = sample.OutputPCM();
//                float[] trimmedClientPCM;
//                if (clientPCM.Length > 96000)
//                {
//                    trimmedClientPCM = SplitRemainder(clientPCM);
//                }
//                else
//                {
//                    trimmedClientPCM = new float[48000 * 2];
//                    clientPCM.CopyTo(trimmedClientPCM, 0);
//                }

//                finalFloatArray = AudioManipulationHelper.MixSamplesClipped(finalFloatArray, trimmedClientPCM, 48000 * 2);
//            }


//            return finalFloatArray;
//        }
//    }
//}
