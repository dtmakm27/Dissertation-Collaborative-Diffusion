using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetButtonSCript : MonoBehaviour
{

    public GridManagerScript gridManager;
    public ParkingLotManager parkingManager;


    public void ResetPressed()
    {
        gridManager.RemoveDiffusionAvatars();
        parkingManager.ResetParkingLots();
        gridManager.ResetAvatarData();
        gridManager.ResetTime();
    }
}