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
using UnityEngine.UI;
using TMPro;
using System.Security.Cryptography;

[System.Serializable]
class SaveData
{
    public double[][][] weights;
    public double[] mutVars;
    public bool[][] droppedNeurons;
    public bool[][][] droppedWeights;
    public int[] layers;
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

public class NetManager : MonoBehaviour
{
    public int populationSize = 100;

    private int timeBetweenSave = 10;
    private int timeBetweenGenerationProgress = 1;
    //private int waitBetweenTestResults = 1;

    public int[] layers = new int[] { 9, 8, 12, 9 }; // No. of inputs and No. of outputs

    public float learningRate = 0.1f;

    public int iterations;
    public int maxIterations = 1000;

    public int mutVarSize = 1;

    public int maxTrialsPerGeneration = 1;
    [ShowOnly] public int trial;

    public Transform spawnPoint;

    public TMP_Text generationText;
    public TMP_Text droppedNeuronsText;
    public TMP_Text genomeList;
    public Slider dropChanceSlider;
    public Toggle optimizeAndShrinkToggle;
    int dropChance = 3;

    public TimeManager timeManager;

    public LaserScript laser;



    public NetUI netUI;

    bool optimizeAndShrinkNet = false;

    public BoardDrawer boardDrawer;

    [Range(0, 100)]
    public int startingNeuronPercent = 97;

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

    List<NeuralNetwork> persistenceNetwork;
    [ShowOnly] public string[] bestGenome = {"", ""};
    [ShowOnly] public string bestHash = "";
    private List<List<NeuralNetwork>> topGenomes;

    [ShowOnly] public int generationNumber = 0;
    public double[] lastWorst = new double[]{100000000, 100000000};
    public double[] lastBest = new double[] { 100000000, 100000000};
    double[] bestError = new double[]{ 100000000, 100000000};
    public double[] bestEverError = new double[2] { 100000000, 100000000 };
    double[] worstError = new double[] { 0, 0};

    bool queuedForUpload = false;
    private List<List<NeuralNetwork>> nets;
    public List<GameObject> entityList = null;
    //bool startup = true;

    public GameObject netEntityPrefab;

    int bestDroppedNeuronsAmnt = 0;
    int totalNeurons = 0;
    
    [ShowOnly] public int trainTeam = 0; // The current team getting trained

    //[ShowOnly] public double[] bestMutVarsBefore;
    //[ShowOnly] public double[] bestMutVars;
    #endregion

    private void Start()
    {
        bestGenome = new string[2];

        InitEntityNeuralNetworks();
        CreateEntityBodies();

        iterations = maxIterations;

        generationText.text = generationNumber.ToString() + " : " + trial.ToString();

        totalNeurons = persistenceNetwork[trainTeam].CountTotalNeurons();

        // If the hist.csv file does not exist, create it and add data labels
        if (!File.Exists("./Assets/dat/hist.csv"))
            using (StreamWriter sw = File.AppendText("./Assets/dat/hist.csv"))
                sw.WriteLine("generation, team A best, team A current, team B best, team B current, Dropped %");
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
                //    nets[trainTeam][i].fitness+=nets[trainTeam][i].pendingFitness;

                //nets.Sort();

                //bestError = nets[trainTeam][nets.Count - 1].fitness;
                //worstError = nets[trainTeam][0].fitness;



                //if (bestError < bestEverError || queuedForUpload == true || generationNumber == 0)
                //{
                //    //persistenceNetwork = nets[trainTeam][nets.Count - 1];
                //    //persistenceNetwork = new NeuralNetwork(nets[trainTeam][nets.Count - 1]);
                //    //persistenceNetwork.CopyWeights(nets[trainTeam][nets.Count - 1].weights);
                //    persistenceNetwork.weights = nets[trainTeam][nets.Count - 1].weights;
                //    //Array.Copy(nets[trainTeam][nets.Count - 1].mutatableVariables, persistenceNetwork.mutatableVariables, mutVarSize);
                //    //persistenceNetwork.mutatableVariables = nets[trainTeam][nets.Count - 1].mutatableVariables;
                //    persistenceNetwork.genome = nets[trainTeam][nets.Count - 1].genome;
                //    bestEverError = bestError;
                //    bestGenome = persistenceNetwork.genome.Substring(0, 8) + "a";

                //    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.csv"))
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
                //    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.csv"))
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
                //    if (nets[trainTeam][i].genome.Substring(0, 8) != lastGenome)
                //    {
                //        topGenomes.Add(new NeuralNetwork(nets[trainTeam][i]));
                //        lastGenome = nets[trainTeam][i].genome.Substring(0, 8);
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

                dropChance = (int)dropChanceSlider.value;

                // Make sure final pendingFitness is added
                for (int i = 0; i < populationSize; i++)
                {
                    nets[trainTeam][i].AddFitness(nets[trainTeam][i].pendingFitness);
                    nets[trainTeam][i].dropChance = dropChance; // (Also apply drop chance here before the finalizer)
                }

                nets[trainTeam].Sort();

                bestError[trainTeam] = nets[trainTeam][nets[trainTeam].Count - 1].fitness;
                worstError[trainTeam] = nets[trainTeam][0].fitness;

                optimizeAndShrinkNet = optimizeAndShrinkToggle.isOn;

                if (generationNumber % timeBetweenSave == 0 && timeBetweenSave != -1)
                {
                    bestGenome[trainTeam] = persistenceNetwork[trainTeam].genome.Substring(0, 8) + "a";

                    StreamWriter persistence = new StreamWriter("./Assets/dat/WeightSaveMeta-0.mta");
                    persistence.WriteLine((generationNumber).ToString() + "#" +
                        (bestEverError[0]).ToString() + "#" +
                        ((int)Time.realtimeSinceStartup + timeManager.offsetTime).ToString() + "#" +
                        bestGenome[0]);
                    persistence.Close();
                    StreamWriter persistence2 = new StreamWriter("./Assets/dat/WeightSaveMeta-1.mta");
                    persistence2.WriteLine((generationNumber).ToString() + "#" +
                        (bestEverError[1]).ToString() + "#" +
                        ((int)Time.realtimeSinceStartup + timeManager.offsetTime).ToString() + "#" +
                        bestGenome[1]);
                    persistence2.Close();

                    #region OLD SAVE SYSTEM
                    //// Save best weights
                    //BinaryFormatter bf = new BinaryFormatter();
                    //using (FileStream fs = new FileStream("./Assets/dat/WeightSave.bin", FileMode.Create))
                    //    bf.Serialize(fs, persistenceNetwork.weights);

                    //// Get hash of best weights
                    //bestHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/WeightSave.bin")));

                    //// Save best mutatable variables
                    //BinaryFormatter bf2 = new BinaryFormatter();
                    //using (FileStream fs2 = new FileStream("./Assets/dat/MutVars.bin", FileMode.Create))
                    //    bf2.Serialize(fs2, persistenceNetwork.mutatableVariables);

                    //// Save best dropped neurons
                    //BinaryFormatter bf3 = new BinaryFormatter();
                    //using (FileStream fs3 = new FileStream("./Assets/dat/DroppedNeurons.bin", FileMode.Create))
                    //    bf3.Serialize(fs3, persistenceNetwork.droppedNeurons);
                    #endregion


                    // NEW SAVE SYSTEM

                    // Save all data into file
                    SaveData sd = new SaveData();
                    sd.droppedNeurons = persistenceNetwork[0].droppedNeurons;
                    sd.droppedWeights = persistenceNetwork[0].droppedWeights;
                    sd.weights = persistenceNetwork[0].weights;
                    sd.layers = persistenceNetwork[0].layers;
                    sd.mutVars = persistenceNetwork[0].mutatableVariables;
                    BinaryFormatter bf3 = new BinaryFormatter();
                    using (FileStream fs3 = new FileStream("./Assets/dat/NetworkSaveData-0.bin", FileMode.Create))
                        bf3.Serialize(fs3, sd);
                    // Save all data into file
                    sd = new SaveData();
                    sd.droppedNeurons = persistenceNetwork[1].droppedNeurons;
                    sd.droppedWeights = persistenceNetwork[1].droppedWeights;
                    sd.weights = persistenceNetwork[1].weights;
                    sd.layers = persistenceNetwork[1].layers;
                    sd.mutVars = persistenceNetwork[1].mutatableVariables;
                    bf3 = new BinaryFormatter();
                    using (FileStream fs3 = new FileStream("./Assets/dat/NetworkSaveData-1.bin", FileMode.Create))
                        bf3.Serialize(fs3, sd);

                    persistence.Close();
                }

                // Change the best ever network data and score to beat if it matches any criteria below
                if (((bestError[trainTeam] < bestEverError[trainTeam]&&!optimizeAndShrinkNet) && // If the error is better than the best ever
                    generationNumber > 1) ||  // If this is the first generation

                    // If the optimizeAndShrinkNet option is true, and the used
                    // neuron amount is less than the previous best, but the error
                    // is still within 3% or lower of the best ever to not have too much of a deviation
                    (optimizeAndShrinkNet && (nets[trainTeam][nets[trainTeam].Count - 1].CountDroppedNeurons() >= bestDroppedNeuronsAmnt) && (bestError[trainTeam] <= bestEverError[trainTeam]*1.005f)))
                {
                    //if (optimizeAndShrinkNet && (nets[trainTeam][nets.Count - 1].CountDroppedNeurons() < bestDroppedNeuronsAmnt))
                    //    goto skip0;
                    persistenceNetwork[trainTeam].weights = nets[trainTeam][nets[trainTeam].Count - 1].weights;
                    persistenceNetwork[trainTeam].mutatableVariables = nets[trainTeam][nets[trainTeam].Count - 1].mutatableVariables;
                    persistenceNetwork[trainTeam].droppedNeurons = nets[trainTeam][nets[trainTeam].Count - 1].droppedNeurons;
                    persistenceNetwork[trainTeam].droppedWeights = nets[trainTeam][nets[trainTeam].Count - 1].droppedWeights;
                    persistenceNetwork[trainTeam].genome = nets[trainTeam][nets[trainTeam].Count - 1].genome;
                    if (bestError[trainTeam] < bestEverError[trainTeam])
                        bestEverError[trainTeam] = bestError[trainTeam];
                    bestDroppedNeuronsAmnt = nets[trainTeam][nets[trainTeam].Count - 1].CountDroppedNeurons();

                    //skip0:

                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.csv"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError[0] + ", " + bestError[0] + ", " + bestEverError[1] + ", " + bestError[1] + ", "+((float)bestDroppedNeuronsAmnt / (float)totalNeurons).ToString());

                }
                else if (generationNumber % timeBetweenGenerationProgress == 0 && generationNumber > 1)
                    using (StreamWriter sw = File.AppendText("./Assets/dat/hist.csv"))
                        sw.WriteLine((generationNumber).ToString() + ", " + bestEverError[0] + ", " + bestError[0] + ", " + bestEverError[1] + ", " + bestError[1] + ", " + ((float)bestDroppedNeuronsAmnt / (float)totalNeurons).ToString());

                // Find the top 3 *individual* genomes and add them to a `topGenomes` list
                Debug.Log("lGenome: " + bestGenome[trainTeam]);
                string lastGenome = bestGenome[trainTeam].Substring(0, 8);
                topGenomes = new List<List<NeuralNetwork>>();
                topGenomes.Add(new List<NeuralNetwork>());
                topGenomes.Add(new List<NeuralNetwork>());
                topGenomes[trainTeam].Add(persistenceNetwork[trainTeam]);
                for (int i = nets[trainTeam].Count - 1; i >= 0; i--)
                {
                    if (topGenomes[trainTeam].Count >= 3)
                        break;
                    if (nets[trainTeam][i].genome.Substring(0, 8) != lastGenome)
                    {
                        topGenomes[trainTeam].Add(new NeuralNetwork(nets[trainTeam][i]));
                        lastGenome = nets[trainTeam][i].genome.Substring(0, 8);
                    }
                }
                ListBestGenomes();

                Finalizer();

                lastBest[trainTeam] = bestError[trainTeam];
                lastWorst[trainTeam] = worstError[trainTeam];
                generationNumber++;
                trainTeam = trainTeam == 1 ? 0 : 1;
                trial = 0;
                iterations = maxIterations;

                CreateEntityBodies();
                laser.ResetPosition();

                generationText.text = generationNumber.ToString() + " : " + trial.ToString();
                droppedNeuronsText.text = "Dropped Neurons: " + bestDroppedNeuronsAmnt.ToString() + ", " + Math.Round((float)bestDroppedNeuronsAmnt/ (float)totalNeurons*100f, 1).ToString() + "%";
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

            if (IterateNetEntities() == false || iterations <= 0)
                iterations = 0;
        }
    }

    private void ListBestGenomes()
    {
        string outS = "";
        int count = 10;
        for (int i = nets[trainTeam].Count - 1; i >= 0; i--)
        {
            if (count <= 0)
                break;
            if (!outS.Contains(nets[trainTeam][i].genome))
            {
                count--;
                outS += (nets[trainTeam].Count - 1 - i).ToString() + ". " + nets[trainTeam][i].genome + " " + Math.Round(nets[trainTeam][i].fitness, 3).ToString() + "\n";
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

        List<NeuralNetwork> shuffledOpponents = nets[trainTeam == 0 ? 1 : 0];
        //shuffledOpponents.Shuffle();

        for (int i = 0; i < populationSize; i++)
        {
            GameObject tempEntity = Instantiate(netEntityPrefab, spawnPoint);
            entityList.Add(tempEntity);
            entityList[i].GetComponent<NetEntity>().Init(nets[trainTeam][i], generationNumber, layers[0], maxIterations, trial, netUI, trainTeam, shuffledOpponents[i], null, boardDrawer);
        }
        //}
        //else
        //    for (int i = 0; i < entityList.Count; i++)
        //    {
        //        entityList[i].GetComponent<NetEntity>().Init(nets[trainTeam][i], generationNumber);
        //    }
    }


    private bool IterateNetEntities()
    {
        int amnt = entityList.Count;
        for (int i = 0; i < entityList.Count; i++)
            amnt -= entityList[i].GetComponent<NetEntity>().Elapse(false) ? 0 : 1;
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
        //        nets[trainTeam][i] = new NeuralNetwork(topGenomes[g]);
        //        //nets[trainTeam][i].CopyWeights(topGenomes[g].weights);
        //        //Array.Copy(topGenomes[g].mutatableVariables, nets[trainTeam][i].mutatableVariables, mutVarSize);
        //        nets[trainTeam][i].genome = topGenomes[g].genome;
        //        nets[trainTeam][i].UpdateGenome();
        //        nets[trainTeam][i].Mutate();
        //        //nets[trainTeam][i].MutateMutVars();
        //    }
        //}
        //// Create totally new neural networks with random weights
        //for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * (topGenomes.Count + 1); i < (int)(populationSize * 0.5); i++)
        //{
        //    nets[trainTeam][i].CopyWeights(nets[trainTeam][i].RandomizeWeights());
        //    //nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].RandomizeMutVars();
        //    nets[trainTeam][i].ResetGenome();
        //}
        //// Continue using the best 50% of neural networks and mutate them a bit
        //for (int i = (int)(populationSize * 0.5); i < populationSize - 1; i++)
        //{
        //    if (nets[trainTeam][i].genome.Substring(0, 8) == bestGenome.Substring(0, 8) && // If it is the same genome as the best
        //        Array.IndexOf(nets[trainTeam][i].letters, nets[trainTeam][i].genome[8]) < 5) // And if the mutation level is less than 5 away from the original
        //                                                               // Then randomize it to make population more diverse
        //    {
        //        //UnityEngine.Debug.Log("Resetting of:  " + i.ToString());
        //        nets[trainTeam][i].ResetGenome();
        //        nets[trainTeam][i].CopyWeights(nets[trainTeam][i].RandomizeWeights());
        //        //nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].RandomizeMutVars();
        //    }
        //    else
        //    {
        //        nets[trainTeam][i].Mutate();
        //        //nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars(); // This causes problem in best NN
        //        nets[trainTeam][i].UpdateGenome();
        //        //UnityEngine.Debug.Log("Mutating Vars of:  " + i.ToString() + " , got: " + nets[trainTeam][i].mutatableVariables[0].ToString());
        //    }
        //}

        //// Hanbdle the best network
        ////Debug.Log("Best is index: " + (nets.Count - 1).ToString());
        ////Debug.Log("Vars are: " + (persistenceNetwork.mutatableVariables[0]).ToString());
        ////nets[trainTeam][nets.Count - 1] = new NeuralNetwork(persistenceNetwork);
        //nets[trainTeam][nets.Count - 1] = new NeuralNetwork(topGenomes[0]);
        ////nets[trainTeam][nets.Count - 1].neurons = persistenceNetwork.neurons;
        ////nets[trainTeam][nets.Count - 1].genome = bestGenome;
        ////Array.Copy(persistenceNetwork.mutatableVariables, nets[trainTeam][nets.Count - 1].mutatableVariables, mutVarSize);
        ////Debug.Log("Vars are: " + (nets[trainTeam][nets.Count - 1].mutatableVariables[0]).ToString());

        //// If any neural networks have an invalid genome, reset it and assign a genome
        //for (int i = 0; i < populationSize; i++)
        //{
        //    if (nets[trainTeam][i].genome.Trim() == "")
        //    {
        //        nets[trainTeam][i].ResetGenome();
        //        nets[trainTeam][i].CopyWeights(nets[trainTeam][i].RandomizeWeights());
        //        //nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].RandomizeMutVars();
        //        Debug.LogWarning("Found broken genome " + i.ToString());
        //        nets[trainTeam][i].Mutate();
        //        nets[trainTeam][i].UpdateGenome();
        //    }
        //    nets[trainTeam][i].fitness = 0f;
        //    nets[trainTeam][i].pendingFitness = 0f;
        //    nets[trainTeam][i].isBest = false;
        //    nets[trainTeam][i].netID = i;

        //    // Save temp weights
        //    BinaryFormatter bf = new BinaryFormatter();
        //    using (FileStream fs = new FileStream("./Assets/dat/temp_weights.bin", FileMode.Create))
        //        bf.Serialize(fs, nets[trainTeam][i].weights);
        //    // Get hash of temp weights
        //    nets[trainTeam][i].weightsHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/temp_weights.bin")));

        //}
        //nets[trainTeam][nets.Count - 1].isBest = true;
        ////Array.Copy(persistenceNetwork.mutatableVariables, bestMutVars, mutVarSize);
        ////bestMutVars = persistenceNetwork.mutatableVariables;


        // OLD SYSTEM:
        //
        // If using the optimizers, just create many versions of the best ever network and mutate slightly
        if (optimizeAndShrinkNet)
        {
            //// Create copies of top 3 genomes to replace the worst neural networks
            //for (int g = 0; g < topGenomes.Count; g++)
            //{
            //    for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * g; i < (int)(populationSize * 0.2 / topGenomes.Count) * (g + 1); i++)
            //    {
            //        nets[trainTeam][i] = new NeuralNetwork(topGenomes[trainTeam][g]);
            //        //nets[trainTeam][i].CopyWeights(topGenomes[g].weights);
            //        //Array.Copy(topGenomes[g].mutatableVariables, nets[trainTeam][i].mutatableVariables, mutVarSize);
            //        nets[trainTeam][i].genome = topGenomes[trainTeam][g].genome;
            //        nets[trainTeam][i].Mutate();
            //        nets[trainTeam][i].UpdateGenome();
            //        nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].MutateDroppedNeurons();
            //        nets[trainTeam][i].droppedWeights = nets[trainTeam][i].MutateDroppedWeights();
            //        nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
            //    }
            //}
            //for (int i = (int)(populationSize * 0.2 / topGenomes.Count) * (topGenomes.Count + 1); i < (int)(populationSize * 0.5); i++)
            //{
            //    nets[trainTeam][i] = new NeuralNetwork(persistenceNetwork[trainTeam]);
            //    nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].MutateDroppedNeurons();
            //    nets[trainTeam][i].droppedWeights = nets[trainTeam][i].MutateDroppedWeights();
            //    nets[trainTeam][i].genome = nets[trainTeam][i].GenerateGenome();
            //    nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
            //}
            //// Continue using the best 50% of neural networks and mutate them a bit
            //for (int i = (int)(populationSize * 0.5); i < populationSize; i++)
            //{
            //    if (nets[trainTeam][i].genome.Substring(0, 8) == bestGenome[trainTeam].Substring(0, 8) && // If it is the same genome as the best
            //        Array.IndexOf(nets[trainTeam][i].letters, nets[trainTeam][i].genome[8]) < 5 && // And if the mutation level is less than 5 away from the original
            //        i < populationSize - 11)                                   // And it is not in the top 10
            //                                                                   // Then randomize it to make population more diverse
            //    {
            //        nets[trainTeam][i].ResetGenome();
            //        nets[trainTeam][i].CopyWeights(nets[trainTeam][i].RandomizeWeights());
            //        nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].RandomizeDroppedNeurons();
            //        nets[trainTeam][i].droppedWeights = nets[trainTeam][i].RandomizeDroppedWeights();
            //    }
            //    else
            //    {
            //        nets[trainTeam][i] = new NeuralNetwork(nets[trainTeam][i]);
            //        nets[trainTeam][i].Mutate();
            //        nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].MutateDroppedNeurons();
            //        nets[trainTeam][i].droppedWeights = nets[trainTeam][i].MutateDroppedWeights();
            //        nets[trainTeam][i].UpdateGenome();
            //        nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
            //    }
            //}
        }
        // Otherwise, use normal distribution and randomization on population
        else
        {
            // Create copies of top 3 genomes to replace the worst neural networks
            for (int g = 0; g < topGenomes[trainTeam].Count; g++)
            {
                for (int i = (int)(populationSize * 0.2 / topGenomes[trainTeam].Count) * g; i < (int)(populationSize * 0.2 / topGenomes[trainTeam].Count) * (g + 1); i++)
                {
                    nets[trainTeam][i] = new NeuralNetwork(topGenomes[trainTeam][g]);
                    //nets[trainTeam][i].CopyWeights(topGenomes[g].weights);
                    //Array.Copy(topGenomes[g].mutatableVariables, nets[trainTeam][i].mutatableVariables, mutVarSize);
                    nets[trainTeam][i].genome = topGenomes[trainTeam][g].genome;
                    nets[trainTeam][i].Mutate();
                    nets[trainTeam][i].UpdateGenome();
                    nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].MutateDroppedNeurons();
                    nets[trainTeam][i].droppedWeights = nets[trainTeam][i].MutateDroppedWeights();
                    nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
                }
            }
            // Create create totally new neural networks with random weights
            for (int i = (int)(populationSize * 0.2 / topGenomes[trainTeam].Count) * (topGenomes[trainTeam].Count + 1); i < (int)(populationSize * 0.5); i++)
            {
                nets[trainTeam][i] = new NeuralNetwork(persistenceNetwork[trainTeam]);
                nets[trainTeam][i].weights = nets[trainTeam][i].RandomizeWeights();
                nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].RandomizeDroppedNeurons();
                nets[trainTeam][i].droppedWeights = nets[trainTeam][i].RandomizeDroppedWeights();
                nets[trainTeam][i].genome = nets[trainTeam][i].GenerateGenome();
                nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
            }
            // Continue using the best 50% of neural networks and mutate them a bit
            for (int i = (int)(populationSize * 0.5); i < populationSize; i++)
            {
                if (nets[trainTeam][i].genome.Substring(0, 8) == bestGenome[trainTeam].Substring(0, 8) && // If it is the same genome as the best
                    Array.IndexOf(nets[trainTeam][i].letters, nets[trainTeam][i].genome[8]) < 5 && // And if the mutation level is less than 5 away from the original
                    i < populationSize - 11)                                   // And it is not in the top 10
                                                                               // Then randomize it to make population more diverse
                {
                    nets[trainTeam][i].ResetGenome();
                    nets[trainTeam][i].CopyWeights(nets[trainTeam][i].RandomizeWeights());
                    nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].RandomizeDroppedNeurons();
                    nets[trainTeam][i].droppedWeights = nets[trainTeam][i].RandomizeDroppedWeights();
                }
                else
                {
                    nets[trainTeam][i] = new NeuralNetwork(nets[trainTeam][i]);
                    nets[trainTeam][i].Mutate();
                    nets[trainTeam][i].droppedNeurons = nets[trainTeam][i].MutateDroppedNeurons();
                    nets[trainTeam][i].droppedWeights = nets[trainTeam][i].MutateDroppedWeights();
                    nets[trainTeam][i].UpdateGenome();
                    nets[trainTeam][i].mutatableVariables = nets[trainTeam][i].MutateMutVars();
                }
            }
        }
        for (int i = 0; i < populationSize; i++)
        {
            nets[trainTeam][i].isBest = false;
            nets[trainTeam][i].netID = i;
        }
        nets[trainTeam][0] = new NeuralNetwork(persistenceNetwork[trainTeam]);
        nets[trainTeam][0].droppedNeurons = persistenceNetwork[trainTeam].droppedNeurons;
        nets[trainTeam][0].droppedWeights = persistenceNetwork[trainTeam].droppedWeights;
        nets[trainTeam][0].mutatableVariables = persistenceNetwork[trainTeam].mutatableVariables;
        nets[trainTeam][0].genome = persistenceNetwork[trainTeam].genome;
        nets[trainTeam][0].isBest = true;


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

        nets = new List<List<NeuralNetwork>>();
        nets.Add(new List<NeuralNetwork>());
        nets.Add(new List<NeuralNetwork>());

        for (int t = 0; t < 2; t++)
            for (int i = 0; i < populationSize; i++)
            {
                // If no weights were loaded, create random network
                Debug.Log(bestGenome.Length);
                if (bestGenome[t] == "")
                {
                    NeuralNetwork net = new NeuralNetwork(layers, null);
                    //Debug.Log("* Creating net: " + i + " of " + populationSize);

                    net.learningRate = learningRate;
                    net.layers = layers;
                    //net.mutVarSize = mutVarSize;
                    net.genome = net.GenerateGenome();
                    net.CopyWeights(net.RandomizeWeights());
                    //net.droppedNeurons = net.RandomizeDroppedNeurons();
                    //net.mutatableVariables = net.RandomizeMutVars();
                    net.mutatableVariables[0] = 0.25f;
                    net.startingNeuronPercent = startingNeuronPercent;
                    net.droppedNeurons = net.InitDroppedNeurons();
                    net.droppedWeights = net.InitDroppedWeights();
                    //net.genome = "blankgena";

                    nets[t].Add(net);
                }
                // Else load persistence weights
                else
                {
                    NeuralNetwork net = new NeuralNetwork(persistenceNetwork[t]);
                    //Debug.Log("* Creating net: " + i + " of " + populationSize);

                    net.learningRate = learningRate;
                    net.layers = layers;
                    //net.mutVarSize = mutVarSize;
                    net.genome = bestGenome[t];
                    net.mutatableVariables = persistenceNetwork[t].mutatableVariables;
                    net.startingNeuronPercent = startingNeuronPercent;

                    //Array.Copy(persistenceNetwork.mutatableVariables, net.mutatableVariables, mutVarSize);

                    nets[t].Add(net);
                }
            }
        //nets[trainTeam][0].isBest = true;

        if (bestGenome[0] == "")
        {
            bestGenome[0] = nets[0][0].GenerateGenome();
            persistenceNetwork[0].genome = bestGenome[0];
            persistenceNetwork[0].mutatableVariables[0] = 0.25f;
            persistenceNetwork[0].CopyWeights(persistenceNetwork[0].RandomizeWeights());
            persistenceNetwork[0].startingNeuronPercent = startingNeuronPercent;
            //persistenceNetwork.InitDroppedNeurons();
            persistenceNetwork[0].droppedNeurons = persistenceNetwork[0].InitDroppedNeurons();
            persistenceNetwork[0].droppedWeights = persistenceNetwork[0].InitDroppedWeights();

            bestGenome[1] = nets[1][0].GenerateGenome();
            persistenceNetwork[1].genome = bestGenome[1];
            persistenceNetwork[1].mutatableVariables[1] = 0.25f;
            persistenceNetwork[1].CopyWeights(persistenceNetwork[1].RandomizeWeights());
            persistenceNetwork[1].startingNeuronPercent = startingNeuronPercent;
            //persistenceNetwork.InitDroppedNeurons();
            persistenceNetwork[1].droppedNeurons = persistenceNetwork[1].InitDroppedNeurons();
            persistenceNetwork[1].droppedWeights = persistenceNetwork[1].InitDroppedWeights();

            //persistenceNetwork[trainTeam].mutatableVariables = persistenceNetwork[trainTeam].RandomizeMutVars();
            //persistenceNetwork[trainTeam].RandomizeWeights();
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
            persistenceNetwork = new List<NeuralNetwork>();
            persistenceNetwork.Add(new NeuralNetwork(layersWithBiases, null));
            persistenceNetwork.Add(new NeuralNetwork(layersWithBiases, null));

            #region OLD LOADING SYSTEM
            // OLD LOADING SYSTEM
            //// Load weights data into `persistenceNetwork`
            //BinaryFormatter bf = new BinaryFormatter();
            //using (FileStream fs = new FileStream("./Assets/dat/WeightSave.bin", FileMode.Open))
            //    persistenceNetwork.weights = (double[][][])bf.Deserialize(fs);
            //// Load mutVar data into `persistenceNetwork`
            //BinaryFormatter bf2 = new BinaryFormatter();
            //using (FileStream fs2 = new FileStream("./Assets/dat/MutVars.bin", FileMode.Open))
            //    persistenceNetwork.mutatableVariables = (double[])bf2.Deserialize(fs2);
            //// Load dropped neurons data into `persistenceNetwork`
            //BinaryFormatter bf3 = new BinaryFormatter();
            //using (FileStream fs3 = new FileStream("./Assets/dat/DroppedNeurons.bin", FileMode.Open))
            //    persistenceNetwork.droppedNeurons = (bool[][])bf3.Deserialize(fs3);
            ////bestMutVars = persistenceNetwork.mutatableVariables;

            //// Get hash of best weights
            //bestHash = ByteArrayToString(new MD5CryptoServiceProvider().ComputeHash(System.IO.File.ReadAllBytes("./Assets/dat/WeightSave.bin")));
            #endregion

            // NEW LOADING SYSTEM

            // Load save data into persistence network
            SaveData sd = new SaveData();
            BinaryFormatter bf3 = new BinaryFormatter();
            using (FileStream fs3 = new FileStream("./Assets/dat/NetworkSaveData-0.bin", FileMode.Open))
                sd = (SaveData)bf3.Deserialize(fs3);
            persistenceNetwork[0].weights = sd.weights;
            persistenceNetwork[0].mutatableVariables = sd.mutVars;
            persistenceNetwork[0].droppedNeurons = sd.droppedNeurons;
            persistenceNetwork[0].droppedWeights = sd.droppedWeights;
            using (FileStream fs3 = new FileStream("./Assets/dat/NetworkSaveData-1.bin", FileMode.Open))
                sd = (SaveData)bf3.Deserialize(fs3);
            persistenceNetwork[1].weights = sd.weights;
            persistenceNetwork[1].mutatableVariables = sd.mutVars;
            persistenceNetwork[1].droppedNeurons = sd.droppedNeurons;
            persistenceNetwork[1].droppedWeights = sd.droppedWeights;
            bestDroppedNeuronsAmnt = persistenceNetwork[trainTeam].CountDroppedNeurons();


            // Load metadata like best error and generation
            StreamReader sr = File.OpenText("./Assets/dat/WeightSaveMeta-0.mta");
            string firstLine = sr.ReadLine().Trim();
            generationNumber = int.Parse(firstLine.Split('#')[0]) + 1;
            bestEverError[0] = double.Parse(firstLine.Split('#')[1]);
            timeManager.offsetTime = int.Parse(firstLine.Split('#')[2]);
            bestGenome[0] = firstLine.Split('#')[3];
            persistenceNetwork[0].genome = bestGenome[0];
            sr.Close();

            // Load metadata like best error and generation
            StreamReader sr2 = File.OpenText("./Assets/dat/WeightSaveMeta-1.mta");
            string firstLine2 = sr2.ReadLine().Trim();
            //generationNumber = int.Parse(firstLine.Split('#')[0]) + 1;
            bestEverError[1] = double.Parse(firstLine2.Split('#')[1]);
            //timeManager.offsetTime = int.Parse(firstLine.Split('#')[2]);
            bestGenome[1] = firstLine2.Split('#')[3];
            persistenceNetwork[1].genome = bestGenome[1];
            sr2.Close();

        }
        catch (Exception)
        {
            bestGenome[0] = "blankgena";
            bestGenome[1] = "blankgena";
            Debug.LogWarning("Failed to load network data, possible mismatch in size?");
        }
    }

    static void Upload(float fitness)
    {
        File.Copy("./Assets/dat/WeightSave.dat", "./Assets/dat/" + fitness + "_WeightSave.dat");
        Debug.Log("* Copied \"./Assets/dat/WeightSave.dat\" to \"./Assets/dat/" + fitness + "_WeightSave.dat\"");
        File.Copy("./Assets/dat/WeightSaveMeta.mta", "./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        Debug.Log("* Copied \"./Assets/dat/WeightSaveMeta.mta\" to \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        // Upload weight save
        Debug.Log("* Uploading \"./Assets/dat/" + fitness + "_WeightSave.dat\" to http://achillium.us.to/digitneuralnet/");
        System.Net.WebClient Client = new System.Net.WebClient();
        Client.Headers.Add("enctype", "multipart/form-data");
        byte[] result = Client.UploadFile("http://achillium.us.to/digitneuralnet/uploadweights.php", "POST", "./Assets/dat/" + fitness + "_WeightSave.dat");
        string s = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);
        Debug.Log("* Uploaded \"./Assets/dat/" + fitness + "_WeightSave.dat\"");

        // Upload weight save meta
        Debug.Log("* Uploading \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\" to http://achillium.us.to/digitneuralnet/");
        System.Net.WebClient ClientTwo = new System.Net.WebClient();
        ClientTwo.Headers.Add("enctype", "multipart/form-data");
        byte[] resultTwo = ClientTwo.UploadFile("http://achillium.us.to/digitneuralnet/uploadweights.php", "POST", "./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        string sTwo = System.Text.Encoding.UTF8.GetString(resultTwo, 0, resultTwo.Length);
        Debug.Log("* Uploaded \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        File.Delete("./Assets/dat/" + fitness + "_WeightSave.dat");
        Debug.Log("* Deleted Copy at \"./Assets/dat/" + fitness + "_WeightSave.dat\"");
        File.Delete("./Assets/dat/" + fitness + "_WeightSaveMeta.mta");
        Debug.Log("* Deleted Copy at \"./Assets/dat/" + fitness + "_WeightSaveMeta.mta\"");

        Debug.Log("* Synced with server");
    }

    static void Download(string s)
    {
        System.Net.WebClient Client = new System.Net.WebClient();

        Debug.Log("* Downloading \"" + s + "_WeightSave.dat\" from http://achillium.us.to/digitneuralnet/" + s + "_WeightSave.dat");
        Client.DownloadFile(new Uri("http://achillium.us.to/digitneuralnet/" + s + "_WeightSave.dat"), @".\dat\temp_WeightSave.dat");
        Debug.Log("* Downloaded \"" + s + "_WeightSave.dat\"");
        Debug.Log("* Downloading \"" + s + "_WeightSaveMeta.mta\" from http://achillium.us.to/digitneuralnet/" + s + "_WeightSaveMeta.mta");
        Client.DownloadFile(new Uri("http://achillium.us.to/digitneuralnet/" + s + "_WeightSaveMeta.mta"), @".\dat\temp_WeightSaveMeta.mta");
        Debug.Log("* Downloaded \"" + s + "_WeightSaveMeta.mta\"");

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

        Debug.Log("* Synced with server");
    }
}
