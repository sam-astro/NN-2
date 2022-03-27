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

    double[] correctData;

    #endregion

    private void Start()
    {
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
