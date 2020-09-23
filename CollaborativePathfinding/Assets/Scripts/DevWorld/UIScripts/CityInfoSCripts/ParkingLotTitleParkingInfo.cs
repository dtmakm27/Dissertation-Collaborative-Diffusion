using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ParkingLotTitleParkingInfo : MonoBehaviour {

    public ParkingDisplayPanel myPanel;
    public UnityEngine.UI.InputField cap;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (myPanel && myPanel.currentLot)
        {
            GetComponent<UnityEngine.UI.Text>().text = myPanel.currentLot.parkingLotName + " Information";
            cap.text = myPanel.currentLot.capacity.ToString();

        }
        else
        {
            GetComponent<UnityEngine.UI.Text>().text = "< ParkingLotName > Information";
        }
    }

    public void capacityChange(string cap) {
        int capacity = int.Parse(cap);
        if (myPanel && myPanel.currentLot)
        {
            
            myPanel.currentLot.capacity = capacity>0? capacity : 0;
            
        }

    }


    public void exportToCsv() {
        ParkingLot myNode = myPanel.currentLot.GetComponent<ParkingLot>();

        string csv = string.Join(
        System.Environment.NewLine,
        myNode.capacityData.Select(d => (d.Key + "," + d.Value)).ToArray()
    );
        csv = "hour,occupancy" + System.Environment.NewLine + csv;
        System.IO.File.WriteAllText( myNode.parkingLotName.Replace("/","").Replace(":","")+".csv", csv);

    }

}
