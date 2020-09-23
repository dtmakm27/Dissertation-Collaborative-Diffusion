using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberOfCarsParkedParkingLotInfo : MonoBehaviour {

    public ParkingDisplayPanel myPanel;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (myPanel && myPanel.currentLot)
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Parked: " + myPanel.currentLot.currentOccupancy;
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Number of Cars Parked: ";
        }
    }
}
