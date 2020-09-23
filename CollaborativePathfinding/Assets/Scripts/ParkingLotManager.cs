using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingLotManager : MonoBehaviour {

    public float timeStep;              // How long in seconds an hour takes in the simulation 
    public GameObject[] parkingLots;    // The parking lots
    public float totalRevenue = 0;      // The total revenue of the managed lots
    public GridManagerScript myManager; // Manager for clock and standard deviation calculations

    // Use this for initialization
    void Start () {
        //synchronise timestep with clock hours
        timeStep = (float)(3600/(GridManagerScript.secondsPerSecond));
        //setup manager for parking lots
        foreach (var lot in parkingLots)
        {
            lot.GetComponent<ParkingLot>().myManager = this;
        }
	}
	
	// Update is called once per frame
	void Update () {
        
        //recalculate
        timeStep = (float)(3600 / (GridManagerScript.secondsPerSecond));
        
        CalculateRevenue();
    }

    // Gets the total number of parked cars
    public int GetNumberOfParkedCars()
    {
        // Sets the value to zero then loops through all of the lots adding thier occupancy
        int numOfCars = 0;

        foreach (GameObject parkingLot in parkingLots)
        {
            numOfCars += parkingLot.GetComponent<ParkingLot>().currentOccupancy;
        }

        return numOfCars;
    }

    // Calculates the reveue of all the parking lots 
    void CalculateRevenue()
    {
        // Sets revenue to zero then loops through adding the revenue of all of the parking lots
        totalRevenue = 0;
        foreach (GameObject parkingLot in parkingLots)
        {
            totalRevenue += parkingLot.GetComponent<ParkingLot>().totalRevenue;
        }
    }

    // Sets the total revenue to zero and calls reset for all parking lots
    public void ResetParkingLots()
    {
        totalRevenue = 0;
        foreach (GameObject lot in parkingLots)
        {
            lot.GetComponent<ParkingLot>().ResetParkingLot();
        }
    }
}
