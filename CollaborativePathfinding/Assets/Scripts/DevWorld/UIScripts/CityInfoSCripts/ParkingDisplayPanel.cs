using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingDisplayPanel : MonoBehaviour {

    public NodeScript[] parkingLots;
    public GameObject panel;
    public ParkingLot currentLot;
  

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        bool highlightedNode = false;
        foreach (NodeScript node in parkingLots)
        {
            if(node.GetHighlighted())
            {
                highlightedNode = node.GetHighlighted();
                currentLot = node.GetComponentInParent<ParkingLot>();
                break;
            }
        }

        panel.SetActive(highlightedNode);
    }
}
