using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingLot : MonoBehaviour {

    public int capacity;                   // Number of parking spaces in the lot
    public int currentOccupancy;           // Number of cars in the lot
    public float stayMin;                  // Minimum stay time
    public float stayMax;                  // Maximum stay time
    public float percentageFilled;         // Percentage the lot is filled
    public float totalRevenue;             // Total revenue generated
    public float timeStep;                 // The time step used
    public string parkingLotName;          // The name
    public ParkingLotManager myManager;    // The manager for this lot
    public float[] stayBandDuration;       // The array of stay durations
    public float[] pricingBand;            // The array of costs matching the stay durations

    public Dictionary<double, double> capacityData = new Dictionary<double, double>();
    public Dictionary<double, int> samplesAtHour = new Dictionary<double, int>();
    public List<Guid> savedSpaces;

	// Use this for initialization
	void Start () {
        for (int i = 0; i < 24; i++)
        {
            for (int b = 0; b < 60; b++)
            {
                capacityData.Add(System.Math.Round(i*1.0 + b/ 100.0,2), 0);
                samplesAtHour.Add(System.Math.Round(i*1.0 + b/ 100.0,2), 0);
            }
        }

        savedSpaces = new List<Guid>();
        // Set revenue to zero clamp values to sensible ones
        totalRevenue = 0;
        if(percentageFilled < 0)
        {
            percentageFilled = 0;
        }
        else if (percentageFilled > 1)
        {
            percentageFilled = 1;
        }

        if(capacity <= 0)
        {
            capacity = 1;
        }

        if(stayMin < 0)
        {
            stayMin = 0;
        }

        if(stayMax < stayMin)
        {
            stayMax = stayMin + 1;
        }

        timeStep = myManager.timeStep;



        // Set current occupancey to percentage
        currentOccupancy = (int)(capacity * percentageFilled);

        // Loop through each parked car and give a stay time
        for (int i = 0; i < currentOccupancy; ++i)
        {
            float stayTime = PurelyRandomWaitTime() * timeStep;
            Invoke("ReleaseCar", stayTime);
        }
	}


    public Dictionary<double,double> calcAveragesCapacity(){
        Dictionary<double,double> temp = new Dictionary<double, double>();

        foreach(var key in capacityData.Keys)
        {
            temp[key] = 0;
            if(samplesAtHour[key]>0)
                temp[key] = capacityData[key]/samplesAtHour[key]*1.0;
        }
        return temp;
    }

    private float PurelyRandomWaitTime()
    {
        return UnityEngine.Random.Range(stayMin *
        timeStep, stayMax * timeStep);
    } 



    // Update is called once per frame
    void Update () {
        timeStep = myManager.timeStep;
    }
    void addOccupancyEntry(){
        double t = GridManagerScript.getTimeOfDay();
        samplesAtHour[t] = samplesAtHour[t] + 1;
        capacityData[t] = capacityData[t]+ percentageFilled;

    }
    // Removes a car from the lot and spawns an avatar
    void ReleaseCar()
    {
        if(currentOccupancy > 0)
        {
            GridManagerScript.numberOfVehicles[GridManagerScript.getTimeOfDay()] += 1;
            --currentOccupancy;
            percentageFilled = currentOccupancy * 1.0f / capacity * 1.0f;
            GetComponent<NodeScript>().myManager.SpawnDiffusionAvatar(GetComponent<NodeScript>(),entering:false);
            addOccupancyEntry();

        }

        if (currentOccupancy < capacity) {
            // Checks if the lots should be open
            bool open = currentOccupancy < capacity && !GetComponent<NodeScript>().forceCloseParking;
            GetComponent<NodeScript>().openParking = open;
            GetComponent<NodeScript>().goal = open;
            //GetComponent<NodeScript>().goalDiffusion = open ? 1000000 : 0;
            GetComponent<NodeScript>().NodeStatus = open ? NodeScript.NODE_STATUS.END : NodeScript.NODE_STATUS.UNSEARCHED;
        }
    }

    public bool reserveSpace(Guid id) {
        if (currentOccupancy < capacity)
        {
            savedSpaces.Add(id);
            currentOccupancy++;
            percentageFilled = currentOccupancy * 1.0f / capacity * 1.0f;
            return true;

        }
        else return false;

    }

    // Adds a car to the lot and assigns it a stay time
    public static int sum = 0;
    public void AddCar(DiffusionAvatarScript avatar)
    {

        if (savedSpaces.Contains(avatar.id))
        {
            savedSpaces.Remove(avatar.id);

            float stayTime = RandomiseWaitTime(avatar) * timeStep;
            CalculateRevenue(stayTime);
            Invoke("ReleaseCar", stayTime);
            //take measurement
            addOccupancyEntry();
        }
        //
        else if (currentOccupancy < capacity)
        {
            GridManagerScript.numberOfVehicles[GridManagerScript.getTimeOfDay()] =
                System.Math.Max(GridManagerScript.numberOfVehicles[GridManagerScript.getTimeOfDay()]-1,0);
            ++currentOccupancy;
            percentageFilled = currentOccupancy * 1.0f / capacity * 1.0f;

            //
            float stayTime = RandomiseWaitTime(avatar) * timeStep;
            CalculateRevenue(stayTime);
            Invoke("ReleaseCar", stayTime);
            //take measurement
            addOccupancyEntry();


        }
        else {
            // Checks if the lots should be open
            bool open = currentOccupancy < capacity && !GetComponent<NodeScript>().forceCloseParking;
            GetComponent<NodeScript>().openParking = open;
            GetComponent<NodeScript>().goal = open;
            //GetComponent<NodeScript>().goalDiffusion = open ? 1000000 : 0;
            GetComponent<NodeScript>().NodeStatus = open ? NodeScript.NODE_STATUS.END : NodeScript.NODE_STATUS.UNSEARCHED;
        }

        //worthless non-nullable check
      

 

    }

    private float RandomiseWaitTime(DiffusionAvatarScript avatar)
    {
        // double randNum = GridManagerScript.NextGaussianDoubleBoxMuller(0, 1) * avatar.groupId.standartDeviation + avatar.groupId.avrgStayTime; // calculate stay time for group mean and deviation
        // //check for negatives
        // return (float) (randNum>0?randNum:0);
        return avatar.groupId.avrgStayTime;
    }

    // Calculates the revenue based on the stay time of the car
    void CalculateRevenue(float stayTime)
    {
        int index = 0;

        // Loop through the stay durations
        for(int i = 0; i < stayBandDuration.Length; ++i)
        {
            index = i;
            //If the stay time is less than the band duration the correct price has been found
            if(stayTime < stayBandDuration[i])
            {
                break;
            }
        }

        // Add the ticket cost of that stay length to the revenue
        totalRevenue += pricingBand[index];
    }


  


    // Reset the parking lot values
    public void ResetParkingLot()
    {
        currentOccupancy = 0;
        percentageFilled = 0;
        totalRevenue = 0;
    }
}


/*
   private float RandomiseWaitTime(DiffusionAvatarScript avatar)
    {

        
     
        //N nums
        //each depends on the current time, each one depends on its rush hour
        //whichever one is closer to current time wins
        //how close it is to the goal time

        double randNum = GridManagerScript.NextGaussianDoubleBoxMuller(0, 1); //calculate random Gausian double (mean is 0)
        float timeOfDay = myManager.myManager.hours * 1.0f + myManager.myManager.minutes / 100.0f; //get current time

        //calc for each and get min
        //calculate for given mean and deviation for each group//mean is time of day and take the one with the smallest absolute difference between the current time
        // higher chance to get a groups avrg stay if they come between the groups rush hours

        double min = double.MaxValue; // make sure first one is lower than min
        PeakingGroup minGroup = null; // reference group
        //Debug.Log(myManager.myManager.groups.Count);
        foreach (GameObject obj in myManager.myManager.groups)
        {
            PeakingGroup group = obj.GetComponent<PeakingGroup>();
            double randNumGroup = randNum * group.standartDeviation + group.rushHour; // calc for group mean and deviation
            if (randNumGroup < 0) randNumGroup = 24 + randNumGroup; // format for 24h time and check for negatives
            double absDiff = Math.Abs(timeOfDay-randNumGroup); //get diference between current time
            float chanceToSpawnScale = UnityEngine.Random.Range(0.0f, group.spawnRate);
            absDiff = absDiff - absDiff * chanceToSpawnScale;

            //normally randomise number and add it to the diff depending ot spawn rate
            //randomise from 0 to spawn rate, get boost depending on the number
            //100 always
            //80 sometimes 
            //20 sometimes 
            //diff1 diff2 = 4 diff3 = 5
            //sp1= 80 sp2 = 20 sp3 = 100





            //give random chance decided by the spawn rate
            //spawn rate: lower should get further away, higher should get closer relative to itself
            //equation is absDiff - absDiff * (spawnrate/SpawnRateSumOverall)
            //have x % chance of removing yourself to the absDiff 
            //chances that have nothing to do with this will be taken in account 
            //say 80 and 20 // won't be 80% of the time won't be 20%

            //this solves situation where you have two absDiffs with the same value but different spawn rates
            //solves also normal situations and gives more favourability to groups with higher probability of getting spawned

            //lowest difference wins
            if (absDiff < min)
            {
              
                min = absDiff;
                minGroup = group;
            }
        }

        if (minGroup)
        {
            if(minGroup.groupName == "lo")
                Debug.Log(minGroup.groupName);

            randNum = randNum * minGroup.standartDeviation + minGroup.avrgStayTime; // calculate stay time for winning group mean and deviation
            

        }
        else
            randNum = 0; //err?


        return (float) (randNum>0?randNum:0);
    }
    
  */
