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
using System.Security.Cryptography;

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

    public int mutVarSize = 1;

    public int maxTrialsPerGeneration = 1;
    [ShowOnly] public int trial;

    public Transform spawnPoint;

    public TMP_Text generationText;
    public TMP_Text genomeList;

    public TimeManager timeManager;

    public LaserScript laser;

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
    [ShowOnly] public string bestGenome = "";
    [ShowOnly] public string bestHash = "";
    private List<NeuralNetwork> topGenomes;

    [ShowOnly] public int generationNumber = 1;
    [ShowOnly] public double lastWorst = 100000000;
    [ShowOnly] public double lastBest = 100000000;
    double bestError = 100000000;
    public double bestEverError = 100000000;
    double worstError = 0;

    bool queuedForUpload = false;
    private List<NeuralNetwork> nets;
    private List<GameObject> entityList = null;
    //bool startup = true;

    public GameObject netEntityPrefab;

    //[ShowOnly] public double[] bestMutVarsBefore;
    //[ShowOnly] public double[] bestMutVars;
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
                //generationText.text = "processing...";

                //// Make sure final pendingFitness is added
                //for (int i = 0; i < populationSize; i++)
                //    nets[i].fitness+=nets[i].pendingFitness;

                //nets.Sort();

                //bestError = nets[nets.Count - 1].fitness;
                //worstError = nets[0].fitness;



                //if (bestError < bestEverError || queuedForUpload == true || generationNumber == 0)
                //{
                //    //persistenceNetwork = nets[nets.Count - 1];
                //    //persistenceNetwork = new NeuralNetwork(nets[nets.Count - 1]);
                //    //persistenceNetwork.CopyWeights(nets[nets.Count - 1].weights);
                //    persistenceNetwork.weights = nets[nets.Count - 1].weights;
                //    //Array.Copy(nets[nets.Count - 1].mutatableVariables, persistenceNetwork.mutatableVariables, mutVarSize);
                //    //persistenceNetwork.mutatableVariables = nets[nets.Count - 1].mutatableVariables;
                //    persistenceNetwork.genome = nets[nets.Count - 1].genome;
                //    bestEverError = bestError;
                //    bestGenome = persistenceNetwork.genome.Substring(0, 8) + "a";

                //    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                //        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                //    // Save best weights
                //    BinaryFormatter bf = new BinaryFormatter();
                //    using (FileStream fs = new FileStream("./Assets/dat/WeightSave.bin", FileMode.Create))
                //        bf.Serialize(fs, persistenceNetwork.weights);

                //    // Get hash of best weights
                //    bestHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/WeightSave.bin")));

                ////    // Save best mutatable variables
                ////    BinaryFormatter bf2 = new BinaryFormatter();
                ////    using (FileStream fs2 = new FileStream("./Assets/dat/MutVars.bin", FileMode.Create))
                ////        bf2.Serialize(fs2, persistenceNetwork.mutatableVariables);

                //}
                //else if (generationNumber % timeBetweenGenerationProgress == 0)
                //    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                //        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                //if (generationNumber % timeBetweenSave == 0 && timeBetweenSave != -1)
                //{
                //    // Save metadata
                //    StreamWriter ps = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
                //    ps.WriteLine((generationNumber).ToString() + "#" +
                //        (bestEverError).ToString() + "#" +
                //        ((int)Time.realtimeSinceStartup + timeManager.offsetTime).ToString() + "#" +
                //        bestGenome);
                //    ps.Close();
                //}

                //// Find the top 3 *individual* genomes and add them to a `topGenomes` list
                //Debug.Log("lGenome: " + bestGenome);
                //string lastGenome = bestGenome.Substring(0, 8);
                //topGenomes = new List<NeuralNetwork>();
                //topGenomes.Add(persistenceNetwork);
                //for (int i = nets.Count - 1; i >= 0; i--)
                //{
                //    if (topGenomes.Count >= 3)
                //        break;
                //    if (nets[i].genome.Substring(0, 8) != lastGenome)
                //    {
                //        topGenomes.Add(new NeuralNetwork(nets[i]));
                //        lastGenome = nets[i].genome.Substring(0, 8);
                //    }
                //}

                //ListBestGenomes();

                //Finalizer();

                //lastBest = bestError;
                //lastWorst = worstError;
                //generationNumber++;
                //trial = 0;
                //iterations = maxIterations;
                //laser.ResetPosition();

                //CreateEntityBodies();

                //generationText.text = generationNumber.ToString() + " : " + trial.ToString();



                //
                // OLD SYSTEM: 
                //

                generationText.text = "processing...";

                // Make sure final pendingFitness is added
                for (int i = 0; i < populationSize; i++)
                    nets[i].AddFitness(nets[i].pendingFitness);

                nets.Sort();

                bestError = nets[nets.Count - 1].fitness;
                worstError = nets[0].fitness;

                if (generationNumber % timeBetweenSave == 0 && timeBetweenSave != -1)
                {
                    bestGenome = persistenceNetwork.genome.Substring(0, 8) + "a";

                    StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta.mta");
                    persistence.WriteLine((generationNumber).ToString() + "#" +
                        (bestEverError).ToString() + "#" +
                        ((int)Time.realtimeSinceStartup + timeManager.offsetTime).ToString() + "#" +
                        bestGenome);

                    BinaryFormatter bf = new BinaryFormatter();
                    using (FileStream fs = new FileStream("./Assets/dat/WeightSave.bin", FileMode.Create))
                        bf.Serialize(fs, persistenceNetwork.weights);
                    // Get hash of best weights
                    bestHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/WeightSave.bin")));


                    persistence.Close();
                }

                if (bestError < bestEverError || queuedForUpload == true || generationNumber == 0)
                {
                    persistenceNetwork.weights = nets[nets.Count - 1].weights;
                    persistenceNetwork.genome = nets[nets.Count - 1].genome;
                    bestEverError = bestError;

                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                }
                else if (generationNumber % timeBetweenGenerationProgress == 0)
                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.txt"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError);

                // Find the top 3 *individual* genomes and add them to a `topGenomes` list
                Debug.Log("lGenome: " + bestGenome);
                string lastGenome = bestGenome.Substring(0, 8);
                topGenomes = new List<NeuralNetwork>();
                topGenomes.Add(persistenceNetwork);
                for (int i = nets.Count - 1; i >= 0; i--)
                {
                    if (topGenomes.Count >= 3)
                        break;
                    if (nets[i].genome.Substring(0, 8) != lastGenome)
                    {
                        topGenomes.Add(new NeuralNetwork(nets[i]));
                        lastGenome = nets[i].genome.Substring(0, 8);
                    }
                }
                ListBestGenomes();

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
                laser.ResetPosition();

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

    private void ListBestGenomes()
    {
        string outS = "";
        int count = 10;
        for (int i = nets.Count - 1; i >= 0; i--)
        {
            if (count <= 0)
                break;
            if (!outS.Contains(nets[i].genome))
            {
                count--;
                outS += (nets.Count - 1 - i).ToString() + ". " + nets[i].genome + " " + Math.Round(nets[i].fitness, 3).ToString() + "\n";
            }
        }
        genomeList.text = outS;
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
        //// Create copies of top 3 genomes to replace the worst neural networks
        //for (int g = 0; g < topGenomes.Count; g++)
        //{
        //    for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * g; i < (int)(populationSize * 0.2 / topGenomes.Count) * (g + 1); i++)
        //    {
        //        nets[i] = new NeuralNetwork(topGenomes[g]);
        //        //nets[i].CopyWeights(topGenomes[g].weights);
        //        //Array.Copy(topGenomes[g].mutatableVariables, nets[i].mutatableVariables, mutVarSize);
        //        nets[i].genome = topGenomes[g].genome;
        //        nets[i].UpdateGenome();
        //        nets[i].Mutate();
        //        //nets[i].MutateMutVars();
        //    }
        //}
        //// Create totally new neural networks with random weights
        //for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * (topGenomes.Count + 1); i < (int)(populationSize * 0.5); i++)
        //{
        //    nets[i].CopyWeights(nets[i].RandomizeWeights());
        //    //nets[i].mutatableVariables = nets[i].RandomizeMutVars();
        //    nets[i].ResetGenome();
        //}
        //// Continue using the best 50% of neural networks and mutate them a bit
        //for (int i = (int)(populationSize * 0.5); i < populationSize - 1; i++)
        //{
        //    if (nets[i].genome.Substring(0, 8) == bestGenome.Substring(0, 8) && // If it is the same genome as the best
        //        Array.IndexOf(nets[i].letters, nets[i].genome[8]) < 5) // And if the mutation level is less than 5 away from the original
        //                                                               // Then randomize it to make population more diverse
        //    {
        //        //UnityEngine.Debug.Log("Resetting of:  " + i.ToString());
        //        nets[i].ResetGenome();
        //        nets[i].CopyWeights(nets[i].RandomizeWeights());
        //        //nets[i].mutatableVariables = nets[i].RandomizeMutVars();
        //    }
        //    else
        //    {
        //        nets[i].Mutate();
        //        //nets[i].mutatableVariables = nets[i].MutateMutVars(); // This causes problem in best NN
        //        nets[i].UpdateGenome();
        //        //UnityEngine.Debug.Log("Mutating Vars of:  " + i.ToString() + " , got: " + nets[i].mutatableVariables[0].ToString());
        //    }
        //}

        //// Hanbdle the best network
        ////Debug.Log("Best is index: " + (nets.Count - 1).ToString());
        ////Debug.Log("Vars are: " + (persistenceNetwork.mutatableVariables[0]).ToString());
        ////nets[nets.Count - 1] = new NeuralNetwork(persistenceNetwork);
        //nets[nets.Count - 1] = new NeuralNetwork(topGenomes[0]);
        ////nets[nets.Count - 1].neurons = persistenceNetwork.neurons;
        ////nets[nets.Count - 1].genome = bestGenome;
        ////Array.Copy(persistenceNetwork.mutatableVariables, nets[nets.Count - 1].mutatableVariables, mutVarSize);
        ////Debug.Log("Vars are: " + (nets[nets.Count - 1].mutatableVariables[0]).ToString());

        //// If any neural networks have an invalid genome, reset it and assign a genome
        //for (int i = 0; i < populationSize; i++)
        //{
        //    if (nets[i].genome.Trim() == "")
        //    {
        //        nets[i].ResetGenome();
        //        nets[i].CopyWeights(nets[i].RandomizeWeights());
        //        //nets[i].mutatableVariables = nets[i].RandomizeMutVars();
        //        Debug.LogWarning("Found broken genome " + i.ToString());
        //        nets[i].Mutate();
        //        nets[i].UpdateGenome();
        //    }
        //    nets[i].fitness = 0f;
        //    nets[i].pendingFitness = 0f;
        //    nets[i].isBest = false;
        //    nets[i].netID = i;

        //    // Save temp weights
        //    BinaryFormatter bf = new BinaryFormatter();
        //    using (FileStream fs = new FileStream("./Assets/dat/temp_weights.bin", FileMode.Create))
        //        bf.Serialize(fs, nets[i].weights);
        //    // Get hash of temp weights
        //    nets[i].weightsHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/temp_weights.bin")));

        //}
        //nets[nets.Count - 1].isBest = true;
        ////Array.Copy(persistenceNetwork.mutatableVariables, bestMutVars, mutVarSize);
        ////bestMutVars = persistenceNetwork.mutatableVariables;



        // OLD SYSTEM:
        //
        // Create copies of top 3 genomes to replace the worst neural networks
        for (int g = 0; g < topGenomes.Count; g++)
        {
            for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * g; i < (int)(populationSize * 0.2 / topGenomes.Count) * (g + 1); i++)
            {
                nets[i] = new NeuralNetwork(topGenomes[g]);
                //nets[i].CopyWeights(topGenomes[g].weights);
                //Array.Copy(topGenomes[g].mutatableVariables, nets[i].mutatableVariables, mutVarSize);
                nets[i].genome = topGenomes[g].genome;
                nets[i].UpdateGenome();
                nets[i].Mutate();
                //nets[i].MutateMutVars();
            }
        }
        // Create create totally new neural networks with random weights
        for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * (topGenomes.Count + 1); i < (int)(populationSize * 0.5); i++)
        {
            nets[i] = new NeuralNetwork(persistenceNetwork);
            nets[i].weights = nets[i].RandomizeWeights();
            nets[i].isBest = false;
            nets[i].genome = nets[i].GenerateGenome();
        }
        // Continue using the best 50% of neural networks and mutate them a bit
        for (int i = (int)(populationSize * 0.5); i < populationSize - 1; i++)
        {
            if (nets[i].genome.Substring(0, 8) == bestGenome.Substring(0, 8) && // If it is the same genome as the best
                Array.IndexOf(nets[i].letters, nets[i].genome[8]) < 5 && // And if the mutation level is less than 5 away from the original
                i < populationSize-11)                                   // And it is not in the top 10
                                                                         // Then randomize it to make population more diverse
            {
                nets[i].ResetGenome();
                nets[i].CopyWeights(nets[i].RandomizeWeights());
                nets[i].isBest = false;
            }
            else
            {
                nets[i] = new NeuralNetwork(nets[i]);
                nets[i].Mutate();
                nets[i].isBest = false;
                nets[i].UpdateGenome();
            }
        }
        nets[populationSize - 1] = new NeuralNetwork(persistenceNetwork);
        nets[populationSize - 1].isBest = true;


    }

    private string ByteArrayToString(byte[] arrInput)
    {
        int i;
        StringBuilder sOutput = new StringBuilder(arrInput.Length);
        for (i = 0; i < arrInput.Length; i++)
        {
            sOutput.Append(arrInput[i].ToString("X2"));
        }
        return sOutput.ToString();
    }

    void InitEntityNeuralNetworks()
    {
        GatherPersistence();

        if (populationSize % 2 != 0)
            populationSize++;

        nets = new List<NeuralNetwork>();

        for (int i = 0; i < populationSize; i++)
        {
            // If no weights were loaded, create random network
            if (bestGenome == "")
            {
                NeuralNetwork net = new NeuralNetwork(layers, null);
                //Debug.Log("* Creating net: " + i + " of " + populationSize);

                net.learningRate = learningRate;
                net.layers = layers;
                //net.mutVarSize = mutVarSize;
                net.ResetGenome();
                //net.mutatableVariables = net.RandomizeMutVars();

                nets.Add(net);
            }
            // Else load persistence weights
            else
            {
                NeuralNetwork net = new NeuralNetwork(persistenceNetwork);
                //Debug.Log("* Creating net: " + i + " of " + populationSize);

                net.learningRate = learningRate;
                net.layers = layers;
                //net.mutVarSize = mutVarSize;
                net.genome = bestGenome;

                //Array.Copy(persistenceNetwork.mutatableVariables, net.mutatableVariables, mutVarSize);

                nets.Add(net);
            }
        }

        if (bestGenome == "")
        {
            bestGenome = nets[0].GenerateGenome();
            persistenceNetwork.genome = bestGenome;
            //persistenceNetwork.mutatableVariables = persistenceNetwork.RandomizeMutVars();
            //persistenceNetwork.RandomizeWeights();
        }

        //startup = false;
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
            int[] layersWithBiases = layers;
            //for (int i = 0; i < layers.Length; i++)
            //    layersWithBiases[i] += 1;
            persistenceNetwork = new NeuralNetwork(layersWithBiases, null);
            //for (int i = 0; i < persistenceNetwork.weights.Length; i++)
            //    Debug.Log("w: " + persistenceNetwork.weights[i].Length);

            // Load weights data into `persistenceNetwork`
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fs = new FileStream("./Assets/dat/WeightSave.bin", FileMode.Open))
                persistenceNetwork.weights = (double[][][])bf.Deserialize(fs);
            //// Load mutVar data into `persistenceNetwork`
            //BinaryFormatter bf2 = new BinaryFormatter();
            //using (FileStream fs2 = new FileStream("./Assets/dat/MutVars.bin", FileMode.Open))
            //    persistenceNetwork.mutatableVariables = (double[])bf2.Deserialize(fs2);
            //bestMutVars = persistenceNetwork.mutatableVariables;

            // Get hash of best weights
            bestHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/WeightSave.bin")));



            // Load metadata like best error and generation
            StreamReader sr = File.OpenText("./Assets/dat/WeightSaveMeta.mta");
            string firstLine = sr.ReadLine().Trim();
            generationNumber = int.Parse(firstLine.Split('#')[0]) + 1;
            bestEverError = double.Parse(firstLine.Split('#')[1]);
            timeManager.offsetTime = int.Parse(firstLine.Split('#')[2]);
            bestGenome = firstLine.Split('#')[3];
            persistenceNetwork.genome = bestGenome + "a";
            sr.Close();

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