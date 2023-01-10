using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using TMPro;

public class Manager : MonoBehaviour
{
    public AudioClip[] clips;
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

    // Start is called before the first frame update
    void Start()
    {
        // Create 3 versions of all of the sounds
        for (int i = 0; i < clips.Length; i++)
        {
                sources.Add(Instantiate(audioSourcePrefab).GetComponent<AudioSource>());
                sources[i].clip = clips[i];
        }

        PlayPrompt();
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

    public void PlayPrompt()
    {
        prompt = promptInput.text;
        promptArray = new string[] { "" };

        //// Add pause before prompt
        //prompt = "_ " + prompt;

        if (prompt.Split(' ').Length > 1)
            promptArray = prompt.Split(' ');
        else
            promptArray[0] = prompt;
        Debug.Log("prompts: " + promptArray.Length);
        for (int i = 0; i < promptArray.Length; i++)
            Debug.Log(promptArray[i]);

        // Get the sounds required for the prompt
        promptSources = new List<AudioSource>() { sources[0] };
        for (int i = 0; i < promptArray.Length; i++)
        {
            char[] arr = promptArray[i].Where(c => (char.IsLetterOrDigit(c) ||
                             char.IsWhiteSpace(c) ||
                             c == '_')).ToArray();
            promptArray[i] = new string(arr).Trim();

            Debug.Log(i + " " + promptArray[i]);
            for (int j = 0; j < clips.Length; j++)
            { // Find matching clip name
                if (clips[j].name == promptArray[i])
                {
                    promptSources.Add(sources[j]);
                    //Debug.Log("soundFound: " + promptArray[i]);
                    Debug.Log("     " + j + " " + clips[j].name + " " + promptArray[i] + "  true");
                }
                else
                    Debug.Log("     " + j + " " + clips[j].name + " " + promptArray[i] + "  false");
                Debug.Log(clips[j].name.Trim().Length + " " + promptArray[i].Trim().Length);
            }
        }
        Debug.Log("sounds: " + promptSources.Count);

        playSound = true;
        currentSoundIndex = -1;
    }

    public static AudioClip Combine(params AudioClip[] clips)
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

        AudioClip result = AudioClip.Create("Combine", length / 2, 2, 44100, false, false);
        result.SetData(data, 0);

        return result;
    }
}
