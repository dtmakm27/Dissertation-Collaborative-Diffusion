using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using MathNet;

public class PeakingGroup : MonoBehaviour
{
    public string groupName = "new Group"; // text to write upon
    public float rushHour = 12.0f; // peak time
    public int avrgStayTime = 1; // average stay time of group
    public float standartDeviation = 1;  // standard deviation for leave time/spawn rate

    public int spawnNumber = 0; // spawn rate
    public int numberAlreadySpawned = 0;

    public float reservedSpawnRate = 0f; // spawn rate for avatars with a reserved space
    public float signRedirectionChance = 0.5f; //chance for the next sign placed to redirect
    


    public List<NodeScript> goals;
    public ParkingLot lastExpanded;
    public ParkingLot origin;
    public bool expanded = false;
    public int index;
    public int originalRadius;
    public List<NodeScript> redirectToGoals;

    public bool hasCheckedRedirectedGoalsLast = false;
    public ParkingLot lastExpandedRedirect;
    public ParkingLot redirectOrigin;
    public bool expandedRedirect = false;



    public string toString() {
        string objString = "" + groupName + ";;" + rushHour + ";;" +avrgStayTime +";;"+
            standartDeviation +";;"+spawnNumber +";;" + reservedSpawnRate +";;"+signRedirectionChance;

        if(redirectOrigin!=null){
            objString += ";;"+ redirectOrigin.parkingLotName;
        }else objString += ";;";
        if(origin!=null){
            objString += ";;" + origin.parkingLotName;
        }else objString += ";;none";
        return objString;
    }
   
    public void injectSpawnerDistribution(Dictionary<double,int> injection){

       //for testing 
        System.Random r = new System.Random();
        int spawnerNum = GridManagerScript.spawners.Count;
        //make sure we don't devide by 0
        if (spawnerNum > 0) {
            int carsSum = injection.Sum(x=>x.Value);
            int toAddToFirst = carsSum % spawnerNum;

            int toDistribute = carsSum - toAddToFirst;

            int carsForEach = toDistribute / spawnerNum;

            //distribute cars for the first node (with the added remainder) using firstNodeCars
            int firstNodeCars = carsForEach + toAddToFirst;

             //now do the rest                                                                                                                                                                                            
            foreach (var spawner in GridManagerScript.spawners)
            {
                int loopsToAllocate = carsForEach;

                if(spawner == GridManagerScript.spawners.First())
                    loopsToAllocate = firstNodeCars;

                Dictionary<double,int> carSpawnerBuckets = new Dictionary<double, int>();
                for (int i = 0; i < 24; i++){
                    for (int c = 0; c < 60; c++){
                        double t = System.Math.Round(i + c/100.0,2);
                        carSpawnerBuckets[t] = 0;
                    }
                }

                //randomise a number and add a car to that slot until all loopstoallocate are allocated
                //subtract from injection
                while(loopsToAllocate>0){
                    //randomise numb from injection keys
                    double t = injection.Keys.ToList()[r.Next(injection.Keys.Count)];
                    injection[t]--;

                    if(injection[t] <= 0)
                        injection.Remove(t);

                    if(carSpawnerBuckets.ContainsKey(t))
                        carSpawnerBuckets[t] += 1;

                    loopsToAllocate--;
                }

                spawner.addSpawnBucket(this,carSpawnerBuckets);
            
            }

        }
    }
    //put the cars to be spawned into a map that tells each spawner node when to spawn some number of cars
    public void setupSpawnersDistribution() {
        //for testing 
        int sum =0;

        int spawnerNum = GridManagerScript.spawners.Count;
        //make sure we don't devide by 0
        if (spawnerNum > 0) {

            int toAddToFirst = spawnNumber % spawnerNum;

            int toDistribute = spawnNumber - toAddToFirst;

            int carsForEach = toDistribute / spawnerNum;

            //distribute cars for the first node (with the added remainder) using firstNodeCars
            int firstNodeCars = carsForEach + toAddToFirst;


            // non deterministic
            MathNet.Numerics.Distributions.Normal normalDist = 
                new MathNet.Numerics.Distributions.Normal(rushHour, standartDeviation);

            //now do the rest                                                                                                                                                                                            
            foreach (var spawner in GridManagerScript.spawners)
            {
                int loopsToAllocate =carsForEach;

                if(spawner == GridManagerScript.spawners.First())
                    loopsToAllocate = firstNodeCars;

                Dictionary<double,int> carSpawnerBuckets = new Dictionary<double, int>();
                for (int i = 0; i < 24; i++){
                    for (int c = 0; c < 60; c++){
                        double t = System.Math.Round(i + c/100.0,2);
                        carSpawnerBuckets[t] = 0;
                    }
                }

                //allocate new buckets
                for (int i = 0; i < loopsToAllocate; i++)
                {
                    double spawnTime = normalDist.Sample();//in hours
                    if(spawnTime < 0)
			            spawnTime = 24 + spawnTime;

                    double minutes = (int)(
                        (spawnTime - System.Math.Truncate(spawnTime))*60)/100.0;

                    double hours = System.Math.Truncate(spawnTime);
                    double timeOfDay = System.Math.Round((minutes + hours)%24,2);
                    try
                    {
                        carSpawnerBuckets[timeOfDay] += 1;
                        sum+=1;
                    }
                    catch (System.Exception)
                    {
                        Debug.Log(timeOfDay);
                        Debug.Log(carSpawnerBuckets.ContainsKey(timeOfDay));
                    }
                }
                spawner.addSpawnBucket(this,carSpawnerBuckets);
            
            }

            //end for non-deterministic



            //Deterministic
            // for (int i = 0; i < 24; i++)
            // {
            //     for (int c = 0; c < 60; c++)
            //     {
            //         //deterministic
            //     }
            // }

            //distribute cars for the other nodes (with no remainder) using carsForEach
            // bool flagFirst = true;
            // foreach (var spawner in GridManagerScript.spawners)
            // {
            //     if (flagFirst)//skip first
            //     {
            //         flagFirst = false;
            //         continue;
            //     }



            // }
        }
    }

    
    public void remove() {
        foreach (var spawner in GridManagerScript.spawners){
            spawner.removeSpawnBucket(this);
        }
    }


    //setup initially goals and set last expanded and origin so you know where you start from and you know where you last got to 
    public void setUpGoals(List<NodeScript> goals,List<NodeScript> redirectGoals) {
        this.hasCheckedRedirectedGoalsLast = true;

        this.goals = goals;
        this.lastExpanded = goals.First().GetComponent<ParkingLot>();
        this.origin = goals.First().GetComponent<ParkingLot>();

        resetRedirect(redirectGoals);

    }

    public void resetRedirect(List<NodeScript> redirectGoals) {
        this.redirectToGoals = redirectGoals;
        this.lastExpandedRedirect = redirectToGoals.First().GetComponent<ParkingLot>();
        this.redirectOrigin = redirectToGoals.First().GetComponent<ParkingLot>();
    }


    //TODO EXPAND REDIRECTED HERE AS WELL
    private void Update()
    {
        //switch checking between normal goals and redirection goals to improve performance 
        if (hasCheckedRedirectedGoalsLast)
        {
            //expand location strategy, find closest parking lot and expand radius to there and reset the stuff with the new radius
            //expand only when new parking lot gets full 
            if (lastExpanded && origin && !lastExpanded.GetComponent<NodeScript>().openParking)
            {

                ParkingLot[] lots = GridManagerScript.ParkingLotManager.parkingLots.Where(x=>!x.name.Contains("streetParking")).Select(x => x.GetComponent<ParkingLot>()).ToArray();
                //if we've gono through all the parking lots and added them as goals don't go in an infinite loop
                if (goals.Count == lots.Length)
                    return;
                //find closest and add everything around 
                //pick first closest node so that it's out of the current radius 
                ParkingLot closest = null;
                float distClosest = float.MaxValue;
                foreach (var item in lots)
                {
                    if (!goals.Contains(item.GetComponent<NodeScript>()))
                    {

                        float distA = Vector3.Distance(item.transform.position, origin.transform.position);
                        distClosest = closest == null ? distClosest : Vector3.Distance(closest.transform.position, origin.transform.position);
                        if (distA < distClosest)
                        {
                            closest = item;
                        }

                    }
                }
                //expand radius with closest distance
                goals = Physics.OverlapSphere(origin.transform.position, distClosest).Where(x => x.tag == "parkingLot").Select(x => x.gameObject.GetComponent<NodeScript>()).ToList();
                //update last expanded
                lastExpanded = closest;
                expanded = true;
                Debug.Log("redir expanded to " + lastExpanded);

            }
            //if origin is 70% free scale back the radius to the original value
            else if (expanded && origin.currentOccupancy * 1f / origin.capacity * 1f <= 0.7f)
            {
                expanded = false;
                lastExpanded = origin;
                goals = Physics.OverlapSphere(origin.transform.position, originalRadius).Where(x => x.tag == "parkingLot"&& !x.name.Contains("streetParking")).Select(x => x.gameObject.GetComponent<NodeScript>()).ToList();
            }
            hasCheckedRedirectedGoalsLast = !hasCheckedRedirectedGoalsLast;
        }

        else {
            //expand location strategy, find closest parking lot and expand radius to there and reset the stuff with the new radius
            //expand only when new parking lot gets full 
            if (lastExpandedRedirect && redirectOrigin && !lastExpandedRedirect.GetComponent<NodeScript>().openParking)
            {

                ParkingLot[] lots = GridManagerScript.ParkingLotManager.parkingLots.Where(x => x.name != "streetParking").Select(x => x.GetComponent<ParkingLot>()).ToArray();
                //if we've gono through all the parking lots and added them as goals don't go in an infinite loop
                if (redirectToGoals.Count == lots.Length)
                    return;
                //find closest and add everything around 
                //pick first closest node so that it's out of the current radius 
                ParkingLot closest = null;
                float distClosest = float.MaxValue;
                foreach (var item in lots)
                {
                    if (!redirectToGoals.Contains(item.GetComponent<NodeScript>()) && !goals.Contains(item.GetComponent<NodeScript>()))
                    {

                        float distA = Vector3.Distance(item.transform.position, redirectOrigin.transform.position);
                        distClosest = closest == null ? distClosest : Vector3.Distance(closest.transform.position, redirectOrigin.transform.position);
                        if (distA < distClosest)
                        {
                            closest = item;
                        }

                    }
                }
                //expand radius with closest distance
                redirectToGoals = Physics.OverlapSphere(redirectOrigin.transform.position, distClosest).Where(x => x.tag == "parkingLot" ).Select(x => x.gameObject.GetComponent<NodeScript>()).ToList();
                //update last expanded
                lastExpandedRedirect = closest;
                expandedRedirect = true;
                Debug.Log("expanded to "+lastExpandedRedirect);
            }
            //if origin is 70% free scale back the radius to the original value
            else if (expandedRedirect && redirectOrigin.currentOccupancy * 1f / redirectOrigin.capacity * 1f <= 0.7f)
            {
                expandedRedirect = false;
                lastExpandedRedirect = redirectOrigin;
                redirectToGoals = Physics.OverlapSphere(redirectOrigin.transform.position, originalRadius).Where(x => x.tag == "parkingLot").Select(x => x.gameObject.GetComponent<NodeScript>()).ToList();
                Debug.Log("shrunk to " + lastExpandedRedirect);

            }

            hasCheckedRedirectedGoalsLast = !hasCheckedRedirectedGoalsLast;
        }

    }

}
