using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using UnityEngine;
using Microsoft.VisualBasic;
using TMPro;
using UnityEngine.UI;

public class NetManager : MonoBehaviour
{
    public int populationSize = 100;

    private int timeBetweenSave = 1;
    private int timeBetweenGenerationProgress = 1;
    //private int waitBetweenTestResults = 1;

    public int[] layers = new int[] { 1, 8, 12, 1 }; // No. of inputs and No. of outputs

    public float learningRate = 0.1f;

    public int iterations;
    public int maxIterations = 1000;

    //double promptMin = 0;
    //double promptMax = 0;
    //public double[][] prompt =
    //    {
    //        new double[1]{180},
    //    };
    //double answerMin = 0;
    //double answerMax = 0;
    //double[][] answer =
    //    {
    //        new double[1]{0.8011526357},
    //    };

    public TMP_Text genTxt;
    public TMP_Text errTxt;

    public Slider progressBar;

    public Toggle justTest;
    public bool test;

    #region Internal Variables
    public int amntLeft;

    private double[][][] collectedWeights;
    private NeuralNetwork.Layer[] preLoadLayers;

    NeuralNetwork persistenceNetwork;

    private int generationNumber = 1;
    double lastBest = 100000000;
    double lastWorst = 100000000;
    public double bestError = 0;
    double worstError = 100000;

    bool queuedForUpload = false;
    private List<NeuralNetwork> nets;
    private List<GameObject> entityList = null;
    bool startup = true;

    public GameObject netEntityPrefab;

    AudioMgr audMgr;

    double[] correctData;

    #endregion

    private void Start()
    {
        double[] l = new double[0];
        double[] r = new double[0];
        audMgr = new AudioMgr();
        audMgr.openWav("./Assets/inAudio2.wav", out l, out r);
        correctData = l;

        maxIterations = l.Length;

        justTest.isOn = test;


        iterations = maxIterations;
        InitEntityNeuralNetworks();
        CreateEntityBodies();
    }

    public void Update()
    {
        test = justTest;

        if (iterations <= 0)
        {
            nets.Sort();

            bestError = nets[nets.Count - 1].error;
            worstError = nets[0].error;

            genTxt.text = generationNumber.ToString();
            errTxt.text = Math.Round(bestError, 2).ToString();

            if (generationNumber % timeBetweenSave == 0 && timeBetweenSave != -1)
            {
                StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
                persistence.WriteLine((generationNumber).ToString() + "#" + (bestError).ToString());

                BinaryFormatter bf = new BinaryFormatter();
                using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Create))
                    bf.Serialize(fs, nets[nets.Count - 1].layers);

                persistence.Close();
            }

            if (((bestError < lastBest || queuedForUpload == true) && generationNumber % timeBetweenGenerationProgress == 0) || test)
            {
                using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                {
                    sw.WriteLine((generationNumber).ToString() + ", " + bestError);
                }

                Debug.Log("╚═ Generation: " + generationNumber + "  |  Population: " + populationSize);
                Debug.Log("  |  ");
                Console.ForegroundColor = ConsoleColor.Green;
                Debug.Log("Error Rate: " + bestError + "\n");
                Console.ResetColor();

                if (nets[0].customAnswer != null)
                    audMgr.SaveAudio(nets[0].customAnswer, bestError);
            }
            else if (generationNumber % timeBetweenGenerationProgress == 0)
            {
                Debug.Log("╚═ Generation: " + generationNumber + "  |  Population: " + populationSize);
                Debug.Log("  |  ");
                Debug.Log("Error Rate: " + (bestError) + "\n");

                using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                {
                    sw.WriteLine((generationNumber).ToString() + ", " + bestError);
                }
            }

            Finalizer();

            lastBest = bestError;
            lastWorst = worstError;
            generationNumber++;

            CreateEntityBodies();
            iterations = maxIterations;
        }
        else
        {
            iterations -= 1;

            progressBar.value = ((float)maxIterations - (float)iterations)/(float)maxIterations;

            if (IterateNetEntities() == false || iterations <= 0)
                iterations = 0;
        }
    }

    private void CreateEntityBodies()
    {
        if (entityList == null)
        {
            entityList = new List<GameObject>();

            for (int i = 0; i < populationSize; i++)
            {
                GameObject tempEntity = Instantiate(netEntityPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                tempEntity.GetComponent<NetEntity>().Init(nets[i], generationNumber, correctData);
                entityList.Add(tempEntity);
            }
        }
        else
            for (int i = 0; i < entityList.Count; i++)
            {
                entityList[i].GetComponent<NetEntity>().Init(nets[i], generationNumber, correctData);
            }
    }

    private bool IterateNetEntities()
    {
        bool outbool = true;
        for (int i = 0; i < entityList.Count; i++)
            outbool = entityList[i].GetComponent<NetEntity>().Elapse(test);
        return outbool;
    }

    void Finalizer()
    {
        //for (int i = 0; i < populationSize - 12; i++)
        //{
        //    nets[i] = nets[populationSize - 1];     //Copies weight values from top half networks to worst half
        //    nets[i].Mutate();
        //    nets[populationSize - 1] = new NeuralNetwork(nets[populationSize - 1]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //    nets[populationSize - 2] = new NeuralNetwork(nets[populationSize - 2]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //}
        //for (int i = populationSize - 12; i < populationSize - 2; i++)
        //{
        //    nets[i] = new NeuralNetwork(nets[populationSize - 1]);     //Copies weight values from top half networks to worst half
        //    nets[i].RandomizeWeights();
        //    nets[populationSize - 1] = new NeuralNetwork(nets[populationSize - 1]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //    nets[populationSize - 2] = new NeuralNetwork(nets[populationSize - 2]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //}


        //for (int i = 0; i < populationSize; i++)
        //{
        //    nets[i].SetFitness(0f);
        //}
    }

    void InitEntityNeuralNetworks()
    {
        GatherPersistence();

        //if (populationSize % 2 != 0)
        //{
        //    populationSize++;
        //}

        nets = new List<NeuralNetwork>();

        Console.ForegroundColor = ConsoleColor.Blue;
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers, preLoadLayers);
            net.customAnswer = new double[correctData.Length];
            Console.WriteLine("* Creating net: " + i + " of " + populationSize);

            //net.learningRate = learningRate;

            //if (persistenceNetwork != null)
            //    net.weights = persistenceNetwork.weights;
            //else
            //    net.RandomizeWeights();

            nets.Add(net);
        }
        Console.ResetColor();

        startup = false;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ EVERYTHING READY ✓");
        Debug.Log("Just let this program process and learn, and only exit if ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Debug.Log("BLUE ");
        Console.ForegroundColor = ConsoleColor.Green;
        Debug.Log("text isn't getting printed to screen. (that is when it is saving or loading data). I have finally implemented networking! Now, as long as you have an internet connection, the weights data will automatically be sent to my server! Hooray!\n");
        Console.ResetColor();
    }

    double[][] NormalizeData(double[][] input, double min, double max)
    {
        // Normalize the values between 0.0 and 1.0 based on min and max
        for (int i = 0; i < input.Length; i++)
        {
            for (int p = 0; p < input[i].Length; p++)
            {
                input[i][p] = (input[i][p] - min) / (max - min);
            }
        }

        return input;
    }

    double DeNormalize(double normalized, double min, double max)
    {
        return (normalized * (max - min) + min);
    }
    double Normalize(double unnormalized, double min, double max)
    {
        return (unnormalized - min) / (max - min);
    }

    void GetMinMax(double[][] input, out double min, out double max)
    {
        min = double.MaxValue;
        max = -double.MaxValue;

        // Get the minimum and maximum values first
        for (int i = 0; i < input.Length; i++)
        {
            for (int p = 0; p < input[i].Length; p++)
            {
                if (input[i][p] < min)
                    min = input[i][p];
                if (input[i][p] > max)
                    max = input[i][p];
            }
        }
    }

    void GatherPersistence()
    {
        try
        {
            persistenceNetwork = new NeuralNetwork(layers);

            // New System
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("* Loading...");
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Open))
                persistenceNetwork.layers = (NeuralNetwork.Layer[])bf.Deserialize(fs);
            Console.WriteLine("* Finished Loading.");
            Console.ResetColor();

            preLoadLayers = persistenceNetwork.layers; //convert to 3D array
        }
        catch (Exception)
        {
        }
    }

    static void Upload(float fitness)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;

        File.Copy("./Assets/dat/WeightSave.dat", "./Assets/dat/" + fitness + "_WeightSave.dat");
        Console.WriteLine("* Copied \"./Assets/dat/WeightSave.dat\" to \"./Assets/dat/" + fitness + "_WeightSave.dat\"");
        File.Copy("./Assets/dat/WeightSaveMeta.mta", "./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        Console.WriteLine("* Copied \"./Assets/dat/WeightSaveMeta.mta\" to \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        // Upload weight save
        Console.WriteLine("* Uploading \"./Assets/dat/" + fitness + "_WeightSave.dat\" to http://achillium.us.to/digitneuralnet/");
        System.Net.WebClient Client = new System.Net.WebClient();
        Client.Headers.Add("enctype", "multipart/form-data");
        byte[] result = Client.UploadFile("http://achillium.us.to/digitneuralnet/uploadweights.php", "POST", "./Assets/dat/" + fitness + "_WeightSave.dat");
        string s = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        Console.WriteLine("* Uploaded \"./Assets/dat/" + fitness + "_WeightSave.dat\"");

        // Upload weight save meta
        Console.WriteLine("* Uploading \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\" to http://achillium.us.to/digitneuralnet/");
        System.Net.WebClient ClientTwo = new System.Net.WebClient();
        ClientTwo.Headers.Add("enctype", "multipart/form-data");
        byte[] resultTwo = ClientTwo.UploadFile("http://achillium.us.to/digitneuralnet/uploadweights.php", "POST", "./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        string sTwo = System.Text.Encoding.UTF8.GetString(resultTwo, 0, resultTwo.Length);
        Console.WriteLine("* Uploaded \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        File.Delete("./Assets/dat/" + fitness + "_WeightSave.dat");
        Console.WriteLine("* Deleted Copy at \"./Assets/dat/" + fitness + "_WeightSave.dat\"");
        File.Delete("./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        Console.WriteLine("* Deleted Copy at \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("* Synced with server");
        Console.ResetColor();
    }

    static void Download(string s)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;

        System.Net.WebClient Client = new System.Net.WebClient();

        Console.WriteLine("* Downloading \"" + s + "_WeightSave.dat\" from http://achillium.us.to/digitneuralnet/" + s + "_WeightSave.dat");
        Client.DownloadFile(new Uri("http://achillium.us.to/digitneuralnet/" + s + "_WeightSave.dat"), @".\dat\temp_WeightSave.dat");
        Console.WriteLine("* Downloaded \"" + s + "_WeightSave.dat\"");
        Console.WriteLine("* Downloading \"" + s + "_WeightSaveMeta.mta\" from http://achillium.us.to/digitneuralnet/" + s + "_WeightSaveMeta.mta");
        Client.DownloadFile(new Uri("http://achillium.us.to/digitneuralnet/" + s + "_WeightSaveMeta.mta"), @".\dat\temp_WeightSaveMeta.mta");
        Console.WriteLine("* Downloaded \"" + s + "_WeightSaveMeta.mta\"");

        if (File.Exists("./Assets/dat/temp_WeightSave.dat"))
        {
            if (File.Exists("./Assets/dat/WeightSave.dat"))
                File.Delete("./Assets/dat/WeightSave.dat");
            File.Move("./Assets/dat/temp_WeightSave.dat", "./Assets/dat/WeightSave.dat");
        }
        if (File.Exists("./Assets/dat/temp_WeightSaveMeta.mta"))
        {
            if (File.Exists("./Assets/dat/WeightSaveMeta.mta"))
                File.Delete("./Assets/dat/WeightSaveMeta.mta");
            File.Move("./Assets/dat/temp_WeightSaveMeta.mta", "./Assets/dat/WeightSaveMeta.mta");
        }

        StreamReader sr = File.OpenText("./Assets/dat/WeightSaveMeta.mta");
        string firstLine = sr.ReadLine().Trim();
        string currentGen = firstLine.Split('#')[0];
        int generationNumber = int.Parse(currentGen) + 1;
        sr.Close();
        StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
        persistence.WriteLine((generationNumber).ToString() + "#" + s);
        persistence.Close();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("* Synced with server");
        Console.ResetColor();
    }
}

class AudioMgr
{
    //static Audio myAudio = new Audio();
    private static byte[] myWaveData;

    // Sample rate (Or number of samples in one second)
    private const int SAMPLE_FREQUENCY = 44100;

    public void SaveAudio(double[] input, double errorRate)
    {
        List<Byte> tempBytes = new List<byte>();

        WaveHeader header = new WaveHeader();
        FormatChunk format = new FormatChunk();
        DataChunk data = new DataChunk();

        SoundGenerator ldat = new SoundGenerator(SAMPLE_FREQUENCY, input);
        SoundGenerator rdat = new SoundGenerator(SAMPLE_FREQUENCY, input);

        data.AddSampleData(ldat.Data, rdat.Data);

        header.FileLength += format.Length() + data.Length();

        tempBytes.AddRange(header.GetBytes());
        tempBytes.AddRange(format.GetBytes());
        tempBytes.AddRange(data.GetBytes());

        myWaveData = tempBytes.ToArray();

        System.IO.File.WriteAllBytes("./Assets/outputAudioFiles/" + errorRate + ".wav", myWaveData);
    }

    // convert two bytes to one double in the range -1 to 1
    static double bytesToDouble(byte firstByte, byte secondByte)
    {
        // convert two bytes to one short (little endian)
        //short s = (secondByte << 8) | firstByte;
        short s = BitConverter.ToInt16(new byte[2] { (byte)firstByte, (byte)secondByte }, 0);
        // convert to range from -1 to (just below) 1
        return s / 32760.0;
    }

    // Returns left and right double arrays. 'right' will be null if sound is mono.
    public void openWav(string filename, out double[] left, out double[] right)
    {
        byte[] wav = File.ReadAllBytes(filename);

        // Determine if mono or stereo
        int channels = wav[22];     // Forget byte 23 as 99.999% of WAVs are 1 or 2 channels

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
        int samples = (wav.Length - pos) / 2;     // 2 bytes per sample (16 bit sound mono)
        if (channels == 2) samples /= 2;        // 4 bytes per sample (16 bit stereo)

        // Allocate memory (right will be null if only mono sound)
        left = new double[samples];
        if (channels == 2)
            right = new double[samples];
        else
            right = null;

        // Write to double array/s:
        int i = 0;
        while (pos < samples * 4)
        {
            left[i] = bytesToDouble(wav[pos], wav[pos + 1]);
            //Console.WriteLine(left[i]);
            pos += 2;
            if (channels == 2)
            {
                right[i] = bytesToDouble(wav[pos], wav[pos + 1]);
                pos += 2;
            }
            i++;
        }
    }
}

