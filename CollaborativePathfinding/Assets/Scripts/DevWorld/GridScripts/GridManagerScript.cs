using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;   
using UnityEditor;


// Path struct used to store information on generasted paths
public struct Path
{
    public List<NodeScript> pathNodes;        // The nodes in the path
    public List<NodeScript> searchedNodes;    // The nodes that were searched
    public NodeScript start;                  // The start
    public NodeScript end;                    // The end
    public bool startAllocated;               // If the start is allocated
    public bool endAllocated;                 // If the end is allocated
    public bool startEndReallocated;          // If the start or end has changed
    public float pathTime;                    // Time taken to create the path
    public float pathDist;                    // The distance of the path
    public AvatarScript avatar;               // Avatar assigned to the path
};

public class GridManagerScript : MonoBehaviour
{

    //chance of spawning a parking space between two nodes
    public float streetParkingFrequency = 0.1f;
    public static bool enableCollisions = true;
    public static bool accelerationCalculations = false;

    public static AStarGenerator AStar;
    public static bool usingAStarOnly = true; //uses a star pathfinding instead of diffusion
    public static bool usingAugmentedDiffusion = false; //when using just the distance the frame rate drops a ton

    public static bool isTest = false;

    public Heatmap heatmap;
    public bool showHeatmap;
    public List<GameObject> groups;
    public GameObject GroupPrefab;
    public GameObject GroupHolder;
    public GameObject ParkingPrefab;
    public GameObject ParkingLots;
    public GameObject ParkingLotDisplayPannel;
    public static ParkingLotManager ParkingLotManager;
    public int raduisGroup = 20;
    public int redirectionRadius = 800; //redirect to a parking lot that is atleast 100 away
    public UnityEngine.UI.InputField highwaySpeedInput;
    public UnityEngine.UI.InputField citySpeedInput;

    public float radSphereNeighboursParkingPlacement = 10f;
    //spawning timers 

    public static GameObject clock; //display time
    public static int hours; //handle time and handle injection distributions at different times
    public static int minutes;

    public static int secondsPerSecond = 1;
    public static double totalSecondsPassed = 0;
    public static int maxSecondsPerSecond = 200;
    public static float citySpeed = 30f;
    public static float highwaySpeed = 40f;

    //there always must be atleast one map section
    public static MapNodeGroup[] mapSections;


    public bool autoGenerateGrid;                       // Should the grid be automatically genetated
    public int pathNumber = 0;                          // The current path index

    public static float evasionStrength = 1f;                       // The strength of the evasion factor


    public Vector2 floorDimentions;                     // Floor size in actual length
    public Vector2Int gridSize;                         // Grid size in nodes
    public Material floorMaterial;                      // Material for the floor
    public Material enteringDiffusionAvatarMaterial;    // Material for the entering avatars
    public Material exitingDiffusionAvatarMaterial;     // Material for leaving avatars

    public GameObject node;                             // The node object for grid generation       

    public bool useDijkstra = false;                    // Dijkstra pathing enabled/disabled
    public bool pureHeuristic = false;                  // Heuristic search enabled/disabled

    public GameObject[] nodeGrid;                       // The node grid as a one dimentional array

    public bool lastUpdatedEntering = false;            // Used for update switching on avatars

    public static float diffusionAvatarSize = 0.500f;                   // Scale for diffusion avatars
    public static float avatarToCar = 0.05f; //what percentage of an avatar represents a car
                                             //1% car to avatar means 1 avatar = 1/100 car
                                             //if we have 12 cars for a given time slot and 0.20 avatar to car representation
                                             //20 cars are represented by an avatar
                                             // x*20 = 1 - x=0.05
                                             // x*30 = 1 -- x = 1/30
                                             //5% of an avatar is a car 1 avatar = 20 cars
                                             //10%  1 av = 10 cars
                                             // 1/40 runs stable for both normal and low
                                             // 1/20 stable for low
                                             // 0.075*x = 13
                                             //200 0.1, cap = 200/10 = 20 
                                             // 1/10 stable for low

    public bool scaleParkingToAvatar = false;


    //currently used only in injection


    public float diffusionCoeff;                        // How quickly the diffusion values propagates through nodes
    public float decayRate;                             // How quickly the diffusion values are removed from nodes

    public bool gridCreated = false;                    // If the grid exists yet

    private bool showDiffusion = true;                  // If the diffusion should be displayed on nodes

    public List<DiffusionAvatarScript> avatars = new List<DiffusionAvatarScript>();         // All diffusion avatars 
                                                                                            // public DiffusionAvatarScript[] avatars123123;         // All diffusion avatars 

    public bool exitAvailable;                          // If exits are active in the simulation

    public bool avatarsPaused;                          // If the avatars are paused

    Path[] paths = new Path[9];                         // The path array for storing path data

    public Material avatarMaterial;                     // Material for avatars

    public float diffusionAvatarSpawnRate;              // Frequency of avatar creation at spawner nodes

    public bool diffusionMode;                          // If diffusion simulation is active
    public NodeScript diffusionEnd;                     // The diffusion end goal

    public int avatarsEntering;                         // Number of avatars entering
    public int avatarsExiting;                          // Number of avatars are exiting
    public static Dictionary<double, int> numberOfVehicles = new Dictionary<double, int>();


    // The heuristics available and the active heuristic used for pathfinding
    public enum Heuristic { PYTHAGORAS = 0, MANHATTAN = 1, COLLABORATIVEDIFF = 2, NUMBER_OF_HEURISTICS = 3 };
    public Heuristic heuristic = Heuristic.PYTHAGORAS;


#if UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void DownloadFile(byte[] array, int byteLength, string fileName);
        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
#endif

    //a list of the spawner nodes
    public static List<NodeScript> spawners = new List<NodeScript>();

    // The three heuristics as lambdas 
    // Manhattan distance
    Func<NodeScript, NodeScript, float> manhattan = (currNode, end) =>
    {
        return (Math.Abs(end.transform.position.x - currNode.transform.position.x)) + (Math.Abs(end.transform.position.z - currNode.transform.position.z));
    };

    // Pythagoras distance
    Func<NodeScript, NodeScript, float> pyDist = (currNode, end) =>
    {

        return (float)Math.Sqrt(((end.transform.position.x - currNode.transform.position.x) * (end.transform.position.x - currNode.transform.position.x))
                              + ((end.transform.position.z - currNode.transform.position.z) * (end.transform.position.z - currNode.transform.position.z)));
    };

    // Collaborative diffusion heuristic
    Func<NodeScript, NodeScript, float> CollaborativeDiffusuion = (currNode, end) =>
    {
        return 1 - (float)currNode.GetDiffusion();
    };

    public void changeCitySpeed(string speed)
    {
        try
        {
            citySpeed = float.Parse(speed);
        }
        catch (Exception e)
        {
            citySpeed = 0.0f;
            Debug.Log(e);
        }
    }


    public void changeHighwaySpeed(string speed)
    {
        try
        {
            highwaySpeed = float.Parse(speed);
        }
        catch (Exception e)
        {
            highwaySpeed = 0.8f;
            Debug.Log(e);
        }
    }


    // Use this for initialization
    void Start()
    {
        mapSections =(MapNodeGroup[]) FindObjectsOfType(typeof(MapNodeGroup));
        heatmap.gameObject.SetActive(showHeatmap);
        ParkingLotManager = ParkingLots.GetComponent<ParkingLotManager>();
        raduisGroup = 20;
        if (autoGenerateGrid) isTest = true;
        //find clock 
        clock = GameObject.Find("Clock");


        if (highwaySpeedInput)
            highwaySpeedInput.text = highwaySpeed.ToString();
        if (citySpeedInput)
            citySpeedInput.text = citySpeed.ToString();
        //start day at 05.00
        //totalSecondsPassed += 18000;



        int count = 0;
        List<GameObject> toAdd = new List<GameObject>();
        //create street parkings
        foreach (GameObject n in nodeGrid)
        {
            try
            {
                NodeScript test = n.GetComponent<NodeScript>();
            }
            catch (Exception) { continue; }


            NodeScript node = n.GetComponent<NodeScript>();

            if (node.spawner)
                spawners.Add(node);

            if (node.hasCreatedStreetParking || node.transform.parent.name == "Highway"
                || streetParkingFrequency < UnityEngine.Random.Range(0.0f, 1.0f)) continue;
            //go to node and make edges
            foreach (var nb in node.neighbours)
            {
                NodeScript neighbour = nb.GetComponent<NodeScript>();

                if (neighbour.hasCreatedStreetParking || neighbour.transform.parent.name == "Highway") continue;

                neighbour.hasCreatedStreetParking = true;
                ParkingLotManager parkingManager = ParkingLots.GetComponent<ParkingLotManager>();

                GameObject parking = Instantiate(ParkingPrefab, ParkingLots.transform, false);

                parking.tag = "parkingLot";
                parking.name = "streetParking" + toAdd.Count;
                parking.GetComponent<ParkingLot>().myManager = parkingManager;
                parking.GetComponent<NodeScript>().myManager = this;
                //in the middle between the nodes
                parking.transform.position = (node.transform.position + neighbour.transform.position) / 2;

                parking.transform.localScale = parking.transform.localScale * 0.1f;
                //add to parking manager
                Array.Resize(ref parkingManager.parkingLots, parkingManager.parkingLots.Length + 1);
                parkingManager.parkingLots[parkingManager.parkingLots.Length - 1] = parking;
                //add to grid
                toAdd.Add(parking);

                //add to pannel
                ParkingDisplayPanel displayPannel = ParkingLotDisplayPannel.GetComponent<ParkingDisplayPanel>();
                Array.Resize(ref displayPannel.parkingLots, displayPannel.parkingLots.Length + 1);
                displayPannel.parkingLots[displayPannel.parkingLots.Length - 1] = parking.GetComponent<NodeScript>();


                parking.GetComponent<NodeScript>().neighbours = new GameObject[] { node.gameObject, nb.gameObject };



                //set capacity
                parking.GetComponent<ParkingLot>().capacity = (int)(Vector3.Distance(node.transform.position, neighbour.transform.position)) / 2;
            }
            node.hasCreatedStreetParking = true;

        }

        Array.Resize(ref nodeGrid, nodeGrid.Length + toAdd.Count);

        int ind = toAdd.Count - 1;

        for (int i = nodeGrid.Length - 1; i >= nodeGrid.Length - toAdd.Count; i--)
        {
            NodeScript parking = toAdd[ind].GetComponent<NodeScript>();
            //get index of node in neighbour
            int ind1 = Array.IndexOf(parking.neighbours[0].GetComponent<NodeScript>().neighbours, parking.neighbours[1]);

            //get index of neighbour in node
            int ind2 = Array.IndexOf(parking.neighbours[1].GetComponent<NodeScript>().neighbours, parking.neighbours[0]);

            parking.neighbours[0].GetComponent<NodeScript>().neighbours[ind1] = parking.gameObject;

            if (ind2 != -1)
            {
                parking.neighbours[1].GetComponent<NodeScript>().neighbours[ind2] = parking.gameObject;
            }


            //switch them
            nodeGrid[i] = toAdd[ind];
            ind--;
        }



        // Sets up the default values for the nine paths
        for (int i = 0; i < 9; ++i)
        {
            paths[i].startAllocated = false;
            paths[i].endAllocated = false;
            paths[i].startEndReallocated = false;
            paths[i].pathTime = 0;
            paths[i].pathDist = 0;
            paths[i].pathNodes = new List<NodeScript>();
            paths[i].searchedNodes = new List<NodeScript>();
        }

        //Generates the grid
        if (autoGenerateGrid)
        {
            // Creates the floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0, 0, 0);
            floor.transform.localScale = new Vector3(floorDimentions.x + 1.5f, 0.5f, floorDimentions.y + 1.5f);
            floor.GetComponent<MeshRenderer>().material = floorMaterial;
            floor.name = "Floor";
            floor.layer = 8;
            floor.AddComponent<Rigidbody>();
            floor.GetComponent<Rigidbody>().useGravity = false;
            floor.GetComponent<Rigidbody>().isKinematic = true;
            floor.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;

            Renderer rend = floor.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = floorMaterial;
            }

            // Creates the node grid
            nodeGrid = new GameObject[gridSize.x * gridSize.y];

            // Sets the position and size of the nodes in the grid
            for (int i = 0; i < gridSize.x; ++i)
            {
                for (int j = 0; j < gridSize.y; ++j)
                {
                    nodeGrid[i + (j * gridSize.x)] = UnityEngine.Object.Instantiate(node);
                    nodeGrid[i + (j * gridSize.x)].transform.position = new Vector3(((floorDimentions.x / gridSize.x) * i) - floorDimentions.x / 2 + ((floorDimentions.x / gridSize.x) / 2), 0.25f,
                                                                    ((floorDimentions.y / gridSize.y) * j) - floorDimentions.y / 2 + ((floorDimentions.y / gridSize.y) / 2));

                    nodeGrid[i + (j * gridSize.x)].GetComponent<Transform>().localScale = transform.localScale = new Vector3((floorDimentions.x / gridSize.x), 0.5f, (floorDimentions.y / gridSize.y));
                    nodeGrid[i + (j * gridSize.x)].GetComponent<NodeScript>().SetManager(this);
                }
            }

            // Creates the edges for the nodes
            for (int i = 0; i < gridSize.x; ++i)
            {
                for (int j = 0; j < gridSize.y; ++j)
                {
                    nodeGrid[i + (j * gridSize.x)].GetComponent<NodeScript>().GenerateEdges();
                }
            }
        }
        else
        {
            // Check nodes have been placed for use
            if (nodeGrid.Length > 0)
            {



                // Loop through all of the nodes and start the manual edge generation and if its a goal set it as the end
                foreach (GameObject node in nodeGrid)
                {
                    if (node != null)
                    {



                        try
                        {
                            node.GetComponent<NodeScript>().ManualEdgeCreation();

                            if (node.GetComponent<NodeScript>().goal)
                            {
                                diffusionEnd = node.GetComponent<NodeScript>();
                            }
                        }

                        catch (Exception e)
                        {
                            Debug.Log(node.name + node.transform.parent.name + e);
                        }
                        count++;



                    }
                }



            }



        }



        avatars = new List<DiffusionAvatarScript>();
        avatarsPaused = false;
        gridCreated = true;

        //test
        //GenerateHeatMapGroups(9);
        AStar = new AStarGenerator(nodeGrid);

        for (int i = 0; i < 24; i++)
        {
            for (int b = 0; b < 60; b++)
            {
                numberOfVehicles.Add(System.Math.Round(i * 1.0 + b / 100.0, 2), 0);
            }
        }

        //scale parking spaces
        if (scaleParkingToAvatar) { 
            int reserve = 0;
            foreach (var parking in ParkingLotManager.parkingLots)
            {
                float actualSpace = parking.GetComponent<ParkingLot>().capacity /  (1/avatarToCar);
                if (actualSpace < 1) {
                    reserve += 1;
                } else if (!parking.name.Contains("streetParking")) {
                    actualSpace += reserve;
                    reserve = 0;
                }
                parking.GetComponent<ParkingLot>().capacity = (int)Math.Round(actualSpace);
            
            }
        }
    }

    class Point
    {
        public float x;
        public float z;

    }

    public HeatGroup[] GenerateHeatMapGroups()
    {
        float rad = diffusionAvatarSize;

        HeatGroup[] heatGroups = nodeGrid.Where(x => x.GetComponent<NodeScript>() != null)
        .Select(x =>
            new HeatGroup(x.GetComponent<NodeScript>(), x.transform.position, rad,x.GetComponent<NodeScript>().overlappedTotalForArea))
        .ToArray();

        return heatGroups;
    }

    private bool withinBounds(float x, float z, float ax, float az, float bx, float bz, float cx, float cz,
    float dx, float dz)
    {

        return x >= ax && z <= az
            && x <= bx && z <= bz
            && x <= cx && z >= cz
            && x >= dx && z >= dz;
    }
    public HeatGroup[] GenerateHeatMapGroups(int numberOfGroups)
    {
        //group on x and z
        //ideas to generate n groups of nodes
        //simple: seperate map on n number of areas and include nodes there
        //smart: group neighbours and neighbours of neighbours until you reach certain value 

        //simple
        float minX = -200;
        float maxX = 200;
        float minZ = -200;
        float maxZ = 200;

        float Xabs = System.Math.Abs(minX) + System.Math.Abs(maxX);
        float Zabs = System.Math.Abs(minZ) + System.Math.Abs(maxZ);

        //make a box and add all node within that box
        float side = (float)(Xabs / System.Math.Sqrt(numberOfGroups));

        //auxil. functions to keep code clean
        //format from 0 to Xabs to x
        Func<float, float> X = (x) => x - System.Math.Abs(System.Math.Min(minX, maxX));
        Func<float, float> Xrev = (x) => x + System.Math.Abs(System.Math.Min(minX, maxX));
        //0 -> min (-200)
        //200 -> mid (0)
        //400 -> max (200)
        //-200

        Func<float, float> Z = (z) => System.Math.Max(minZ, maxZ) - z;
        Func<float, float> Zrev = (z) => System.Math.Max(minZ, maxZ) - z;

        //0 -> max (200)
        //200 -> mid (0)
        //400 -> min (-200)

        Func<Point, string> strr = (p) => "x: " + X(p.x) + ", z: " + Z(p.z);

        //get nodes
        List<NodeScript> nodes = nodeGrid.Select(x => x.GetComponent<NodeScript>()).ToList();

        //setup HeatGroups
        HeatGroup[] heatGroups = new HeatGroup[numberOfGroups];

        Point pointA = new Point() { x = 0, z = 0 };
        Point pointB;
        Point pointC;
        Point pointD;
        //  A---B
        //  |   |
        //  D---C
        //move box starting from top left corner (minX maxZ)
        //for every box..
        for (int xheatG = 0; xheatG < numberOfGroups; xheatG++)
        {
            pointB = new Point() { x = pointA.x + side, z = pointA.z };
            pointC = new Point() { x = pointB.x, z = pointB.z + side };
            pointD = new Point() { x = pointA.x, z = pointC.z };


            // add all within bounds and set
            // Debug.Log("PointA: " + strr(pointA)+ ", PointB: " + strr(pointB)
            //     + ", PointC: " + strr(pointC) + ", PointD: " + strr(pointD));
            NodeScript[] nodesInArea = nodeGrid.Where(x =>
                withinBounds(x.GetComponent<NodeScript>().transform.position.x, x.GetComponent<NodeScript>().transform.position.z,
                X(pointA.x), Z(pointA.z), X(pointB.x), Z(pointB.z), X(pointC.x), Z(pointC.z), X(pointD.x), Z(pointD.z)
                )).Select(x => x.GetComponent<NodeScript>())
                    .ToArray();
            float ln = nodesInArea.Length == 0? 1f : nodesInArea.Length*1f;
            float midXpos = nodesInArea.Sum(x => x.transform.position.x) / ln;
            float midZpos = nodesInArea.Sum(x => x.transform.position.z) / ln;
            //find mid point
            Vector3 midPoint = new Vector3(midXpos, 0, midZpos);

            //find which section has most of the nodes in the area
            //escape null errors
            MapNodeGroup area = nodesInArea.Select(x=>x.overlappedTotalForArea).Distinct()
                .Aggregate(mapSections[0],
                (best,next)=> best =
                    (nodesInArea.Where(x=>x.overlappedTotalForArea==next).Count() >
                    nodesInArea.Where(x=>x.overlappedTotalForArea==best).Count() )?
                  next : best);
            heatGroups[xheatG] = new HeatGroup(nodesInArea, midPoint, side,area);


            //update points 
            //loop for the width and then go to the lower row
            if (pointB.x + side > Xabs)
                pointA = new Point() { x = 0, z = pointA.z + side };
            else
                pointA = new Point() { x = pointB.x, z = pointB.z };


        }


        //smart
        return heatGroups;
        //generate groups
        //then use group script for updates to the heat map
    }





    // Update is called once per frame
    void Update()
    {

        // Deal with any user input
        HandleInput();

        //move time
        if (clock)
            HandleClock();

        // Update either the paths or the diffusion avatars
        if (!diffusionMode)
        {
            PathUpdate();
        }
        else
        {
            DiffusionUpdate();
        }


        // Go through each node and update them
        // Must occur after PathUpdate/DiffusionUpdate
        foreach (GameObject node in nodeGrid)
        {
            if (node)
            {
                node.GetComponent<NodeScript>().NodeUpdate();
            }
        }

    }

    public void saveGroups()
    {
        string toSave = "";
        for (int i = 0; i < groups.Count; i++)
        {
            toSave += groups[i].GetComponent<PeakingGroup>().toString() + "\n";
        }

        saveFile(toSave,"groupSettings.txt");
    }

#if UNITY_WEBGL
    public void loadGrouos()
    {
        UploadFile(gameObject.name, "OnFileUpload", ".txt", false);
    }
#endif
#if UNITY_STANDALONE
    public void loadGrouos(){
        string filepath = EditorUtility.OpenFilePanel("Load Saved Settings","","txt");
        StreamReader reader = new StreamReader(filepath); 
        string settings = reader.ReadToEnd();
        reader.Close();
        parseAndCreateGroups(settings);
    }
#endif
    public void OnFileUpload(string url)
    {
        StartCoroutine(OutputRoutine(url));
    }
    private IEnumerator OutputRoutine(string url)
    {
        var loader = new WWW(url);
        yield return loader;
        parseAndCreateGroups(loader.text);
    }


    /*
    return "" + groupName + ";;" + rushHour + ";;" +avrgStayTime +";;"+
                standartDeviation +";;"+spawnNumber +";;" + reservedSpawnRate +";;"+signRedirectionChance
                 + ";;" + origin.parkingLotName +";;"+ redirectOrigin.parkingLotName;
    */
    private void parseAndCreateGroups(string input)
    {
        input = input.Remove(input.Length - 1, 1);
        string[] groupsSettings = input.Split('\n');
        foreach (var settingsStr in groupsSettings)
        {
            string[] settings = settingsStr.Split(new string[] { ";;" }, StringSplitOptions.None);
            string groupName = settings[0];
            float rushHour = float.Parse(settings[1]);
            int avrgStayTime = int.Parse(settings[2]);
            float std = float.Parse(settings[3]);
            int spawnN = int.Parse(settings[4]);
            float reservedRate = float.Parse(settings[5]);
            float signRedirect = float.Parse(settings[6]);
            string redirOrigin = settings[7];
            string goalOrigin = settings[8];

            CreateGroup(true, groupName, rushHour, avrgStayTime, std, spawnN, reservedRate, signRedirect
            , redirOrigin, goalOrigin);
        }

    }


    public void CreateGroupBtn()
    {
        CreateGroup(false, "", 0, 0, 0, 0, 0, 0, "", "");
    }
    public void CreateGroup(bool fromSave,
    string groupName, float rushHour, int avrgStayTime, float std, int spawnN,
    float reservedRate, float signRedirect, string redirOrigin, string goalOrigin
    )
    {
        //add to scene
        GameObject group = Instantiate(GroupPrefab);
        group.transform.SetParent(GroupHolder.transform, false);
        NodeScript savedGoalOrigin = null;
        NodeScript savedRedirOrigin = null;
        bool isNone = goalOrigin == "none";
        if (fromSave)
        {
            group.GetComponent<PeakingGroup>().groupName = groupName;
            group.GetComponent<PeakingGroup>().rushHour = rushHour;
            group.GetComponent<PeakingGroup>().avrgStayTime = avrgStayTime;
            group.GetComponent<PeakingGroup>().standartDeviation = std;
            group.GetComponent<PeakingGroup>().spawnNumber = spawnN;
            group.GetComponent<PeakingGroup>().reservedSpawnRate = reservedRate;
            group.GetComponent<PeakingGroup>().signRedirectionChance = signRedirect;
            GameObject sg = ParkingLotManager.parkingLots.Where(x => x.GetComponent<ParkingLot>().parkingLotName == goalOrigin).Count() > 0 ?
                ParkingLotManager.parkingLots.Where(x => x.GetComponent<ParkingLot>().parkingLotName == goalOrigin).First() : null;
            savedGoalOrigin = sg == null ? ParkingLotManager.parkingLots[0].GetComponent<NodeScript>() : sg.GetComponent<NodeScript>();
            GameObject sr = ParkingLotManager.parkingLots.Where(x => x.GetComponent<ParkingLot>().parkingLotName == redirOrigin).Count() > 0 ?
                ParkingLotManager.parkingLots.Where(x => x.GetComponent<ParkingLot>().parkingLotName == redirOrigin).First() : null;
            savedRedirOrigin = sr == null ? null : sr.GetComponent<NodeScript>();

        }

        //get remove button and set remove method 
        group.transform.GetChild(0).transform.Find("RemoveGroup").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => RemoveGroup(group));
        group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options =
            ParkingLotManager.parkingLots.Where(x => !x.name.Contains("streetParking")).Select(x => new UnityEngine.UI.Dropdown.OptionData(x.GetComponent<ParkingLot>().parkingLotName)).ToList<UnityEngine.UI.Dropdown.OptionData>();
        group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options.Add(new UnityEngine.UI.Dropdown.OptionData("none"));
        //get the default goal
        NodeScript goalO = fromSave ? savedGoalOrigin
            : ParkingLotManager.parkingLots[0].GetComponent<NodeScript>();

        //setDefault in menu 
        group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().value =
         Array.IndexOf(ParkingLotManager.parkingLots, goalO.gameObject);

        //get the nearest goals and set them to the goals of the group
        List<NodeScript> nearO = new List<NodeScript>();
        nearO.AddRange(Physics.OverlapSphere(goalO.transform.position, raduisGroup).Where(x => x.tag == "parkingLot").Select(x => x.gameObject.GetComponent<NodeScript>()));
        group.GetComponent<PeakingGroup>().originalRadius = raduisGroup;
        //calculate node that is furthest away in redirection range
        List<NodeScript> redirectedG = Physics.OverlapSphere(goalO.transform.position, redirectionRadius).Where(x => x.tag == "parkingLot" && !x.name.Contains("streetParking")).Select(x => x.gameObject.GetComponent<NodeScript>()).
            OrderBy(x => Vector3.Distance(x.transform.position, goalO.transform.position)).ToList();
        //take last two parking spaces
        redirectedG = redirectedG.Skip(Math.Max(0, redirectedG.Count() - 2)).ToList();

        List<UnityEngine.UI.Dropdown.OptionData> opt = ParkingLotManager.parkingLots.Where(x => !x.name.Contains("streetParking")).Select(x => new UnityEngine.UI.Dropdown.OptionData(x.GetComponent<ParkingLot>().parkingLotName)).ToList<UnityEngine.UI.Dropdown.OptionData>();
        //setup redirection list, don't include current goal
        group.transform.GetChild(0).transform.Find("RedirectionGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options = opt;

        //set default elem
        group.transform.GetChild(0).transform.Find("RedirectionGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().value = opt.IndexOf(opt.Find(x => x.text == redirectedG.First().GetComponent<ParkingLot>().parkingLotName));

        group.GetComponent<PeakingGroup>().setUpGoals(nearO, redirectedG);
        group.transform.GetChild(0).transform.Find("RedirectionSignButton").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => placeRedirectionSign(group.GetComponent<PeakingGroup>()));

        if (isNone)
        {
            group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().value = ParkingLotManager.parkingLots.Length; // set to last
            group.GetComponent<PeakingGroup>().goals = null;
            group.GetComponent<PeakingGroup>().redirectToGoals = null;
            group.GetComponent<PeakingGroup>().origin = null;
            group.GetComponent<PeakingGroup>().lastExpanded = null;
            group.GetComponent<PeakingGroup>().expanded = false;
            group.GetComponent<PeakingGroup>().expandedRedirect = false;
            group.GetComponent<PeakingGroup>().redirectOrigin = null;
            group.GetComponent<PeakingGroup>().lastExpandedRedirect = null;
        }
        if (fromSave && savedRedirOrigin!=null)
        {
            group.transform.GetChild(0).transform.Find("RedirectionGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().value = opt.IndexOf(opt.Find(x => x.text == redirOrigin));
            List<NodeScript> nr = new List<NodeScript>();
            nr.Add(savedRedirOrigin);
            group.GetComponent<PeakingGroup>().resetRedirect(nr);
        }

        group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().onValueChanged.AddListener(i =>
        {

            if (group.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options[i].text == "none")
            {
                group.GetComponent<PeakingGroup>().goals = null;
                group.GetComponent<PeakingGroup>().redirectToGoals = null;
                group.GetComponent<PeakingGroup>().origin = null;
                group.GetComponent<PeakingGroup>().lastExpanded = null;
                group.GetComponent<PeakingGroup>().expanded = false;
                group.GetComponent<PeakingGroup>().expandedRedirect = false;
                group.GetComponent<PeakingGroup>().redirectOrigin = null;
                group.GetComponent<PeakingGroup>().lastExpandedRedirect = null;

            }
            else
            {
                //get the goal
                NodeScript goal = ParkingLotManager.parkingLots[i].GetComponent<NodeScript>();
                //get the nearest goals and set them to the goals of the group
                List<NodeScript> near = new List<NodeScript>();
                near.AddRange(Physics.OverlapSphere(goal.transform.position, raduisGroup).Where(x => x.tag == "parkingLot").Select(x => x.gameObject.GetComponent<NodeScript>()));
                //calc furthest away
                List<NodeScript> redirectedGoals = Physics.OverlapSphere(goalO.transform.position, redirectionRadius).Where(x => x.tag == "parkingLot" && !x.name.Contains("streetParking")).Select(x => x.gameObject.GetComponent<NodeScript>()).
                    OrderBy(x => Vector3.Distance(x.transform.position, goal.transform.position)).ToList();
                List<UnityEngine.UI.Dropdown.OptionData> optG = ParkingLotManager.parkingLots.Where(x => !x.name.Contains("streetParking")).Select(x => new UnityEngine.UI.Dropdown.OptionData(x.GetComponent<ParkingLot>().parkingLotName)).ToList<UnityEngine.UI.Dropdown.OptionData>();
                //take last two parking spaces
                redirectedGoals = redirectedGoals.Skip(Math.Max(0, redirectedG.Count() - 2)).ToList();
                //this burns space? 
                group.transform.GetChild(0).transform.Find("RedirectionGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().value = optG.IndexOf(optG.Find(x => x.text == redirectedGoals.First().GetComponent<ParkingLot>().parkingLotName));

                group.GetComponent<PeakingGroup>().setUpGoals(near, redirectedGoals);

            }
        });

        group.transform.GetChild(0).transform.Find("RedirectionGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().onValueChanged.AddListener(i =>
        {
            NodeScript goal = ParkingLotManager.parkingLots[i].GetComponent<NodeScript>();
            List<NodeScript> newRedir = new List<NodeScript>();
            newRedir.Add(goal);
            group.GetComponent<PeakingGroup>().resetRedirect(newRedir);
        });

        
        //add to list 
        groups.Add(group);
        group.GetComponent<PeakingGroup>().setupSpawnersDistribution();

    }



    public void RemoveGroup(GameObject group)
    {
        PeakingGroup gp = group.GetComponent<PeakingGroup>();
        gp.remove();
        //remove from scene
        Destroy(group);

        //remove group from list
        groups.Remove(group);


    }

    public static double getTimeOfDay()
    {
        return System.Math.Round(GridManagerScript.hours*1.0 + GridManagerScript.minutes/ 100.0,2);
    }
    static void HandleClock()
    {
        //simulate time passed
        //actual seconds per second of game
        totalSecondsPassed += (secondsPerSecond * Time.deltaTime);
        //get minutes
        minutes = (int)(totalSecondsPassed / 60);
        //get hours and restrict
        hours = (minutes / 60) % 24;
        //restrict minutes
        minutes = minutes % 60;

        int seconds = (int)totalSecondsPassed % 60;
        //format time
        string h = hours / 10 >= 1 ? "" + hours : "0" + hours;
        string m = minutes / 10 >= 1 ? "" + minutes : "0" + minutes;
        string s = seconds / 10 >= 1 ? "" + seconds : "0" + seconds;

        //print to clock
        clock.GetComponent<UnityEngine.UI.Text>().text = h + ":" + m + ":" + s;

    }

    // Updates the paths
    void PathUpdate()
    {
        // Makes sure the start or end hasn't been blocked
        CheckForBlockedStartOrEnd();

        // Make sure that display all paths isn't on
        if (pathNumber != 9)
        {
            // If there is a start and end node and they need reallocation generate a path and assign the avatar to the start
            if (paths[pathNumber].startAllocated && paths[pathNumber].endAllocated && paths[pathNumber].startEndReallocated)
            {
                GeneratePath();
                paths[pathNumber].startEndReallocated = false;

                if (paths[pathNumber].avatar != null)
                {
                    paths[pathNumber].avatar.SetPosition(paths[pathNumber].start.transform.position + new Vector3(0, 0.5f, 0));
                    paths[pathNumber].avatar.SetPath(paths[pathNumber]);
                }
            }

            // If either start or end isn't allocated reset the time to zero
            if (!paths[pathNumber].startAllocated || !paths[pathNumber].endAllocated)
            {
                paths[pathNumber].pathTime = 0;
            }

            // If a path exists and it has been blocked recalculate the path
            if (paths[pathNumber].pathNodes.Count > 0 && CheckForBlockedPath() && paths[pathNumber].startAllocated && paths[pathNumber].endAllocated)
            {
                GeneratePath();
                paths[pathNumber].startEndReallocated = false;

                if (paths[pathNumber].avatar != null)
                {
                    paths[pathNumber].avatar.SetPosition(paths[pathNumber].start.transform.position + new Vector3(0, 0.5f, 0));
                    paths[pathNumber].avatar.SetPath(paths[pathNumber]);
                }
            }
        }

        // Go through each path and update it's avatars
        foreach (Path path in paths)
        {
            if (path.avatar != null)
            {
                path.avatar.Update();
            }
        }
    }

    public void UnselectAll()
    {

        foreach (var node in nodeGrid)
        {
            if (node != null && nodeGrid != null)
            {
                node.GetComponent<NodeScript>().selected = false;
                node.GetComponent<NodeScript>().UnHighlight();

            }
        }
    }
    // Update for the diffusion avatars
    void DiffusionUpdate()
    {
        // Reset the entering/exit values
        avatarsEntering = 0;
        avatarsExiting = 0;

        // Set up a list for avatars to be removed
        List<DiffusionAvatarScript> avatarsForRemoval = new List<DiffusionAvatarScript>();

        // Go through all avatars
        foreach (DiffusionAvatarScript avatar in avatars)
        {
            // Alternates updates for entering and exiting avatars
            if (lastUpdatedEntering == avatar.entering)
            {
                avatar.Update();
            }

            avatar.UpdateNodeAvatarOverlap();    // Updates nodes overlap values ready for thier updates

            // If they are entereing and not reached the end add to the entering value
            // otherwise remove them from the simulation
            if (avatar.entering)
            {
                if (!avatar.endReached)
                {
                    avatarsEntering++;
                }
                else
                {
                    avatarsForRemoval.Add(avatar);
                }
            }
            // If they are exiting and not reached the end add to the exiting value
            // otherwise remove them from the simulation
            else
            {
                if (!avatar.endReached)
                {
                    avatarsExiting++;
                }
                else
                {
                    avatarsForRemoval.Add(avatar);
                }
            }
        }

        //Switch the update flag
        lastUpdatedEntering = !lastUpdatedEntering;

        // Remove avatars from the simulation
        foreach (DiffusionAvatarScript avatar in avatarsForRemoval)
        {
            avatars.Remove(avatar);
            Destroy(avatar.avatar);

        }
    }

    // Returns the start end reallocated of the current path 
    public bool GetStartEndReallocated()
    {
        return paths[pathNumber].startEndReallocated;
    }

    bool redirectionSignPlace;
    PeakingGroup peakingGroupSignPlace;
    public void placeRedirectionSign(PeakingGroup group)
    {
        redirectionSignPlace = true;
        peakingGroupSignPlace = group;
    }


    void updateMapAreas(){
        foreach(var section in mapSections)
            section.updateSections();
    }
    // Handles all user input
    void HandleInput()
    {

        // Toggles diffusion mode
        if (Input.GetKeyDown(KeyCode.M))
        {
            //diffusionMode = !diffusionMode;
        }


        // Update depending on the current mode 
        if (!diffusionMode)
        {
            PathInput();
        }
        else
        {
            DiffusionInput();
        }
    }

    // Updates the input in path mode
    void PathInput()
    {
        // Toggles show diffusion
        if (Input.GetKeyDown(KeyCode.S))
        {
            showDiffusion = !showDiffusion;
        }

        // Toggles Dijkstra
        if (Input.GetKeyDown(KeyCode.D))
        {
            useDijkstra = !useDijkstra;

            if (useDijkstra)
            {
                pureHeuristic = false;
            }
            paths[pathNumber].startEndReallocated = true;

        }

        // Toggles pure heuristic
        if (Input.GetKeyDown(KeyCode.P))
        {
            pureHeuristic = !pureHeuristic;

            if (pureHeuristic)
            {
                useDijkstra = false;
            }
            paths[pathNumber].startEndReallocated = true;

        }

        // Cycles the heuristics
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (++heuristic == Heuristic.NUMBER_OF_HEURISTICS)
            {
                heuristic = 0;
            }
            paths[pathNumber].startEndReallocated = true;

        }

        // Show path 1-9
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            pathNumber = 0;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            pathNumber = 1;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            pathNumber = 2;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            pathNumber = 3;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            pathNumber = 4;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            pathNumber = 5;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            pathNumber = 6;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            pathNumber = 7;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            pathNumber = 8;
            ResetUnblockedNodes();
            ShowPath(pathNumber);
        }

        // Show all paths
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            pathNumber = 9;
            ResetUnblockedNodes();
            ShowAllPaths();
        }

        // Toggles pause
        if (Input.GetKeyDown(KeyCode.Pause))
        {
            avatarsPaused = !avatarsPaused;
            foreach (Path path in paths)
            {
                if (path.avatar != null)
                {
                    path.avatar.SetPaused(avatarsPaused);
                }
            }
        }

        // Spawn an avatar to follow the paths
        if (Input.GetKeyDown(KeyCode.A) && paths[pathNumber].pathNodes.Count >= 1)
        {

            if (pathNumber == 9)
            {
                for (int i = 0; i < paths.Length; ++i)
                {
                    if (paths[i].avatar == null)
                    {
                        paths[i].avatar = new AvatarScript();
                        paths[i].avatar.Setup();
                        paths[i].avatar.avatar.GetComponent<MeshRenderer>().material = avatarMaterial;
                        paths[i].avatar.SetPath(paths[i]);
                        paths[i].avatar.SetScale(new Vector3((floorDimentions.x / gridSize.x), 1.0f, (floorDimentions.y / gridSize.y)));
                        paths[i].avatar.SetPosition(paths[i].start.transform.position + new Vector3(0, 0.5f, 0));
                        paths[i].avatar.SetPaused(avatarsPaused);
                    }
                    else
                    {
                        paths[i].avatar.SetPosition(paths[i].start.transform.position + new Vector3(0, 0.5f, 0));
                        paths[i].avatar.SetPath(paths[i]);
                    }
                }
            }
            else
            {
                if (paths[pathNumber].avatar == null)
                {
                    paths[pathNumber].avatar = new AvatarScript();
                    paths[pathNumber].avatar.Setup();
                    paths[pathNumber].avatar.avatar.GetComponent<MeshRenderer>().material = avatarMaterial;
                    paths[pathNumber].avatar.SetPath(paths[pathNumber]);
                    paths[pathNumber].avatar.SetScale(new Vector3((floorDimentions.x / gridSize.x), 1.0f, (floorDimentions.y / gridSize.y)));
                    paths[pathNumber].avatar.SetPosition(paths[pathNumber].start.transform.position + new Vector3(0, 0.5f, 0));
                    paths[pathNumber].avatar.SetPaused(avatarsPaused);
                }
                else
                {
                    paths[pathNumber].avatar.SetPosition(paths[pathNumber].start.transform.position + new Vector3(0, 0.5f, 0));
                    paths[pathNumber].avatar.SetPath(paths[pathNumber]);
                }
            }
        }
    }

    // Handles input in diffusion mode 
    void DiffusionInput()
    {
        // Toggle show diffusion
        if (Input.GetKeyDown(KeyCode.S))
        {
            showDiffusion = !showDiffusion;
        }
        if (Input.GetKeyDown(KeyCode.A)){
            usingAStarOnly = !usingAStarOnly;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            usingAugmentedDiffusion = !usingAugmentedDiffusion;
        }

         if (Input.GetMouseButtonDown(1))
        {
            redirectionSignPlace = false;
        }

         if (Input.GetKeyDown(KeyCode.H))
        {
            showHeatmap = !showHeatmap;
            heatmap.gameObject.SetActive(showHeatmap);
        }
        if(Input.GetKeyDown(KeyCode.C)){
            Heatmap.seperateMapSections = !Heatmap.seperateMapSections;
        }

        if (Input.GetMouseButtonDown(0))
        {
            spawnParkingSpaceOnCursor();
        }

        // Pause the avatars
        if (Input.GetKeyDown(KeyCode.Pause))
        {
            avatarsPaused = !avatarsPaused;
            foreach (DiffusionAvatarScript avatar in avatars)
            {
                if (avatar != null)
                {
                    avatar.SetPaused(avatarsPaused);
                }
            }
        }

        //set sensitivity of heatmap
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Heatmap.sensitivity = 1;
            
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            Heatmap.sensitivity = 2;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            Heatmap.sensitivity = 3;
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            Heatmap.sensitivity = 4;
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            Heatmap.sensitivity = 5;
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            Heatmap.sensitivity = 6;
        }
        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            Heatmap.sensitivity = 7;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Heatmap.sensitivity = 8;
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            Heatmap.sensitivity = 9;
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Heatmap.sensitivity = 10;
        }

    }


    void spawnParkingSpaceOnCursor(){
        //check if you've clicked on the map
            GameObject lastClicked;
            Ray ray;
            RaycastHit rayHit;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out rayHit))
            {
                lastClicked = rayHit.collider.gameObject;

                //if not placing a sign place parking
                if (!redirectionSignPlace)
                {
                    if (lastClicked != null && (lastClicked.name == "TopLeft" || lastClicked.name == "TopRight"
                        || lastClicked.name == "BottomLeft" || lastClicked.name == "BottomRight"))
                    {
                        //place parking lot

                        ParkingLotManager parkingManager = ParkingLots.GetComponent<ParkingLotManager>();

                        GameObject parking = Instantiate(ParkingPrefab, ParkingLots.transform, false);

                        parking.tag = "parkingLot";
                        parking.GetComponent<ParkingLot>().myManager = parkingManager;
                        parking.GetComponent<NodeScript>().myManager = this;
                        parking.transform.position = new Vector3(rayHit.point.x, parking.transform.position.y, rayHit.point.z);
                        //add to parking manager
                        Array.Resize(ref parkingManager.parkingLots, parkingManager.parkingLots.Length + 1);
                        parkingManager.parkingLots[parkingManager.parkingLots.Length - 1] = parking;
                        //add to grid
                        Array.Resize(ref nodeGrid, nodeGrid.Length + 1);
                        nodeGrid[nodeGrid.Length - 1] = parking;

                        //add to pannel
                        ParkingDisplayPanel displayPannel = ParkingLotDisplayPannel.GetComponent<ParkingDisplayPanel>();
                        Array.Resize(ref displayPannel.parkingLots, displayPannel.parkingLots.Length + 1);
                        displayPannel.parkingLots[displayPannel.parkingLots.Length - 1] = parking.GetComponent<NodeScript>();

                        //get surrounding nodes in a given rad. and add them as neighbours
                        Collider[] hitColliders = Physics.OverlapSphere(parking.transform.position, radSphereNeighboursParkingPlacement);
                        List<GameObject> neighbours = new List<GameObject>();
                        foreach (var collider in hitColliders)
                        {
                            //check if the object is a node
                            if (collider.gameObject.GetComponent<NodeScript>() && collider.gameObject != parking)
                            {
                                neighbours.Add(collider.gameObject);

                                //add parking to neighbors of new neighbour
                                NodeScript nodeNeighbor = collider.gameObject.GetComponent<NodeScript>();
                                Array.Resize(ref nodeNeighbor.neighbours, nodeNeighbor.neighbours.Length + 1);
                                nodeNeighbor.neighbours[nodeNeighbor.neighbours.Length - 1] = parking;
                                //generate edges again
                                nodeNeighbor.ManualEdgeCreation();


                            }

                        }

                        parking.GetComponent<NodeScript>().neighbours = neighbours.ToArray();
                        //generate edges
                        parking.GetComponent<NodeScript>().ManualEdgeCreation();

                        //update dropdown
                        foreach (var g in groups)
                        {
                            g.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options =
                            ParkingLotManager.parkingLots.Where(x => !x.name.Contains("streetParking")).Select(x => new UnityEngine.UI.Dropdown.OptionData(x.GetComponent<ParkingLot>().parkingLotName)).ToList<UnityEngine.UI.Dropdown.OptionData>();
                            g.transform.GetChild(0).transform.Find("ChooseGoalDropdown").GetComponent<UnityEngine.UI.Dropdown>().options.Add(new UnityEngine.UI.Dropdown.OptionData("none"));

                        }
                        updateMapAreas();

                    }
                }
                //else place a redir sign
                else
                {
                    //node layer
                    if (lastClicked != null && (lastClicked.layer == 9))
                    {

                        lastClicked.AddComponent<RedirectionSignScript>();
                        lastClicked.GetComponent<RedirectionSignScript>().chanceToRedirect = peakingGroupSignPlace == null ? 0.5f : peakingGroupSignPlace.signRedirectionChance;
                    }
                }
            }
    }

    // Returns show diffusion
    public bool GetShowDiffusion()
    {
        return showDiffusion;
    }

    // Returns start allocated
    public bool GetStartAllocated()
    {
        return paths[pathNumber].startAllocated;
    }

    // Returns start node of current path
    public NodeScript GetStart()
    {
        return paths[pathNumber].start;
    }

    // Sets a new start node
    public void SetStart(NodeScript newStart)
    {

        if (newStart != null)
        {
            if (!diffusionMode)
            {
                // If the new start is the current start set to unsearched
                if (newStart == paths[pathNumber].start && paths[pathNumber].start.NodeStatus == NodeScript.NODE_STATUS.START)
                {
                    paths[pathNumber].startAllocated = false;
                    paths[pathNumber].start.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                    ResetNodes();
                }
                // If start is the current end set the start as the end
                else if (newStart == paths[pathNumber].end)
                {
                    paths[pathNumber].endAllocated = false;
                    paths[pathNumber].startAllocated = true;
                    paths[pathNumber].start.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                    paths[pathNumber].start = newStart;
                    paths[pathNumber].start.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.START);
                    ResetNodes();
                }
                // If the node is not the start
                else if (paths[pathNumber].start != newStart || (newStart == paths[pathNumber].start && paths[pathNumber].start.NodeStatus != NodeScript.NODE_STATUS.START))
                {
                    if (paths[pathNumber].startAllocated)
                    {
                        paths[pathNumber].start.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                    }

                    paths[pathNumber].start = newStart;
                    paths[pathNumber].start.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.START);
                    paths[pathNumber].startAllocated = true;
                }
                paths[pathNumber].startEndReallocated = true;
            }
            else
            {
                SpawnDiffusionAvatar(newStart);
            }
        }
    }

    // Spawns an avatar at the sent node
    public void SpawnDiffusionAvatar(NodeScript spawnNode, PeakingGroup group = null, bool entering = true, bool prebooked = false)
    {
        //if the avatar has a group and is preebooked spawn an avatar with a specific goal
        //if the avatar has a group but is not preebooked but has a group spawn an avatar with a goal around the location
        //if the avatar is not part of a group spawn an avatar that goes to the closest goal
        DiffusionAvatarScript newAvatar = group != null ? new DiffusionAvatarScript(group, prebooked) : new DiffusionAvatarScript();
        newAvatar.Setup(!autoGenerateGrid, entering);
        newAvatar.SetNode(spawnNode);
        newAvatar.SetPaused(avatarsPaused);
        newAvatar.avatar.transform.position = spawnNode.transform.position;
        newAvatar.SetScale(new Vector3(((floorDimentions.x / gridSize.x) * diffusionAvatarSize), 1.0f, ((floorDimentions.y / gridSize.y) * diffusionAvatarSize)));
        if (group != null)
            newAvatar.groupId = group;


        // If in a created city
        if (!autoGenerateGrid)
        {
            newAvatar.SetMaterial(entering ? enteringDiffusionAvatarMaterial : exitingDiffusionAvatarMaterial);
        }

        avatars.Add(newAvatar);
    }

    // Returns the path time of the current path
    public float GetPathTime()
    {
        if (pathNumber != 9)
        {
            return paths[pathNumber].pathTime;
        }
        return 0;
    }

    // Returns the path size for the current path
    public int GetPathSize()
    {
        if (pathNumber != 9)
        {
            return paths[pathNumber].pathNodes.Count;
        }
        return 0;
    }

    // Returns the path distance for the current path 
    public float GetPathDistance()
    {
        if (pathNumber != 9 && paths[pathNumber].pathNodes.Count > 0)
        {
            return paths[pathNumber].pathDist;
        }
        else
        {
            return 0;
        }
    }

    // Returns the nodes searched for the current path
    public int GetNodesSearched()
    {
        if (pathNumber != 9)
        {
            return paths[pathNumber].searchedNodes.Count;
        }

        return 0;
    }

    // Returns end allocated
    public bool GetEndAllocated()
    {
        if (!diffusionMode)
        {
            if (pathNumber != 9)
            {
                return paths[pathNumber].endAllocated;
            }

            return false;
        }
        else
        {
            return (diffusionEnd != null);
        }
    }

    // Returns the end node of the current path
    public NodeScript GetEnd()
    {
        if (pathNumber != 9)
        {
            return paths[pathNumber].end;
        }

        return paths[0].end;
    }

    // Sets the end node
    public void SetEnd(NodeScript newEnd)
    {
        if (newEnd != null)
        {
            if (!diffusionMode)
            {
                if (pathNumber != 9)
                {
                    // If the new end is the current end set to unsearched
                    if (newEnd == paths[pathNumber].end && paths[pathNumber].end.NodeStatus == NodeScript.NODE_STATUS.END)
                    {
                        paths[pathNumber].endAllocated = false;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        ResetNodes();
                    }
                    // If end is the current start set the start as the end
                    else if (newEnd == paths[pathNumber].start)
                    {
                        paths[pathNumber].startAllocated = false;
                        paths[pathNumber].endAllocated = true;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        paths[pathNumber].end = newEnd;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                        ResetNodes();
                    }
                    // If the node is not the start
                    else if (paths[pathNumber].end != newEnd || (newEnd == paths[pathNumber].end && paths[pathNumber].end.NodeStatus != NodeScript.NODE_STATUS.END))
                    {
                        if (paths[pathNumber].endAllocated)
                        {
                            paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        }

                        paths[pathNumber].end = newEnd;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                        paths[pathNumber].endAllocated = true;
                    }
                }
                paths[pathNumber].startEndReallocated = true;
            }
            else
            {
                if (diffusionEnd == newEnd && diffusionEnd.NodeStatus == NodeScript.NODE_STATUS.END)
                {
                    diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                }
                else
                {
                    if (diffusionEnd != null)
                    {
                        diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                    }

                    diffusionEnd = newEnd;
                    diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                }
            }
        }
    }


    public NodeScript SetEnd(int endIndex)
    {
        NodeScript newEnd = nodeGrid[endIndex].GetComponent<NodeScript>();
        if (newEnd != null)
        {
            if (!diffusionMode)
            {
                if (pathNumber != 9)
                {
                    // If the new end is the current end set to unsearched
                    if (newEnd == paths[pathNumber].end && paths[pathNumber].end.NodeStatus == NodeScript.NODE_STATUS.END)
                    {
                        paths[pathNumber].endAllocated = false;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        ResetNodes();
                    }
                    // If end is the current start set the start as the end
                    else if (newEnd == paths[pathNumber].start)
                    {
                        paths[pathNumber].startAllocated = false;
                        paths[pathNumber].endAllocated = true;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        paths[pathNumber].end = newEnd;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                        ResetNodes();
                    }
                    // If the node is not the start
                    else if (paths[pathNumber].end != newEnd || (newEnd == paths[pathNumber].end && paths[pathNumber].end.NodeStatus != NodeScript.NODE_STATUS.END))
                    {
                        if (paths[pathNumber].endAllocated)
                        {
                            paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                        }

                        paths[pathNumber].end = newEnd;
                        paths[pathNumber].end.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                        paths[pathNumber].endAllocated = true;
                    }
                }
                paths[pathNumber].startEndReallocated = true;
            }
            else
            {
                if (diffusionEnd == newEnd && diffusionEnd.NodeStatus == NodeScript.NODE_STATUS.END)
                {
                    diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                }
                else
                {
                    if (diffusionEnd != null)
                    {
                        diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);

                    }

                    diffusionEnd = newEnd;
                    diffusionEnd.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.END);
                }
            }
        }
        return newEnd;
    }

    /*
     
          for (int i = 0; i < nodeGrid.Length; i++)
        {
            if (nodeGrid[i].GetComponent<NodeScript>() == newBlock)
            {
                TextWriter tw = new StreamWriter("H:\\Downloads\\testingblocks.txt", true);
                // write a line of text to the file
                tw.WriteLine(i);

                // close the stream
                tw.Close();
            }
        }

     */
    // Set the node as a blocker
    public void SetBlocker(NodeScript newBlock)
    {


        if (pathNumber != 9)
        {
            if (newBlock != null)
            {
                if (newBlock.NodeStatus != NodeScript.NODE_STATUS.BLOCKED)
                {
                    newBlock.ChangeState(NodeScript.NODE_STATUS.BLOCKED);
                }
                else
                {
                    newBlock.ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                }

                CheckForBlockedStartOrEnd();

                paths[pathNumber].startEndReallocated = true;

            }
        }
    }

    public void SetBlocker(int indexOfBlock)
    {


        NodeScript newBlock = nodeGrid[indexOfBlock].GetComponent<NodeScript>();

        if (pathNumber != 9)
        {
            if (newBlock != null)
            {
                if (newBlock.NodeStatus != NodeScript.NODE_STATUS.BLOCKED)
                {
                    newBlock.ChangeState(NodeScript.NODE_STATUS.BLOCKED);
                }
                else
                {
                    newBlock.ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
                }

                CheckForBlockedStartOrEnd();

                paths[pathNumber].startEndReallocated = true;

            }
        }
    }

    // Checks to see if the start or end node has been invalidated by being blocked
    public void CheckForBlockedStartOrEnd()
    {
        if (pathNumber != 9)
        {
            if (paths[pathNumber].start && paths[pathNumber].end)
            {
                if (paths[pathNumber].start.NodeStatus == NodeScript.NODE_STATUS.BLOCKED)
                {
                    paths[pathNumber].startAllocated = false;
                }
                else if (paths[pathNumber].end.NodeStatus == NodeScript.NODE_STATUS.BLOCKED)
                {
                    paths[pathNumber].endAllocated = false;
                    ResetNodes();
                }
            }
        }
    }

    // Returns true if the calculated path has been blocked
    public bool CheckForBlockedPath()
    {
        if (pathNumber != 9)
        {
            foreach (NodeScript node in paths[pathNumber].pathNodes)
            {
                if (node.NodeStatus == NodeScript.NODE_STATUS.BLOCKED)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // Resets all non-start/end/block nodes to an unsearched status, sets the hcost to 0 and sets the parents to null
    public void ResetNodes()
    {
        foreach (GameObject node in nodeGrid)
        {
            if (node.GetComponent<NodeScript>().NodeStatus != NodeScript.NODE_STATUS.BLOCKED &&
                node.GetComponent<NodeScript>().NodeStatus != NodeScript.NODE_STATUS.START &&
                node.GetComponent<NodeScript>().NodeStatus != NodeScript.NODE_STATUS.END)
            {
                node.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
            }

            node.GetComponent<NodeScript>().parentA = null;
            node.GetComponent<NodeScript>().hCost = 0;
        }
    }

    //Show the path at the sent index
    void ShowPath(int pathNumber)
    {
        foreach (NodeScript node in paths[pathNumber].searchedNodes)
        {
            node.ChangeState(NodeScript.NODE_STATUS.SEARCHED);
        }
        foreach (NodeScript node in paths[pathNumber].pathNodes)
        {
            node.ChangeState(NodeScript.NODE_STATUS.PATH);
        }

        if (paths[pathNumber].start != null)
        {
            paths[pathNumber].start.ChangeState(NodeScript.NODE_STATUS.START);
        }
        if (paths[pathNumber].end != null)
        {
            paths[pathNumber].end.ChangeState(NodeScript.NODE_STATUS.END);
        }
    }

    // Draw all of the paths
    void ShowAllPaths()
    {
        foreach (Path path in paths)
        {
            foreach (NodeScript node in path.searchedNodes)
            {
                node.ChangeState(NodeScript.NODE_STATUS.SEARCHED);
            }
        }

        foreach (Path path in paths)
        {
            foreach (NodeScript node in path.pathNodes)
            {
                node.ChangeState(NodeScript.NODE_STATUS.PATH);
            }
        }

        foreach (Path path in paths)
        {
            if (path.start != null)
            {
                path.start.ChangeState(NodeScript.NODE_STATUS.START);
            }
            if (path.start != null)
            {
                path.end.ChangeState(NodeScript.NODE_STATUS.END);
            }
        }
    }

    // Set all unblocked nodes to unsearched
    public void ResetUnblockedNodes()
    {
        foreach (GameObject node in nodeGrid)
        {
            if (node.GetComponent<NodeScript>().NodeStatus != NodeScript.NODE_STATUS.BLOCKED)
            {
                node.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.UNSEARCHED);
            }

            node.GetComponent<NodeScript>().parentA = null;
            node.GetComponent<NodeScript>().hCost = 0;
        }
    }

    //Generates a path using A*
    public void GeneratePath()
    {

        paths[pathNumber].pathTime = Time.realtimeSinceStartup;

        List<NodeScript> openList = new List<NodeScript>();
        List<NodeScript> closedList = new List<NodeScript>();

        ResetNodes();
        paths[pathNumber].pathNodes.Clear();

        closedList.Add(paths[pathNumber].start);

        // Use correct heuristic
        switch (heuristic)
        {
            case Heuristic.PYTHAGORAS:
                paths[pathNumber].start.hCost = CalculateHCost(paths[pathNumber].start, pyDist, 1.0f);
                break;
            case Heuristic.MANHATTAN:
                paths[pathNumber].start.hCost = CalculateHCost(paths[pathNumber].start, manhattan, 1.0f);
                break;
            case Heuristic.COLLABORATIVEDIFF:
                paths[pathNumber].start.hCost = CalculateHCost(paths[pathNumber].start, CollaborativeDiffusuion, 1.0f);
                break;
        }

        AddNeighboursToList(paths[pathNumber].start, openList, closedList);

        // Works through the openlist until the end is found or no more nodes on the open list
        while (openList.Count > 0)
        {
            NodeScript cheapestNode = FindCheapestNode(openList);

            if (cheapestNode == paths[pathNumber].end)
            {
                while (cheapestNode != null)
                {
                    if (cheapestNode != paths[pathNumber].start && cheapestNode != paths[pathNumber].end)
                    {
                        cheapestNode.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.PATH);
                    }
                    paths[pathNumber].pathNodes.Add(cheapestNode);
                    cheapestNode = cheapestNode.parentA;
                }

                paths[pathNumber].pathNodes.Reverse();
                paths[pathNumber].pathTime = (Time.realtimeSinceStartup - paths[pathNumber].pathTime) * 1000;
                paths[pathNumber].pathDist = CalculateGCost(paths[pathNumber].end);
                paths[pathNumber].searchedNodes = closedList;
                return;

            }

            cheapestNode.GetComponent<NodeScript>().ChangeState(NodeScript.NODE_STATUS.SEARCHED);

            openList.Remove(cheapestNode);
            closedList.Add(cheapestNode);

            AddNeighboursToList(cheapestNode, openList, closedList);
        }

        paths[pathNumber].pathTime = (Time.realtimeSinceStartup - paths[pathNumber].pathTime) * 1000;
        return;

    }

    //Finds the cheapest node  with the cheapest fCost or preferably the end node in the list and returns it
    NodeScript FindCheapestNode(List<NodeScript> list)
    {
        NodeScript cheapestNode = list[0];

        foreach (NodeScript node in list)
        {
            if (node == paths[pathNumber].end)
            {
                return node;
            }
            else if (!pureHeuristic && !useDijkstra && (CalculateGCost(cheapestNode) + cheapestNode.hCost) > (CalculateGCost(node) + node.hCost))
            {
                cheapestNode = node;
            }
            else if (pureHeuristic && cheapestNode.hCost > node.hCost)
            {
                cheapestNode = node;
            }
            else if (useDijkstra && CalculateGCost(cheapestNode) > CalculateGCost(node))
            {
                cheapestNode = node;
            }
        }

        return cheapestNode;
    }

    //Adds any neighbours of node to the list and updates any neighbours parents if it is a cheaper route
    void AddNeighboursToList(NodeScript node, List<NodeScript> openList, List<NodeScript> closedList)
    {
        foreach (EdgeScript edge in node.edges)
        {
            if (edge.GetTo().NodeStatus != NodeScript.NODE_STATUS.BLOCKED && !closedList.Contains(edge.GetTo()))
            {
                if (!openList.Contains(edge.GetTo()))
                {
                    edge.GetTo().parentA = node;
                    switch (heuristic)
                    {
                        case Heuristic.PYTHAGORAS:
                            edge.GetTo().hCost = CalculateHCost(edge.GetTo(), pyDist, 1.0f);
                            break;
                        case Heuristic.MANHATTAN:
                            edge.GetTo().hCost = CalculateHCost(edge.GetTo(), manhattan, 1.0f);
                            break;
                        case Heuristic.COLLABORATIVEDIFF:
                            edge.GetTo().hCost = CalculateHCost(edge.GetTo(), CollaborativeDiffusuion, 1.0f);
                            break;
                    }
                    openList.Add(edge.GetTo());
                }
                else
                {
                    if (CalculateGCost(edge.GetTo()) > (edge.GetCost() + CalculateGCost(edge.GetFrom())))
                    {
                        edge.GetTo().parentA = edge.GetFrom();
                    }
                }
            }
        }
    }

    //Calculates the f cost of the node
    float CalculateFCost(NodeScript node, List<Func<NodeScript, NodeScript, float>> heuristics, float[] weights)
    {
        float hCost = CalculateHCost(node, heuristics, weights);
        if (hCost != -1)
        {
            float gCost = CalculateGCost(node);
            return hCost + gCost;
        }

        return -1;
    }

    //Calaculates the gCost of the node by going through the parent references until the start
    float CalculateGCost(NodeScript node)
    {
        float gCost = 0;

        while (node.parentA != null)
        {
            gCost += node.parentA.FindEdgeTo(node).GetCost();
            node = node.parentA;
        }

        return gCost;
    }

    //Calculates the hCost from the heuristics and weights given. Args heuristics and weights should have the same number of elements.
    float CalculateHCost(NodeScript node, List<Func<NodeScript, NodeScript, float>> heuristics, float[] weights)
    {
        if (node && heuristics.Count > 0 && weights.Length >= heuristics.Count)
        {
            float HCost = 0;

            for (short i = 0; i < heuristics.Count; ++i)
            {
                HCost += heuristics[i](node, paths[pathNumber].end) * weights[i];
            }

            return HCost;
        }
        return -1;
    }

    //Calculates the hCost based on the heuristic and weight passed in.
    float CalculateHCost(NodeScript node, Func<NodeScript, NodeScript, float> heuristic, float weight)
    {
        if (node)
        {
            float HCost = 0;

            HCost += heuristic(node, paths[pathNumber].end) * weight;

            return HCost;
        }
        return -1;
    }

    // Reset the diffusion values of all the nodes
    public void ResetDiffusion()
    {
        foreach (GameObject node in nodeGrid)
        {
            if (node && node.GetComponent<NodeScript>() != paths[pathNumber].end)
            {
                node.GetComponent<NodeScript>().SetDiffusion(0);
            }
        }
    }

    // Remove all diffusion avatars
    public void RemoveDiffusionAvatars()
    {
        List<DiffusionAvatarScript> avatarsForRemoval = new List<DiffusionAvatarScript>();

        foreach (DiffusionAvatarScript avatar in avatars)
        {
            avatarsForRemoval.Add(avatar);
        }

        foreach (DiffusionAvatarScript avatar in avatarsForRemoval)
        {
            avatars.Remove(avatar);
            Destroy(avatar.avatar);
        }
    }

    //standard deviation random number generator
    public static double NextGaussianDoubleBoxMuller(float mean, float stdDev)
    {
        double u1 = 1.0 - UnityEngine.Random.value; //uniform(0,1] 
        double u2 = 1.0 - UnityEngine.Random.value;
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                     Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
        double randNormal =
                     mean + stdDev * randStdNormal;
        return randNormal;
    }

    public void exportNodeToCsv()
    {
        NodeScript myNode = null;
        foreach (var node in nodeGrid)
        {
            if (!node) continue;
            if (node.GetComponent<NodeScript>().GetHighlighted())
            {
                myNode = node.GetComponent<NodeScript>();
            }
        }

        if (myNode != null)
        {
            string csv = string.Join(
            System.Environment.NewLine,
            myNode.avatarsOvelapped.Select(d => (d.Key + "," + d.Value)).ToArray()
            );
            csv = "hour,traffic" + System.Environment.NewLine + csv;
            saveFile(csv,myNode.name+".csv");

        }
    }



    public string generateNodeTrafficCSV()
    {
        Dictionary<float, int> overall = new Dictionary<float, int>();

        for (int i = 0; i < 24; i++)
        {
            for (int k = 0; k < 6; k++)
            {
                overall.Add(i * 1.0f + k * 1.0f / 10, 0);

            }
        }

        foreach (var node in nodeGrid)
        {
            if (!node) continue;
            var myNode = node.GetComponent<NodeScript>();
            foreach (var entry in myNode.floats)
            {
                overall[entry.Key] = overall[entry.Key] + entry.Value;
            }
        }

        string csv = string.Join(
        System.Environment.NewLine,
        overall.Select(d => (d.Key + "," + d.Value)).ToArray());
        csv = "hour,traffic" + System.Environment.NewLine + csv;

        return csv;
    }

    public string generateNodeTrafficCSV24H()
    {
        Dictionary<int, int> avatarsOvelapped = new Dictionary<int, int>();

        for (int i = 0; i < 24; i++)
        {
            avatarsOvelapped.Add(i, 0);
        }

        foreach (var node in nodeGrid)
        {
            if (!node) continue;
            var myNode = node.GetComponent<NodeScript>();
            foreach (var entry in myNode.avatarsOvelapped)
            {
                avatarsOvelapped[entry.Key] = avatarsOvelapped[entry.Key] + entry.Value;
            }
        }

        string csv = string.Join(
       System.Environment.NewLine,
       avatarsOvelapped.Select(d => (d.Key + "," + d.Value)).ToArray());
        csv = "hour,traffic" + System.Environment.NewLine + csv;

        return csv;
    }

    public string uniqueIdGenerate(){
        int len = 7;
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0";
        return new string(Enumerable.Repeat(chars, len)
        .Select(s => s[(int)System.Math.Round((float)UnityEngine.Random.Range(0,s.Length-1))]).ToArray());
    }

    public string generateOverallTrafficEachNode()
    {
        string csv = string.Join(
            System.Environment.NewLine,
            nodeGrid.Where(x=>x.GetComponent<NodeScript>()!=null).Select(x=>x.GetComponent<NodeScript>()).
                Select(x=>uniqueIdGenerate()+","+
                x.transform.position.x+","+
                x.transform.position.y+"," +
                x.transform.position.z+"," +
                x.overlappedTotal
                ).ToArray()
            );
        
        csv = "NodeId,X,Y,Z,TotalTraffic" + System.Environment.NewLine + csv;
        return csv;
    }

    public string generateParkingCapacitiesCSV(){
        

         //first get all parkingLots
        var lots = ParkingLotManager.parkingLots.Select(x=>x.GetComponent<ParkingLot>());
        var count = lots.Count();

        //generate road % capacity over time in a 24 00 format
        string csv = string.Join(
            System.Environment.NewLine,
            //for each lot calculate the average capacity over time for each time
            lots.
            //returns a list of Dictionary<time,congestion>
            Select(x=>x.calcAveragesCapacity()).
            //Reduce the dictionaries to one and sum for each time
            //We do this because we want the sum of the congestion for each time, not the avrg congestion
            Aggregate(new Dictionary<double,double>(),(d,next)=>{
                foreach(var key in next.Keys){
                    if(!d.ContainsKey(key))
                        d[key] = 0;
                    d[key]+=next[key];
                }
                return d;
                //get the value for each hour and the avrg value for each hour
                }).Select(x=>x.Key + "," + x.Value + "," + x.Value/count*1.0).ToArray()
        );
        //include the number of lots as well as some side info
        csv = "Hour,Occupancy/Capacity Sum,Occupancy/Capacity Avrg" + System.Environment.NewLine + csv;



        //for each get the averages for each hour
        return csv;
    }

    
    public string generateTimeLeftArrivedVTCCSV()
    {
        string csv = string.Join(
            System.Environment.NewLine,
            DiffusionAvatarScript.timeLeftArrivedVTCAll.Select(x=>x.Item1+","+x.Item2 +","+x.Item3).ToArray()
        );
        csv = "LeftAt,ArrivedAt,VMT" + System.Environment.NewLine + csv;

        return csv;
    }

    public string generateRoadCapacitiesCSV()
    {
        //first get all edges
        var edg = nodeGrid.Where(x=>x.GetComponent<NodeScript>()!=null).
            SelectMany(x=>x.GetComponent<NodeScript>().edges);
        var count = edg.Count();
        

        //generate road % capacity over time in a 24 00 format
        string csv = string.Join(
            System.Environment.NewLine,
            //for each edge calculate the average capacity over time for each time
            edg.
            //returns a list of Dictionary<time,congestion>
            Select(x=>x.calcAverages()).
            //Reduce the dictionaries to one and sum for each time
            //We do this because we want the sum of the congestion for each time, not the avrg congestion
            Aggregate(new Dictionary<double,double>(),(d,next)=>{
                foreach(var key in next.Keys){
                    if(!d.ContainsKey(key))
                        d[key] = 0;
                    d[key]+=next[key];
                }
                return d;
                //get the value for each hour and the avrg value for each hour
                }).Select(x=>x.Key + "," + x.Value + "," + x.Value/count*1.0).ToArray()
        );
        //include the number of edges as well as some side info
        csv = "Hour,Occupancy/Capacity Sum,Occupancy/Capacity Avrg" + System.Environment.NewLine + csv;



        //for each get the averages for each hour
        return csv;
    }

     
    public string generateSpawnTimesCSV()
    {
        string csv = string.Join(
            System.Environment.NewLine,
            spawners.Select(x=>x.groupsSpawnBuckets).SelectMany(x=>x.Values)
            .Aggregate(new Dictionary<double,int>(),(d,next)=>{
                foreach(var key in next.Keys){
                    if(!d.ContainsKey(key))
                        d[key] = 0;
                    d[key]+=next[key];
                }
                return d;
                }).Select(x=> x.Key + "," + x.Value).ToArray());
        csv = "Time,Spawned" + System.Environment.NewLine + csv;
        return csv;
    }

    public string genGroupSpawnTimesCSV(PeakingGroup group) {
        string csv = string.Join(
            System.Environment.NewLine,
            spawners.Select(x => x.groupsSpawnBuckets[group])
            .Aggregate(new Dictionary<double, int>(), (d, next) => {
                foreach (var key in next.Keys)
                {
                    if (!d.ContainsKey(key))
                        d[key] = 0;
                    d[key] += next[key];
                }
                return d;
            }).Select(x => x.Key + "," + x.Value).ToArray());

        csv = "Time," + group.groupName + System.Environment.NewLine + csv;
        return csv;
    }

    public string generateVehicleCountCSV()
    {
        string csv = string.Join(
            System.Environment.NewLine,
            numberOfVehicles.Select(x => x.Key + "," + x.Value).ToArray());

        csv = "Time,Vehicles" + System.Environment.NewLine + csv;
        return csv;
    }
    

    public void saveFile(string data,string fileName){

#if UNITY_WEBGL
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(data);
        DownloadFile(bytes,bytes.Count(),fileName);
#endif

#if UNITY_STANDALONE
        System.IO.File.WriteAllText(fileName, data);
#endif
    }


    public void exportToCsv()
    {


        //add in time arrived
        string nodePointTrafficCsv = generateNodeTrafficCSV();
        string nodePointTrafficCsv24Hformat = generateNodeTrafficCSV24H();
        string timeArrivedCsv = generateTimeLeftArrivedVTCCSV();
        string roadCapacitiesCsv = generateRoadCapacitiesCSV();
        string overallTrafficEachNode = generateOverallTrafficEachNode();
        string parkingCapacities = generateParkingCapacitiesCSV();
        string vehicleCountSpawn = generateSpawnTimesCSV();
        string vehicleCount = generateVehicleCountCSV();

        //save
        saveFile(nodePointTrafficCsv,"Point_Traffic_Over_Time.csv");
        saveFile(nodePointTrafficCsv24Hformat,"Point_Traffic_Over_Time_24h_Format.csv");
        saveFile(timeArrivedCsv,"Car_Time_Left_Arrived_and_VTC.csv");
        saveFile(roadCapacitiesCsv,"Percentage_Road_Capacities_Over_Time.csv");
        saveFile(overallTrafficEachNode,"Overall_Traffic_Each_Node.csv");
        saveFile(parkingCapacities,"Parking_Space_Capacities_Over_Time.csv");
        saveFile(vehicleCountSpawn,"Vehicle_Spawn_Times.csv");
        saveFile(vehicleCount, "Vehicle_Count.csv");

        int i = 0;
        foreach (var group in groups.Select(x=>x.GetComponent<PeakingGroup>()))
        {
            string genGroupCsv = genGroupSpawnTimesCSV(group);
            saveFile(genGroupCsv, group.groupName+i+".csv");
            i++;
        }
    }

    public void TestNums()
    {

        List<double> nums = new List<double>();
        for (int i = 0; i < 100000; i++)
        {
            nums.Add(NextGaussianDoubleBoxMuller(6, 1));
        }
        string csv = string.Join(
        System.Environment.NewLine,
        nums.Select(d => (d.ToString())).ToArray()
        );
        saveFile(csv,"testingennumbers.csv");

    }

    public void TestNumsNorm()
    {
        Dictionary<double, double> overall = new Dictionary<double, double>();

        List<double> nums = new List<double>();
        double m = 0;
        int samples = 100000;
        double mean = 6;
        double std = 1;
        for (int i = 0; i < samples; i++)
        {
            double num = Math.Round(NormalDistribution(m % 24f, mean, std), 10);
            overall.Add(m, m <= mean ? num : 1 - num);
            m += 24.0 / samples;
        }
        string csv = string.Join(
       System.Environment.NewLine,
       overall.Select(d => (d.Key + "," + d.Value)).ToArray()
   );

        csv = "hour,traffic" + System.Environment.NewLine + csv;

        saveFile(csv,"testingnumbersnorm.csv");
    }

    public void ResetAvatarData()
    {
        foreach (var node in nodeGrid)
        {
            if (!node) continue;
            var myNode = node.GetComponent<NodeScript>();

            Dictionary<int, int> avatarsOverlapped = new Dictionary<int, int>();
            for (int i = 0; i < 24; i++)
            {
                avatarsOverlapped.Add(i, 0);
            }
            myNode.avatarsOvelapped = avatarsOverlapped;

        }

    }

    internal void ResetTime()
    {
        totalSecondsPassed = 0;
    }







    /// <summary>
    /// Returns the cumulative density function evaluated at A given value.
    /// </summary>
    /// <param name="x">A position on the x-axis.</param>
    /// <param name="mean"></param>
    /// <param name="sigma"></param>
    /// <returns>The cumulative density function evaluated at <C>x</C>.</returns>
    /// <remarks>The value of the cumulative density function at A point <C>x</C> is
    /// probability that the value of A random variable having this normal density is
    /// less than or equal to <C>x</C>.
    /// </remarks>
    public static double NormalDistribution(double x, double mean, double sigma)
    {
        // This algorithm is ported from dcdflib:
        // Cody, W.D. (1993). "ALGORITHM 715: SPECFUN - A Portabel FORTRAN
        // Package of Special Function Routines and Test Drivers"
        // acm Transactions on Mathematical Software. 19, 22-32.
        int i;
        double del, xden, xnum, xsq;
        double result, ccum;
        double arg = (x - mean) / sigma;
        const double sixten = 1.60e0;
        const double sqrpi = 3.9894228040143267794e-1;
        const double thrsh = 0.66291e0;
        const double root32 = 5.656854248e0;
        const double zero = 0.0e0;
        const double min = Double.Epsilon;
        double z = arg;
        double y = Math.Abs(z);
        const double half = 0.5e0;
        const double one = 1.0e0;

        double[] a =
            {
                2.2352520354606839287e00, 1.6102823106855587881e02, 1.0676894854603709582e03,
                1.8154981253343561249e04, 6.5682337918207449113e-2
            };

        double[] b =
            {
                4.7202581904688241870e01, 9.7609855173777669322e02, 1.0260932208618978205e04,
                4.5507789335026729956e04
            };

        double[] c =
            {
                3.9894151208813466764e-1, 8.8831497943883759412e00, 9.3506656132177855979e01,
                5.9727027639480026226e02, 2.4945375852903726711e03, 6.8481904505362823326e03,
                1.1602651437647350124e04, 9.8427148383839780218e03, 1.0765576773720192317e-8
            };

        double[] d =
            {
                2.2266688044328115691e01, 2.3538790178262499861e02, 1.5193775994075548050e03,
                6.4855582982667607550e03, 1.8615571640885098091e04, 3.4900952721145977266e04,
                3.8912003286093271411e04, 1.9685429676859990727e04
            };
        double[] p =
            {
                2.1589853405795699e-1, 1.274011611602473639e-1, 2.2235277870649807e-2,
                1.421619193227893466e-3, 2.9112874951168792e-5, 2.307344176494017303e-2
            };


        double[] q =
            {
                1.28426009614491121e00, 4.68238212480865118e-1, 6.59881378689285515e-2,
                3.78239633202758244e-3, 7.29751555083966205e-5
            };
        if (y <= thrsh)
        {
            //
            // Evaluate  anorm  for  |X| <= 0.66291
            //
            xsq = zero;
            if (y > double.Epsilon) xsq = z * z;
            xnum = a[4] * xsq;
            xden = xsq;
            for (i = 0; i < 3; i++)
            {
                xnum = (xnum + a[i]) * xsq;
                xden = (xden + b[i]) * xsq;
            }
            result = z * (xnum + a[3]) / (xden + b[3]);
            double temp = result;
            result = half + temp;
        }

        //
        // Evaluate  anorm  for 0.66291 <= |X| <= sqrt(32)
        //
        else if (y <= root32)
        {
            xnum = c[8] * y;
            xden = y;
            for (i = 0; i < 7; i++)
            {
                xnum = (xnum + c[i]) * y;
                xden = (xden + d[i]) * y;
            }
            result = (xnum + c[7]) / (xden + d[7]);
            xsq = Math.Floor(y * sixten) / sixten;
            del = (y - xsq) * (y + xsq);
            result = Math.Exp(-(xsq * xsq * half)) * Math.Exp(-(del * half)) * result;
            ccum = one - result;
            if (z > zero)
            {
                result = ccum;
            }
        }

        //
        // Evaluate  anorm  for |X| > sqrt(32)
        //
        else
        {
            xsq = one / (z * z);
            xnum = p[5] * xsq;
            xden = xsq;
            for (i = 0; i < 4; i++)
            {
                xnum = (xnum + p[i]) * xsq;
                xden = (xden + q[i]) * xsq;
            }
            result = xsq * (xnum + p[4]) / (xden + q[4]);
            result = (sqrpi - result) / y;
            xsq = Math.Floor(z * sixten) / sixten;
            del = (z - xsq) * (z + xsq);
            result = Math.Exp(-(xsq * xsq * half)) * Math.Exp(-(del * half)) * result;
            ccum = one - result;
            if (z > zero)
            {
                result = ccum;
            }
        }

        if (result < min)
            result = 0.0e0;
        return result;
    }



#if UNITY_WEBGL
    bool uploadedFirst = false;
    string f1 = "";
    public void injectCSV()
    {
        if(!uploadedFirst)
            UploadFile(gameObject.name, "OnFileUploadCSV", ".csv", false);
        else {
            UploadFile(gameObject.name, "OnFileUploadCSVSettings", ".csv", false);
        }
    }
        public void OnFileUploadCSV(string url)
    {
        StartCoroutine(OutputRoutineCSV(url));
    }

        public void OnFileUploadCSVSettings(string url)
    {
        StartCoroutine(OutputRoutineCSVS(url));
    }

    private IEnumerator OutputRoutineCSV(string url)
    {
        var loader = new WWW(url);
        yield return loader;
        f1 = loader.text;
        uploadedFirst = true;
        injectCSV();
    }

    private IEnumerator OutputRoutineCSVS(string url)
    {
        var loader = new WWW(url);
        yield return loader;
        injectData(f1,loader.text);
        uploadedFirst = false;
    }
#endif
#if UNITY_STANDALONE
    public void injectCSV(){
        string filepath = EditorUtility.OpenFilePanel("Load Data To Be Injected","","csv");
        StreamReader reader = new StreamReader(filepath); 
        string csv = reader.ReadToEnd();

        string beh = EditorUtility.OpenFilePanel("Specify Group Behaviour", "", "csv");
        StreamReader r = new StreamReader(beh);
        string csvSettings = r.ReadToEnd();

        reader.Close();
        r.Close();
        injectData(csv,csvSettings);
    }
#endif



    public void injectData(string data,string groupSettings){
        //csv can contain any of the following options


        string first_row = data.Split('\n').First();

 
        //read csv file into dictionary
        //time data has to be in the format of 08:50:00
        //seconds are disregarded and data in fields with them will be lost e.g. 08:50:11 will be lost
        //make sure to group data by hours:minutes
        //first field is time, second field is the value
        //headers are accounted for and removed
        //08:07:00,12.5
        Func<string,double> formatTime = (str) => 
        System.Math.Round(
            double.Parse(string.Join(".",str.Split(':').Take(str.Split(':').Count() - 1)))
            ,2);

        Func<string,double> formatNumber = (str) => 
            double.Parse(str) * avatarToCar;

        Dictionary<double,double> injData = data.Split('\n').Skip(1).Where(x=>x!="")
        .Aggregate(new Dictionary<double,double>(),
                        (d,next)=>{
                            d[formatTime(next.Split(',')[0])] 
                        = formatNumber(next.Split(',')[1]);
                        return d;
                        });
        int carsSum = (int)injData.Sum(x=>x.Value);



        //interpret settings file
        //should be csv file in the format of
        //name, wait_time, time_start, time_end, percentage_affected_within_range,goal_origin
        //last group specifies behaviour of all the leftover agents that have not been specified
        int groupsLen = groupSettings.Split('\n').Skip(1).Count();
        int count = 0;
        foreach (var groupStr in groupSettings.Split('\n').Skip(1))
        {
            count++;
            bool isLast = groupsLen == count;
            Dictionary<double, int> newGroup = new Dictionary<double, int>();
            string[] settingsStr = groupStr.Split(',');
            if (settingsStr.Length < 1) continue;
            string name = settingsStr.Length > 0 && settingsStr[0] != "" ? settingsStr[0] : "Group";
            int wait_time = settingsStr.Length > 1 && settingsStr[1] != "" ? int.Parse(settingsStr[1]) : 1;
            string goal_origin = "none";
            if (!isLast)
            {
                double time_start = settingsStr.Length > 2 && settingsStr[2] != "" ? double.Parse(settingsStr[2]) : 0;
                double time_end = settingsStr.Length > 3 && settingsStr[3] != "" ? double.Parse(settingsStr[3]) : 0;
                int percentage_affected = settingsStr.Length > 4 && settingsStr[4] != "" ? int.Parse(settingsStr[4]) : 0;
                goal_origin = settingsStr.Length > 5 && settingsStr[5] != "" ? settingsStr[5] : "none";
                goal_origin = "none";
                
                //populate list and remove from general data
                double[] ar = injData.Where(x => x.Key > time_start && x.Key < time_end).Select(k => k.Key).ToArray();
                foreach (var entry in ar)
                {
                    if (wait_time == 0)
                    {
                        double carsInBin = (injData[entry] * percentage_affected / 100.0);
                        newGroup[entry] = (int)Math.Round(carsInBin/2);
                        injData[entry] = Math.Max(injData[entry] - carsInBin, 0);
                        
                    }
                    else
                    {
                        double carsInBin = injData[entry] * percentage_affected / 100.0;
                        newGroup[entry] = (int)Math.Round(carsInBin);
                        injData[entry] = Math.Max(injData[entry] - carsInBin, 0);
                        double rmOnExit = Math.Round((entry + wait_time) % 24.0, 2);
                        if (injData.ContainsKey(rmOnExit))
                        {
                            injData[rmOnExit] = Math.Max(injData[rmOnExit] - carsInBin, 0);

                        }
                    }
                }
            }
            else {
                Dictionary<double, double> holder = new Dictionary<double, double>();
                foreach (var item in injData)
                {
                    holder[item.Key] = item.Value;
                }

                //calc in holder
                double[] ar = injData.Keys.ToArray();
                double rollover = 0;
                foreach (var k in ar)
                {
                    double rmOnExit = Math.Round((k + wait_time) % 24.0, 2);

                    if (holder.ContainsKey(rmOnExit))
                    {
 
                        if(wait_time>0)
                        {
                            //expected that rmOnExit is more but if less rollover 
                            double val = (holder[rmOnExit] - holder[k]) - rollover;
                            //rollover = Math.Abs(Math.Min(val, 0));

                            holder[rmOnExit] = Math.Max(val, 0);
                        }

                    }
                }

                //add new group
                newGroup = new Dictionary<double, int>();

                foreach (var item in holder)
                {
                    newGroup.Add(item.Key, (int)Math.Round(item.Value));
                }
            }
            int carsSumGroup = newGroup.Sum(x => x.Value);

            //create and add peaking group with spawns at specific times
            CreateGroup(true, name, 12, wait_time, 1, carsSumGroup, 0, 0
               , "", goal_origin);
            //behaviours should be deliminated
            //setup buckets
            groups.Last().GetComponent<PeakingGroup>().remove();
            groups.Last().GetComponent<PeakingGroup>().injectSpawnerDistribution(newGroup);

        }


    }



}
