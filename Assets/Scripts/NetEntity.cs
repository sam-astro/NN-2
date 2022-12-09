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
    [Header("Optional Variables")]
    public string objectToSenseForTag;
    public Transform objectToSenseFor;
    public LayerMask intersectionMask;
    public float initialDistance = 10.0f;
    public float sinMultiplier = 10.0f;

    [ShowOnly] public float lastOutput;

    public void Initialize(GameObject obj)
    {
        if (objectToSenseFor == null && objectToSenseForTag != "")
            objectToSenseFor = GameObject.FindGameObjectWithTag(objectToSenseForTag).transform;
        if (initialDistance == 0)
            initialDistance = Vector2.Distance(obj.transform.position, objectToSenseFor.position);

        lastOutput = (float)GetSensorValue(obj);
    }

    // Gets sensor value, normalized between 0 and 1
    public double GetSensorValue(int type, GameObject obj)
    {
        if (type == 0 && distanceToObject)
            return Vector2.Distance(obj.transform.position, objectToSenseFor.position) / initialDistance;
        if (type == 1 && horizontalDifference)
            return (Mathf.Abs(obj.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        if (type == 2 && verticalDifference)
            return (Mathf.Abs(obj.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        if (type == 3 && intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                return 1;
            else
                return 0;
        }
        if (type == 4 && intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
            {
                lastOutput = Vector2.Distance(r.point, obj.transform.position) / initialDistance;
                return lastOutput;
            }
            else
                return 1;
        }
        if (timeElapsedAsSine)
        {
            lastOutput = (Mathf.Sin(type * sinMultiplier)+1)/2;
            return lastOutput;
        }

        return 0;
    }

    // Gets sensor value, normalized between 0 and 1
    public double GetSensorValue(GameObject obj)
    {
        if (distanceToObject)
            return Vector2.Distance(obj.transform.position, objectToSenseFor.position) / initialDistance;
        if (horizontalDifference)
            return (Mathf.Abs(obj.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        if (verticalDifference)
            return (Mathf.Abs(obj.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        if (intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                return 1;
            else
                return 0;
        }
        if (intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(obj.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
            {
                lastOutput = Vector2.Distance(r.point, obj.transform.position) / initialDistance;
                return lastOutput;
            }
            else
                return 1;
        }
        if (rotationZ)
            return (Mathf.Clamp(obj.transform.rotation.eulerAngles.z, -180, 180)+180)/360;

        return 0;
    }
}

public class NetEntity : MonoBehaviour
{
    public NeuralNetwork net;

    public Sense[] senses;

    double[] outputs;
    public bool networkRunning = false;
    public int generation;

    int timeElapsed = 0;

    public SpriteRenderer[] mainSprites;
    public HingeJoint2D[] hinges;
    public bool randomizeSpriteColor = true;

    float bestDistance = 10000;


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

            double[] inputs = new double[senses.Length];

            for (int p = 0; p < inputs.Length; p++)
            {
                inputs[p] = senses[p].GetSensorValue(mainSprites[0].gameObject);
                // Body touched ground, end and turn invisible
                if (p >2 && inputs[p] < 0.2f)
                {
                    networkRunning = false;
                    for (int i = 0; i < mainSprites.Length; i++)
                        mainSprites[i].color = Color.clear;
                    return false;
                }
            }
            inputs[0] = senses[0].GetSensorValue(timeElapsed, mainSprites[0].gameObject);

            outputs = net.FeedForward(inputs);

            for (int i = 0; i < hinges.Length; i++)
            {
                JointMotor2D changemotor = hinges[i].motor;
                changemotor.motorSpeed = (float)(outputs[i])*720*(i<2?-1:1);
                hinges[i].motor = changemotor;
            }

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

            float senseVal = (float)senses[1].GetSensorValue(mainSprites[0].gameObject);
            if (senseVal < bestDistance)
            {
                net.fitness = senseVal;
                bestDistance = senseVal;
            }



            timeElapsed += 1;

            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork net, int generation)
    {
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity;
        this.net = net;
        this.generation = generation;
        networkRunning = true;
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

