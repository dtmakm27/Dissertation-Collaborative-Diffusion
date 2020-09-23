using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarScript {

    bool paused;                  // Is the avatar paused
    public GameObject avatar;     // The game object part of the avatar
    List<NodeScript> nodePath;    // The path to follow
    ushort pathIndex;             // Current index of the path
    float speed = 2.0f;           // The speed of the avatar while moving between nodes //max speed



    // Use this for initialization
    void Start() {
        
    }

    // Update is called once per frame
    public void Update() {

        if (!paused)
        {
            // Move the avatar in the direction of the next node
            avatar.transform.position += ((nodePath[pathIndex].transform.position - avatar.transform.position).normalized * speed);

            // Check if the avatar is within the bounds of the next node's centre
            if (avatar.transform.position.x >= nodePath[pathIndex].transform.position.x - 1.0f &&
               avatar.transform.position.y >= nodePath[pathIndex].transform.position.y - 1.0f &&
               avatar.transform.position.z >= nodePath[pathIndex].transform.position.z - 1.0f &&
               avatar.transform.position.x <= nodePath[pathIndex].transform.position.x + 1.0f &&
               avatar.transform.position.y <= nodePath[pathIndex].transform.position.y + 1.0f &&
               avatar.transform.position.z <= nodePath[pathIndex].transform.position.z + 1.0f)
            {
                // Increase the path index
                ++pathIndex;

                // If the end is reached return to the first node and
                // follow the path again.
                if (pathIndex == nodePath.Count)
                {
                    pathIndex = 0;
                    avatar.transform.position = nodePath[pathIndex].transform.position;
                }
            }
        }
    }

    // Sets up the avatar ready for use in simulation.
    public void Setup()
    {
        avatar = GameObject.CreatePrimitive(PrimitiveType.Cube);    // Creates a cube for the avatar script to use as visual reprisentation
        avatar.layer = 8;                                           // Sets the noe to the Non-blocking layer
        avatar.transform.position = new Vector3(0, 0, 0);
        avatar.name = "Avatar";                                     // Sets the name to Avatar 
        avatar.AddComponent<Rigidbody>();                           // Adds a rigid body and removes gravity, collisions, movement in the y axis
        avatar.GetComponent<Rigidbody>().useGravity = false;        // and rotation in both the x and z axis
        avatar.GetComponent<Rigidbody>().isKinematic = false;
        avatar.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX |
                                                       RigidbodyConstraints.FreezeRotationZ;
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
    }

    // Gives the avatar a new path
    public void SetPath(Path newPath)
    {
        nodePath = new List<NodeScript>(newPath.pathNodes);
        pathIndex = 0;
    }

    //  Sets the avatar position using x, y and z parameters
    public void SetPosition(float x, float y, float z)
    {
        avatar.transform.position = new Vector3(x, y, z);
    }

    //  Sets the avatar position using a Vector3
    public void SetPosition(Vector3 position)
    {
        avatar.transform.position = new Vector3(position.x, position.y, position.z);
    }

    // Sets isPaused
    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
    }
}
