using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Threading;

namespace WaveUtils
{
    // Class for saving unity AudioClip type as a .WAV file
    public class SavWav
    {
        const int HEADER_SIZE = 44;
        struct ClipData
        {

            public int samples;
            public int channels;
            public float[] samplesData;

        }

        public bool Save(string filename, AudioClip clip)
        {
            if (!filename.ToLower().EndsWith(".wav"))
            {
                filename += ".wav";
            }

            var filepath = Application.persistentDataPath + "/" + filename;

            Debug.Log(filepath);

            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            ClipData clipdata = new ClipData();
            clipdata.samples = clip.samples;
            clipdata.channels = clip.channels;
            float[] dataFloat = new float[clip.samples * clip.channels];
            clip.GetData(dataFloat, 0);
            clipdata.samplesData = dataFloat;
            using (var fileStream = CreateEmpty(filepath))
            {
                MemoryStream memstrm = new MemoryStream();
                ConvertAndWrite(memstrm, clipdata);
                memstrm.WriteTo(fileStream);
                WriteHeader(fileStream, clip);
            }

            return true; // TODO: return false if there's a failure saving the file
        }

        public AudioClip TrimSilence(AudioClip clip, float min)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
        }

        public AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
        {
            return TrimSilence(samples, min, channels, hz, false, false);
        }

        public AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream)
        {
            int i;

            for (i = 0; i < samples.Count; i++)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(0, i);

            for (i = samples.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(i, samples.Count - i);

            var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

            clip.SetData(samples.ToArray(), 0);

            return clip;
        }

        FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        void ConvertAndWrite(MemoryStream memStream, ClipData clipData)
        {
            float[] samples = new float[clipData.samples * clipData.channels];

            samples = clipData.samplesData;

            Int16[] intData = new Int16[samples.Length];

            Byte[] bytesData = new Byte[samples.Length * 2];

            const float rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                //Debug.Log (samples [i]);
            }
            Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
            memStream.Write(bytesData, 0, bytesData.Length);
        }

        void WriteHeader(FileStream fileStream, AudioClip clip)
        {

            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            Byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            UInt16 two = 2;
            UInt16 one = 1;

            Byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            Byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            Byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            UInt16 blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            UInt16 bps = 16;
            Byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);

            //		fileStream.Close();
        }
    }

    // Class for converting byte array to a .WAV type, and can load it into unity AudioClip type
    // example use:
    //
    //   WAV wav = new WAV(rawData);
    //   Debug.Log(wav);
    //   AudioClip audioClip = AudioClip.Create("testSound", wav.SampleCount, 1, wav.Frequency, false, false);
    //   audioClip.SetData(wav.LeftChannel, 0);
    //   audio.clip = audioClip;
    //   audio.Play();
    public class WAV
    {

        // convert two bytes to one float in the range -1 to 1
        static float bytesToFloat(byte firstByte, byte secondByte)
        {
            // convert two bytes to one short (little endian)
            short s = (short)((secondByte << 8) | firstByte);
            // convert to range from -1 to (just below) 1
            return s / 32768.0F;
        }

        static int bytesToInt(byte[] bytes, int offset = 0)
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= ((int)bytes[offset + i]) << (i * 8);
            }
            return value;
        }

        private static byte[] GetBytes(string filename)
        {
            return File.ReadAllBytes(filename);
        }
        // properties
        public float[] LeftChannel { get; internal set; }
        public float[] RightChannel { get; internal set; }
        public int ChannelCount { get; internal set; }
        public int SampleCount { get; internal set; }
        public int Frequency { get; internal set; }

        // Returns left and right double arrays. 'right' will be null if sound is mono.
        public WAV(string filename) : this(GetBytes(filename)) { }

        public WAV(byte[] wav)
        {

            // Determine if mono or stereo
            ChannelCount = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

            // Get the frequency
            Frequency = bytesToInt(wav, 24);

            // Get past all the other sub chunks to get to the data subchunk:
            int pos = 12;   // First Subchunk ID from 12 to 16

            // Keep iterating until we find the data chunk (i.e. 64 61 74 61 ...... (i.e. 100 97 116 97 in decimal))
            while (!(wav[pos] == 100 && wav[pos + 1] == 97 && wav[pos + 2] == 116 && wav[pos + 3] == 97))
            {
                pos += 4;
                int chunkSize = wav[pos] + wav[pos + 1] * 256 + wav[pos + 2] * 65536 + wav[pos + 3] * 16777216;
                pos += 4 + chunkSize;
            }
            pos += 8;

            // Pos is now positioned to start of actual sound data.
            SampleCount = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
            if (ChannelCount == 2) SampleCount /= 2;        // 4 bytes per sample (16 bit stereo)

            // Allocate memory (right will be null if only mono sound)
            LeftChannel = new float[SampleCount];
            if (ChannelCount == 2) RightChannel = new float[SampleCount];
            else RightChannel = null;

            // Write to double array/s:
            int i = 0;
            while (pos < wav.Length)
            {
                LeftChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                pos += 2;
                if (ChannelCount == 2)
                {
                    RightChannel[i] = bytesToFloat(wav[pos], wav[pos + 1]);
                    pos += 2;
                }
                i++;
            }
        }

        public override string ToString()
        {
            return string.Format("[WAV: LeftChannel={0}, RightChannel={1}, ChannelCount={2}, SampleCount={3}, Frequency={4}]", LeftChannel, RightChannel, ChannelCount, SampleCount, Frequency);
        }
    }

    // This class is for storing clip data for individual sounds, like fading in/out, pitch, and overall volume
    public class Clip
    {
        // properties
        public AudioClip clip { get; set; } // The AudioClip object associated with this Clip
        public float Pitch { get; set; } // Value between 0.5 and 2 to change the pitch of the clip (default 1)
        public float Volume { get; set; } // Volume of the clip. (default 1)
        public float FadeInTime { get; set; } // Value between 0 and 0.5, changing the time between when the fade ends between the beginning (0), and halfway (0.5). (default 0)
        public float FadeOutTime { get; set; } // Value between 0.5 and 1, changing the time between when the fade starts between the middle (0.5), and end (1). (default 1)

        // Apply all of the effects to the AudioClip and resets the parameters to their default values
        public void ApplyEffects()
        {
            AudioFX afx = new AudioFX();
            // Apply effects in this order: Fade in, Fade out, Volume, Pitch
            clip =
                afx.ChangePitch(
                afx.Volume(
                afx.FadeOut(
                afx.FadeIn(clip, FadeInTime), FadeOutTime), Volume), Pitch
                );

            FadeInTime = 0f;
            FadeOutTime = 1f;
            Volume = 1f;
            Pitch = 1f;
        }

        public Clip(AudioClip audioClip)
        {
            clip = audioClip;
        }

        public Clip(AudioClip audioClip, float pitch, float volume, float fadeIn, float fadeOut)
        {
            clip = audioClip;
            Pitch = pitch;
            Volume = volume;
            FadeInTime = fadeIn;
            FadeOutTime = fadeOut;
        }
    }


    public class AudioFX
    {
        public AudioClip Combine(params AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            int length = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                length += clips[i].samples * clips[i].channels;
            }

            float[] data = new float[length];
            length = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null)
                    continue;

                float[] buffer = new float[clips[i].samples * clips[i].channels];
                clips[i].GetData(buffer, 0);
                //System.Buffer.BlockCopy(buffer, 0, data, length, buffer.Length);
                buffer.CopyTo(data, length);
                length += buffer.Length;
            }

            if (length == 0)
                return null;

            AudioClip result = AudioClip.Create("Combined", length, 1, clips[0].frequency, false);
            result.SetData(data, 0);

            return result;
        }

        public AudioClip ChangePitch(AudioClip clip, float pitch)
        {
            // Load clip data into float array
            float[] dataFloat = new float[clip.samples * clip.channels];
            clip.GetData(dataFloat, 0);
            // Create pitch shifter class and call shift function
            PitchShifter ps = new PitchShifter();
            dataFloat = ps.PitchShift(pitch, dataFloat.Length, 1024, dataFloat); // Change pitch

            // Copy final data into clip and return it
            clip.SetData(dataFloat, 0);
            return clip;
        }

        public AudioClip FadeIn(AudioClip clip, float fadeInTime)
        {
            if (clip == null || fadeInTime < 0.05f)
                return clip;

            int length = clip.samples * clip.channels;

            // Load clip data into array
            float[] data = new float[length];
            clip.GetData(data, 0);

            // Iterate all data and change floats according to fade in
            for (int i = 0; i < data.Length / 2; i++)
            {
                float percentageComplete = (float)i / (float)data.Length; // Get percent done

                // If the percent done is greater than the end time of the effect, end because there is no more work to be done
                if (percentageComplete > fadeInTime)
                    break;

                // Calculate exponental multiplier to be applied to data point
                float mult = (1f / Mathf.Pow(fadeInTime, 2f)) * Mathf.Pow(percentageComplete, 2f);
                // Multiply the current datapoint with the multiplier
                data[i] *= Mathf.Clamp(mult, 0, 1);
            }

            clip.SetData(data, 0);

            return clip;
        }

        public AudioClip FadeOut(AudioClip clip, float fadeOutTime)
        {
            if (clip == null || fadeOutTime > 0.95f)
                return clip;

            int length = clip.samples * clip.channels;

            // Load clip data into array
            float[] data = new float[length];
            clip.GetData(data, 0);

            // Iterate all data and change floats according to fade in
            for (int i = data.Length / 2; i < data.Length; i++)
            {
                float percentageComplete = (float)i / (float)data.Length; // Get percent done

                // If the percent done is less than the start of the effect, the fade hasn't started yet so keep iterating
                if (percentageComplete < fadeOutTime)
                    continue;

                // Calculate exponental multiplier to be applied to data point
                float mult = (1f / Mathf.Pow(1f - fadeOutTime, 2f)) * Mathf.Pow(percentageComplete - 1f, 2f);
                // Multiply the current datapoint with the multiplier
                data[i] *= Mathf.Clamp(mult, 0, 1);
            }

            clip.SetData(data, 0);

            return clip;
        }

        public AudioClip Volume(AudioClip clip, float volume)
        {
            if (clip == null || volume > 0.95f)
                return clip;

            int length = clip.samples * clip.channels;

            // Load clip data into array
            float[] data = new float[length];
            clip.GetData(data, 0);

            // Iterate all data and change floats according to volume
            for (int i = 0; i < data.Length; i++)
                // Multiply the current datapoint with the volume
                data[i] *= Mathf.Clamp(volume, 0, 1);

            clip.SetData(data, 0);

            return clip;
        }
    }
}