using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Test1Diffusion : MonoBehaviour
{
    public GridManagerScript grid;
    public bool createdTestScenario = false;
    public bool spawnedAvatars = false;
    public string blocksToBlockFile = "blocksToBlockT1.txt";
    NodeScript startBlock;
    public int numOfAvatars = 50;
    // Start is called before the first frame update
    void Start()
    {
        createdTestScenario = false;
        spawnedAvatars = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (grid && grid.autoGenerateGrid && grid.gridCreated && createdTestScenario==false)
        {
            createdTestScenario = true;
            //create scenario
            string block;
            StreamReader reader = new StreamReader(blocksToBlockFile);
            while ((block = reader.ReadLine()) != null)
            {
                if (block.Contains("end="))
                {
                    block = block.Replace("end=", "");
                    int endarray = int.Parse(block);
                    grid.diffusionMode = true;
                    NodeScript nd = grid.SetEnd(endarray);
                    nd.diffusion = nd.goalDiffusion;
                }
                else if (block.Contains("start=")) {
                    block = block.Replace("start=", "");
                    int startarr = int.Parse(block);
                    startBlock = grid.nodeGrid[startarr].GetComponent<NodeScript>();
                }
                else
                {
                    int blockArrNum = int.Parse(block);
                    grid.SetBlocker(blockArrNum);
                }
            }
            reader.Close();
        }

        //spawn avatars
        if (grid && grid.autoGenerateGrid && grid.gridCreated && spawnedAvatars == false && createdTestScenario == true)
        {
            spawnedAvatars = true;
            StartCoroutine(MySpawnCoroutine());


           
        }


    }

    IEnumerator MySpawnCoroutine()
    {
        for (int i = 0; i < numOfAvatars; i++)
        {
            grid.SpawnDiffusionAvatar(startBlock);
            yield return 20;
        }

    }
}
