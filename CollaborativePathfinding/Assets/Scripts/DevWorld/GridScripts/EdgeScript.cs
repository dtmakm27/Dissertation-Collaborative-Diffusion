using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EdgeScript {


    private NodeScript from;  // Node start point
    private NodeScript to;    // Node end point
    private float cost;       // Cost for travelling between the nodes
    public int nodesTraffic = 0;
    private int capacity;
    public int occupancy;
    //public bool[] openSlots;
    public float roadLen;
    private float minSpeed = 2;
    //public Vector3[] slotsPosition;
    public Dictionary<double,int> occupancyOverTimeInstances = new Dictionary<double, int>();
    public Dictionary<double,double> occupancyOverTime = new Dictionary<double,double>();
    public EdgeScript()
    {
        for (int i = 0; i < 24; i++){
            for (int c = 0; c < 60; c++){
                double t = System.Math.Round(i + c/100.0,2);
                occupancyOverTime[t] = 0;
                occupancyOverTimeInstances[t] =0;


            }
        }
        
    }

    public Dictionary<double,double> calcAverages(){
        Dictionary<double,double> temp = new Dictionary<double, double>();

        foreach(var key in occupancyOverTime.Keys)
        {
            temp[key] = 0;
            if(occupancyOverTimeInstances[key]>0)
                temp[key] = occupancyOverTime[key]/occupancyOverTimeInstances[key]*1.0;
        }
        return temp;
    }

    public void addOccupancy(double t){
        occupancy = Math.Min(occupancy+1,capacity);
        occupancyOverTime[t] += getPercentClosed();
        occupancyOverTimeInstances[t] += 1;

    }

    public void removeOccupancy(double t){
        occupancy = Math.Max(occupancy-1,0);
        occupancyOverTime[t] += getPercentClosed();
        occupancyOverTimeInstances[t] += 1;

    }

    public float getPercentOpen(){
       // return openSlots.Where(x=>x==true).Count()/openSlots.Length;
       return 1f - occupancy*1f/capacity*1f;
    }

     public double getPercentClosed(){ //zaetost
       // return openSlots.Where(x=>x==true).Count()/openSlots.Length;
       return occupancy*1f/capacity*1f;
    }


    public EdgeScript(NodeScript from, NodeScript to)
    {
        for (int i = 0; i < 24; i++){
            for (int c = 0; c < 60; c++){
                double t = System.Math.Round(i + c/100.0,2);
                occupancyOverTime[t] = 0;
                occupancyOverTimeInstances[t] = 0;
            }
        }
        this.from = from;
        this.to = to;

        float dist = Vector3.Distance(from.transform.position, to.transform.position);
        //always have atleast 1
        //THIS SHOULD BE ACTUAL SIZE OF CAR WITH RESPECTS TO THE SIZE OF THE MAP
        occupancy = 0;
        capacity = dist > GridManagerScript.diffusionAvatarSize ? (int)Math.Floor(dist / GridManagerScript.diffusionAvatarSize) : 1;
        roadLen = dist;
        //5.6
        //# - - - - -  #
        //float step = dist / capacity;
        //get center
        //set array sizes
        // slotsPosition = new Vector3[capacity];
        // openSlots = new bool[capacity];

        //foreach slot
        //nextV should be equal to the position of the next vector between from and to
        //set first pos as center of first slot
        // slotsPosition[0] = Vector3.MoveTowards(from.transform.position, to.transform.position, step / 2);
        // openSlots[0] = true;
        // for (int i = 1; i < capacity; i++)
        // {
        //     Vector3 nextV = Vector3.MoveTowards(from.transform.position, to.transform.position, step);
        //     slotsPosition[i] = nextV;
        //     openSlots[i] = true;
        // }
        //last one is actual goal
        // slotsPosition[capacity - 1] = to.transform.position;

    }

    // private void Update() {
        
    // }


    ~EdgeScript()
    {

    }


    // Return the from node
    public NodeScript GetFrom()
    {
        return from;
    }

    // Set the from node
    public void SetFrom(NodeScript newFrom)
    {
        if (newFrom != null)
        {
            from = newFrom;
        }
        roadLen = Vector3.Distance(from.transform.position, to.transform.position);

    }

    // Return the to node
    public NodeScript GetTo()
    {
        return to;
    }

    // Set the to node
    public void SetTo(NodeScript newTo)
    {
        if (newTo != null)
        {
            to = newTo;
        }
        roadLen = Vector3.Distance(from.transform.position, to.transform.position);

    }

    // Return the cost
    public float GetCost()
    {
        return cost;
    }

    // Set the cost
    public void SetCost(float newCost)
    {
        cost = newCost;
    }

    //set speed for highways
    public float GetSpeed()
    {   
        if (from.highwayNode && to.highwayNode)
        {
            return GridManagerScript.highwaySpeed* getPercentOpen() + minSpeed;
        }
        else return GridManagerScript.citySpeed * getPercentOpen() + minSpeed;
    }

    public void closeSlot() {

    }

    public void openSlot() {

    }


}
