using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Recording
{
    internal class ClientTransmissionBuffer
    {
        private List<LinkedList<DeJitteredTransmission>> _clientAudioSamples;
        private long lastAccess;
        readonly int _sampleRate = 48000; 
            
        public ClientTransmissionBuffer()
        {
            _clientAudioSamples = new List<LinkedList<DeJitteredTransmission>>();
        }

        public void AddSample(DeJitteredTransmission clientAudio)
        {
            if (clientAudio.ReceiveTime > lastAccess + TimeSpan.FromMilliseconds(400).Ticks || _clientAudioSamples.Count == 0)
            {
                _clientAudioSamples.Add(new LinkedList<DeJitteredTransmission>());
                _clientAudioSamples[_clientAudioSamples.Count - 1].AddFirst(clientAudio);
            }
        }

        public float[] OutputPCM()
        {
            List<float[]> assembledOut = new List<float[]>();

            foreach(var transmission in _clientAudioSamples)
            {
                if (lastAccess > 0)
                {
                    long timeBetween = (transmission.First.Value.ReceiveTime - lastAccess);

                    // Discard multiple intervals of silence
                    if (timeBetween > TimeSpan.TicksPerSecond * 2)
                    {
                        timeBetween = timeBetween % (TimeSpan.TicksPerSecond * 2);
                    }
                    // assume all gaps smaller than 45ms aren't actually gaps, is this necessary? 
                    if (timeBetween / TimeSpan.TicksPerMillisecond > 45)
                    {
                        assembledOut.Add(new float[(timeBetween / TimeSpan.TicksPerSecond) * _sampleRate]);
                    }
                }

                //May require using LongLength()?
                lastAccess = transmission.Last.Value.ReceiveTime + (transmission.Last.Value.PCMMonoAudio.Length / _sampleRate) * TimeSpan.TicksPerSecond;

                var fulltransmission = transmission.SelectMany(x => x.PCMMonoAudio).ToArray();
                assembledOut.Add(fulltransmission);
            }

            _clientAudioSamples.Clear();
            float[] completeOutput = assembledOut.SelectMany(x => x).ToArray();

            return completeOutput;
        }
    }
}
