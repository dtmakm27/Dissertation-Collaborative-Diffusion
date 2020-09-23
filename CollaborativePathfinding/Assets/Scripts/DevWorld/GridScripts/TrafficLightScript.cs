using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficLightScript : MonoBehaviour
{
    NodeScript node;
    public bool greenLight = true;
    public float switchTimeSeconds = 60;
    private Material greenLightColor;
    private Material redLightColor;
    public float secondsToWait = 0;
    public static float secondstoW = 0;
    // Start is called before the first frame update
    void Start()
    {
        node = GetComponent<NodeScript>();
        BoxCollider collider = this.GetComponent<BoxCollider>();
        collider.size = new Vector3(6f, 6f, 6f);
        gameObject.layer = 8;
        StartCoroutine(ChangeLightsCoRoutine());
        gameObject.tag = "trafficLight";
        switchTimeSeconds = 4;
        //secondsToWait = switchTimeSeconds / GridManagerScript.secondsPerSecond;
        secondsToWait = switchTimeSeconds;
        secondstoW = secondsToWait;
        redLightColor = (Material)Resources.Load("Materials/RedLightMaterial", typeof(Material));
        greenLightColor = (Material)Resources.Load("Materials/GreenLightMaterial", typeof(Material));

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator ChangeLightsCoRoutine()
    {

        while (true)
        {

            //secondsToWait = switchTimeSeconds / GridManagerScript.secondsPerSecond;
            greenLight = true;
            //change color to green
            GetComponent<MeshRenderer>().material = greenLightColor;
            yield return new WaitForSeconds(secondsToWait);
            greenLight = false;
            //change color red
            //secondsToWait = switchTimeSeconds / GridManagerScript.secondsPerSecond;
            GetComponent<MeshRenderer>().material = redLightColor;
            yield return new WaitForSeconds(secondsToWait);


        }

    }



}
