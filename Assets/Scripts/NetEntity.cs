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

    public SpriteRenderer[] mainSprites;
    public HingeJoint2D[] hinges;
    public bool randomizeSpriteColor = true;

    float bestDistance = 10000;

    [Header("Fitness Modifiers")]
    public bool touchingGroundIsBad = false;
    public bool rotationIsBad = false;
    public bool rewardTimeAlive = false;
    public bool heightIsGood = false;
    public bool slowRotationIsBad = false;
    public bool distanceIsGood = false;

    private bool[] directions = { false, false }; // Array of directions of each motor
    public float[] directionTimes = { 0, 0 }; // Amount of time each direction has been used
    private float finalErrorOffset = 0;

    [ShowOnly] public double totalFitness;
    int totalIterations;
    [ShowOnly] public int trial;
    public float[] trialValues;

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


            double[] inputs = new double[numberOfInputs];

            for (int p = 0; p < inputs.Length; p++)
            {
                inputs[p] = senses[p].GetSensorValue(mainSprites[0].gameObject);
            }
            inputs[0] = senses[0].GetSensorValue(timeElapsed, mainSprites[0].gameObject);

            outputs = net.FeedForward(inputs);

            for (int i = 0; i < outputs.Length; i++)
            {
                JointMotor2D changemotor = hinges[i].motor;
                changemotor.motorSpeed = ((float)outputs[i] - 0.5f) * 360.0f;
                hinges[i].motor = changemotor;

                // Get direction and see if it changed for joints
                if (i <= 1)
                {
                    bool direction = hinges[i].motor.motorSpeed > 0 ? true : false;
                    if (directions[i] == direction)
                    {  // It is still going in the same direction
                        //directionTimes[i] += 1+Mathf.Abs(hinges[i].motor.motorSpeed)/90.0f;
                        directionTimes[i] += 1;
                    }
                    else // It changed direction
                    {
                        finalErrorOffset += Mathf.Pow(directionTimes[i], 2) / (float)(totalIterations * totalIterations);
                        directionTimes[i] = 0;
                        directions[i] = !directions[i];
                    }
                    if (slowRotationIsBad)
                        // If slow motor speed, also add penalty
                        if (Mathf.Abs(hinges[i].motor.motorSpeed) < 20)
                            directionTimes[i] += (20f - Mathf.Abs(hinges[i].motor.motorSpeed)) / 20f;
                }
            }

            if (rotationIsBad)
                totalRotationalDifference += Mathf.Abs(senses[1].lastOutput);

            // if (senses[2].GetSensorValue(gameObject) <= 0.25d) // If touching ground
            // {
            //     if (Mathf.Abs((float)outputs[0]) > 0.25f)
            //         transform.position += transform.right / ((1.0f - (float)outputs[0]) * 100.0f);
            // }
            // else
            //     transform.position -= new Vector3(0, 0.01f);


            ////transform.position += new Vector3((float)outputs[0]*2.0f-1.0f, (float)outputs[1] * 2.0f - 1.0f) / 100.0f;
            //Vector3 directionVector = new Vector2((float)Math.Cos((float)outputs[0] * 6.28319f), (float)Math.Sin((float)outputs[0] * 6.28319f));
            //transform.position += directionVector / 100.0f;

            //Vector3 dir = (transform.position - senses[0].objectToSenseFor.position).normalized;
            //net.AddFitness(Vector3.Distance(dir, directionVector));
            //net.error += (senses[0].GetSensorValue(0, gameObject));

            //if (timeElapsed % 50 == 0)
            //{
            //    double[] correct = { 1.0f };
            //    //net.BackProp(correct);
            //}
            float height = (float)senses[2].GetSensorValue(mainSprites[0].gameObject);
            totalheightDifference += 1f-height;

            float distance = (float)senses[7].GetSensorValue(mainSprites[4].gameObject);
            totalDistanceOverTime += distance;
            //if (senseVal < bestDistance)
            //{
            net.pendingFitness = 0;
            if (distanceIsGood)
                net.pendingFitness = (totalDistanceOverTime/ (float)timeElapsed) +
                    finalErrorOffset +
                    Mathf.Pow(directionTimes[0], 2) / (float)(totalIterations * totalIterations) +
                    Mathf.Pow(directionTimes[1], 2) / (float)(totalIterations * totalIterations);
            if (rotationIsBad)
                net.pendingFitness += totalRotationalDifference / (float)timeElapsed;
            if (rewardTimeAlive)
                net.pendingFitness += ((totalIterations - timeElapsed) / (float)totalIterations);
            if (heightIsGood)
                net.pendingFitness += totalheightDifference / (float)timeElapsed;
            //bestDistance = senseVal;
            //}


            if (touchingGroundIsBad)
                // If body touched ground, end and turn invisible
                if (senses[8].GetSensorValue(mainSprites[0].gameObject) == 1)
                {
                    networkRunning = false;
                    for (int i = 0; i < mainSprites.Length; i++)
                        Destroy(mainSprites[i].gameObject);
                    return false;
                }


            timeElapsed += 1;


            totalFitness = net.fitness;

            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork net, int generation, int numberOfInputs, int totalIterations, int trial)
    {
        transform.localPosition = Vector3.zero;
        transform.eulerAngles = Quaternion.Euler(0, 0, trialValues[trial]).eulerAngles;
        this.net = net;
        this.generation = generation;
        this.numberOfInputs = numberOfInputs;
        this.totalIterations = totalIterations;
        this.totalRotationalDifference = 0;
        this.trial = trial;
        networkRunning = true;
        this.net.fitness += this.net.pendingFitness;
        this.net.pendingFitness = 0;
        //net.error = 0;
        timeElapsed = 0;
        bestDistance = 10000;

        foreach (var s in senses)
        {
            s.Initialize(mainSprites[0].gameObject);
        }

        if (randomizeSpriteColor)
        {
            Color col = new Color32((byte)UnityEngine.Random.Range(0, 256),
                    (byte)UnityEngine.Random.Range(0, 256),
                    (byte)UnityEngine.Random.Range(0, 256), 255);
            for (int i = 0; i < mainSprites.Length; i++)
            {
                mainSprites[i].color = col;
            }
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

