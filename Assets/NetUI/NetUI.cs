using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class NetUI : MonoBehaviour
{
    public GameObject linePrefab;
    public GameObject nodePrefab;
    public NetManager netManager;

    public SpriteRenderer[][] nodes;
    public LineRenderer[][][] lines;

    float height = 0;

    void Start()
    {
        // Generate grid of nodes and lines
        float curX = 0;
        float curY = 0;
        int[] layers = netManager.layers;
        // Size node array
        List<SpriteRenderer[]> tmpNodes = new List<SpriteRenderer[]>();
        for (int i = 0; i < layers.Length; i++)
            tmpNodes.Add(new SpriteRenderer[layers[i] + (i == layers.Length - 1 || i == 0 ? 0 : 1)]);
        nodes = tmpNodes.ToArray();
        // Size line array
        InitLines();
        // For each layer, add a column of nodes
        for (int i = 0; i < nodes.Length; i++)
        {
            float shiftDistance = (i == 0 || i == nodes.Length - 1) ? 0.6f : 0.2f;
            curY = -(nodes[i].Length * shiftDistance / 2f);
            if (nodes[i].Length * shiftDistance > height)
                height = nodes[i].Length * shiftDistance;
            float scaleAmount = (i == 0 || i == nodes.Length - 1) ? 0.4f : 0.1f;
            for (int j = 0; j < nodes[i].Length; j++) // For each neuron add a node
            {
                nodes[i][j] = Instantiate(nodePrefab, new Vector3((curX + shiftDistance) + transform.position.x, curY + transform.position.y, -0.7f), Quaternion.identity).GetComponent<SpriteRenderer>();
                nodes[i][j].transform.localScale = new Vector3(scaleAmount, scaleAmount, scaleAmount);
                nodes[i][j].gameObject.hideFlags = HideFlags.HideInHierarchy;
                nodes[i][j].transform.parent = transform;
                curY += shiftDistance;
            }
            if (i == nodes.Length - 1)
                break;
            curX += shiftDistance * 2f;
        }

        // For each node, add its line connections
        for (int i = 1; i < nodes.Length; i++)
        {
            for (int j = 0; j < nodes[i].Length; j++) // For each neuron
            {
                // For all synapses connected to that neuron
                for (int k = 0; k < nodes[i - 1].Length; k++)
                {
                    lines[i - 1][j][k] = Instantiate(linePrefab, new Vector3(0, 0, -0.7f), Quaternion.identity).GetComponent<LineRenderer>();
                    lines[i - 1][j][k].transform.parent = transform;
                    lines[i - 1][j][k].gameObject.hideFlags = HideFlags.HideInHierarchy;
                    LineRenderer lr = lines[i - 1][j][k];
                    lr.SetPosition(0, nodes[i - 1][k].transform.position);
                    lr.SetPosition(1, nodes[i][j].transform.position);
                }

                //nodes[i][j] = (Instantiate(nodePrefab, new Vector3(curX, curY, 0), Quaternion.identity));
            }
        }

        // Scale the entire thing so it fits within the screen
        transform.localScale = new Vector3(7f / height, 7f / height, 7f / height);
    }

    void InitLines()
    {
        List<LineRenderer[][]> weightsList = new List<LineRenderer[][]>(); //weights list which will later be converted into a weights 3D array

        //itterate over all layers
        for (int i = 1; i < nodes.Length; i++)
        {
            List<LineRenderer[]> layerWeightsList = new List<LineRenderer[]>(); //layer weight list for this current layer (will be converted to 2D array)

            int neuronsInPreviousLayer = nodes[i - 1].Length;

            //itterate over all neurons in this current layer
            for (int j = 0; j < nodes[i].Length; j++)
            {
                LineRenderer[] neuronWeights = new LineRenderer[neuronsInPreviousLayer]; //neurons weights

                //itterate over all neurons in the previous layer and set the weights randomly between 0.5f and -0.5
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                    neuronWeights[k] = null;

                layerWeightsList.Add(neuronWeights); //add neuron weights of this current layer to layer weights
            }

            weightsList.Add(layerWeightsList.ToArray()); //add this layers weights converted into 2D array into weights list
        }

        lines = weightsList.ToArray(); //convert to 3D array
    }

    public void UpdateWeightLines(double[][][] weights)
    {
        if (weights != null)
            for (int i = 0; i < lines.Length; i++)
            {
                for (int j = 0; j < lines[i].Length; j++)
                {
                    for (int k = 0; k < lines[i][j].Length; k++)
                    {
                        // Set color, blue is negative, red is positive, (clear is 0), and the thicker a
                        // line is the higher and closer it is to +=1
                        Color c = weights[i][j][k] > 0 ? Color.red : Color.blue;
                        if (weights[i][j][k] == 0)
                            c = Color.clear;
                        float width = Mathf.Abs((float)weights[i][j][k]) / 10f;
                        lines[i][j][k].startColor = c;
                        lines[i][j][k].endColor = c;
                        lines[i][j][k].startWidth = width;
                        lines[i][j][k].endWidth = width;
                        //lines[i][j][k].sortingOrder = 1500+(100-(int)(width*1000));
                        lines[i][j][k].sortingOrder = 1500 + (UnityEngine.Random.Range(0, 500));
                    }
                }
            }
    }

    public void UpdateOutputs(double[] outputs)
    {
        // For all nodes in the final layer, set equal to the corresponding output
        for (int i = 0; i < nodes[nodes.Length - 1].Length; i++)
        {
            float a = (float)outputs[i];
            nodes[nodes.Length - 1][i].color = new Color(a, a, a);
        }
    }

    public void UpdateInputs(double[] inputs)
    {
        // For all nodes in the final layer, set equal to the corresponding output
        for (int i = 0; i < inputs.Length; i++)
        {
            float a = (float)(inputs[i] + 1f) / 2f;
            nodes[0][i].color = new Color(a, a, a);
        }
    }

    void Update()
    {

    }
}
