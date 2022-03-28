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

[System.Serializable]
public class Classify
{
    public Texture2D tex;
    [HideInInspector]
    public List<double> pixelArray = new List<double>();
    public int classifyAs;
}

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
    public TMP_Text bestErrTxt;
    public TMP_Text bestGuessTxt;

    public Slider progressBar;

    public Toggle justTest;
    public bool test = false;

    public List<Classify> classifiedImages;

    public Image[] outputsPreview;
    public Material inputPreview;

    public Texture2D drawOnTexture;

    public Button clearImageButton;

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

    double[][] correctData = new double[][] {
        new double[] { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        new double[] { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0 },
        new double[] { 0, 0, 1, 0, 0, 0, 0, 0, 0, 0 },
        new double[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 },
        new double[] { 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
        new double[] { 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
        new double[] { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
        new double[] { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
        new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
        new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 },
    };
    double[] inputData;

    #endregion


    public void Awake()
    {
        justTest.isOn = test;
    }

    public void Start()
    {
        foreach (var cl in classifiedImages)
        {
            Color[] colors = cl.tex.GetPixels(0, 0, 28, 28);
            for (int i = 0; i < colors.Length; i++)
                cl.pixelArray.Add((double)colors[i].r);
        }

        for (int n = 0; n < 10; n++)
        {
            byte[] bytes = System.IO.File.ReadAllBytes("./Assets/LargeInputImages/" + n + ".jpg");
            Texture2D s = new Texture2D(1, 1);
            s.filterMode = FilterMode.Point;
            s.LoadImage(bytes);
            for (int i = 0; i < 40; i++)
            {
                Classify tC = new Classify();

                tC.tex = new Texture2D(28, 28);
                tC.tex.filterMode = FilterMode.Point;
                tC.tex.SetPixels(0, 0, 28, 28, s.GetPixels(28 * i, 0, 28, 28));
                tC.tex.Apply();

                tC.pixelArray = Texture2DToList(tC.tex);

                tC.classifyAs = n;

                classifiedImages.Add(tC);
            }
        }

        maxIterations = classifiedImages.Count - 1;


        drawOnTexture = TextureDrawer.Clear(drawOnTexture);
        inputPreview.SetTexture("_MainTex", drawOnTexture);

        try
        {
            // Load best error
            StreamReader persistence = new StreamReader("./Assets/dat/WeightSaveMeta.mta");
            string str = persistence.ReadLine();
            generationNumber = int.Parse(str.Split('#')[0]);
            genTxt.text = generationNumber.ToString();
            lastBest = double.Parse(str.Split('#')[1]);
            bestError = double.Parse(str.Split('#')[1]);
            bestErrTxt.text = Math.Round(bestError, 3).ToString();
            persistence.Close();
        }
        catch (Exception)
        {
            Debug.LogWarning("Save data not found, continuing...");
        }

        iterations = maxIterations;
        InitEntityNeuralNetworks();
        CreateEntityBodies();
    }

    public void Update()
    {
        test = justTest.isOn;

        if (test == false)
        {
            clearImageButton.interactable = false;
            if (iterations < 0)
            {
                nets.Sort();

                bestError = nets[nets.Count - 1].error;
                worstError = nets[0].error;

                genTxt.text = generationNumber.ToString();
                errTxt.text = Math.Round(bestError, 3).ToString();

                if ((bestError < lastBest && generationNumber % timeBetweenGenerationProgress == 0) || test)
                {
                    // Save metadata
                    StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
                    persistence.WriteLine((generationNumber).ToString() + "#" + (bestError).ToString());
                    persistence.Close();

                    // Save weights
                    BinaryFormatter bf = new BinaryFormatter();
                    using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Create))
                        bf.Serialize(fs, nets[nets.Count - 1].layers);

                    // Log history
                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                    {
                        sw.WriteLine((generationNumber).ToString() + ", " + bestError);
                    }


                    bestErrTxt.text = Math.Round(bestError, 2).ToString();
                    lastBest = bestError;
                }
                else if (generationNumber % timeBetweenGenerationProgress == 0)
                {
                    // Log history
                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                    {
                        sw.WriteLine((generationNumber).ToString() + ", " + bestError);
                    }
                }

                //Finalizer();

                lastWorst = worstError;
                generationNumber++;

                CreateEntityBodies();
                iterations = maxIterations;


                // Randomize order of list
                classifiedImages.Shuffle();

                return;
            }
            else
            {
                //inputPreview.mainTexture = classifiedImages[iterations];
                inputPreview.SetTexture("_MainTex", classifiedImages[iterations].tex);

                inputData = classifiedImages[iterations].pixelArray.ToArray();

                progressBar.value = ((float)maxIterations - (float)iterations) / (float)maxIterations;

                if (IterateNetEntities() == false)
                    iterations = -1;

                float greatestValue = 0;
                int greatestIndex = 0;
                for (int i = 0; i < nets[nets.Count - 1].publicOutputs.Length; i++)
                {
                    if ((float)nets[nets.Count - 1].publicOutputs[i] > greatestValue)
                    {
                        greatestValue = (float)nets[nets.Count - 1].publicOutputs[i];
                        greatestIndex = i;
                    }

                    float val = Mathf.Clamp((float)nets[nets.Count - 1].publicOutputs[i], 0.0f, 1.0f);
                    outputsPreview[i].color = new Color(val, val, val);
                }

                bestGuessTxt.text = greatestIndex + "  -  " + Math.Round(greatestValue * 100, 2).ToString() + "% confidence";
                //return;
            }
            iterations -= 1;
        }
        else
        {
            clearImageButton.interactable = true;

            //if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
            //{
            inputData = Texture2DToArray(drawOnTexture);

            IterateNetEntities();

            float greatestValue = 0;
            int greatestIndex = 0;
            for (int i = 0; i < nets[nets.Count - 1].publicOutputs.Length; i++)
            {
                if ((float)nets[nets.Count - 1].publicOutputs[i] > greatestValue)
                {
                    greatestValue = (float)nets[nets.Count - 1].publicOutputs[i];
                    greatestIndex = i;
                }

                float val = Mathf.Clamp((float)nets[nets.Count - 1].publicOutputs[i], 0.0f, 1.0f);
                outputsPreview[i].color = new Color(val, val, val);
            }

            bestGuessTxt.text = greatestIndex + "  -  " + Math.Round(greatestValue * 100, 2).ToString() + "% confidence";

            //}
        }
    }

    private double[] Texture2DToArray(Texture2D tex)
    {
        Color[] colors = tex.GetPixels(0, 0, 28, 28);
        List<double> tempPixelArray = new List<double>();
        for (int i = 0; i < colors.Length; i++)
        {
            tempPixelArray.Add((double)colors[i].r);
        }

        return tempPixelArray.ToArray();
    }
    private List<double> Texture2DToList(Texture2D tex)
    {
        Color[] colors = tex.GetPixels(0, 0, 28, 28);
        List<double> tempPixelArray = new List<double>();
        for (int i = 0; i < colors.Length; i++)
        {
            tempPixelArray.Add((double)colors[i].r);
        }

        return tempPixelArray;
    }

    private void CreateEntityBodies()
    {
        if (entityList == null)
        {
            entityList = new List<GameObject>();

            for (int i = 0; i < populationSize; i++)
            {
                GameObject tempEntity = Instantiate(netEntityPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                tempEntity.GetComponent<NetEntity>().Init(nets[i], generationNumber);
                entityList.Add(tempEntity);
            }
        }
        else
            for (int i = 0; i < entityList.Count; i++)
            {
                entityList[i].GetComponent<NetEntity>().Init(nets[i], generationNumber);
            }
    }

    private bool IterateNetEntities()
    {
        bool outbool = true;
        for (int i = 0; i < entityList.Count; i++)
            if (entityList[i].GetComponent<NetEntity>().Elapse(test, inputData, correctData[classifiedImages[iterations].classifyAs]) == false)
                outbool = false;
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

        //Console.ForegroundColor = ConsoleColor.Blue;
        for (int i = 0; i < populationSize; i++)
        {
            NeuralNetwork net = new NeuralNetwork(layers, preLoadLayers);
            net.customAnswer = new double[correctData.Length];
            //Console.WriteLine("* Creating net: " + i + " of " + populationSize);

            //net.learningRate = learningRate;

            //if (persistenceNetwork != null)
            //    net.weights = persistenceNetwork.weights;
            //else
            //    net.RandomizeWeights();

            nets.Add(net);
        }
        //Console.ResetColor();

        startup = false;
        //Console.Clear();
        //Console.ForegroundColor = ConsoleColor.Green;
        //Console.WriteLine("✓ EVERYTHING READY ✓");
        //Debug.Log("Just let this program process and learn, and only exit if ");
        //Console.ForegroundColor = ConsoleColor.Blue;
        //Debug.Log("BLUE ");
        //Console.ForegroundColor = ConsoleColor.Green;
        //Debug.Log("text isn't getting printed to screen. (that is when it is saving or loading data). I have finally implemented networking! Now, as long as you have an internet connection, the weights data will automatically be sent to my server! Hooray!\n");
        //Console.ResetColor();
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
            //Console.ForegroundColor = ConsoleColor.Blue;
            //Console.WriteLine("* Loading...");
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream("./Assets/dat/WeightSave.dat", FileMode.Open))
                persistenceNetwork.layers = (NeuralNetwork.Layer[])bf.Deserialize(fs);
            //Console.WriteLine("* Finished Loading.");
            //Console.ResetColor();

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

static class MyExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}