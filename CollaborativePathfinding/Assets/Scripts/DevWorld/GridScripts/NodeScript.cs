using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeScript : MonoBehaviour
{
    // The node ststus enum for keeping track of the nodes state
    public enum NODE_STATUS { UNSEARCHED = 0, START = 1, END = 2, SEARCHED = 3, PATH = 4, BLOCKED = 5 };

    public GridManagerScript myManager;    // The nodes grid manger
    public bool hasCreatedStreetParking = false; // track wether node has created street parking between edges
    public NODE_STATUS NodeStatus;         // The nodes status
    public int overlappedTotal =1;
    //0: Basic, 1: Start, 2: End,
    //3: Searched, 4: Path, 5: Blocked
    // 6 is the highlight material for blending with versions of Basic, Start, 
    //End, Searched, Path and blocked.
    public Material[] materials;

    public GameObject[] neighbours;    // Neighbours for the purpose of manually setting edge values                           


    public List<EdgeScript> edges = new List<EdgeScript>();    // The edges coming from this node

    private bool highlighted;    // Is the node highlighted


    private bool calculatedDiffOnLastFrame;    // Used as a toggle for which calculation should be done

    public MapNodeGroup overlappedTotalForArea;

    public NodeScript parentA = null;    // Used for A* pathfinding
    public float hCost = 0;             // The calculated heuristic cost for this node used for A*

    public int avatarsOverlapping = 0;    // The number of overlapping avatars
    public Dictionary<int,int> avatarsOvelapped = new Dictionary<int, int>();

    public bool exit;           // Is the node an exit
    public bool goal;           // Is the node a goal
    public bool openParking;    // Is there a open parking on this node
    public bool highwayNode;    // Is the node part of highway
    //float recoveryRate = 0.2f;

    public float goalDiffusion = 1000000.0f;    // The goal value of diffusion set high to prevent the 
                                                // worst of floating point calculations

    public bool spawner;    // Is this node spawning avatars
    internal bool forceCloseParking = false;

    public float diffusion;        // The goal diffusion value
    public float exitDiffusion;    // The diffusion value used for the exits to the city
    public float staticDiffusionBase;
    public float staticExitDiffusion;


    public static PeakingGroup[] groups;
    public float[] staticDiffusionBases;
    public float[] diffusions;
    public float[] redirectionDiffusion;
    public float[] redirectionStaticDiffusion;
    public float[] goalTests;
    float timeOfDayE = 0;
    public Dictionary<float,int> floats = new Dictionary<float,int>();
    public int overalaping;
    private double previousTimeOfDay = -1;
    public Dictionary<PeakingGroup, Dictionary<double,int> > groupsSpawnBuckets =
        new Dictionary<PeakingGroup, Dictionary<double,int> >();
    public Dictionary<PeakingGroup,int> spawnGroupSums = new Dictionary<PeakingGroup, int>();



    public void addSpawnBucket(PeakingGroup g,Dictionary<double,int> carSpawnerBuckets) {
        this.groupsSpawnBuckets[g] = carSpawnerBuckets;
        spawnGroupSums[g] = 0;
    }

    public void removeSpawnBucket(PeakingGroup g) {
        this.groupsSpawnBuckets.Remove(g);
        this.spawnGroupSums.Remove(g);
    }

    int getSumUpUntilTime(Dictionary<double,int> d,double time){
        return d.Where(x => x.Key < time).Select(x => x.Value).Sum();
    }

    void Start()
    {
        for (int i = 0; i < 24; i++)
        {
            for (int k = 0; k < 6; k++)
            {
                floats.Add(i*1.0f+k*1.0f/10, 0);

            }
        }


        for (int i = 0; i < 24; i++)
        {
            avatarsOvelapped.Add(i,0);
        }

        // Sets the node to default values
        avatarsOverlapping = 0;
        Renderer render = GetComponent<MeshRenderer>();
        NodeStatus = NODE_STATUS.UNSEARCHED;

        diffusion = 0;
        exitDiffusion = 0;
        highlighted = false;
        calculatedDiffOnLastFrame = true;
        groups = myManager.groups.Select(x => x.GetComponent<PeakingGroup>()).ToArray();
        for (int i = 0; i < myManager.groups.Count; i++)
        {
            myManager.groups[i].GetComponent<PeakingGroup>().index = i;
        }
        staticDiffusionBases = new float[myManager.groups.Count];
        diffusions = new float[myManager.groups.Count];
        redirectionDiffusion = new float[myManager.groups.Count];
        redirectionStaticDiffusion = new float[myManager.groups.Count];
        goalTests = new float[myManager.groups.Count];
        // Sets the nodes default material
        if (render != null)
        {
            render.material = materials[(int)NodeStatus];
        }

        // Sets the node up as a goal
        if (goal)
        {
            NodeStatus = NODE_STATUS.END;
            diffusion = goalDiffusion;
            staticDiffusionBase = goalDiffusion;

            openParking = !forceCloseParking;
            goal = !forceCloseParking;
            //GetComponent<NodeScript>().goalDiffusion = open ? 1000000 : 0;
            NodeStatus = !forceCloseParking ? NodeScript.NODE_STATUS.END : NodeScript.NODE_STATUS.UNSEARCHED;

        }
     

       

        // Sets the node up as a exit
        if (exit)
        {
            NodeStatus = NODE_STATUS.END;
            exitDiffusion = goalDiffusion;
            staticExitDiffusion = goalDiffusion;
        }
    }



    void Update()
    {

        //updates static
        if (myManager.groups.Count != groups.Length) {
           
            groups = myManager.groups.Select(x=> x.GetComponent<PeakingGroup>()).ToArray();
            for (int i = 0; i < myManager.groups.Count; i++)
            {
                myManager.groups[i].GetComponent<PeakingGroup>().index = i;
            }
 

        }

        //updates local
        if (groups.Length != staticDiffusionBases.Length) {

            staticDiffusionBases = new float[groups.Length];
            diffusions = new float[groups.Length];
            goalTests = new float[groups.Length];
            redirectionDiffusion = new float[myManager.groups.Count];
            redirectionStaticDiffusion = new float[myManager.groups.Count];

        }



        //Next update
        /*
         don't randomly spawn them
         instead you should


         Make it so that you give a number of cars to be spawned
         you give a peak time for them to spawn and they spawn 

         to achieve this you have to find out at each point (of time) how many cars you are supposed to spawn at each point 

         check the z score for each time and make sure the % of cars that has to be spawned have been spawned 

        TODO:
        pre-compute Z table
        and distribute spawn rate for each node
        

        

         */
       

        //randomly distributes injections
        //waves peak around mean (rush hour)
        if (spawner) 
        {
            //timespeedlow less spawn
            //timespeedhigh more spawn

            //double flatRandNum = UnityEngine.Random.Range(0.0f, 1.01f);
            //double randNum = GridManagerScript.NextGaussianDoubleBoxMuller(0, 1); //calculate random Gausian double (mean is null)
            timeOfDayE = (float) (GridManagerScript.hours * 1.0f + (Math.Round(GridManagerScript.minutes * 0.1f / 10,1)%0.6)); //get the current time
            double timeOfDay = GridManagerScript.getTimeOfDay(); //get the current time
            //this is the x value

            /*foreach (GameObject obj in myManager.groups)
            {

                PeakingGroup group = obj.GetComponent<PeakingGroup>();
                double num = GridManagerScript.NormalDistribution(timeOfDay, group.rushHour, group.standartDeviation);
                num = timeOfDay <= group.rushHour ? num : 1-num;


                double randNumGroup = group.rushHour + group.standartDeviation * randNum;  //calculate for given mean and deviation //normally distributed
                if (randNumGroup < 0) randNumGroup = 24 + randNumGroup; // format for 24h time and check for negatives
                                                                        // spawn more when nearing peak hour and scale chance by myManager.diffusionAvatarSpawnRate
                                                                        //normal distributed chance around rush hour mean


 
                if (UnityEngine.Random.Range(0.0f, 0.5f) < num 
                        && UnityEngine.Random.Range(0.0f, 1.0f) < group.spawnRate //injection control over current group
                            && UnityEngine.Random.Range(0.0f, 1.0f) < myManager.diffusionAvatarSpawnRate) //injection control overall groups
                {

                    // chance to spawn pre-booked parking space car
                    if (UnityEngine.Random.Range(0.0f, 1f) < group.reservedSpawnRate)
                        myManager.SpawnDiffusionAvatar(this, group: group, prebooked: true);
                    else
                        myManager.SpawnDiffusionAvatar(this, group: group, prebooked: false);
                }
            }
            */




            //itteration 2
            //check if you need to reset
            //reset if max spawned is reached already and the NumberOfCarsAtPointX is not the max
            //TODO if you just do a normal Z table map and spawn like that you'll solve the problem
            // foreach (GameObject obj in myManager.groups)
            // {

            //     PeakingGroup group = obj.GetComponent<PeakingGroup>();
            //     int numberSpawned = group.numberAlreadySpawned;
            //     int numberOfCarsToSpawn = group.spawnNumber;

            //     double percentageOfCarsAtPointX = GridManagerScript.NormalDistribution(timeOfDay, group.rushHour, group.standartDeviation);
            //     int NumberOfCarsAtPointX = (int)Math.Round(numberOfCarsToSpawn * percentageOfCarsAtPointX);

            //     int carsToBeSpawned = NumberOfCarsAtPointX - numberSpawned;

            //     if ( numberSpawned >= numberOfCarsToSpawn && NumberOfCarsAtPointX < numberSpawned)
            //         group.numberAlreadySpawned = 0;

            //     for (int i = 0; i < carsToBeSpawned; i++)
            //     {
            //         // chance to spawn pre-booked parking space car
            //         if (UnityEngine.Random.Range(0.0f, 1f) < group.reservedSpawnRate)
            //             myManager.SpawnDiffusionAvatar(this, group: group, prebooked: true);
            //         else
            //             myManager.SpawnDiffusionAvatar(this, group: group, prebooked: false);

            //         group.numberAlreadySpawned += 1;
            //     }

            // }


            //3rd itteration
            if(timeOfDay != previousTimeOfDay){
                previousTimeOfDay = timeOfDay;
                foreach (GameObject obj in myManager.groups)
                {

                    PeakingGroup group = obj.GetComponent<PeakingGroup>();
                    if(groupsSpawnBuckets.ContainsKey(group)){
                        int numberOfCarsToSpawn = groupsSpawnBuckets[group][timeOfDay];

                        //get current group spawns 
                        int actualCurrentSumForGroup = spawnGroupSums[group];
                        //compensate for frame rate drops
                        //calculate expected sum at current time 
                        //e.g. it's 1545 getSumUpUntilTime calculates spawned until 1544
                        //predicted number of current cars at 1544 is 500
                        //actual sum of spawned cars is 490
                        //frameCompensationForGroup is 500 - 490 = 10 (i.e. need to spawn 10 more cars
                        //to compensate for the frame skipping)
                        //that frameCompensation is then added to the number of cars to spawn
                        //each frame compensates for all previous frames
                        int predictedCurrentSum = getSumUpUntilTime(groupsSpawnBuckets[group],timeOfDay); //for one group
                        //calculate compensation
                        int frameCompensationForGroup = (predictedCurrentSum - actualCurrentSumForGroup)>0?
                         (predictedCurrentSum - actualCurrentSumForGroup) : 0;
                        for (int i = 0; i < (numberOfCarsToSpawn+frameCompensationForGroup); i++)
                        {
                            spawnGroupSums[group] += 1;
                            // chance to spawn pre-booked parking space car commented out
                            //if (UnityEngine.Random.Range(0.0f, 1f) < group.reservedSpawnRate)
                              //  myManager.SpawnDiffusionAvatar(this, group: group, prebooked: true);
                            //else
                               myManager.SpawnDiffusionAvatar(this, group: group, prebooked: false);

                            GridManagerScript.numberOfVehicles[timeOfDay] += 1;
                        }

                    }

                }
            }
        }
    }


    // The update used for the node so that their is correct update 
    // order in the GridManager
    public void NodeUpdate()
    {

        // If there is an exit
        if (myManager.exitAvailable)
        {
            // Due to Unity not playing nice with threads in this version
            // I have set it up to calculate each diffusion value on seperate frames
            if (calculatedDiffOnLastFrame)
            {
                CalculateExitDiffusion();
            }
            else
            {
                CalculateDiffusion();
            }
            calculatedDiffOnLastFrame = !calculatedDiffOnLastFrame;
        }
        else
        {

            CalculateDiffusion();
            calculatedDiffOnLastFrame = true;
        }
        avatarsOverlapping = 0; //reset avatars
        //UPDATES ARE DONE BY THE DIFFUSION AVATAR SCRIPT CHECK IF IT IS GARBAGE// Reset the overlap value for teh next frame
        // If the diffusion is being displayed
        if (myManager.GetShowDiffusion())
        {
            // Blend the node material with the diffusion material
            GetComponent<MeshRenderer>().material.Lerp(materials[(int)NodeStatus], materials[(int)NODE_STATUS.END], diffusion / goalDiffusion);

            //If there is an exit available display the exit diffusion as well
            if (myManager.exitAvailable)
            {
                GetComponent<MeshRenderer>().material.Lerp(GetComponent<MeshRenderer>().material, myManager.exitingDiffusionAvatarMaterial, exitDiffusion / goalDiffusion);
            }
        }
        else
        {
            GetComponent<MeshRenderer>().material.Lerp(materials[(int)NodeStatus], materials[(int)NODE_STATUS.END], 0);
        }

        // If highlighted blend with the highlight material
        if (highlighted)
        {
            GetComponent<MeshRenderer>().material.Lerp(materials[(int)NodeStatus], materials[6], 0.25f);
        }
    }

    public float div = 1f;
    //Calculates the diffusion value for the node
    private void CalculateDiffusion()
    {
        for (int i = 0; i < groups.Length; i++)
        {

            //check for desync
            if (i>=staticDiffusionBases.Length) return;

            PeakingGroup group = groups[i].GetComponent<PeakingGroup>();

            //if non-goal oriented group skip
            if (groups[i].goals == null) {
                continue;
            }

            // If there is an end and this node isn't the goal and not blocked

            if (myManager.GetEndAllocated() && !group.goals.Contains(this) && NodeStatus != NODE_STATUS.BLOCKED)
            {

            
            // Stores the origional value for the calculation
                float originalDiff = diffusions[group.index];
                float originalBaseDiffusion = staticDiffusionBases[group.index];
                //specific goal (only goes straight to)
                float originalSpecificGoalDiffusion = goalTests[group.index];
                
                //reedirection diffusions when avatar is redirected
                float originalRedirDiff = redirectionDiffusion[group.index];
                float originalRedirStaticDiff = redirectionStaticDiffusion[group.index];
                bool isRedirGoal = group.redirectToGoals.Contains(this) && openParking; //is this a open redirection goal

                // For each of the nodes neighbours 

                foreach (EdgeScript edge in edges)
            {
                
                // If they are not a blocked node use thier diffusion value for the equation
                if (edge.GetTo().NodeStatus != NODE_STATUS.BLOCKED && !edge.GetTo().spawner )
                {
                        try
                        {
                            staticDiffusionBases[group.index] += (myManager.diffusionCoeff * (edge.GetTo().staticDiffusionBases[group.index] - originalBaseDiffusion));
                            diffusions[group.index] += (myManager.diffusionCoeff * (edge.GetTo().diffusions[group.index] - originalDiff));

                            goalTests[group.index] += (myManager.diffusionCoeff * (edge.GetTo().goalTests[group.index] - originalSpecificGoalDiffusion));

                            if (!isRedirGoal)
                            {
                                redirectionDiffusion[group.index] += (myManager.diffusionCoeff * (edge.GetTo().redirectionDiffusion[group.index] - originalRedirDiff));
                                redirectionStaticDiffusion[group.index] += (myManager.diffusionCoeff * (edge.GetTo().redirectionStaticDiffusion[group.index] - originalRedirStaticDiff));
                            }


                        }
                        catch (IndexOutOfRangeException) {
                            //doesn't affect anything goes in due to bad referencing and race condition when placing new parking lot
                        }
                }
            }

                staticDiffusionBases[group.index] = staticDiffusionBases[group.index] - (staticDiffusionBases[group.index] * (myManager.decayRate));
                diffusions[group.index] = (diffusions[group.index] - (diffusions[group.index] * (myManager.decayRate)));    // Apply the decay rate to the diffusion

                goalTests[group.index] = (goalTests[group.index] - (goalTests[group.index] * (myManager.decayRate)));

                if (!isRedirGoal)
                {
                    redirectionDiffusion[group.index] = redirectionDiffusion[group.index] - (redirectionDiffusion[group.index] * (myManager.decayRate));
                    redirectionStaticDiffusion[group.index] = (redirectionStaticDiffusion[group.index] - (redirectionStaticDiffusion[group.index] * (myManager.decayRate)));
                }
            }
        // If the end hasn't been allocated only apply the decay rate
        else if (!myManager.GetEndAllocated())
        {
             
                staticDiffusionBases[group.index] = staticDiffusionBases[group.index] - (staticDiffusionBases[group.index] * (myManager.decayRate));
                diffusions[group.index] = (diffusions[group.index] - (diffusions[group.index] * myManager.decayRate));

                goalTests[group.index] = (goalTests[group.index] - (goalTests[group.index] * (myManager.decayRate)));
                redirectionDiffusion[group.index] = redirectionDiffusion[group.index] - (redirectionDiffusion[group.index] * (myManager.decayRate));
                redirectionStaticDiffusion[group.index] = (redirectionStaticDiffusion[group.index] - (redirectionStaticDiffusion[group.index] * (myManager.decayRate)));


            }


            // If the goal node
            if (group.goals.Contains(this))
            {

                // If there is parking available change the value based on the occupancy
                if (openParking)
                {
                    staticDiffusionBases[group.index] = goalDiffusion;
                    diffusions[group.index] = goalDiffusion;
                    NodeStatus = NODE_STATUS.END;

                    if (group.origin == GetComponent<ParkingLot>())
                    {
                        goalTests[group.index] = goalDiffusion;
                    }

                }
                
                // If not open treat the node as a standard node
                else
                {
                    NodeStatus = NODE_STATUS.UNSEARCHED;
                    diffusions[group.index] = 0;
                    staticDiffusionBases[group.index] = 0;
                }

             
            }
            //if if redir goals and actual goals overlap 
            //MAKE ELSE IF IF YOU WANT TO NOT INCLUDE THE GOALS THAT ARE IN THE GOALS LIST
            if (group.redirectToGoals.Contains(this))
            {
                // If there is parking available change the value based on the occupancy
                if (openParking)
                {
                    redirectionDiffusion[group.index] = goalDiffusion;
                    redirectionStaticDiffusion[group.index] = goalDiffusion;
                    NodeStatus = NODE_STATUS.END;
                }

                // If not open treat the node as a standard node
                else
                {
                    NodeStatus = NODE_STATUS.UNSEARCHED;
                    redirectionDiffusion[group.index] = 0;
                    redirectionStaticDiffusion[group.index] = 0;
                }

            }

        }



        //update overall diffusions for redirected goals
        // If there is an end and this node isn't the end and not blocked
        //NONGOALS
        if (myManager.GetEndAllocated() && !goal && NodeStatus != NODE_STATUS.BLOCKED)
        {
            // Stores the origional value for the calculation
            float originalDiff = diffusion;
            float originalBaseDiffusion = staticDiffusionBase;

            float sumDifference = 1f;
            float divne = 0;
            // For each of the nodes neighbours 

            foreach (EdgeScript edge in edges)
            {

                // If they are not a blocked node use thier diffusion value for the equation
                if (edge.GetTo().NodeStatus != NODE_STATUS.BLOCKED && !edge.GetTo().spawner)
                {

                    staticDiffusionBase +=(myManager.diffusionCoeff * (edge.GetTo().staticDiffusionBase - originalBaseDiffusion));
                    diffusion +=(myManager.diffusionCoeff * (edge.GetTo().diffusion - originalDiff));
                    divne +=  (originalBaseDiffusion - edge.GetTo().staticDiffusionBase);
                    sumDifference +=(Math.Abs(edge.GetTo().div - div));
                }
            }
            div = divne;
            staticDiffusionBase = staticDiffusionBase - (staticDiffusionBase * (0.1f));
            
            diffusion = (diffusion + sumDifference*1f)* (1-0.1f);    // Apply the decay rate to the diffusion
            //Debug.Log("diffusion is: "+(diffusion-originalDiff) + "sumdiff: " + sumDifference * 0.6f * (1 - myManager.decayRate));
        }
        // If the end hasn't been allocated only apply the decay rate
        else if (!myManager.GetEndAllocated())
        {
            staticDiffusionBase = staticDiffusionBase - (staticDiffusionBase * (myManager.decayRate));
            diffusion = (diffusion - (diffusion * myManager.decayRate));

        }
        //GOALS
        // If the goal node is in the list of goals give it goal diffusion
         if (goal)
        {

            // If there is parking available change the value based on the occupancy
            if (openParking)
            {
                staticDiffusionBase = goalDiffusion;
                NodeStatus = NODE_STATUS.END;
                diffusion = goalDiffusion;
               
            }
            // If not open treat the node as a standard node
            else
            {
                NodeStatus = NODE_STATUS.UNSEARCHED;
                diffusion = 0;
                staticDiffusionBase = 0;
                    
            }
        }




        // If there's any overlapping avatars
        // update each group outside of loop only once
        if (avatarsOverlapping >= 1)
        {
            overlappedTotalForArea.overlappedTotal += avatarsOverlapping;

            overlappedTotal += avatarsOverlapping;//56 -> 5.6 -> 5.0 -> 0.5 
            timeOfDayE = (float)(GridManagerScript.hours * 1.0f + Math.Round((Math.Floor(GridManagerScript.minutes * 0.1f)*0.1),1)); //get the current time
            floats[timeOfDayE] = floats[timeOfDayE] + avatarsOverlapping;

            avatarsOvelapped[GridManagerScript.hours] = avatarsOvelapped[GridManagerScript.hours] + avatarsOverlapping;

            for (int i = 0; i < myManager.groups.Count; i++)
            {
                PeakingGroup group = groups[i].GetComponent<PeakingGroup>();

                // In not a destination node
                if (group.goals!=null && !group.goals.Contains(this))
                {
                   diffusions[group.index] /= GridManagerScript.evasionStrength * avatarsOverlapping;
                }
            }
            //actual diffusion
            diffusion /= GridManagerScript.evasionStrength * avatarsOverlapping;
        }

    }

   

    //Calculates the Exit diffusion for the node
    void CalculateExitDiffusion()
    {

        // If there is an end and this node isn't the end and not blocked
        if (myManager.GetEndAllocated() && !exit && NodeStatus != NODE_STATUS.BLOCKED && (NodeStatus != NODE_STATUS.END || (NodeStatus == NODE_STATUS.END && openParking) ) )
        {
            float originalDiff = exitDiffusion;    // Stores the origional value for the calculation
            float originalExitDiff = staticExitDiffusion;
            // For each of the nodes neighbours
            foreach (EdgeScript edge in edges)
            {
                // If they are not a blocked node use thier exit diffusion value for the equation
                if (edge.GetTo().NodeStatus != NODE_STATUS.BLOCKED)
                {
                    exitDiffusion += (myManager.diffusionCoeff * (edge.GetTo().exitDiffusion - originalDiff));
                    staticExitDiffusion += (myManager.diffusionCoeff * (edge.GetTo().staticExitDiffusion - originalExitDiff));
                }
            }

           staticExitDiffusion = (staticExitDiffusion - (staticExitDiffusion * myManager.decayRate));    
           exitDiffusion = (exitDiffusion - (exitDiffusion * myManager.decayRate));    // Apply the decay rate to the exit diffusion
        }

        // If the end hasn't been allocated only apply the decay rate
        else if (!myManager.GetEndAllocated())
        {
            staticExitDiffusion = (staticExitDiffusion - (staticExitDiffusion * myManager.decayRate));
            exitDiffusion = (exitDiffusion - (exitDiffusion * myManager.decayRate));
        }


        // If there's any overlapping avatars
        if (avatarsOverlapping >= 1 && !exit)
        {
            exitDiffusion /= GridManagerScript.evasionStrength * avatarsOverlapping;
        }

    }

    // Sets the node as blocked if an object comes in contact with it
    //probably not what you want to do
    private void OnTriggerEnter(Collider other)
    {
        
        //NodeStatus = NODE_STATUS.BLOCKED;
        //diffusion = 0;
    }

    // Sets the node as unsearched once no longer blocked
    private void OnTriggerExit(Collider other)
    {
       // NodeStatus = NODE_STATUS.UNSEARCHED;
    }

    // Changes the state of the node
    public void ChangeState(NODE_STATUS newState)
    {
        if (NodeStatus != newState)
        {
            NodeStatus = newState;
            GetComponent<MeshRenderer>().material = materials[(int)NodeStatus];
            if (newState == NODE_STATUS.BLOCKED)
            {
                diffusion = 0;
                staticDiffusionBase = 0;
            }
        }
    }



    // Sets the node as blocked if highlighted and the space bar is pressed on 
    // sets start with left click and end with right click
    void OnMouseOver()
    {

        if (myManager.autoGenerateGrid)
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {

                myManager.SetBlocker(this);
            }
            else if (NodeStatus != NODE_STATUS.BLOCKED)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    myManager.SetStart(this);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    myManager.SetEnd(this);
                    diffusion = goalDiffusion;
                    staticDiffusionBase = goalDiffusion;
                }

            }
        }
        // Sets the node as blocked if highlighted and the space bar is pressed on that frame
        else
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                myManager.SetBlocker(this);
                forceCloseParking = !forceCloseParking;
                openParking = !forceCloseParking;
                GetComponent<NodeScript>().goal = !forceCloseParking;
                //GetComponent<NodeScript>().goalDiffusion = open ? 1000000 : 0;
                GetComponent<NodeScript>().NodeStatus = !forceCloseParking ? NodeScript.NODE_STATUS.END : NodeScript.NODE_STATUS.UNSEARCHED;
            }
        }

        highlighted = true;
    }

    public bool selected = false;
    // Unhighlight node 
    void OnMouseExit()
    {
        if (!selected)
        {
            UnHighlight();
        }
    }
    public void UnHighlight()
    {
        highlighted = false;
    }

    private void OnMouseDown()
    {
        if (selected)
        {
            selected = false;
            UnHighlight();
        }

        else
        {
            myManager.UnselectAll();
            selected = true;
        }

    }

    // Set the grid manager
    public void SetManager(GridManagerScript newManager)
    {
        myManager = newManager;
    }

    // Return the diffusion value
    public float GetDiffusion()
    {
        return diffusion;
    }

    // Set diffusion value
    public void SetDiffusion(int newDiffusion)
    {
        diffusion = newDiffusion;
    }

    // Generate edges for auto grid generation
    public void GenerateEdges()
    {
        Collider[] neighbourNodes = Physics.OverlapBox(transform.position, transform.localScale);

        foreach (Collider nodeCollider in neighbourNodes)
        {
            NodeScript node = nodeCollider.gameObject.GetComponent<NodeScript>();

            if (node != null && node != this)
            {
                EdgeScript edge = new EdgeScript(this,node);
                edge.SetCost((node.transform.position - this.transform.position).magnitude);

                edges.Add(edge);
            }
        }
    }


    // Uses the neighbours given to the node to generate edges
    public void ManualEdgeCreation()
    {
        foreach (GameObject node in neighbours)
        {
            NodeScript neighbourNode = node.GetComponent<NodeScript>();
                  
            //don't add edge if already exists
            if (node != null && node != this && this.FindEdgeTo(neighbourNode) == null)
            {
             
                EdgeScript edge = new EdgeScript(this,neighbourNode);
                edge.SetCost((node.transform.position - this.transform.position).magnitude);
               
                edges.Add(edge);
               
            }
            
        }
      
    }

    // Attempts to find an edge with the parameter node as the to part of the edge
    public EdgeScript FindEdgeTo(NodeScript node)
    {
        foreach (EdgeScript edge in edges)
        {
            if (edge.GetTo() == node)
            {
                return edge;
            }
        }

        return null;
    }

    // Returns highlighted
    public bool GetHighlighted()
    {
        return highlighted;
    }
}
