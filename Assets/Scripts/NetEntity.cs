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
    [Header("Optional Variables")]
    public string objectToSenseForTag;
    public Transform objectToSenseFor;
    public LayerMask intersectionMask;
    public float initialDistance = 10.0f;

    [ShowOnly] public float lastOutput;

    public void Initialize(GameObject gameObject)
    {
        if (objectToSenseFor == null || objectToSenseForTag != "")
            objectToSenseFor = GameObject.FindGameObjectWithTag(objectToSenseForTag).transform;
        if (initialDistance == 0)
            initialDistance = Vector2.Distance(gameObject.transform.position, objectToSenseFor.position);

        lastOutput = (float)GetSensorValue(gameObject);
    }

    public double GetSensorValue(int type, GameObject gameObject)
    {
        if (type == 0 && distanceToObject)
            return Vector2.Distance(gameObject.transform.position, objectToSenseFor.position) / initialDistance;
        if (type == 1 && horizontalDifference)
            return (Mathf.Abs(gameObject.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        if (type == 2 && verticalDifference)
            return (Mathf.Abs(gameObject.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        if (type == 3 && intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(gameObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                return 1;
            else
                return 0;
        }
        if (type == 4 && intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(gameObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
            {
                lastOutput = Vector2.Distance(r.point, gameObject.transform.position) / initialDistance;
                return lastOutput;
            }
            else
                return 1;
        }

        return 0;
    }
    public double GetSensorValue(GameObject gameObject)
    {
        if (distanceToObject)
            return Vector2.Distance(gameObject.transform.position, objectToSenseFor.position) / initialDistance;
        if (horizontalDifference)
            return (Mathf.Abs(gameObject.transform.position.x) + Mathf.Abs(objectToSenseFor.position.x)) / initialDistance;
        if (verticalDifference)
            return (Mathf.Abs(gameObject.transform.position.y) + Mathf.Abs(objectToSenseFor.position.y)) / initialDistance;
        if (intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(gameObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                return 1;
            else
                return 0;
        }
        if (intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(gameObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
            {
                lastOutput = Vector2.Distance(r.point, gameObject.transform.position) / initialDistance;
                return lastOutput;
            }
            else
                return 1;
        }

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

    public SpriteRenderer mainSprite;
    public bool randomizeSpriteColor = true;

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
                inputs[p] = senses[p].GetSensorValue(gameObject);
            }

            outputs = net.FeedForward(inputs);


            if (senses[2].GetSensorValue(gameObject) <= 0.25d) // If touching ground
            {
                if (Mathf.Abs((float)outputs[0]) > 0.25f)
                    transform.position += transform.right / ((1.0f - (float)outputs[0]) * 100.0f);
            }
            else
                transform.position -= new Vector3(0, 0.01f);


            ////transform.position += new Vector3((float)outputs[0]*2.0f-1.0f, (float)outputs[1] * 2.0f - 1.0f) / 100.0f;
            //Vector3 directionVector = new Vector2((float)Math.Cos((float)outputs[0] * 6.28319f), (float)Math.Sin((float)outputs[0] * 6.28319f));
            //transform.position += directionVector / 100.0f;

            //Vector3 dir = (transform.position - senses[0].objectToSenseFor.position).normalized;
            //net.AddFitness(Vector3.Distance(dir, directionVector));
            //net.error += (senses[0].GetSensorValue(0, gameObject));

            if (timeElapsed % 50 == 0)
            {
                double[] correct = { 1.0f };
                //net.BackProp(correct);
            }

            timeElapsed += 1;

            return true;
        }
        return false;
    }

    public void Init(NeuralNetwork net, int generation)
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        this.net = net;
        this.generation = generation;
        networkRunning = true;
        //net.error = 0;
        timeElapsed = 0;

        foreach (var s in senses)
        {
            s.Initialize(gameObject);
        }

        if (randomizeSpriteColor)
            mainSprite.color = new Color32((byte)UnityEngine.Random.Range(0, 256),
                (byte)UnityEngine.Random.Range(0, 256),
                (byte)UnityEngine.Random.Range(0, 256), 255);
    }

    public void End()
    {
        double[] correct = { 0, 0 };
        //net.BackPropagation(correct);
        networkRunning = false;
    }
}

