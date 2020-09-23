using System.Collections;
using System.Linq;

using System.Collections.Generic;
using UnityEngine;



public class RedirectionSignScript : MonoBehaviour
{
    NodeScript node;
    NodeScript redirOrigin;
    public float chanceToRedirect = 1f;

    // Start is called before the first frame update
    void Start()
    {


        node = GetComponent<NodeScript>();
        BoxCollider collider = this.GetComponent<BoxCollider>();
        collider.size = new Vector3(6f, 6f, 6f);
        gameObject.layer = 8;
        gameObject.tag = "trafficSign";


    }

    public void ReDirect(DiffusionAvatarScript a) {
        a.redirected = true;
    }

    void Update()
    {


    }

}
