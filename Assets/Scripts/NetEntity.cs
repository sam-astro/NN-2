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
    [Header("What to sense for:")]
    public bool distanceToObject;
    public bool horizontalDifference;
    public bool verticalDifference;
    public bool intersectingTrueFalse;
    public bool intersectingDistance;
    [Header("Optional Variables")]
    public string objectToSenseForTag;
    private Transform objectToSenseFor;
    public LayerMask intersectionMask;
    public float initialDistance = 10.0f;

    public void Initialize(GameObject gameObject)
    {
        objectToSenseFor = GameObject.FindGameObjectWithTag(objectToSenseForTag).transform;
        initialDistance = Vector2.Distance(gameObject.transform.position, objectToSenseFor.position);
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
            if (Physics.Linecast(gameObject.transform.position, objectToSenseFor.position, out RaycastHit sensedInfo, intersectionMask))
                return 1;
            else
                return 0;
        if (type == 4 && intersectingDistance)
            if (Physics.Linecast(gameObject.transform.position, objectToSenseFor.position, out RaycastHit sensedInfo, intersectionMask))
                return Vector2.Distance(sensedInfo.point, gameObject.transform.position) / initialDistance;
            else
                return initialDistance;

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
            if (Physics.Linecast(gameObject.transform.position, objectToSenseFor.position, out RaycastHit sensedInfo, intersectionMask))
                return 1;
            else
                return 0;
        if (intersectingDistance)
            if (Physics.Linecast(gameObject.transform.position, objectToSenseFor.position, out RaycastHit sensedInfo, intersectionMask))
                return Vector2.Distance(sensedInfo.point, gameObject.transform.position) / initialDistance;
            else
                return initialDistance;

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
            double[] inputs = new double[senses.Length];

            for (int p = 0; p < inputs.Length; p++)
            {
                inputs[p] = senses[p].GetSensorValue(gameObject);
            }

            outputs = net.FeedForward(inputs);

            //transform.position += new Vector3((float)outputs[0]*2.0f-1.0f, (float)outputs[1] * 2.0f - 1.0f) / 100.0f;
            Vector3 directionVector = new Vector2((float)Math.Cos((float)outputs[0] * 360.0f * Mathf.PI / 180.0f), (float)Math.Sin((float)outputs[0] * 360.0f * Mathf.PI / 180.0f));
            transform.position += directionVector * (float)outputs[1] / 100.0f;

            net.AddFitness(senses[0].GetSensorValue(0, gameObject));

            //if (transform.position.y > 0)
            //    net.AddFitness(10);
            //if (transform.position.x < 0)
            //    net.AddFitness(10);

            //networkRunning = false;
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
        net.SetFitness(0);
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
}

