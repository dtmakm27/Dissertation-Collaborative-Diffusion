using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RedirectedGroupScript : MonoBehaviour
{
    public List<NodeScript> goals;
    public ParkingLot[] lastExpandeds;
    public ParkingLot[] origins;

    public int originalRadius = 15;
    // Start is called before the first frame update
    void Start()
    {

    }

    public void addRedirectionGoal() {

    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < origins.Length; i++)
        {

            ParkingLot lastExpanded = lastExpandeds[i];
            ParkingLot origin = origins[i];
            //expand location strategy, find closest parking lot and expand radius to there and reset the stuff with the new radius
            //expand only when new parking lot gets full 
            if (lastExpanded && origin && !lastExpanded.GetComponent<NodeScript>().openParking)
            {
                ParkingLot[] lots = GridManagerScript.ParkingLotManager.parkingLots.Select(x => x.GetComponent<ParkingLot>()).ToArray();
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

            }
            //if origin is 70% free scale back the radius to the original value
            else if (origin && lastExpanded && lastExpanded!=origin && origin.currentOccupancy * 1f / origin.capacity * 1f <= 0.7f)
            {
                lastExpanded = origin;
                goals = Physics.OverlapSphere(origin.transform.position, originalRadius).Where(x => x.tag == "parkingLot").Select(x => x.gameObject.GetComponent<NodeScript>()).ToList();
            }
        }
    }

}
