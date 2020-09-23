using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class BusScript : MonoBehaviour
{
    public NodeScript[] route;
    public int currentGoal;
    public int lastGoal;
    public NodeScript nextNode;
    // Start is called before the first frame update
    void Start()
    {
        if (route.Length < 2)
            Destroy(this);
        else {
            currentGoal = 1;
            lastGoal = 0;
            transform.position = route[lastGoal].transform.position;
            nextNode = route[currentGoal];
            this.tag = "bus";
            this.gameObject.layer = 8;
            GetComponent<BusTrigger>().bus = this;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (currentGoal >= route.Length)
        {
            route = route.Reverse().ToArray();
            lastGoal = 0;
            currentGoal = 1;
        }
        nextNode = route[currentGoal];
        if (GetComponent<BusTrigger>().stop) {
            return;
        }

        //foreach frame translate 
        //checkout 123 for explanation
        //1 mile irl = 240p in game
        //add about 20p to make up for inconsistency of time.deltatime
        //every update calc how much of a second has passed in game and add it on the speed


        float speedLimit = GridManagerScript.citySpeed;
        float speedinpixels = (speedLimit * 260) / 3600; //mile/h
        float translatePerFrame = speedinpixels * GridManagerScript.secondsPerSecond * Time.deltaTime;

        Vector3 translateAvatar = transform.position + (route[currentGoal].transform.position - transform.position).normalized * translatePerFrame; //vector to translate by
        //get the distance between avatar and goal and see if you're overshooting
        //if you're not overshooting just continue 
        //if you are overshooting just arrive
        //Db (distance before) is hypothenos from origin to goal (basic distance)
        //Da (distance after) is the hypothenos from origin to the potential new destination (towards goal) 
        float Db = Vector3.Distance(this.transform.position, route[currentGoal].transform.position);
        float Da = Vector3.Distance(this.transform.position, translateAvatar);

        if (Da > Db)
        {
            transform.position = route[currentGoal].transform.position;
        }
        else
            transform.position = translateAvatar;

        route[currentGoal].avatarsOverlapping++;

        // Check if the avatar is within the bounds of the next node's centre
        if (transform.position.x >= route[currentGoal].transform.position.x - 1.0f &&
           transform.position.y >= route[currentGoal].transform.position.y - 1.0f &&
           transform.position.z >= route[currentGoal].transform.position.z - 1.0f &&
           transform.position.x <= route[currentGoal].transform.position.x + 1.0f &&
           transform.position.y <= route[currentGoal].transform.position.y + 1.0f &&
           transform.position.z <= route[currentGoal].transform.position.z + 1.0f)
        {
            currentGoal++;
            lastGoal++;

        }

    }
}
