
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger : MonoBehaviour
{
    public bool stop = false;
    public DiffusionAvatarScript diff;
    public bool trafficStop = false;
    public bool test = false;
    int randmSwitch = 5;
    BusScript bus;


    Collider other;//when other is destroyed on trigger exit won't be triggered so need to constantly check if the object was destroyed
    bool triggered = false;

    void OnTriggerEnter(Collider other)
    {

   
        if (other.gameObject.tag == "avatar")
        {
            if(stop)
                return;
            Trigger hit = other.gameObject.GetComponent<Trigger>();
            DiffusionAvatarScript diffHit = hit.diff;
            if (diffHit.nextNode != diff.nextNode)
                return;

            float distanceMe = Vector3.Distance(diff.avatar.transform.position, diff.nextNode.transform.position);
            float distanceHit = Vector3.Distance(diffHit.avatar.transform.position, diffHit.nextNode.transform.position);
            //if collider is closer to goal stop else don't

            //if at the back
            if (distanceMe > distanceHit && ((diffHit.entering && diff.entering) || (!diffHit.entering && !diff.entering)))
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
        else if (other.gameObject.tag == "trafficLight")
        {
            bool changeTo = other.gameObject.GetComponent<TrafficLightScript>().greenLight;
            trafficStop = !changeTo;
            stop = !changeTo;
            // randmSwitch = (int)UnityEngine.Random.Range(0f,10f);
        }

        else if (other.gameObject.tag == "trafficSign")
        {
            if (UnityEngine.Random.Range(0.0f, 1f) < other.gameObject.GetComponent<RedirectionSignScript>().chanceToRedirect)
                other.gameObject.GetComponent<RedirectionSignScript>().ReDirect(diff);
        }
        else if (other.gameObject.tag == "bus") {

            BusTrigger hit = other.gameObject.GetComponent<BusTrigger>();
            BusScript diffHit = hit.GetComponent<BusScript>();


            float distanceMe = Vector3.Distance(diff.avatar.transform.position, diff.nextNode.transform.position);
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

        if (other.gameObject.tag == "avatar")
        {
            DiffusionAvatarScript diffHit = other.gameObject.GetComponent<Trigger>().diff;
            if (((diffHit.entering && diff.entering) || (!diffHit.entering && !diff.entering)))
            {
                stop = false;
            }
        }
        else if (other.gameObject.tag == "bus") {
            stop = false;
        }
    }

    //if the car infront or around you changes it's light change yours as well
    private void OnTriggerStay(Collider other)
    {


        if (other.gameObject.tag == "trafficLight")
        {
            //change depending on traffic light if you're staying in it
            bool changeTo = other.gameObject.GetComponent<TrafficLightScript>().greenLight;
            trafficStop = !changeTo;
            stop = !changeTo;
        }
        else if (other.gameObject.tag == "avatar")
        {
            Trigger hit = other.gameObject.GetComponent<Trigger>();
            trafficStop = hit.trafficStop;
            if(stop)
                return;
            

            DiffusionAvatarScript diffHit = hit.diff;
            if (diffHit.nextNode != diff.nextNode)
                return;

            float distanceMe = Vector3.Distance(diff.avatar.transform.position, diff.nextNode.transform.position);
            float distanceHit = Vector3.Distance(diffHit.avatar.transform.position, diffHit.nextNode.transform.position);
            //if collider is closer to goal stop else don't

            //if at the back
            if (distanceMe > distanceHit && ((diffHit.entering && diff.entering) || (!diffHit.entering && !diff.entering)))
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
        else if (other.gameObject.tag == "bus") {
            BusTrigger diffHit = other.gameObject.GetComponent<BusTrigger>();
            trafficStop = diffHit.trafficStop;
        }
        
    }
    

    private void Update()
    {

        //if object destroyed and onTriggExitNotRun
        if (triggered && !other) {
            stop = false;
        }
        if (trafficStop)
        {
            stop = true;
        }
        if (diff.redirected)
            test = true;
                //kill switch if we've been in the same place for a long time 
        if(stop){
            //e.g. never stay still more than 4 seconds
           if(GridManagerScript.minutes % (int)System.Math.Floor(TrafficLightScript.secondstoW+randmSwitch) == 0){
                stop = false;
                trafficStop = false;
                triggered = false;
           }
        }
    }
}