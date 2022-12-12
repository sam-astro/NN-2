
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Neural Network C# (Unsupervised)
/// </summary>
public class NeuralNetwork : IComparable<NeuralNetwork>
{
    public int[] layers; //layers
    public double[][] neurons; //neuron matix
    public double[][] neuronError; //calculated error for each neuroon
    public double[][] droppedNeurons; //dropped neuron matix
    public double[][][] weights; //weight matrix
    public double fitness; //fitness of the network
    public double pendingFitness; // pending trial fitness of the network

    public float learningRate = 1f;


    /// <summary>
    /// Initilizes and neural network with random weights
    /// </summary>
    /// <param name="layers">layers to the neural network</param>
    public NeuralNetwork(int[] layers, double[][][] persistenceWeights)
    {
        //deep copy of layers of this network 
        this.layers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            this.layers[i] = layers[i];
        }


        //generate matrix
        InitNeurons();
        InitWeights(persistenceWeights);
    }

    /// <summary>
    /// Deep copy constructor 
    /// </summary>
    /// <param name="copyNetwork">Network to deep copy</param>
    public NeuralNetwork(NeuralNetwork copyNetwork)
    {
        this.layers = new int[copyNetwork.layers.Length];
        for (int i = 0; i < copyNetwork.layers.Length; i++)
        {
            this.layers[i] = copyNetwork.layers[i];
        }

        InitNeurons();
        InitWeights(null);
        CopyWeights(copyNetwork.weights);
    }

    private void CopyWeights(double[][][] copyWeights)
    {
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    weights[i][j][k] = copyWeights[i][j][k];
                }
            }
        }
    }

    /// <summary>
    /// Create neuron matrix
    /// </summary>
    private void InitNeurons()
    {
        //Neuron Initilization
        List<double[]> neuronsList = new List<double[]>();

        for (int i = 0; i < layers.Length; i++) //run through all layers
        {
            neuronsList.Add(new double[layers[i]]); //add layer to neuron list
        }

        neurons = neuronsList.ToArray(); //convert list to array
        droppedNeurons = neurons;
    }

    public void RandomizeWeights()
    {
        List<double[][]> weightsList = new List<double[][]>(); //weights list which will later be converted into a weights 3D array

        //itterate over all neurons that have a weight connection
        for (int i = 1; i < layers.Length; i++)
        {
            List<double[]> layerWeightsList = new List<double[]>(); //layer weight list for this current layer (will be converted to 2D array)

            int neuronsInPreviousLayer = layers[i - 1];

            //itterate over all neurons in this current layer
            for (int j = 0; j < neurons[i].Length; j++)
            {
                double[] neuronWeights = new double[neuronsInPreviousLayer]; //neruons weights

                //itterate over all neurons in the previous layer and set the weights randomly between 0.5f and -0.5
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    //give random weights to neuron weights
                    //neuronWeights[k] = UnityEngine.Random.Range(-0.5f,0.5f);
                    neuronWeights[k] = new Random().Next(-50, 50) / 100.0d;
                }

                layerWeightsList.Add(neuronWeights); //add neuron weights of this current layer to layer weights
            }

            weightsList.Add(layerWeightsList.ToArray()); //add this layers weights converted into 2D array into weights list
        }

        weights = weightsList.ToArray(); //convert to 3D array
    }

    /// <summary>
    /// Create weights matrix.
    /// </summary>
    private void InitWeights(double[][][] persistenceWeights)
    {
        //StreamReader streamReader = File.OpenText("./Assets/dat/WeightSave.dat");
        //string[] lines = streamReader.ReadToEnd().Split('\n');
        //streamReader.Close();

        if (persistenceWeights != null)
            weights = persistenceWeights;
        else
        {
            List<double[][]> weightsList = new List<double[][]>(); //weights list which will later be converted into a weights 3D array

            //itterate over all neurons that have a weight connection
            for (int i = 1; i < layers.Length; i++)
            {
                List<double[]> layerWeightsList = new List<double[]>(); //layer weight list for this current layer (will be converted to 2D array)

                int neuronsInPreviousLayer = layers[i - 1];

                //itterate over all neurons in this current layer
                for (int j = 0; j < neurons[i].Length; j++)
                {
                    double[] neuronWeights = new double[neuronsInPreviousLayer]; //neurons weights

                    //itterate over all neurons in the previous layer and set the weights randomly between 0.5f and -0.5
                    for (int k = 0; k < neuronsInPreviousLayer; k++)
                    {
                        //give random weights to neuron weights
                        neuronWeights[k] = UnityEngine.Random.Range(0f, 2f);
                        //neuronWeights[k] = new Random().Next(-50, 50) / 100.0d;
                    }

                    layerWeightsList.Add(neuronWeights); //add neuron weights of this current layer to layer weights
                }

                weightsList.Add(layerWeightsList.ToArray()); //add this layers weights converted into 2D array into weights list
            }

            weights = weightsList.ToArray(); //convert to 3D array
        }
    }

    /// <summary>
    /// Feed forward this neural network with a given input array
    /// </summary>
    /// <param name="inputs">Inputs to network</param>
    /// <returns></returns>
    public double[] FeedForward(double[] inputs)
    {
        //Add inputs to the neuron matrix
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        //itterate over all neurons and compute feedforward values 
        for (int i = 1; i < layers.Length; i++) // For each layer
        {
            for (int j = 0; j < neurons[i].Length; j++) // For each neuron in that layer
            {
                double value = 0f;

                for (int k = 0; k < neurons[i - 1].Length; k++) // For all synapses connected to that neuron, add up the weight*neuron
                    value += (weights[i - 1][j][k] * neurons[i - 1][k]);


                // If the layer is the final output layer
                if (i == neurons.Length - 1)
                    neurons[i][j] = (double)Sigmoid(value); // Use TanH activation function 
                else
                {
                    //if (droppedNeurons[i][j] == 10)
                    //    neurons[i][j] = 0.0001d;
                    //else if (droppedNeurons[i][j] != 10)
                    neurons[i][j] = (double)Tanh(value); // Use Leaky ReLU Function
                }

                //neurons[i][j] = (double)Math.Tanh(value); //Hyperbolic tangent activation
                //neurons[i][j] = value;
            }
        }

        return neurons[neurons.Length - 1]; //return output layer
    }
    public static double Sigmoid(double value)
    {
        return 1.0d / (1.0d + (double)Math.Exp(-value));
    }
    public static double dSigmoid(double value)
    {
        return value * (1.0d - value);
    }
    public static double dTanh(double value)
    {
        return 1.0d - Math.Pow((double)Math.Tanh(value), 2.0d);
    }
    public static double Tanh(double value)
    {
        return (double)Math.Tanh(value);
    }
    public static double LeakyReLU(double value)
    {
        return (double)Math.Max(0.01d * value, value);
    }

    public void BackPropagation(double[] expectedOutput)
    {
        neuronError = neurons;

        //itterate over output neurons and compute error values
        for (int j = 0; j < neurons[neurons.Length - 1].Length; j++)
        {
            neuronError[neurons.Length - 1][j] = dSigmoid(neurons[neurons.Length - 1][j]) * (expectedOutput[j] - neurons[neurons.Length - 1][j]);
            for (int k = 0; k < weights[neurons.Length - 2][j].Length; k++)
            {
                weights[neurons.Length - 2][j][k] += neuronError[neurons.Length - 1][j] * (neurons[neurons.Length - 1][j] * weights[neurons.Length - 2][j][k]);
                // Also add to   bias[i][j] += neuronError[i][j];
            }
        }


        //itterate over all neurons BACKWARDS and compute error values
        for (int i = neurons.Length - 2; i > 0; i--)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                //double value = 0f;

                double allConnectedWeights = 1;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    allConnectedWeights *= weights[i - 1][j][k];
                }

                neuronError[i][j] = dSigmoid(neurons[i][j]) * (neuronError[neurons.Length - 1][0] * allConnectedWeights);
                //neuronError[i][j] = dSigmoid(hiddenNeuron1.output) * outputNeuron.error * outputNeuron.weights[0];

                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    weights[i - 1][j][k] += neuronError[i][j] * (neuronError[i - 1][k] * weights[i - 1][j][k]);
                    // Also add to   bias[i][j] += neuronError[i][j];
                }
            }
        }
    }

    /// <summary>
    /// Mutate neural network weights
    /// </summary>
    public void Mutate()
    {
        Parallel.For(0, weights.Length, i =>
        {
            Parallel.For(0, weights[i].Length, j =>
            {
                Parallel.For(0, weights[i][j].Length, k =>
                {
                    double weight = weights[i][j][k];

                    //mutate weight value 
                    double randomNumber = new Random().Next(0, 100) * (1d - learningRate);

                    if (randomNumber <= 2f)
                    { //if 3
                      //randomly increase by 0% to 1%
                        double factor = new Random().Next(0, 100) / 10000.0d;
                        weight += factor;
                    }
                    else if (randomNumber <= 4f)
                    { //if 4
                      //randomly change by -1% to 1%
                        double factor = new Random().Next(-100, 100) / 10000.0d;
                        weight += factor;
                    }
                    else if (randomNumber <= 8f)
                    { //if 5
                      //randomly increase or decrease weight by tiny amount
                        double factor = new Random().Next(-1000, 1000) / 100.0d / 10000.0d;
                        weight += factor;
                    }
                    //else if (randomNumber <= 10f)
                    //{
                    //    //flip sign
                    //    weight *= -1;
                    //}
                    else if (randomNumber <= 12f)
                    {
                        //add 0.001
                        weight += 0.001d;
                    }
                    else if (randomNumber <= 14f)
                    {
                        //sub 0.001
                        weight -= 0.001d;
                    }
                    else if (randomNumber <= 13f)
                    {
                        //totally randomize
                        weight = UnityEngine.Random.Range(-10f, 10f); ;
                    }
                    else if (randomNumber <= 19f)
                    { //if 5
                      //randomly increase or decrease weight by tiny amount
                        double factor = new Random().Next(-1000, 1000) / 100.0d / 10000.0d;
                        weight += factor;
                    }

                    weights[i][j][k] = weight;
                });
            });
        });
        //for (int i = 0; i < weights.Length; i++)
        //{
        //    for (int j = 0; j < weights[i].Length; j++)
        //    {
        //        for (int k = 0; k < weights[i][j].Length; k++)
        //        {
        //            double weight = weights[i][j][k];

        //            //mutate weight value 
        //            double randomNumber = new Random().Next(0, 100);

        //            if (randomNumber <= 2f)
        //            { //if 3
        //              //randomly increase by 0% to 1%
        //                double factor = new Random().Next(0, 100) / 10000.0f;
        //                weight += factor;
        //            }
        //            else if (randomNumber <= 4f)
        //            { //if 4
        //              //randomly decrease by 0% to 1%
        //                double factor = new Random().Next(-100, 100) / 10000.0f;
        //                weight -= factor;
        //            }
        //            else if (randomNumber <= 8f)
        //            { //if 5
        //              //randomly increase or decrease weight by tiny amount
        //                double factor = new Random().Next(-1000, 1000) / 100.0f / 100000;
        //                weight += factor;
        //            }
        //            //else
        //            //{
        //            //    //pick random weight between -1 and 1
        //            //    weight = new Random().Next(-100, 100) / 100.0f;
        //            //}

        //            weights[i][j][k] = weight;
        //        }
        //    }
        //}
    }

    public void AddFitness(double fit)
    {
        fitness += fit;
    }

    public void SetFitness(double fit)
    {
        fitness = fit;
    }

    public double GetFitness()
    {
        return fitness;
    }

    /// <summary>
    /// Compare two neural networks and sort based on fitness
    /// </summary>
    /// <param name="other">Network to be compared to</param>
    /// <returns></returns>
    public int CompareTo(NeuralNetwork other)
    {
        if (other == null) return 1;

        if (fitness > other.fitness)
            return -1;
        else if (fitness < other.fitness)
            return 1;
        else
            return 0;
    }
}