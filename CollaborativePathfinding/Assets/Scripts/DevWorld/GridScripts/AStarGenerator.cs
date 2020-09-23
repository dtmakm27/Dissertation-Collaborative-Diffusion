using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Runtime.InteropServices;

//create a data structure that directs you to the next node depending on a goal
//in a grid using A*
public class AStarGenerator
{

    class Node
    {
        public NodeScript parent = null;
        public double h = 0;
        public double g = 0;
        public double f = 0;
    }

    // Pythagoras distance
    // heuristics
    Func<NodeScript, NodeScript[], float> pyDist = (currNode, end) =>
    {
        return end.Aggregate(0.0f, (min, next) => {
            float d1 = (float)Math.Sqrt(((next.transform.position.x - currNode.transform.position.x) * (next.transform.position.x - currNode.transform.position.x))
                              + ((next.transform.position.z - currNode.transform.position.z) * (next.transform.position.z - currNode.transform.position.z)));
            if (d1 < min)
                return d1;
            else
                return min;
        }); 
    };
    // Collaborative diffusion heuristic
    Func<NodeScript, NodeScript, float> CollaborativeDiffusuion = (currNode, end) =>
    {
        return 1 - (float)currNode.GetDiffusion();
    };
    //end of heuristics

    Func<NodeScript, NodeScript[], float> heuristic;

    NodeScript[] allExitGoals;
    NodeScript[] allParkingGoals;
    NodeScript[] grid;

    //cached results from previous paths
    //key in first dict is the start node
    //key in second dict is the goal node
    Dictionary<NodeScript, Dictionary<NodeScript[], List<NodeScript>>> cachedResults;

    float weight = 1.0f;
    public AStarGenerator(GameObject[] nodegrid)
    {
        cachedResults = new Dictionary<NodeScript, Dictionary<NodeScript[], List<NodeScript>>>();
        grid = nodegrid.Select(x => x.GetComponent<NodeScript>()).ToArray();
        heuristic = pyDist;
        allExitGoals = grid.Where(x => x.exit).ToArray();
        allParkingGoals = grid.Where(x => x.goal).ToArray();//and not a parking

    }
    //interface methods

    public List<NodeScript> GenerateEntryPath(NodeScript a, NodeScript[] goals)
    {
        var actualGoals = goals == null
            //park anywhere where it's open
            ? allParkingGoals.Where(x => x.goal)
            //get all goals that are currently open
            : goals.Where(x => x.goal);

        // NodeScript goal = actualGoals.Aggregate(actualGoals.First(),
        //     (best, next) => best = pyDist(next, a) < pyDist(best, a) ? next : best);
        //NodeScript goal = actualGoals.First();
        //foreach (var g in actualGoals)
        //{
           // if( pyDist(a, g) < pyDist(a, goal))
         //       goal = g;
        //}
        return GeneratePath(a, actualGoals.ToArray());
    }

    //List<NodeScript> nullList = new List<NodeScript>();
    public List<NodeScript> GenerateExitPath(NodeScript a)
    {
        
        // NodeScript goal = allExitGoals.First();
        // foreach (var g in allExitGoals)
        // {
        //     if( pyDist(a, g) < pyDist(a, goal))
        //         goal = g;
        // }
       // NodeScript goal;
        //bool nul = false;
        /*
        do
        {
            goal = allExitGoals.Aggregate(allExitGoals.First(),
            (best, next) => best = (!nullList.Contains(next) &&
                 pyDist(next, a) < pyDist(best, a)) ? next : best);

            GeneratePath(a, goal);

            nul = GeneratePath(a, goal)==null;
            if(nul)
                nullList.Add(goal);
        } while (nul);
        */

        return GeneratePath(a, allExitGoals);
    }






    List<NodeScript> GeneratePath(NodeScript start, NodeScript[] goals)
    {
        if (cachedResults.ContainsKey(start))
        {
            if (cachedResults[start].ContainsKey(goals))
            {
                return cachedResults[start][goals];
            }
        }
        //find the path in grid
        var openList = new List<NodeScript>();
        var closedList = new List<NodeScript>();


        Dictionary<NodeScript, Node> nodeScriptToNode = new Dictionary<NodeScript, Node>();
        nodeScriptToNode[start] = new Node
        {
            parent = null,
            g = 0,
            h = heuristic(start, goals) * weight,
            f = 0
        };


        closedList.Add(start);
        AddNeighboursToList(start, openList, closedList, nodeScriptToNode, goals);


        while (openList.Count > 0)
        {
            //find the cheapest node            
            NodeScript cheapestNode = FindCheapestNode(openList, goals, nodeScriptToNode);

            if (goals.Contains(cheapestNode))
            { //backtrack path
                List<NodeScript> path = new List<NodeScript>();
                while (cheapestNode != null)
                {
                    path.Add(cheapestNode);
                    cheapestNode = nodeScriptToNode[cheapestNode].parent;
                }
                path.Reverse();

                if (!cachedResults.ContainsKey(start))
                    cachedResults[start] = new Dictionary<NodeScript[], List<NodeScript>>();

                cachedResults[start][goals] = path;

                return path;
            }

            openList.Remove(cheapestNode);
            closedList.Add(cheapestNode);

            AddNeighboursToList(cheapestNode, openList, closedList, nodeScriptToNode, goals);
        }
        return null;
    }

    //Adds any neighbours of node to the list and updates any neighbours parents if it is a cheaper route
    void AddNeighboursToList(NodeScript node, List<NodeScript> openList, List<NodeScript> closedList,
    Dictionary<NodeScript, Node> nodeScriptToNode, NodeScript[] goals)
    {
        foreach (EdgeScript edge in node.edges)
        {
            if (edge.GetTo().NodeStatus != NodeScript.NODE_STATUS.BLOCKED
                && !closedList.Contains(edge.GetTo()))
            {
                if (!openList.Contains(edge.GetTo()))
                {
                    nodeScriptToNode[edge.GetTo()] =
                        new Node { parent = node };

                    double gc = CalculateGCost(edge.GetTo(), nodeScriptToNode);
                    double hc = heuristic(edge.GetTo(), goals) * weight;
                    nodeScriptToNode[edge.GetTo()].h = hc;
                    nodeScriptToNode[edge.GetTo()].g = gc;
                    nodeScriptToNode[edge.GetTo()].f = hc + gc;
                    openList.Add(edge.GetTo());
                }
                else
                {
                    if (nodeScriptToNode[edge.GetTo()].g >
                     (edge.GetCost() + nodeScriptToNode[edge.GetFrom()].g))
                    {
                        nodeScriptToNode[edge.GetTo()].parent = edge.GetFrom();
                        //update g cost from new parent
                        nodeScriptToNode[edge.GetTo()].g = CalculateGCost(edge.GetTo(), nodeScriptToNode);
                    }
                }
            }
        }
    }

    NodeScript FindCheapestNode(List<NodeScript> openList, NodeScript[] goals,
        Dictionary<NodeScript, Node> nodeScriptToNode)
    {

        NodeScript cheapestNode = openList[0];
        foreach (NodeScript node in openList)
        {
            if (goals.Contains(node))
                return node;
            else if (nodeScriptToNode[node].f < nodeScriptToNode[cheapestNode].f)
                cheapestNode = node;
        }
        return cheapestNode;
    }

    double CalculateGCost(NodeScript node, Dictionary<NodeScript, Node> nodeScriptToNode)
    {
        //distance from strt in term of edge costs
        float gCost = 0;

        while (nodeScriptToNode[node].parent != null)
        {
            gCost += nodeScriptToNode[node].parent.FindEdgeTo(node).GetCost();
            node = nodeScriptToNode[node].parent;
        }

        return gCost;
    }

}
