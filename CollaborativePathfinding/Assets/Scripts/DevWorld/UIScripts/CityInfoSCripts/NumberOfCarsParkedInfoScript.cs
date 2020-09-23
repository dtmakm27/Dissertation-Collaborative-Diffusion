using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberOfCarsParkedInfoScript : MonoBehaviour {

    public ParkingLotManager parkingManager;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (parkingManager)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Parked: " + parkingManager.GetNumberOfParkedCars();
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Parked: ";
        }
    }
}
