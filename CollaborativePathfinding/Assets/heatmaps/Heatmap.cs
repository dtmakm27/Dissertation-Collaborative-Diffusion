using UnityEngine;
using System.Collections;

public class Heatmap : MonoBehaviour
{

    public static float sensitivity = 10f;
    public float sensitivityH = 10f;
    public static bool seperateMapSections = false;
    public bool seperateMapSectionsH = false;


    public Vector4[] positions;
    public Vector4[] properties;

    public Material material;

    public int count = 0;
    public GridManagerScript grid;
    public HeatGroup[] heatGroups;

    public int numberOfGroups = 9;

    public bool noClustering = false;


    void Start()
    {
        sensitivity = sensitivityH;
        seperateMapSections = seperateMapSectionsH;
        if(noClustering){
            heatGroups = grid.GenerateHeatMapGroups();
        }
        else{
            heatGroups = grid.GenerateHeatMapGroups(numberOfGroups);
        }

        positions = new Vector4[heatGroups.Length];
        count = heatGroups.Length;
        properties = new Vector4[heatGroups.Length];
        for (int i = 0; i < heatGroups.Length; i++)
        {
            positions[i] = heatGroups[i].position;
            properties[i] = heatGroups[i].GetProperties();
        }

    }

    // //properties are radius and intensity (-1f,+1f)
    // //positions for x and z only (y is ignored)
    // public void setUpVariables(int count, Vector4[] positions,Vector4[] properties){
    //     this.count = count;
    //     this.positions = positions;
    //     this.properties = properties;
    // }

    void Update()
    {
        if (grid.showHeatmap)
        {
            for (int i = 0; i < count; i++)
            {   
                properties[i] = heatGroups[i].GetProperties();
            }
            material.SetInt("_Points_Length", count);
            material.SetVectorArray("_Points", positions);
            material.SetVectorArray("_Properties", properties);
        }
    }
}