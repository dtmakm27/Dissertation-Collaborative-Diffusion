using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalRevenueInfoScript : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Total Revenue: £" + string.Format("{0:0.00}", parkingManager.totalRevenue);
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Total Revenue: ";
        }
    }
}
