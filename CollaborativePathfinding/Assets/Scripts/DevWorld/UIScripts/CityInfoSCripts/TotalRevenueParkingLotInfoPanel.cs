using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TotalRevenueParkingLotInfoPanel : MonoBehaviour {

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
            GetComponent<UnityEngine.UI.Text>().text = "Total Revenue: £" + string.Format("{0:0.00}", myPanel.currentLot.totalRevenue);
        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "Total Revenue: ";
        }
    }
}
