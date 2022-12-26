using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Common;
using NAudio.Lame;
using NAudio.Wave;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Managers;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Audio.Models;
using System.Linq;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.Recording
{
    internal class PerRadioLameRecordingWriter : AudioRecordingLameWriterBase
    {
        private readonly Dictionary<int, string> _filePaths;
        private readonly LameMP3FileWriter[] _mp3FileWriters;
        private int _radioNum;
        private float[] _floatArray;
        private float[] _selfArray;
        private readonly int _bufferSize = 120000;
        private readonly float[] _silenceArray;

        public PerRadioLameRecordingWriter(int sampleRate, int radioNum) : base(sampleRate)
        {
            _filePaths = new Dictionary<int, string>();
            _mp3FileWriters = new LameMP3FileWriter[11];
            _radioNum = radioNum;
            _silenceArray = new float[_bufferSize];
            _floatArray = new float[_bufferSize];
            _selfArray = new float[_bufferSize];
        }

        private void OutputToFile(int radio, float[] floatArray)
        {
            try
            {
                if (_mp3FileWriters[radio] != null)
                {
                    // create a byte array and copy the floats into it...
                    var byteArray = new byte[floatArray.Length * 4];
                    Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

                    _mp3FileWriters[radio].Write(byteArray, 0, byteArray.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Unable to write audio samples to output file: {ex.Message}");
            }
        }

        public override void ProcessAudio(List<CircularFloatBuffer> perRadioAudio, List<CircularFloatBuffer> perSelfAudio)
        {
            for (int i = 0; i < perRadioAudio.Count; i++)
            {
                if (perRadioAudio[i].Count > 0 || perSelfAudio[i].Count > 0)
                {
                    perRadioAudio[i].Read(_floatArray, 0, perRadioAudio[i].Count);
                    _floatArray = _floatArray.Zip(_silenceArray, (x, y) => x + y).ToArray();
                    perSelfAudio[i].Read(_selfArray, 0, perSelfAudio[i].Count);
                    _floatArray = AudioManipulationHelper.MixArraysClipped(_floatArray, _bufferSize, _selfArray, _bufferSize, out int _);
                    OutputToFile(i, _floatArray);
                    Array.Clear(_floatArray, 0, _bufferSize);
                }
                else
                {
                    OutputToFile(i, _silenceArray);
                }

            }
        }

        public override void Start(string aircraftName)
        {
            string partialFilePath = base.CreateFilePath();

            var lamePreset = (LAMEPreset)Enum.Parse(typeof(LAMEPreset),
                    GlobalSettingsStore.Instance.GetClientSetting(GlobalSettingsKeys.RecordingQuality).RawValue);
            for (int i = 0; i < 11; i++)
            {
                _filePaths.Add(i, $"{partialFilePath}-{aircraftName}-Radio{i}.mp3");

                _mp3FileWriters[i] = new LameMP3FileWriter(_filePaths[i], _waveFormat, lamePreset);
            }
        }

        public override void Stop()
        {
            _filePaths.Clear();
            for (int i = 0; i < 11; i++)
            {
                _mp3FileWriters[i].Dispose();
                _mp3FileWriters[i] = null;
            }
        }
    }
}