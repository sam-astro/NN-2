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
    [ShowOnly] public int generation;

    public int numberOfInputs;

    int timeElapsed = 0;

    [ShowOnly] public float totalRotationalDifference = 0;

    public SpriteRenderer[] mainSprites;
    public HingeJoint2D[] hinges;
    public bool randomizeSpriteColor = true;

    float bestDistance = 10000;

    int totalIterations = 1;

    public float[] trialRotations;

    [ShowOnly] public int trial;
    int stillTime = 0;


    [ShowOnly] public double fitness;
    [ShowOnly] public double totalFitness;

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

            outputs = net.FeedForward(inputs);

            //mainSprites[0].GetComponent<Rigidbody2D>().AddForce(new Vector2((float)outputs[0] * 100, 0), ForceMode2D.Force);
            //float direction = 0;
            //if (outputs[0] > 0.25f)
            //    direction = 4;
            //if (outputs[0] < -0.25f)
            //    direction = -4;
            //mainSprites[0].GetComponent<Rigidbody2D>().velocity = new Vector2((float)outputs[0]*5, 0);

            //float direction = 0;
            //float dif = Mathf.Abs((float)outputs[0] - (float)outputs[1]);
            //if (dif > 0.1f)
            //    direction = outputs[0] > outputs[1] ? Mathf.Abs((float)outputs[0]) * 4 : Mathf.Abs((float)outputs[1]) * -4;
            //mainSprites[0].GetComponent<Rigidbody2D>().velocity = new Vector2(direction, 0);

            float direction = ((float)outputs[0]-0.5f)*3.0f;
            mainSprites[0].GetComponent<Rigidbody2D>().velocity = new Vector2(direction, 0);

            if ((Mathf.Abs(mainSprites[0].transform.position.x) < 0.2f))
                stillTime++;


            totalRotationalDifference += Mathf.Abs((float)senses[0].GetSensorValue(mainSprites[1].gameObject));
            //net.fitness = (totalRotationalDifference / timeElapsed)+ ((totalIterations - timeElapsed) / (float)totalIterations);



            net.pendingFitness = ((totalIterations - timeElapsed) / (float)totalIterations) +
                (stillTime / (float)totalIterations * 10.0f) +
                (totalRotationalDifference / timeElapsed);
            //if (senses[4].GetSensorValue(gameObject) == 1) // If pole ground
            //{
            //    networkRunning = false;
            //    for (int i = 0; i < mainSprites.Length; i++)
            //        mainSprites[i].color = Color.clear;
            //    //net.fitness += (totalIterations - timeElapsed) / totalIterations;
            //    return false;
            //}
            if (Mathf.Abs((float)inputs[0]) > 0.25f) // If the pole has reached too shallow of an angle, fail
            {
                networkRunning = false;
                for (int i = 0; i < mainSprites.Length; i++)
                    Destroy(mainSprites[i].gameObject);
                //net.fitness += fitness / (Mathf.Pow(trial, 2) / 2.0f + 1); // Add this trial's fitness to total
                //net.fitness += fitness; // Add this trial's fitness to total
                return false;
            }


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

            // Only update fitness every 10 cycles
            //if (timeElapsed % 10 == 0)
            //{




            timeElapsed += 1;

            totalFitness = net.fitness;


            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork net, int generation, int numberOfInputs, int totalIterations, int trial)
    {
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        this.net = net;
        this.generation = generation;
        this.numberOfInputs = numberOfInputs;
        this.totalIterations = totalIterations;
        this.trial = trial;
        networkRunning = true;
        this.net.fitness += this.net.pendingFitness;
        this.net.pendingFitness = 0;
        //net.error = 0;
        timeElapsed = 0;
        bestDistance = 10000;
        if (trialRotations.Length > trial)
            mainSprites[1].gameObject.transform.eulerAngles = Quaternion.Euler(0, 0, trialRotations[trial]).eulerAngles;
        else
            mainSprites[1].gameObject.transform.eulerAngles = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-10f, 10f)).eulerAngles;

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

