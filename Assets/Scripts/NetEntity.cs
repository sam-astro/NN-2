using System.Collections;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[System.Serializable]
public class Sense
{
    public string name;
    [Header("What to sense for:")]
    public bool distanceToObject;
    public bool horizontalDifference;
    public bool verticalDifference;
    public bool intersectingTrueFalse;
    public bool intersectingDistance;
    public bool timeElapsedAsSine;
    public bool rotationZ;
    public bool checkIfColliding;
    public bool xVelocity;
    [Header("Optional Variables")]
    public string objectToSenseForTag;
    public Transform objectToSenseFor;
    public LayerMask intersectionMask;
    public float initialDistance = 10.0f;
    public float sinMultiplier = 10.0f;

    [ShowOnly] public float lastOutput;

    public void Initialize(GameObject obj)
    {
        if (checkIfColliding)
            return;

        if (objectToSenseFor == null && objectToSenseForTag != "")
            objectToSenseFor = GameObject.FindGameObjectWithTag(objectToSenseForTag).transform;
        if (initialDistance == 0)
            initialDistance = Vector2.Distance(obj.transform.position, objectToSenseFor.position);

        lastOutput = (float)GetSensorValue(obj);
    }

    // Gets sensor value, normalized between 0 and 1
    public double GetSensorValue(int type, GameObject obj)
    {
        double val = 0;
        if (type == 0 && distanceToObject)
            val = Vector2.Distance(obj.transform.position, objectToSenseFor.position) / initialDistance;
        else if (type == 1 && horizontalDifference)
            val = (Mathf.Abs(obj.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        else if (type == 2 && verticalDifference)
            val = (Mathf.Abs(obj.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        else if (type == 3 && intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                val = 1;
            else
                val = 0;
        }
        else if (type == 4 && intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
            {
                val = Vector2.Distance(r.point, obj.transform.position) / initialDistance;
            }
            else
                val = 1;
        }
        else if (timeElapsedAsSine)
        {
            val = Mathf.Sin(type * sinMultiplier);
        }
        else if (xVelocity)
            val = objectToSenseFor.GetComponent<Rigidbody2D>().velocity.x / (float)type;

        lastOutput = (float)val;
        return val;
    }

    // Gets sensor value, normalized between 0 and 1
    public double GetSensorValue(GameObject obj)
    {
        double val = 0;
        if (distanceToObject)
            val = Vector2.Distance(obj.transform.position, objectToSenseFor.position) / initialDistance;
        else if (horizontalDifference)
            val = (Mathf.Abs(obj.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        else if (verticalDifference)
            val = (Mathf.Abs(obj.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        else if (intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                val = 1;
            else
                val = 0;
        }
        else if (intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                val = Vector2.Distance(r.point, obj.transform.position) / initialDistance;
            else
                val = 1;
        }
        else if (rotationZ)
            // Rotation normalized between -1 and 1
            val = ((objectToSenseFor.transform.eulerAngles.z > 180 ? 180 - (objectToSenseFor.transform.eulerAngles.z - 180) : -objectToSenseFor.transform.eulerAngles.z)) / 180.0f;
        //Debug.Log(((obj.transform.eulerAngles.z>180? 180-(obj.transform.eulerAngles.z-180): obj.transform.eulerAngles.z)) / 180.0f);
        else if (checkIfColliding)
            try
            {
                val = objectToSenseFor.GetComponent<IsColliding>().isColliding ? 1 : 0;
            }
            catch (Exception)
            {

                throw;
            }
        else if (xVelocity)
            val = objectToSenseFor.GetComponent<Rigidbody2D>().velocity.x / (float)4;

        lastOutput = (float)val;
        return val;
    }
}

public class NetEntity : MonoBehaviour
{
    public NeuralNetwork net;

    public Sense[] senses;

    [ShowOnly] public double[] outputs;
    public bool networkRunning = false;
    public int generation;

    public int numberOfInputs;

    int timeElapsed = 0;

    float totalRotationalDifference = 0;
    float totalheightDifference = 0;
    float totalDistanceOverTime = 0;
    float totalXVelocity = 0;

    public SpriteRenderer[] mainSprites;
    public HingeJoint2D[] hinges;
    public bool randomizeSpriteColor = true;

    float bestDistance = 10000;

    [Header("Fitness Modifiers")]
    public bool rewardTimeAlive = false;

    private float finalErrorOffset = 0;

    [ShowOnly] public string genome = "blankgen";
    [ShowOnly] public double totalFitness;
    int totalIterations;
    [ShowOnly] public int trial;
    public float[] trialValues;
    
    public double[] gameBoard = new double[9];
    
    int team = 0;

    [ShowOnly] public double[] mutVars;
    [ShowOnly] public int netID;
    [ShowOnly] public string weightsHash;

    public GameObject bestCrown;
    
    public NetEntity opponent = new NetEntity();

    [HideInInspector] public NetUI netUI;
    
    public bool HasWon(int team){
        double piece = -1;
        
        if(team == 1)
            piece = 1;
    
        if(gameBoard[0] == piece && gameBoard[3] == piece && gameBoard[6] == piece)
            return true;
        if(gameBoard[1] == piece && gameBoard[4] == piece && gameBoard[7] == piece)
            return true;
        if(gameBoard[2] == piece && gameBoard[5] == piece && gameBoard[8] == piece)
            return true;
        if(gameBoard[0] == piece && gameBoard[1] == piece && gameBoard[2] == piece)
            return true;
        if(gameBoard[3] == piece && gameBoard[4] == piece && gameBoard[5] == piece)
            return true;
        if(gameBoard[6] == piece && gameBoard[7] == piece && gameBoard[8] == piece)
            return true;
        if(gameBoard[0] == piece && gameBoard[4] == piece && gameBoard[8] == piece)
            return true;
        if(gameBoard[2] == piece && gameBoard[4] == piece && gameBoard[6] == piece)
            return true;
            
        return false;
    }

    public bool Elapse()
    {
        if (networkRunning == true)
        {

            //string weightLengths = "";
            //for (int i = 0; i < net.weights.Length; i++)
            //{
            //    weightLengths += net.weights[i].Length * net.weights[i][0].Length + ", ";
            //}
            //Debug.Log(weightLengths);


                double[] inputs = gameBoard;

                outputs = net.FeedForward(inputs);
                if (net.isBest)
                {
                    netUI.UpdateInputs(inputs);
                    netUI.UpdateOutputs(outputs);
                }
                
            int highestIndex = 0;
            double highestValue = 0;
            for(int i = 0; i < outputs.Length; i++){
                if(outputs[i]>highestValue)
                {
                    highestIndex = i;
                    highestValue = outputs[i];
                }
            }
                
            
                
            if (rewardTimeAlive)
                net.pendingFitness += ((totalIterations - timeElapsed) / (float)totalIterations);
            //bestDistance = senseVal;
            //}


            if(HasWon(team)){
                networkRunning = false;
                net.pendingFitness += -0.1f;
                return false;
            }


            timeElapsed += 1;


            totalFitness = net.fitness + net.pendingFitness;

            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork neti, int generation, int numberOfInputs, int totalIterations, int trial, NetUI netUI, int team, NeuralNetwork opponentNet, NetEntity netent)
    {
        transform.localPosition = Vector3.zero;
        transform.eulerAngles = Quaternion.Euler(0, 0, trialValues[trial]).eulerAngles;
        this.net = neti;
        this.generation = generation;
        this.numberOfInputs = numberOfInputs;
        this.totalIterations = totalIterations;
        this.totalRotationalDifference = 0;
        this.trial = trial;
        networkRunning = true;
        this.net.fitness += this.net.pendingFitness;
        this.net.pendingFitness = 0;
        this.genome = net.genome;
        this.netID = net.netID;
        this.weightsHash = net.weightsHash;
        this.netUI = netUI;
        this.team = team;
        //net.error = 0;
        timeElapsed = 0;
        bestDistance = 10000;
        
        if(team == 0)
            opponent = Instantiate(gameObject, transform);
            opponent.Init(opponentNet, generation, numberOfInputs, totalIterations, trial, !team, this);
        }
        else
            opponent = netent;

        //mutVars = net.mutatableVariables;
        if (net.isBest)
        {
            netUI.RemakeDrawing(net.droppedNeurons);
            netUI.UpdateWeightLines(net.weights);

            // Count total dropped neurons and print
            int total = 0;
            for (int i = 0; i < net.droppedNeurons.Length; i++)
                for (int j = 0; j < net.droppedNeurons[i].Length; j++)
                    total += net.droppedNeurons[i][j] == true ? 1 : 0;
            Debug.Log(total);
        }

        // Show the crown if this is the best network
        bestCrown.SetActive(net.isBest);
        // Set the sprite layer to be the very front if this is the best network
        if (net.isBest)
            for (int i = 0; i < mainSprites.Length; i++)
                mainSprites[i].sortingOrder = 1000;
        else
            for (int i = 0; i < mainSprites.Length; i++)
                mainSprites[i].sortingOrder = netID;

        foreach (var s in senses)
            s.Initialize(mainSprites[0].gameObject);

        if (randomizeSpriteColor)
        {
            //Color col = new Color32((byte)UnityEngine.Random.Range(0, 256),
            //        (byte)UnityEngine.Random.Range(0, 256),
            //        (byte)UnityEngine.Random.Range(0, 256), 255);
            //for (int i = 0; i < mainSprites.Length; i++)
            //    mainSprites[i].color = col;
        }
    }

    //public void End()
    //{
    //    net.fitness = senses[8].GetSensorValue(mainSprites[0].gameObject);

    //    double[] correct = { 0, 0 };
    //    //net.BackPropagation(correct);
    //    networkRunning = false;
    //}
}

