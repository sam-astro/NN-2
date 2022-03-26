using System;

using UnityEngine;

    public class SoundGenerator
    {
        private readonly UInt32 _sampleRate;
        private short[] _dataBuffer;
        private double[] _customData;

        public short[] Data { get { return _dataBuffer; } }

        public SoundGenerator(UInt32 sampleRate, double[] customData)
        {
            _sampleRate = sampleRate;
            _customData = customData;
            GenerateData();
        }

        private void GenerateData()
        {
            uint _secondsInLength = (uint)(_sampleRate * _customData.Length);

            uint bufferSize = (uint)_customData.Length;
            _dataBuffer = new short[bufferSize];

            int amplitude = 32760;

            //for (uint index = 0; index < bufferSize - 1; index++)
            //{
            //    double d = random.NextDouble();
            //    _dataBuffer[index] = Convert.ToInt16(amplitude * (d*2.0d - 1.0d));
            //}
            for (uint index = 0; index < bufferSize - 1; index++)
            {
                _dataBuffer[index] = Convert.ToInt16(Mathf.Clamp((float)_customData[index], -1.0f, 1.0f)*amplitude);
            }
        }
    }
