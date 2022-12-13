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
using TMPro;

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

    public int maxTrialsPerGeneration = 1;
    [ShowOnly] public int trial;

    public Transform spawnPoint;

    public TMP_Text generationText;

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


    #region Internal Variables
    [ShowOnly] public int amntLeft;

    NeuralNetwork persistenceNetwork;

    [ShowOnly] public int generationNumber = 1;
    double lastBest = 100000000;
    public double lastWorst = 100000000;
    double bestError = 100000000;
    public double bestEverError = 100000000;
    double worstError = 0;

    bool queuedForUpload = false;
    private List<NeuralNetwork> nets;
    private List<GameObject> entityList = null;
    bool startup = true;

    public GameObject netEntityPrefab;
    #endregion

    private void Start()
    {
        InitEntityNeuralNetworks();
        CreateEntityBodies();

        iterations = maxIterations;

        generationText.text = generationNumber.ToString() + " : " + trial.ToString();
    }

    public void FixedUpdate()
    {
        if (iterations <= 0) // If this trial is over, do another one
        {
            if (trial >= maxTrialsPerGeneration - 1) // If the final trial is over, finalize and go to next generation
            {
                generationText.text = "processing...";

                // Make sure final pendingFitness is added
                for (int i = 0; i < populationSize; i++)
                    nets[i].AddFitness(nets[i].pendingFitness);

                nets.Sort();

                bestError = nets[nets.Count - 1].fitness;
                worstError = nets[0].fitness;

                if (generationNumber % timeBetweenSave == 0 && timeBetweenSave != -1)
                {
                    StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
                    persistence.WriteLine((generationNumber).ToString() + "#" + (bestEverError).ToString());

                    BinaryFormatter bf = new BinaryFormatter();
                    using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Create))
                        bf.Serialize(fs, persistenceNetwork.weights);

                    persistence.Close();
                }

                if (bestError < bestEverError || queuedForUpload == true || generationNumber == 0)
                {
                    persistenceNetwork.weights = nets[nets.Count - 1].weights;
                    bestEverError = bestError;

                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                }
                else if (generationNumber % timeBetweenGenerationProgress == 0)
                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                Finalizer();

                lastBest = bestError;
                lastWorst = worstError;
                generationNumber++;
                trial = 0;
                iterations = maxIterations;

                CreateEntityBodies();

                generationText.text = generationNumber.ToString() + " : " + trial.ToString();
            }
            else // Otherwise, create next trial and reset entities
            {
                iterations = maxIterations;
                trial += 1;
                CreateEntityBodies();

                generationText.text = generationNumber.ToString() + " : " + trial.ToString();
            }
        }
        else
        {
            iterations -= 1;

            if (iterations % 10 == 0)
                if (IterateNetEntities() == false || iterations <= 0)
                    iterations = 0;
        }
    }

    private void CreateEntityBodies()
    {
        if (entityList != null)
        {
            for (int i = 0; i < entityList.Count; i++)
            {
                Destroy(entityList[i]);
            }
        }
        //if (entityList == null)
        //{
        entityList = new List<GameObject>();

        for (int i = 0; i < populationSize; i++)
        {
            GameObject tempEntity = Instantiate(netEntityPrefab, spawnPoint);
            tempEntity.GetComponent<NetEntity>().Init(nets[i], generationNumber, layers[0], maxIterations, trial);
            entityList.Add(tempEntity);
        }
        //}
        //else
        //    for (int i = 0; i < entityList.Count; i++)
        //    {
        //        entityList[i].GetComponent<NetEntity>().Init(nets[i], generationNumber);
        //    }
    }

    private bool IterateNetEntities()
    {
        int amnt = entityList.Count;
        for (int i = 0; i < entityList.Count; i++)
            amnt -= entityList[i].GetComponent<NetEntity>().Elapse() ? 0 : 1;
        amntLeft = amnt;
        return amnt != 0;
    }

    void Finalizer()
    {
        //nets.Sort();
        //for (int i = 0; i < populationSize - 1; i++)
        //{
        //    // Totally randomize
        //    if (i < (int)(populationSize * 0.25f))
        //    {
        //        nets[i] = new NeuralNetwork(nets[populationSize - 1]);
        //        nets[i].RandomizeWeights();
        //    }
        //    // Copy then mutate
        //    else
        //    {
        //        nets[i] = new NeuralNetwork(nets[populationSize - 1]);
        //        nets[i].Mutate();
        //    }
        //}

        // Create copies of best-ever network and replace the worst neural networks
        for (int i = 0; i < (int)(populationSize * 0.2); i++)
        {
            nets[i] = new NeuralNetwork(persistenceNetwork);
            nets[i].Mutate();
        }
        // Create create totally new neural networks with random weights
        for (int i = (int)(populationSize * 0.2); i < (int)(populationSize * 0.5); i++)
        {
            nets[i].RandomizeWeights();
        }
        for (int i = (int)(populationSize * 0.5); i < populationSize - 1; i++)
        {
            nets[i].Mutate();
        }
        nets[populationSize - 1] = new NeuralNetwork(persistenceNetwork); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol


        //for (int i = 0; i < populationSize - 2; i++)
        //{
        //    nets[i] = new NeuralNetwork(persistenceNetwork);     //Copies weight values from top half networks to worst half
        //    nets[i].Mutate();
        //    nets[populationSize - 1] = new NeuralNetwork(persistenceNetwork); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //    nets[populationSize - 2] = new NeuralNetwork(nets[populationSize - 1]); //too lazy to write a reset neuron matrix values method....so just going to make a deepcopy lol
        //}

        for (int i = 0; i < populationSize; i++)
        {
            nets[i].SetFitness(0f);
            nets[i].pendingFitness = 0f;
        }

        //CreateEntityBodies(nets, populationSize);
        //return nets;
    }

    void InitEntityNeuralNetworks()
    {
        GatherPersistence();

        if (populationSize % 2 != 0)
        {
            populationSize++;
        }

        nets = new List<NeuralNetwork>();

        Console.ForegroundColor = ConsoleColor.Blue;
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(persistenceNetwork);
            Console.WriteLine("* Creating net: " + i + " of " + populationSize);

            net.learningRate = learningRate;

            if (persistenceNetwork == null)
                net.RandomizeWeights();

            nets.Add(net);
        }
        Console.ResetColor();

        startup = false;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("✓ EVERYTHING READY ✓");
        Console.Write("Just let this program process and learn, and only exit if ");
        Console.ForegroundColor = ConsoleColor.Blue;
        Console.Write("BLUE ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("text isn't getting printed to screen. (that is when it is saving or loading data). I have finally implemented networking! Now, as long as you have an internet connection, the weights data will automatically be sent to my server! Hooray!\n");
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
            persistenceNetwork = new NeuralNetwork(layers, null);

            // Load weights data into `persistenceNetwork`
            Debug.Log("Loading Weights...");
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("* Loading...");
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Open))
                persistenceNetwork.weights = (double[][][])bf.Deserialize(fs);
            Console.WriteLine("* Finished Loading.");
            Console.ResetColor();
            Debug.Log("Done");


            // Load metadata like best error and generation
            Debug.Log("Loading Metadata...");
            StreamReader sr = File.OpenText("./Assets/dat/WeightSaveMeta.mta");
            string firstLine = sr.ReadLine().Trim();
            generationNumber = int.Parse(firstLine.Split('#')[0]) + 1;
            bestEverError = double.Parse(firstLine.Split('#')[1]);
            sr.Close();
            Debug.Log("Done");

        }
        catch (Exception)
        {
            Debug.LogWarning("Failed to load weights data");
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