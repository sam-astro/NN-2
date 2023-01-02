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
    public Transform compareToObject;
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
        if (compareToObject == null)
            compareToObject = obj.transform;
        double val = 0;
        if (distanceToObject)
            val = Vector2.Distance(compareToObject.transform.position, objectToSenseFor.position) / initialDistance;
        else if (horizontalDifference)
            val = (compareToObject.transform.position.x - objectToSenseFor.position.x) / initialDistance;
        //(objectToSenseFor.position.x < obj.transform.position.x ? -1 : 1); // Make negative if it is less than
        else if (verticalDifference)
            val = (compareToObject.transform.position.y - objectToSenseFor.position.y) / initialDistance;
        //(objectToSenseFor.position.x < obj.transform.position.x ? -1 : 1); // Make negative if it is less than
        else if (intersectingTrueFalse)
        {
            RaycastHit2D r = Physics2D.Linecast(compareToObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                val = 1;
            else
                val = 0;
        }
        else if (intersectingDistance)
        {
            RaycastHit2D r = Physics2D.Linecast(compareToObject.transform.position, objectToSenseFor.position, intersectionMask);
            if (r)
                val = Vector2.Distance(r.point, compareToObject.transform.position) / initialDistance;
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

    [ShowOnly] public int timeElapsed = 0;

    public SpriteRenderer[] mainSprites;
    public HingeJoint2D[] hinges;
    public bool randomizeSpriteColor = true;

    [Header("Fitness Modifiers")]
    public bool bodyTouchingGroundIsBad = false;
    public bool upperLegsTouchingGroundIsBad = false;
    public bool touchingLaserIsBad = true;
    public bool rotationIsBad = false;
    public bool rewardTimeAlive = false;
    public bool heightIsGood = false;
    public bool slowRotationIsBad = false;
    public bool distanceIsGood = false;
    public bool useAverageDistance = false;
    public bool directionChangeIsGood = false;
    public bool xVelocityIsGood = false;
    public bool outputAffectsSin = false;

    private bool[] directions = { false, false }; // Array of directions of each motor
    public float[] directionTimes = { 0, 0 }; // Amount of time each direction has been used
    private float finalErrorOffset = 0;

    [ShowOnly] public string genome = "blankgen";
    [ShowOnly] public double totalFitness;
    [ShowOnly] public double tempFitness;
    [ShowOnly] public int totalIterations;
    [ShowOnly] public int trial;
    public Vector2[] trialValues;

    [ShowOnly] public double[] mutVars;
    [ShowOnly] public int netID;
    [ShowOnly] public string weightsHash;

    public GameObject bestCrown;

    public GrabberScript grabber;
    public GameObject grabbableObject;
    public GameObject objectGoal;

    float initialDistance = 1f;

    float railMoveSpeed = 0f;

    [ShowOnly] public float timeToGoal = 0f;

    int elapseSlowAmount = 1;

    Vector3 originalPosition;

    public Gradient accuracyGradient;

    [HideInInspector]
    public NetUI netUI;

    Vector3 targetLocation;

    float totalDistance;

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

            if (timeElapsed % 2 == 0)
            {
                double[] inputs = new double[numberOfInputs];

                for (int p = 0; p < inputs.Length; p++)
                {
                    //if (p == 6)
                    //    continue;
                    inputs[p] = senses[p].GetSensorValue(mainSprites[0].gameObject);
                }
                //if (Vector2.Distance(grabber.transform.position, targetLocation) <= 0.3f)
                if (grabber.isCollidingScript.isColliding)
                    inputs[3] = 1f;
                else
                    inputs[3] = -1f;
                senses[3].lastOutput = (float)inputs[3];
                //inputs[6] = (inputs[6]>0?-1f:1f)+inputs[6];
                //inputs[6] = senses[6].lastOutput;

                outputs = net.FeedForward(inputs);
                if (net.isBest)
                {
                    netUI.UpdateOutputs(outputs);
                    netUI.UpdateInputs(inputs);
                }
            }

            // Change rail move speed based on output

            if (timeElapsed % 2 == 0)
                railMoveSpeed = ((float)outputs[3] - 0.5f) * 9f;

            float newX = Mathf.Clamp(mainSprites[0].transform.position.x + railMoveSpeed * Time.deltaTime, originalPosition.x - 3.5f, originalPosition.x + 3.5f);
            Rigidbody2D bodyRb = mainSprites[0].GetComponent<Rigidbody2D>();
            // Determine if the velocity should get added or not based on the current position and clamp parameters
            if (railMoveSpeed > 0 && bodyRb.position.x >= originalPosition.x + 3.5f)
                railMoveSpeed = 0;
            else if (railMoveSpeed < 0 && bodyRb.position.x <= originalPosition.x - 3.5f)
                railMoveSpeed = 0;
            bodyRb.velocity = (new Vector2(
                railMoveSpeed,
                0
                ));
            //bodyRb.position = new Vector3(
            //    Mathf.Clamp(bodyRb.position.x, originalPosition.x - 3.5f, originalPosition.x + 3.5f),
            //    originalPosition.y,
            //    originalPosition.z
            //    );

            for (int i = 0; i < hinges.Length; i++)
            {
                if (timeElapsed % 2 == 0)
                {
                    // Change motor speed based on output
                    JointMotor2D changemotor = hinges[i].motor;
                    changemotor.motorSpeed = ((float)outputs[i] - 0.5f) * 180.0f;
                    hinges[i].motor = changemotor;
                }

                //// Get direction and see if it changed for joints
                //if (directionChangeIsGood)
                //    if (i <= 1)
                //    {
                //        bool direction = hinges[i].motor.motorSpeed > 0 ? true : false;
                //        if (directions[i] == direction)
                //        {  // It is still going in the same direction
                //           //directionTimes[i] += 1+Mathf.Abs(hinges[i].motor.motorSpeed)/90.0f;
                //            directionTimes[i] += 1;
                //        }
                //        else // It changed direction
                //        {
                //            finalErrorOffset += Mathf.Pow(directionTimes[i], 2) / (float)(totalIterations * totalIterations);
                //            directionTimes[i] = 0;
                //            directions[i] = !directions[i];
                //        }
                //        if (slowRotationIsBad)
                //            // If slow motor speed, also add penalty
                //            if (Mathf.Abs(hinges[i].motor.motorSpeed) < 20)
                //                directionTimes[i] += (20f - Mathf.Abs(hinges[i].motor.motorSpeed)) / 20f;
                //    }
            }

            if (outputs[3] > 0.5f && !grabber.isGrabbing)
            {
                finalErrorOffset = -0.2f;
                grabber.Grab();
            }
            if (grabber.isGrabbing)
                targetLocation = objectGoal.transform.position;
            //else
            //    grabber.Drop();

            // If the grabber is grabbing, then change the moveto location to the goal, otherwise change it back
            if (grabber.isGrabbing)
            {
                senses[3].objectToSenseFor = objectGoal.transform;
                senses[4].objectToSenseFor = objectGoal.transform;
                senses[5].objectToSenseFor = objectGoal.transform;
                senses[6].objectToSenseFor = objectGoal.transform;
            }
            else
            {
                senses[3].objectToSenseFor = grabbableObject.transform;
                senses[4].objectToSenseFor = grabbableObject.transform;
                senses[5].objectToSenseFor = grabbableObject.transform;
                senses[6].objectToSenseFor = grabbableObject.transform;
            }

            //// Set the sin multiplier based off of output 4
            //if (outputAffectsSin)
            //    senses[0].sinMultiplier = 5f * (float)outputs[4];

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


            //if (senseVal < bestDistance)
            //{
            float goalDist = Vector2.Distance(grabbableObject.transform.position, objectGoal.transform.position);
            //float goalDist = (float)senses[6].GetSensorValue(grabber.gameObject);
            totalDistance += Vector2.Distance(grabber.transform.position, targetLocation);

            //// If the box is within a small range of the goal and holding object, it can count as winning this test
            //if (goalDist <= 0.3f && grabber.isGrabbing)
            //{
            //    net.pendingFitness = -0.05f + goalDist/initialDistance + finalErrorOffset;

            //    //grabber.anim.SetBool("grabbing", true);

            //    //// Add best time to fitness, so the agent wants to reach the goal faster
            //    //if (rewardTimeAlive)
            //    //    net.pendingFitness += timeToGoal / 4f;

            //    // Change goal color
            //    objectGoal.GetComponent<SpriteRenderer>().color = Color.green;
            //}
            //else
            //{
            //    net.pendingFitness = goalDist/initialDistance + finalErrorOffset;

            //    //// Keep increasing the timeToGoal variable until the player reaches the goal
            //    //timeToGoal = (float)(timeElapsed) / ((float)totalIterations / (float)elapseSlowAmount);

            //    //// Add current time to fitness, so the agent wants to reach the goal faster
            //    //if (rewardTimeAlive)
            //    //    net.pendingFitness += timeToGoal / 4f;

            //    // Change goal color
            //    objectGoal.GetComponent<SpriteRenderer>().color = Color.gray;
            //}
            net.pendingFitness = (grabber.isGrabbing)?goalDist / initialDistance + finalErrorOffset:2f;

            //net.pendingFitness = (1.5f+ 3.68f) - (grabbableObject.transform.position.y+3.68f);
            //if (directionChangeIsGood)
            //    net.pendingFitness += finalErrorOffset +
            //        Mathf.Pow(directionTimes[0], 2) / (float)(totalIterations * totalIterations) +
            //        Mathf.Pow(directionTimes[1], 2) / (float)(totalIterations * totalIterations);
            //if (rotationIsBad)
            //    net.pendingFitness += totalRotationalDifference / (float)timeElapsed;
            //if (rewardTimeAlive)
            //    net.pendingFitness += ((totalIterations - timeElapsed) / (float)totalIterations);
            //if (heightIsGood)
            //    net.pendingFitness += totalheightDifference / (float)timeElapsed;
            //if (xVelocityIsGood)
            //    net.pendingFitness += 2.0f - (totalXVelocity / (float)timeElapsed);
            //bestDistance = senseVal;
            //}


            //if (bodyTouchingGroundIsBad)
            //    // If body touched ground, end and turn invisible
            //    if (senses[12].GetSensorValue(mainSprites[0].gameObject) == 1)
            //    {
            //        networkRunning = false;
            //        for (int i = 0; i < mainSprites.Length; i++)
            //            Destroy(mainSprites[i].gameObject);
            //        //net.pendingFitness += 0.3f;
            //        //return false;
            //    }
            //if (upperLegsTouchingGroundIsBad)
            //    // If upper leg parts touched ground, end and turn invisible
            //    if (senses[13].GetSensorValue(mainSprites[0].gameObject) == 1 ||
            //        senses[14].GetSensorValue(mainSprites[0].gameObject) == 1)
            //    {
            //        networkRunning = false;
            //        for (int i = 0; i < mainSprites.Length; i++)
            //            Destroy(mainSprites[i].gameObject);
            //        //net.pendingFitness += 0.3f;
            //        //return false;
            //    }
            //if (touchingLaserIsBad)
            //    // If any body part touches the laser, end and turn invisible
            //    if (senses[12].objectToSenseFor.GetComponent<IsColliding>().failed ||  // Body
            //        senses[9].objectToSenseFor.GetComponent<IsColliding>().failed ||   // Leg A
            //        senses[10].objectToSenseFor.GetComponent<IsColliding>().failed     // Leg B
            //        )
            //    {
            //        networkRunning = false;
            //        for (int i = 0; i < mainSprites.Length; i++)
            //            Destroy(mainSprites[i].gameObject);
            //        //net.pendingFitness += 0.15f;
            //        //return false;
            //    }


            timeElapsed += 1;


            totalFitness = net.fitness + net.pendingFitness;
            tempFitness = net.pendingFitness;

            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        if (networkRunning && net.isBest)
        {
            // Draw line from grabber to next target
            if (targetLocation != objectGoal.transform.position)
            {
                Gizmos.color = accuracyGradient.Evaluate(Mathf.Clamp(1f - Vector2.Distance(grabber.transform.position, targetLocation)/8f, 0, 1));
                Gizmos.DrawLine(grabber.transform.position, targetLocation);
            }
            // Draw line from box to goal
            Gizmos.color = accuracyGradient.Evaluate(Mathf.Clamp(1f - Vector2.Distance(grabbableObject.transform.position, objectGoal.transform.position) / 8f, 0, 1));
            Gizmos.DrawLine(grabbableObject.transform.position, objectGoal.transform.position);
        }
    }

    public void Init(NeuralNetwork net, int generation, int numberOfInputs, int totalIterations, int trial, int elapseSlowAmount)
    {
        //transform.localPosition = Vector3.zero;
        //transform.eulerAngles = Quaternion.Euler(0, 0, trialValues[trial]).eulerAngles;
        this.net = net;
        this.generation = generation;
        this.numberOfInputs = numberOfInputs;
        this.totalIterations = totalIterations;
        this.trial = trial;
        networkRunning = true;
        this.net.fitness += this.net.pendingFitness;
        if (this.net.pendingFitness != 0f)
            this.net.trialFitnesses.Add(this.net.pendingFitness);
        this.net.pendingFitness = 0f;
        genome = net.genome;
        netID = net.netID;
        weightsHash = net.weightsHash;
        this.elapseSlowAmount = elapseSlowAmount;
        //net.error = 0;
        timeElapsed = 0;
        timeToGoal = 0;
        tempFitness = 0;
        totalDistance = 0;
        finalErrorOffset = 1;

        grabbableObject.transform.position = new Vector3(trialValues[trial].x + transform.position.x, grabbableObject.transform.position.y);
        //objectGoal.transform.position = new Vector3(trialValues[trial].x + transform.position.x, UnityEngine.Random.Range(-1.7f, -3.7f));
        objectGoal.transform.position = new Vector3(trialValues[trial].y + transform.position.x, objectGoal.transform.position.y);

        initialDistance = Vector2.Distance(grabbableObject.transform.position, objectGoal.transform.position);
        //initialDistance = Vector2.Distance(grabber.transform.position, objectGoal.transform.position);
        targetLocation = grabbableObject.transform.position;

        originalPosition = mainSprites[0].transform.position;

        mainSprites[0].GetComponent<Rigidbody2D>().position = new Vector3(
            originalPosition.x,
            originalPosition.y,
            originalPosition.z
            );
        mainSprites[0].GetComponent<Rigidbody2D>().velocity = Vector3.zero;

        if (net.isBest)
            netUI.UpdateWeightLines(net.weights);
        //senses[4].objectToSenseFor = grabbableObject.transform;
        //senses[5].objectToSenseFor = grabbableObject.transform;
        //senses[6].objectToSenseFor = grabbableObject.transform;

        //// Set the sin multiplier based off of mutVar 0
        //if (outputAffectsSin)
        //    senses[0].sinMultiplier = (float)net.mutatableVariables[0];
        //senses[0].sinMultiplier = 2.0f * (float)net.mutatableVariables[0];

        //mutVars = net.mutatableVariables;

        // Show the crown if this is the best network
        bestCrown.SetActive(net.isBest);
        //// Set the sprite layer to be the very front if this is the best network
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
            Color col = new Color32((byte)UnityEngine.Random.Range(0, 256),
                    (byte)UnityEngine.Random.Range(0, 256),
                    (byte)UnityEngine.Random.Range(0, 256), 255);
            for (int i = 0; i < mainSprites.Length; i++)
                mainSprites[i].color = col;
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

