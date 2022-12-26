using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Threading;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Models;
using System.Collections.Generic;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Managers;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Providers;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Recording;
using Ciribob.DCS.SimpleRadio.Standalone.Common;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Setting;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Linq;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio;
using static Ciribob.DCS.SimpleRadio.Standalone.Common.RadioInformation;
using MathNet.Numerics.Optimization;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Recording
{
    class AudioRecordingManager
    {
        public bool Enabled { get { return !_stop; } }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static volatile AudioRecordingManager _instance = new AudioRecordingManager();
        private static object _lock = new Object();

        private readonly ClientEffectsPipeline pipeline = new ClientEffectsPipeline();

        private readonly int _sampleRate;
        private readonly List<CircularFloatBuffer> _clientMixDownQueue;
        private readonly List<CircularFloatBuffer> _selfMixDownQueue;

        private long[] _lastUpdate;
        private long[] _lastUpdateSelf;

        private bool _stop;
        private AudioRecordingLameWriterBase _audioRecordingWriter;

        private float[] _mixBuffer = new float[AudioManager.OUTPUT_SEGMENT_FRAMES * 10];
        private float[] _secondaryMixBuffer = new float[AudioManager.OUTPUT_SEGMENT_FRAMES * 10];

        private List<DeJitteredTransmission>[] _mainAudioWithSilence;
        private List<DeJitteredTransmission>[] _secondaryAudioWithSilence;

        private readonly ConnectedClientsSingleton _connectedClientsSingleton = ConnectedClientsSingleton.Instance;

        private AudioRecordingManager()
        {
            _sampleRate = 48000;

            _clientMixDownQueue = new List<CircularFloatBuffer>();
            _selfMixDownQueue = new List<CircularFloatBuffer>();

            GlobalSettingsStore.Instance.SetClientSetting(GlobalSettingsKeys.RecordAudio, false);
            _stop = true;
        }

        public static AudioRecordingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AudioRecordingManager();
                    }
                }
                return _instance;
            }
        }

        private void ProcessQueues()
        {
            while (!_stop)
            {
                //todo leave the thread running but paused if you dont opt in to recording
                if (!GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.RecordAudio))
                {
                    _stop = true;
                }

                Thread.Sleep(2500);
                try
                {
                    _audioRecordingWriter.ProcessAudio(_clientMixDownQueue, _selfMixDownQueue);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Recording process failed: {ex}");
                }
            }
        }

        private float[] SingleRadioMixDown(AudioRecordingSample sample, out int count)
        {
            Array.Clear(_mixBuffer, 0, _mixBuffer.Length);

            //should be no more than 80 ms of audio
            //should really be 40 but just in case

            int primarySamples = 0;
            int secondarySamples = 0;

            //run this sample through - mix down all the audio for now PER radio 
            //we can then decide what to do with it later
            //same pipeline (ish) as RadioMixingProvider
            if (sample.MainRadioClientTransmissions?.Count > 0)
            {
                _mixBuffer = pipeline.ProcessClientTransmissions(_mixBuffer, sample.MainRadioClientTransmissions,
                    out primarySamples);
            }

            //handle guard
            if (sample.SecondaryRadioClientTransmissions?.Count > 0)
            {
                _secondaryMixBuffer = pipeline.ProcessClientTransmissions(_secondaryMixBuffer, sample.SecondaryRadioClientTransmissions, out secondarySamples);
            }

            _mixBuffer = AudioManipulationHelper.MixArraysClipped(_mixBuffer, primarySamples, _secondaryMixBuffer, secondarySamples, out int outputSamples);

            count = outputSamples;

            return _mixBuffer;
        }

        internal void Start()
        {
            _logger.Debug("Transmission recording started.");

            long startTime = DateTime.Now.Ticks;

            int _radioNum = ClientStateSingleton.Instance.DcsPlayerRadioInfo.radios.Sum(x => x.modulation != RadioInformation.Modulation.DISABLED ? 1 : 0);

            _mainAudioWithSilence = new List<DeJitteredTransmission>[_radioNum];
            _secondaryAudioWithSilence = new List<DeJitteredTransmission>[_radioNum];
            //_selfAudioWithSilence = new List<DeJitteredTransmission>[_radioNum];

            _lastUpdate = new long[_radioNum];
            _lastUpdateSelf = new long[_radioNum];

            for (int i = 0; i < _lastUpdate.Length; i++)
            {
                _lastUpdate[i] = startTime;
            }

            //for (int i = 0; i < _radioNum; i++)
            //{
            //    //TODO check size
            //    //5 seconds of audio
            //    _clientMixDownQueue.Add(new CircularFloatBuffer((int)(AudioManager.OUTPUT_SAMPLE_RATE * 2.5)));
            //    _selfMixDownQueue.Add(new CircularFloatBuffer((int)(AudioManager.OUTPUT_SAMPLE_RATE * 2.5)));
            //}

            //TODO: Implement a MixDownWriter for new pipeline
            //if (GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.SingleFileMixdown))
            //{
            //    //_audioRecordingWriter = new MixDownLameRecordingWriter(_sampleRate);
            //}
            //else
            {
                _audioRecordingWriter = new PerRadioLameRecordingWriter(_sampleRate, _radioNum);
            }
            _audioRecordingWriter.Start(ClientStateSingleton.Instance.DcsPlayerRadioInfo.unit);
            _stop = false;

            _clientMixDownQueue.Clear();
            _selfMixDownQueue.Clear();

            for (int i = 0; i < _radioNum; i++)
            {
                //TODO: I changed this to 2.5 due to OutOfRange exceptions in CircularBuffer.Read when attempting to copy
                //likely I've missed an implementation detail?
                _clientMixDownQueue.Add(new CircularFloatBuffer((int)(AudioManager.OUTPUT_SAMPLE_RATE * 2.5)));
                _selfMixDownQueue.Add(new CircularFloatBuffer((int)(AudioManager.OUTPUT_SAMPLE_RATE * 2.5)));

                _mainAudioWithSilence[i] = new List<DeJitteredTransmission>();
                _secondaryAudioWithSilence[i] = new List<DeJitteredTransmission>();
                //_selfAudioWithSilence[i] = new List<DeJitteredTransmission>();
            }

            var processingThread = new Thread(ProcessQueues);
            processingThread.Start();
        }

        internal void Stop()
        {
            if (_stop) { return; }
            _stop = true;
            _audioRecordingWriter.Stop();
            _logger.Debug("Transmission recording stopped.");
            Array.Clear(_mixBuffer, 0, _mixBuffer.Length);
            Array.Clear(_secondaryMixBuffer, 0, _secondaryMixBuffer.Length);
        }

        internal void AppendCaptureAudio(DeJitteredTransmission selfAudio)
        {
            if (GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.RecordAudio))
            {
                DeJitteredTransmission selfAudioWithSilence = ExpandGapSilence(selfAudio, ref _lastUpdateSelf);
                _selfMixDownQueue[selfAudio.ReceivedRadio].Write(selfAudioWithSilence.PCMMonoAudio, 0, selfAudioWithSilence.PCMAudioLength);
            }
        }

        internal void AppendClientAudio(List<DeJitteredTransmission> mainAudio, List<DeJitteredTransmission> secondaryAudio, int radioId)
        {
            //only record if we need too
            if (GlobalSettingsStore.Instance.GetClientSettingBool(GlobalSettingsKeys.RecordAudio))
            {
                ProcessSilenceGaps(mainAudio, _mainAudioWithSilence[radioId], radioId);
                ProcessSilenceGaps(secondaryAudio, _secondaryAudioWithSilence[radioId], radioId);

                float[] buf = SingleRadioMixDown(new AudioRecordingSample()
                {
                    MainRadioClientTransmissions = _mainAudioWithSilence[radioId],
                    SecondaryRadioClientTransmissions = _secondaryAudioWithSilence[radioId],
                    RadioId = radioId
                }, out int count);
                if (count > 0)
                {
                    _clientMixDownQueue[radioId].Write(buf, 0, count);
                }

                _mainAudioWithSilence[radioId].Clear();
                _secondaryAudioWithSilence[radioId].Clear();
            }
        }

        private void ProcessSilenceGaps(List<DeJitteredTransmission> audioSamples, List<DeJitteredTransmission> audioSamplesWithSilence, int radioId)
        {
            if (audioSamples.Count == 0)
            {
                long newTime = DateTime.Now.Ticks;
                int silenceLength = (int)((newTime - _lastUpdate[radioId]) / TimeSpan.TicksPerSecond * _sampleRate);


                audioSamplesWithSilence.Add(
                    new DeJitteredTransmission
                    {
                        PCMMonoAudio = new float[silenceLength],
                        PCMAudioLength = silenceLength,
                        IsSilence = true
                    }
                    );

                _lastUpdate[radioId] = newTime;

                return;
            }

            foreach (DeJitteredTransmission dejitterSample in audioSamples)
            {
                audioSamplesWithSilence.Add(ExpandGapSilence(dejitterSample, ref _lastUpdate));
            }
        }


        private DeJitteredTransmission ExpandGapSilence(DeJitteredTransmission audioSample, ref long[] lastUpdate)
        {
            long silenceLength = (audioSample.ReceiveTime - lastUpdate[audioSample.ReceivedRadio]) / TimeSpan.TicksPerSecond * _sampleRate;
            // Guard against neg values when transmission recv before AudioRecordingManager instantiated
            silenceLength = silenceLength < 0 ? 0 : silenceLength;

            // DateTime.Now ticks accuracy is too coarse, assume only (comparatively) larger gaps may be silence
            if (silenceLength / 10000 < 35)
            {
                return audioSample;
            }
            else
            {
                return
                    new DeJitteredTransmission
                    {
                        PCMMonoAudio = new float[silenceLength],
                        PCMAudioLength = (int)silenceLength,
                        IsSilence = true
                    };
            }
        }

        internal void Toggle()
        {
            if (_stop)
            {
                Start();
                GlobalSettingsStore.Instance.SetClientSetting(GlobalSettingsKeys.RecordAudio, true);
            }
            else
            {
                Stop();
                GlobalSettingsStore.Instance.SetClientSetting(GlobalSettingsKeys.RecordAudio, false);
            }
        }
    }
}