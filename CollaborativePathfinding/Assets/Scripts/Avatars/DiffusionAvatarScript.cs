using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class DiffusionAvatarScript {

    public static List<Tuple<float,float,float>> timeLeftArrivedVTCAll = new List<Tuple<float, float,float>>();

    float totalPathLength = 0;
    float timeLeft;
    float timeArrived;
    
    bool paused;                      // Is the avatar paused
    public GameObject avatar;         // The game object part of the avatar
    public NodeScript previousNode;   //TESTT THIS OUT, the previous node visited by the avatar
    public NodeScript currentNode;    // The node the avatar has just passed
    public NodeScript nextNode;       // The Node the avatar is moving to
    public bool endReached;           // Has the avatar reached the goal node
    public bool entering;             // Is the avatar entering the city
    public bool isCityAvatar;         // Is the avatar on the newcastle simulation city
    public PeakingGroup groupId;
    float accelerationSteepness = 1.01f;      //acceleration steepness modifier
    float acceleration = 0.1f;  //acceleration as a constant
    float decceleration = 0.025f;   //linear decceleration
    float atmSpeed = 0.1f;  //current speed
    float parkingSpeed = 0.5f; //max speed to park at
    public PeakingGroup group;
    //should you have exit goals?
    float magicScalarValue = 1000;
    bool prebooked = false;
    public Guid id;
    public NodeScript goal = null;
    //a switch for switching between redirected and undirected paths
    //if undirected goals are every node
    //redirected are specific goals only
    public bool redirected = false;
    List<NodeScript> AStarPath = new List<NodeScript>();
    int indexAtAstarPath = 0;

    public DiffusionAvatarScript(PeakingGroup group, bool prebooked):base() {
        
        timeLeft = (float)getTimeOfDay(); //get the current time
        if (group.goals == null)
        {
            return;
        }

        this.group = group;
        //check space in main parking destination, check if prebooking
        //prebooked ones head towards the goal and the goal is always open for them
        if (prebooked) {
            id = Guid.NewGuid();
            if (group.goals.First().GetComponent<ParkingLot>().reserveSpace(id)) {
                //have only one goal
                goal = group.goals.First();
                //change booked status
                this.prebooked = prebooked;
            }

        }
    }
    public DiffusionAvatarScript() {
            timeLeft = (float)getTimeOfDay(); //get the current time

        // if no goal go to the closest spot you can find (normal behaviour as it is currently)
    }
    // Updates the nodes for thier avatar overlap values
    public void UpdateNodeAvatarOverlap()
    {
        
        // not working on last node in devworld due to the lack of time to decrement the overlapping avatars when entering time
        currentNode.avatarsOverlapping++;
        //CHECK THIS< IS PROBABLY GARBAGE CODE
        if (previousNode)
        {
            if (GridManagerScript.isTest){
                previousNode.avatarsOverlapping = 0;
                return;
            }
            previousNode.avatarsOverlapping = previousNode.avatarsOverlapping - 1 >= 0 ? previousNode.avatarsOverlapping - 1 : 0;
            
        }
        //nextNode.avatarsOverlaping++;

    }

    public double getTimeOfDay()
        {
            return System.Math.Round(GridManagerScript.hours*1.0 + GridManagerScript.minutes/ 100.0,2);
        }
    public static int sum = 0;
    // Update is called once per frame
    
    public void Update() {
        if (group&& group.goals == null) {
            group = null;
            goal = null;
        }
      
        

        if (!paused)
        {
            if (GridManagerScript.enableCollisions && avatar.GetComponent<Trigger>().stop) {
                return;
            }
            //sanity check
            if(nextNode==null || currentNode==null || currentNode.FindEdgeTo(nextNode) == null)
                return;
            
            //update a the path when end is closed
            if(GridManagerScript.usingAStarOnly && 
                (AStarPath.Count==0 || (entering && !AStarPath.Last().goal))){
                    Debug.Log("NewPath " + AStarPath.Last());
                    indexAtAstarPath=0;
                AStarPath = GridManagerScript.AStar.
                    GenerateEntryPath(currentNode,group != null? group.goals.ToArray() : null);
                    nextNode = GetBestDiffusionNode(currentNode);
            }
            
            if (!GridManagerScript.isTest)
            {

                float speedLimit = GridManagerScript.citySpeed;
                //get the edge speed and calculate speed limit if it exist, else assume no difference
                //calc traffic
                EdgeScript edge = currentNode.FindEdgeTo(nextNode);
                if (currentNode != null && edge != null)
                {
                    //speed in miles per hour 
                    //0.0003miles/s = 0.066p/second per second (fs - fake seconds)
                    speedLimit = edge.GetSpeed();
                    //GET THIS CHECKED OUT
                    // speedLimit = speedLimit - speedLimit * (edge.nodesTraffic / EdgeScript.maxTraffic) * EdgeScript.maxTrafficP;
                }
                if(GridManagerScript.accelerationCalculations){
                    //if the next node is the parking node and avatar has some speed0 slow down
                    if (nextNode.openParking && (nextNode.NodeStatus == NodeScript.NODE_STATUS.END && atmSpeed >= (speedLimit * parkingSpeed)))
                        atmSpeed -= atmSpeed * (decceleration * 2);

                    //else accelerate// decelerate
                    else
                    {
                        //calc deceleration
                        //if above speed limit or 
                        if (atmSpeed > speedLimit)
                            atmSpeed -= atmSpeed * decceleration;
                        //calc acceleration
                        else if (atmSpeed < speedLimit)
                        {
                            acceleration = acceleration * accelerationSteepness;
                            atmSpeed += atmSpeed * acceleration;
                            if (atmSpeed > speedLimit) atmSpeed = speedLimit; //skip coming into calculations for dec or acc next time
                        }
                    }
                    speedLimit = atmSpeed;

                }
                //foreach frame translate 
                //checkout 123 for explanation
                //1 mile irl = 240p in game
                //add about 20p to make up for inconsistency of time.deltatime
                //every update calc how much of a second has passed in game and add it on the speed

                float speedinpixels = (speedLimit * 260) / 3600; //mile/h
                float translatePerFrame = speedinpixels * GridManagerScript.secondsPerSecond * Time.deltaTime;

                Vector3 translateAvatar = avatar.transform.position + (nextNode.transform.position - avatar.transform.position).normalized * translatePerFrame; //vector to translate by

                //get the distance between avatar and goal and see if you're overshooting
                //if you're not overshooting just continue 
                //if you are overshooting just arrive
                //Db (distance before) is hypothenos from origin to goal (basic distance)
                //Da (distance after) is the hypothenos from origin to the potential new destination (towards goal) 
                float Db = Vector3.Distance(avatar.transform.position, nextNode.transform.position);
                float Da = Vector3.Distance(avatar.transform.position, translateAvatar);

                if (Da > Db)
                {
                    avatar.transform.position = nextNode.transform.position;
                }
                else
                    avatar.transform.position = translateAvatar;

            }
            else
                avatar.transform.position = avatar.transform.position + (nextNode.transform.position - avatar.transform.position).normalized;

            // Check if the avatar is within the bounds of the next node's centre
            if (avatar.transform.position.x >= nextNode.transform.position.x - 1.0f &&
               avatar.transform.position.y >= nextNode.transform.position.y - 1.0f &&
               avatar.transform.position.z >= nextNode.transform.position.z - 1.0f &&
               avatar.transform.position.x <= nextNode.transform.position.x + 1.0f &&
               avatar.transform.position.y <= nextNode.transform.position.y + 1.0f &&
               avatar.transform.position.z <= nextNode.transform.position.z + 1.0f)
            {
                double timeOfDay = getTimeOfDay();

                currentNode.FindEdgeTo(nextNode).removeOccupancy(timeOfDay);
                // If not at the destination
                //TODO REWRITE THIS CHECK AND THE ONES BELOW
                //if no specific goal any goals count 
                //if specific goal, all goals count
                if ((goal ==null && ((nextNode.NodeStatus != NodeScript.NODE_STATUS.END && !isCityAvatar) ||
                    (!nextNode.goal && isCityAvatar && entering) || (!nextNode.exit && isCityAvatar && !entering)))
                    ||  (goal!=null&& goal!=nextNode))
                {
                    //set the previous node to the current node
                    previousNode = currentNode;
                    // Set the current node as the next node
                    currentNode = nextNode;
                    // Calculate the next node
                    if (entering)
                    {
                        nextNode = GetBestDiffusionNode(nextNode);
                    }
                    else
                    {   
                        nextNode = GetBestExitDiffusionNode(nextNode);
                    }
                    currentNode.FindEdgeTo(nextNode).addOccupancy(timeOfDay);
                    totalPathLength += currentNode.FindEdgeTo(nextNode).roadLen;

                }
                else
                {


                    // check if destination reached is in goals
                    //if goal is not specified arrive at any of the goals
                    //if goal is specified only  arrive at the given goal
                    //only arrive at redir goal
                    if ((goal == null && (group == null || group.goals == null || (group.goals.Contains(nextNode) || (redirected && group.redirectToGoals.Contains(nextNode))) || !entering))
                        || (goal == nextNode))
                    {



                        //reset the overlapping avatars on the node behind you
                        //if(GridManagerScript.isTest) currentNode.avatarsOverlapping = -1;
                        // If your destination was a parking lot add a car to the lot
                        if ((entering && nextNode.goal && nextNode.GetComponentInParent<ParkingLot>() || goal == nextNode))
                        {
                            timeArrived = (float)timeOfDay; //get the current time
                            timeLeftArrivedVTCAll.Add(new Tuple<float, float, float>(timeLeft, timeArrived, totalPathLength));
                            nextNode.GetComponentInParent<ParkingLot>().AddCar(this);
                            endReached = true;
                        }
                        else if (!entering)
                        {
                            endReached = true;
                        }

                    }
                    
                    //go to the actual goal
                    else
                    {
                        //set the previous node to the current node
                        previousNode = currentNode;
                        // Set the current node as the next node
                        currentNode = nextNode;

                        // Calculate the next node
                        //passes current end that is not it's own goal
                        nextNode = GetBestDiffusionNode(nextNode);
                        currentNode.FindEdgeTo(nextNode).addOccupancy(timeOfDay);
                        totalPathLength += currentNode.FindEdgeTo(nextNode).roadLen;


                    }
                }

            }
        }
    }
    

    //Calculates the next node based to the highest diffusion value of it's neighbours
    NodeScript GetBestDiffusionNode(NodeScript node)
    {

        //if a* get next
        if (GridManagerScript.usingAStarOnly){
            //return next node in path
            indexAtAstarPath++;
            return indexAtAstarPath < AStarPath.Count? AStarPath[indexAtAstarPath] : node;  

            // for (int i = 0; i < AStarPath.Count; i++)
            // {
            //     if( AStarPath[i] == node){
                    
            //     }
            // } 
        }


        // If the node sent is the destination return the node
        if( (goal==null && ((node.goal && isCityAvatar && (!group ||group.goals.Contains(node) || (redirected && group.redirectToGoals.Contains(node)))) || node.edges.Count == 0)) ||  
            (goal==node))
        {
           
            return node;

        }

        // Sets a node for comparison
        NodeScript bestNode = node.edges[0].GetTo(); //this 
        // If the best node is not the destination
        if (!bestNode.openParking || ( goal==null && (!(!group || group.goals.Contains(bestNode) || (redirected && group.redirectToGoals.Contains(bestNode))) || (!bestNode.goal && isCityAvatar))) || 
            (goal!= bestNode))
        {

            Func< NodeScript,NodeScript, bool> better;
            //are we using collaborative diffusion
            if (!GridManagerScript.usingAugmentedDiffusion)
            {
                //if it's part of a group
                if(group){
                    //if redirected go to nearest redir goal
                    if(redirected){
                        better = (other,best) => other && best && other != previousNode &&
                            (other.redirectionStaticDiffusion[group.index] > best.redirectionStaticDiffusion[group.index]);
                    }
                    //else normal cases
                    else{
                        //if specific goal go to that goal
                        if(goal!=null){
                            better = (other,best) => other && best && other != previousNode &&
                                (other.goalTests[group.index] > best.goalTests[group.index]);
                        }
                        //otherwise go to the group goals
                        else{
                            better = (other,best) => other && best && other != previousNode &&
                                (other.staticDiffusionBases[group.index] > best.staticDiffusionBases[group.index]);
                        }
                    }
                }
                //if not in group go to any goal
                else
                {
                    better = (other,best) => other && best && other != previousNode && 
                        (other.staticDiffusionBase > best.staticDiffusionBase);
                }
            }
            //if we are using collaborative diffusion
            else
            {
                //if it's part of a group
                if(group){
                    //if redirected go to nearest redir goal
                    if(redirected){
                        better = (other,best) => other && best && other != previousNode &&

                            (other.redirectionDiffusion[group.index] * (other.redirectionStaticDiffusion[group.index] / magicScalarValue)) /
                             ((other.avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >
                           (best.redirectionDiffusion[group.index] * (best.redirectionStaticDiffusion[group.index] / magicScalarValue)) /
                            ((best.avatarsOverlapping * GridManagerScript.evasionStrength + 1));
                    }
                    //else normal cases
                    else{
                        //if specific goal go to that goal
                        if(goal!=null){
                            better = (other,best) => other && best && other != previousNode &&
                               (other.goalTests[group.index] > best.goalTests[group.index]);
                        }
                        //otherwise go to the group goals
                        else{
                            better = (other,best) => other && best && other != previousNode &&

                                (other.diffusions[group.index] * (other.staticDiffusionBases[group.index] / magicScalarValue)) /
                                 ((other.avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >
                                (best.diffusions[group.index] * (best.staticDiffusionBases[group.index] / magicScalarValue)) /
                                 ((best.avatarsOverlapping * GridManagerScript.evasionStrength + 1));
                        }
                    }
                }
                //if not in group go to any goal
                else
                {
                    better = (other,best) => other && best && other != previousNode &&

                    (other.diffusion) >

                    (best.diffusion);
                    
                }
            }



            // Loop through each neighbour
            foreach (EdgeScript edge in node.edges)
            {

                //so you don't accedently dev by 0
                if ((goal==null && edge.GetTo().goal && (!group || (group.goals.Contains(edge.GetTo()) || (redirected && group.redirectToGoals.Contains(edge.GetTo())) ))) || goal==edge.GetTo()  ) {

                    bestNode = edge.GetTo();
                    break;

                }

                //O.G. (bestNode.diffusion)/((bestNode.avatarsOverlaping*GridManagerScript.evasionStrength+1)/(bestNode.staticDiffusion/ GridManagerScript.evasionStrength))
                // If the neighbour has a better diffusion value change bestNode to the neighbour factoring by the nodes traffic and if not going back

                //MAGIC NUMBERS CHECK IT

                //use direct shortest path to get to goal or diffused algorithm?

                //taking account of the distance of it's goal location
                if(better(edge.GetTo(),bestNode)){
                    bestNode = edge.GetTo();
                }


            }
        }


        return bestNode;
    }

    //Calculates the next node based to the highest exitDiffusion value of it's neighbours
    NodeScript GetBestExitDiffusionNode(NodeScript node)
    {

         //if a* get next
        if (GridManagerScript.usingAStarOnly){
            indexAtAstarPath++;
            return indexAtAstarPath < AStarPath.Count? AStarPath[indexAtAstarPath] : node;  
            // //return next node in path
            // for (int i = 0; i < AStarPath.Count; i++)
            // {
            //     if( AStarPath[i] == node)
            //        return i+1 < AStarPath.Count? AStarPath[i+1] : node;
            // } 
        }

        // If the node sent is the destination return the node
        if (node.exit || node.edges.Count == 0)
        {
            return node;
        }

        // Sets a node for comparison
        NodeScript bestNode = node.edges[0].GetTo();

        Func< NodeScript,NodeScript, bool> better;

        if(!GridManagerScript.usingAugmentedDiffusion)
            better = (other,best) => other && best && other != previousNode &&
             (other.staticExitDiffusion > best.staticExitDiffusion);
        else
            better = (other,best) => other && best && other != previousNode && 
                (other.exitDiffusion * (other.staticExitDiffusion / magicScalarValue)) /
                 ((other.avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >

                (best.exitDiffusion * (best.staticExitDiffusion / magicScalarValue)) /
                 ((best.avatarsOverlapping * GridManagerScript.evasionStrength + 1));


        // If the best node is not the destination
        if (!bestNode.exit)
        {
            // Loop through each neighbour
            foreach (EdgeScript edge in node.edges)
            {
                //so you don't accedently dev by 0
                if (edge.GetTo().exit)
                {
                    bestNode = edge.GetTo();

                    break;
                }
            
                else if (better(edge.GetTo(),bestNode)) {
                    bestNode = edge.GetTo();
                }
            }
        }

        return bestNode;
    }

    // Sets up the avatar ready for use in simulation.
    public void Setup(bool cityAvatar, bool avatarEntering = true)
    {
        isCityAvatar = cityAvatar;                                  
        avatar = GameObject.CreatePrimitive(PrimitiveType.Cube);    // Creates a cube for the avatar script to use as visual reprisentation

        avatar.layer = 8;                                           // Sets the node to the Non-blocking layer
        avatar.transform.position = new Vector3(0, 0, 0);
        avatar.name = "Avatar";                                     // Sets the name to Avatar 
        avatar.AddComponent<Rigidbody>();                           // Adds a rigid body and removes gravity, collisions, movement in the y axis
        avatar.GetComponent<Rigidbody>().useGravity = false;        // and rotation in the x, y and z axis
        avatar.GetComponent<Rigidbody>().isKinematic = true;
        avatar.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX |
                                                       RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        

        entering = avatarEntering;
    }

    // Sets the material
    public void SetMaterial(Material newMat)
    {
        avatar.GetComponent<Renderer>().material = newMat;
    }

    // Gives the avatar a new scale using x, y and z parameters
    public void SetScale(float x, float y, float z)
    {
        avatar.transform.localScale = new Vector3(x, y, z);
    }

    // Gives the avatar a new scale using a Vector3
    public void SetScale(Vector3 scale)
    {
        avatar.transform.localScale = new Vector3(scale.x, scale.y, scale.z);
        //add collider depending on size collider is 0.1 bigger than actual object
        BoxCollider collider = avatar.GetComponent<BoxCollider>();
        collider.size = new Vector3(2f,1.2f,2f);
        if(GridManagerScript.enableCollisions){
            collider.isTrigger = true;
            avatar.gameObject.tag = "avatar";
            avatar.AddComponent<Trigger>();
            avatar.GetComponent<Trigger>().diff = this;
        }
    }

    // Gives the avatar a new node and calculates the nextNode
    public void SetNode(NodeScript newNode)
    {
        if (entering)
        {
            
            currentNode = newNode;
            AStarPath = GridManagerScript.AStar.
                    GenerateEntryPath(currentNode,group != null? group.goals.ToArray() : null);
            nextNode = GetBestDiffusionNode(currentNode);
            
        }
        else
        {

            currentNode = newNode;
            AStarPath = GridManagerScript.AStar.
                    GenerateExitPath(currentNode);
            nextNode = GetBestExitDiffusionNode(currentNode);
            
        }
    }

    // Sets the avatar position using x, y and z parameters
    public void SetPosition(float x, float y, float z)
    {
        avatar.transform.position = new Vector3(x, y, z);
    }

    // Sets the avatar position using a Vector3
    public void SetPosition(Vector3 position)
    {
        avatar.transform.position = new Vector3(position.x+20, position.y, position.z);
    }

    // Sets isPaused
    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
    }
}



/*
     // Loop through each neighbour
            foreach (EdgeScript edge in node.edges)
            {

                //so you don't accedently dev by 0
                if ((goal==null && edge.GetTo().goal && (!group || (group.goals.Contains(edge.GetTo()) || (redirected && group.redirectToGoals.Contains(edge.GetTo())) ))) || goal==edge.GetTo()  ) {

                    bestNode = edge.GetTo();
                    break;

                }


                //diffusion /= GridManagerScript.evasionStrength * avatarsOverlaping;

                //O.G. (bestNode.diffusion)/((bestNode.avatarsOverlaping*GridManagerScript.evasionStrength+1)/(bestNode.staticDiffusion/ GridManagerScript.evasionStrength))
                // If the neighbour has a better diffusion value change bestNode to the neighbour factoring by the nodes traffic and if not going back


                //MAGIC NUMBERS CHECK IT

                //use direct shortest path to get to goal or diffused algorithm?
                if (!GridManagerScript.usingAugmentedDiffusion)
                {
                    //if it's part of a group
                    if (group)
                    {
                        //if redirected go to nearest redir goal else normal cases
                        if (redirected && (edge.GetTo() && edge.GetTo() != previousNode && (edge.GetTo().redirectionStaticDiffusion[group.index] > bestNode.redirectionStaticDiffusion[group.index])))
                        {
                            bestNode = edge.GetTo();
                        }
                        //if specific goal
                        if (!redirected && goal != null && (edge.GetTo() && edge.GetTo() != previousNode && (edge.GetTo().goalTests[group.index] > bestNode.goalTests[group.index]))) {
                            bestNode = edge.GetTo();
                        }
                        //take the quickest route disregarding traffic
                        else if (!redirected && goal ==null && (edge.GetTo() && edge.GetTo() != previousNode && (edge.GetTo().staticDiffusionBases[group.index] > bestNode.staticDiffusionBases[group.index])))
                        {
                            bestNode = edge.GetTo();
                        }
                    }
                    else {
                        //if not in group go to any goal
                        if ( (edge.GetTo() && edge.GetTo() != previousNode && (edge.GetTo().staticDiffusionBase > bestNode.staticDiffusionBase)))
                        {
                            bestNode = edge.GetTo();
                        }
                    }
                }
                else
                {
                    //if goal is specified
                    if (group)
                    {
                        //if redirected go to nearest redir goal else normal cases
                        if (redirected && (edge.GetTo() != previousNode && edge.GetTo() &&
                           (edge.GetTo().redirectionDiffusion[group.index] * (edge.GetTo().redirectionStaticDiffusion[group.index] / magicScalarValue)) / ((edge.GetTo().avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >
                           (bestNode.redirectionDiffusion[group.index] * (bestNode.redirectionStaticDiffusion[group.index] / magicScalarValue)) / ((bestNode.avatarsOverlapping * GridManagerScript.evasionStrength + 1))))
                        {
                            bestNode = edge.GetTo();
                        }
                        //if specific goal use only static def
                        else if (!redirected && goal != null && (edge.GetTo() && edge.GetTo() != previousNode && (edge.GetTo().goalTests[group.index] > bestNode.goalTests[group.index])))
                        {
                            bestNode = edge.GetTo();
                        }
                        //if any goal in the group
                        else if (!redirected && goal == null && (edge.GetTo() != previousNode && edge.GetTo() &&
                        (edge.GetTo().diffusions[group.index] * (edge.GetTo().staticDiffusionBases[group.index] / magicScalarValue)) / ((edge.GetTo().avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >
                        (bestNode.diffusions[group.index] * (bestNode.staticDiffusionBases[group.index] / magicScalarValue)) / ((bestNode.avatarsOverlapping * GridManagerScript.evasionStrength + 1))))
                        {
                            bestNode = edge.GetTo();
                        }
                    }
                    else
                    { 
                    //if no goals is specified
                       
                        if ((edge.GetTo() != previousNode && edge.GetTo() &&
                            (edge.GetTo().diffusion * (edge.GetTo().staticDiffusionBase / magicScalarValue)) / ((edge.GetTo().avatarsOverlapping * GridManagerScript.evasionStrength + 1)) >
                            (bestNode.diffusion * (bestNode.staticDiffusionBase / magicScalarValue)) / ((bestNode.avatarsOverlapping * GridManagerScript.evasionStrength + 1))))
                            {
                                bestNode = edge.GetTo();
                            }
                    }
                }

                //taking account of the distance of it's goal location

            }
*/