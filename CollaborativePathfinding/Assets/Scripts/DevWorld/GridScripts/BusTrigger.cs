using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BusTrigger : MonoBehaviour
{

    public bool stop = false;
    UnityEngine.Random rand = new UnityEngine.Random();
    public bool trafficStop = false;
    public bool test = false;
    public BusScript bus;


    Collider other;//when other is destroyed on trigger exit won't be triggered so need to constantly check if the object was destroyed
    bool triggered = false;

    private void Start() {
        if(GridManagerScript.enableCollisions){
            Destroy(GetComponent<BoxCollider>());
        }
        

    }

    void OnTriggerEnter(Collider other)
    {

      
         if (other.gameObject.tag == "trafficLight")
        {
            bool changeTo = other.gameObject.GetComponent<TrafficLightScript>().greenLight;
            trafficStop = !changeTo;
            stop = !changeTo;
        }

        else if (other.gameObject.tag == "bus")
        {

            BusTrigger hit = other.gameObject.GetComponent<BusTrigger>();
            BusScript diffHit = hit.GetComponent<BusScript>();
       

            float distanceMe = Vector3.Distance(bus.transform.position, bus.nextNode.transform.position);
            float distanceHit = Vector3.Distance(diffHit.transform.position, diffHit.nextNode.transform.position);
            //if collider is closer to goal stop else don't

            //if at the back
            if (distanceMe > distanceHit)
            {
                triggered = true;
                this.other = other;
                //see if the car infront is at a red light
                stop = true;
            }
            //at the front
            else
            {

            }

        }
    }

    //figure out block condition
    //here
    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "avatar" || other.gameObject.tag == "bus")
        {
            stop = false;
        }
    }

    //if the car infront or around you changes it's light change yours as well
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "avatar" || other.gameObject.tag == "bus")
        {
            Trigger diffHit = other.gameObject.GetComponent<Trigger>();
            trafficStop = diffHit.trafficStop;
        }
        else if (other.gameObject.tag == "trafficLight")
        {
            //change depending on traffic light if you're staying in it
            bool changeTo = other.gameObject.GetComponent<TrafficLightScript>().greenLight;
            trafficStop = !changeTo;
            stop = !changeTo;
        }
    }

    private void Update()
    {

        //if object destroyed and onTriggExitNotRun
        if (triggered && !other)
        {
            stop = false;
        }
        if (trafficStop)
        {
            stop = true;
        }

    }
}
