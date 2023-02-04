using System.Collections;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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

    public string genome = "blankgenn";
    [ShowOnly] public double totalFitness;
    int totalIterations;
    [ShowOnly] public int trial;
    public float[] trialValues;
    
    public double[] gameBoard = new double[9];

    [ShowOnly] public int team = 0;

    [ShowOnly] public double[] mutVars;
    [ShowOnly] public int netID;
    [ShowOnly] public string weightsHash;

    public GameObject bestCrown;
    
    public NetEntity opponent;

    [HideInInspector] public NetUI netUI;
    public BoardDrawer boardDrawer;
    
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

    public bool IsDraw()
    {
        for (int i = 0; i < gameBoard.Length; i++)
            if (gameBoard[i] == 0)
                return false;
        return true;
    }

    public bool Elapse(bool isSecondary)
    {
        if (networkRunning == true)
        {
        
            // If this player is on team 1, get team 0's turn first
            if(team == 1 && isSecondary == false){
                // Prompt the other player network to move
                opponent.gameBoard = gameBoard; // copy new game board to opponent
                opponent.Elapse(true); // Get opponent move
                gameBoard = opponent.gameBoard; // Update our gameboard
            }

            double[] inputs = gameBoard;

            outputs = net.FeedForward(inputs);
            if (net.isBest)
            {
                netUI.UpdateInputs(inputs);
                netUI.UpdateOutputs(outputs);
            }

            List<double> outList = outputs.ToList();
            
            // Sort all outputs from least to greatest
            var sorted = outList
                .Select((x, i) => new KeyValuePair<double, int>(x, i))
                .OrderBy(x => x.Key)
                .ToList();
            List<double> B = sorted.Select(x => x.Key).ToList();
            List<int> idx = sorted.Select(x => x.Value).ToList();
            
            for(int i = idx.Count-1; i >= 0; i--){
                // Make sure the current game board location is empty, otherwise go to next greatest choice
                if(gameBoard[idx[i]]==0d)
                {
                    // Place piece
                    gameBoard[idx[i]] = team == 1 ? 1 : -1;
                    break;
                }
            }
                
            
                
            if (rewardTimeAlive)
                net.pendingFitness = ((totalIterations - timeElapsed) / (float)totalIterations);
            //bestDistance = senseVal;
            //}

            // Check if this network has won, and end the game if so.
            if (HasWon(team))
            {
                networkRunning = false;
                net.pendingFitness += ((float)timeElapsed / (float)totalIterations); // Give point bonus since they won
                opponent.networkRunning = false;
                opponent.net.pendingFitness += 1f; // Give opponent point penalty since they lost
                boardDrawer.Draw(gameBoard);
                //Debug.Log("Team: " + team + " has won!");
                return false;
            }
            // Else check if this game is a draw
            else if (IsDraw())
            {
                networkRunning = false;
                net.pendingFitness += -((float)timeElapsed / (float)totalIterations)/2f; // Give smaller point bonus since draw
                opponent.networkRunning = false;
                opponent.net.pendingFitness += -((float)timeElapsed / (float)totalIterations)/2f; // Give smaller point bonus since draw
                boardDrawer.Draw(gameBoard);
                //Debug.Log("Draw.");
                return false;
            }

            // If this player is on team 0, prompt team 1 to move their piece
            if (team == 0 && isSecondary == false){
                // Prompt the other player network to move
                opponent.gameBoard = gameBoard; // copy new game board to opponent
                bool opp = opponent.Elapse(true); // Get opponent move
                gameBoard = opponent.gameBoard; // Update our gameboard
                //if(opp == false && networkRunning) // If the opponent has won, finish game.
                //{
                //    networkRunning = false;
                //    net.pendingFitness += 0.1f; // Give point penalty since they lost
                //    opponent.networkRunning = false;
                //    opponent.net.pendingFitness += -0.1f; // Give opponent point bonus since they won
                //    //boardDrawer.Draw(gameBoard);
                //    return false;
                //}
            }
            
            //boardDrawer.Draw(gameBoard);

            timeElapsed += 1;


            totalFitness = net.fitness + net.pendingFitness;

            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork neti, int generation, int numberOfInputs, int totalIterations, int trial, NetUI netUI, int team, NeuralNetwork opponentNet, NetEntity netent, BoardDrawer boardDrawer)
    {
        transform.localPosition = Vector3.zero;
        //transform.eulerAngles = Quaternion.Euler(0, 0, trialValues[trial]).eulerAngles;
        this.net = neti;
        this.generation = generation;
        this.numberOfInputs = numberOfInputs;
        this.totalIterations = totalIterations;
        this.totalRotationalDifference = 0;
        this.trial = trial;
        networkRunning = true;
        //this.net.fitness += this.net.pendingFitness;
        this.net.pendingFitness = 0;
        this.genome = net.genome;
        this.netID = net.netID;
        this.weightsHash = net.weightsHash;
        this.netUI = netUI;
        this.team = team;
        this.boardDrawer = boardDrawer;
        //net.error = 0;
        timeElapsed = 0;
        bestDistance = 10000;
        
        if(team == 0) {
            opponent = Instantiate(gameObject, transform).GetComponent<NetEntity>();
            opponent.Init(opponentNet, generation, numberOfInputs, totalIterations, trial, netUI, 1, this.net, this, boardDrawer);
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
            //Debug.Log(total);
        }

        // Show the crown if this is the best network
        bestCrown.SetActive(net.isBest);
        // Set the sprite layer to be the very front if this is the best network
        //if (net.isBest)
        //    for (int i = 0; i < mainSprites.Length; i++)
        //        mainSprites[i].sortingOrder = 1000;
        //else
        //    for (int i = 0; i < mainSprites.Length; i++)
        //        mainSprites[i].sortingOrder = netID;

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

