using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TMPro;

using WaveUtils;

using SoundComparer.WaveUtils;

using Spectrogram;

public class VoiceMaker : MonoBehaviour
{
    public AudioClip[] inputClips;
    private List<Clip> OutputClips = new List<Clip>();
    public GameObject audioSourcePrefab;

    List<AudioSource> sources = new List<AudioSource>();

    public string prompt = "s i n th eh s i s";
    string[] promptArray;

    public List<AudioSource> promptSources = new List<AudioSource>();

    int currentSoundIndex = -1;
    public bool playSound = false;

    public TMP_Text promptInput;

    public float timeScale = 1;

    [Range(0.0f, 1.0f)]
    public float overlapAmount = 0;

    private AudioFX AudioEffectMaker = new AudioFX();

    WaveSound ws;
    WaveSound ws2;

    public AudioClip trialOutput;
    public AudioClip expectedOutput;

    //Spectrogram.Spectrogram sp = new Spectrogram.Spectrogram;

    // Start is called before the first frame update
    void Start()
    {
        // Create all of the sounds
        for (int i = 0; i < inputClips.Length; i++)
        {
            sources.Add(Instantiate(audioSourcePrefab).GetComponent<AudioSource>());
            sources[i].clip = inputClips[i];
            //OutputClips.Add(new Clip(inputClips[i]));
        }

        ws = new WaveSound("./Assets/Voice/testOutput.wav");
        ws.ReadWavFile();
        ws2 = new WaveSound("./Assets/Voice/expectedOutput.wav");
        ws2.ReadWavFile();

        //PlayPrompt();
    }


    // Update is called once per frame
    void Update()
    {
        if (playSound)
        {
            if (currentSoundIndex >= promptSources.Count)
            {
                playSound = false;
                return;
            }

            Time.timeScale = timeScale;


            if (currentSoundIndex == -1)
            {
                //promptSources[0].pitch = 0.7f;
                promptSources[0].Play();
                currentSoundIndex++;
            }
            else if (promptSources[currentSoundIndex].isPlaying == false || promptSources[currentSoundIndex].clip.length - promptSources[currentSoundIndex].time <= promptSources[currentSoundIndex].clip.length * overlapAmount)
            {
                //promptSources[currentSoundIndex].pitch = 0.7f;
                if (currentSoundIndex == 0)
                    // Check if the last one
                    if (currentSoundIndex == promptSources.Count - 1)
                        playSound = false;
                    else
                    {
                        currentSoundIndex++;
                        //promptSources[currentSoundIndex].pitch = Random.Range(0.9f, 1.1f);
                        //promptSources[currentSoundIndex].volume = Random.Range(0.88f, 0.95f);
                        promptSources[currentSoundIndex].Play();
                    }
                else if (promptSources[currentSoundIndex - 1].isPlaying == false)
                    // Check if the last one
                    if (currentSoundIndex == promptSources.Count - 1)
                        playSound = false;
                    else
                    {
                        currentSoundIndex++;
                        //promptSources[currentSoundIndex].pitch = Random.Range(0.9f, 1.1f);
                        //promptSources[currentSoundIndex].volume = Random.Range(0.88f, 0.95f);
                        promptSources[currentSoundIndex].Play();
                    }
            }
        }

    }

    // Add a sound with effects to the list of output sounds
    public void AddSound(string soundName, float pitch, float volume, float fadeIn, float fadeOut)
    {
        // Find index of matching AudioClip
        for (int j = 0; j < inputClips.Length; j++)
            if (inputClips[j].name == soundName)
                // Add clip and it's parameters to list of output clips
                OutputClips.Add(new Clip(inputClips[j], pitch, volume, fadeIn, fadeOut));
    }

    // Apply all of the effects to all of the individual sounds
    // in the output sounds list
    public void ApplyAllEffects()
    {
        // For all Clips, apply their effects
        for (int j = 0; j < OutputClips.Count; j++)
            OutputClips[j].ApplyEffects();
    }

    // Combine all Clips and use FFT algorithm to compare the
    // output clip with what it should actually sound like
    public float Finalize()
    {
        float score = 1f;

        List<AudioClip> cl = new List<AudioClip>(); // gather unity clips from Clip class
        for (int i = 0; i < OutputClips.Count; i++)
            cl.Add(OutputClips[i].clip);
        AudioClip final = AudioEffectMaker.Combine(cl.ToArray()); // combine clips into single one
        //// save final clip to file
        //SavWav savWav = new SavWav();
        //savWav.Save("testOutput.wav", final);

        // Compare
        WaveSound ws = new WaveSound("./Assets/Voice/full.wav");
        ws.ReadWavFile();


        return score;
    }


    public void PlayPrompt()
    {
        Debug.Log("Simmilarity: " + ws.Compare(ws2).ToString() + "%");
        //return;
        prompt = promptInput.text;
        promptArray = new string[] { "" };

        //// Add pause before prompt
        //prompt = "_ " + prompt;

        if (prompt.Split(' ').Length > 1)
            promptArray = prompt.Split(' ');
        else
            promptArray[0] = prompt;
        //Debug.Log("prompts: " + promptArray.Length);
        //for (int i = 0; i < promptArray.Length; i++)
        //    Debug.Log(promptArray[i]);

        // Get the sounds required for the prompt
        promptSources = new List<AudioSource>() { sources[0] };
        for (int i = 0; i < promptArray.Length; i++)
        {
            char[] arr = promptArray[i].Where(c => (char.IsLetterOrDigit(c) ||
                             char.IsWhiteSpace(c) ||
                             c == '_')).ToArray();
            promptArray[i] = new string(arr).Trim();

            //Debug.Log(i + " " + promptArray[i]);
            for (int j = 0; j < inputClips.Length; j++)
            { // Find matching clip name
                if (inputClips[j].name == promptArray[i])
                {
                    promptSources.Add(sources[j]);
                    //Debug.Log("soundFound: " + promptArray[i]);
                    //Debug.Log("     " + j + " " + inputClips[j].name + " " + promptArray[i] + "  true");
                }
                //else
                //    Debug.Log("     " + j + " " + inputClips[j].name + " " + promptArray[i] + "  false");
                //Debug.Log(inputClips[j].name.Trim().Length + " " + promptArray[i].Trim().Length);
            }
        }
        Debug.Log("sounds: " + promptSources.Count);

        // Test combining and saving final audio clips
        List<AudioClip> cl = new List<AudioClip>(); // gather clips
        for (int i = 0; i < promptSources.Count; i++)
            cl.Add(promptSources[i].clip);
        AudioClip final = AudioEffectMaker.Combine(cl.ToArray()); // combine clips into single one
        // save final clip to file
        SavWav savWav = new SavWav();
        savWav.Save("testOutput.wav", final);

        SaveSpectrogram(expectedOutput, "./Assets/expectedSpectrogram.png");
        SaveSpectrogram(trialOutput, "./Assets/trialSpectrogram.png");


        playSound = true;
        currentSoundIndex = -1;
    }

    public void SaveSpectrogram(AudioClip audioClip, string path)
    {
        // Generate spectrogram of wave file
        var sg = new SpectrogramGenerator(1024, fftSize: 256, stepSize: 100, maxFreq: 3000);
        float[] finalData = new float[audioClip.samples];
        audioClip.GetData(finalData, 0);
        double[] converted = new double[finalData.Length];
        for (int i = 0; i < converted.Length; i++)
            converted[i] = (float)finalData[i];
        sg.Add(converted);

        double[][] ou = sg.GetFFTs().ToArray();
        Texture2D tex = new Texture2D(ou.Length, ou[0].Length);
        for (int x = 0; x < ou.Length; x++)
        {
            for (int y = 0; y < ou[x].Length; y++)
            {
                float col = (float)ou[x][y] * 100f;

                int neighbors = CountNeighbors(ref ou, x, y, 0.3f, 100f);
                if (neighbors == 0)
                    col = 0;

                col = Mathf.Clamp(col, 0, 1) >= 0.015f ? 1 : 0;
                tex.SetPixel(x, y, new Color(col, col, col));
            }
        }
        tex.Apply();
        //sg.SaveImage("testOutputSpectrogram.png");
        SaveTextureAsPNG(tex, path);
    }

    public int CountNeighbors(ref double[][] m, int x, int y, float threshold, float multiplier, bool includeDiagonals = false)
    {
        int count = 0;
        // Cross
        if (x - 1 >= 0)
            count += m[x - 1][y] * multiplier >= threshold ? 1 : 0;
        if (x + 1 < m.Length)
            count += m[x + 1][y] * multiplier >= threshold ? 1 : 0;
        if (y - 1 >= 0)
            count += m[x][y - 1] * multiplier >= threshold ? 1 : 0;
        if (y + 1 < m[0].Length)
            count += m[x][y + 1] * multiplier >= threshold ? 1 : 0;
        // Diagonals
        if (includeDiagonals)
        {
            if (x - 1 >= 0 && y - 1 >= 0)
                count += m[x - 1][y - 1] * multiplier >= threshold ? 1 : 0;
            if (x + 1 < m.Length && y + 1 < m[0].Length)
                count += m[x + 1][y + 1] * multiplier >= threshold ? 1 : 0;
            if (y - 1 >= 0 && x + 1 < m.Length)
                count += m[x + 1][y - 1] * multiplier >= threshold ? 1 : 0;
            if (y + 1 < m[0].Length && x - 1 >= 0)
                count += m[x - 1][y + 1] * multiplier >= threshold ? 1 : 0;
        }

        return count;
    }

    public static void SaveTextureAsPNG(Texture2D _texture, string _fullPath)
    {
        byte[] _bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(_fullPath, _bytes);
        Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
    }
}
