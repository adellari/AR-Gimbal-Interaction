using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionObject : MonoBehaviour
{
    

    public interactScope scope;
    public List<InteractionAxis> interactions;

    public bool changedScope = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void UpdateOrientation()
    {
        // For the X Ring (Pitch)
        Vector3 xRingForward = -transform.forward;
        Vector3 xRingUp = transform.right;
        var obj = interactions[0].transform;
        obj.rotation = Quaternion.LookRotation(xRingForward, xRingUp);
        obj.GetChild(0).position = obj.position + obj.up * .425f;
        //obj.GetChild(0).GetComponent<InteractionAxis>().relToObj = obj.GetChild(0).position - transform.position;
        obj.GetChild(1).position = obj.position + obj.up * 0.495f;

        // For the Y Ring (Yaw)
        Vector3 yRingForward = transform.forward;
        Vector3 yRingUp = transform.up;
        obj = interactions[1].transform;
        obj.rotation = Quaternion.LookRotation(yRingForward, yRingUp);
        obj.GetChild(0).position = obj.position + obj.up * 0.425f;
        //obj.GetChild(0).GetComponent<InteractionAxis>().relToObj = obj.GetChild(0).position - transform.position;
        obj.GetChild(1).position = obj.position + obj.up * 0.495f;

        // For the Z Ring (Roll)
        Vector3 zRingForward = -transform.up;
        Vector3 zRingUp = transform.forward;
        obj = interactions[2].transform;
        obj.rotation = Quaternion.LookRotation(zRingForward, zRingUp);
        obj.GetChild(0).position = obj.position + obj.up * 0.425f;
        //obj.GetChild(0).GetComponent<InteractionAxis>().relToObj = obj.GetChild(0).position - transform.position;
        obj.GetChild(1).position = obj.position + obj.up * 0.495f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!changedScope){
            //scap axes back to what they should be
            //changedScope = true;
        }

        switch(scope){
            case interactScope.local:
            //Could 100% use meshes with different orientations instead of this but i already implemented this so ehh
            //List<Vector3> bases = new List<Vector3>{transform.forward, -transform.up};

            //interactions[0].transform.LookAt(-transform.forward, transform.right);
            //interactions[1].transform.LookAt(transform.forward, transform. up);
            //interactions[2].transform.LookAt(-transform.up, transform.forward);
            // if(_type == interactType.rotate)
            UpdateOrientation();
            break;

            case interactScope.world:
            break;
        }

    }
}
