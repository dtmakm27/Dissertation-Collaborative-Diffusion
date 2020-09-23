using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class HeatGroup
{
   public NodeScript[] elements;
   //seperate the groups for each to take totals from each area

   public readonly Vector4 position;

   public float radius;

   public MapNodeGroup cityArea;
    public HeatGroup(NodeScript element, Vector3 position,float radius,MapNodeGroup cityArea){
        this.elements = new NodeScript[]{element};
        this.position = new Vector4(position.x,2,position.z,0);
        this.radius = radius;
        this.cityArea = cityArea;
         if(cityArea == null)
            this.cityArea = GridManagerScript.mapSections[0];
    }

   public HeatGroup(NodeScript[] elements, Vector3 position,float radius,MapNodeGroup cityArea){
       this.elements = elements;
       this.position = new Vector4(position.x,2,position.z,0);
       this.radius = radius;
       this.cityArea = cityArea;
        if(cityArea == null)
            this.cityArea = GridManagerScript.mapSections[0];
   }

   public float getIntensity(){
       if(Heatmap.seperateMapSections)
        return (elements.Sum(x=>x.overlappedTotal-1)*1f/cityArea.overlappedTotal*1f)*Heatmap.sensitivity;
       else
        return (elements.Sum(x=>x.overlappedTotal-1)*1f/GridManagerScript.
            mapSections.Sum(x=>x.overlappedTotal)*1f)*Heatmap.sensitivity;
   }

   public float GetRadius(){
       return radius;
   }

   public Vector4 GetProperties(){
       return new Vector4(GetRadius(),getIntensity(),0,0);
   }

   


}
