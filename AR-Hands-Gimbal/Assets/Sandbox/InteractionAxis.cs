using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum axis{
    x,
    y,
    z
};

public enum interactType{
    translate,
    rotate,
    scale
};

public enum interactScope {
        local,
        world
    };

public class InteractionAxis : MonoBehaviour
{

    public axis _axis;
    public interactType _type;
    public Transform interactObject;

    public float momentumThreshold = 10000f;
    //current interaction momentum
    private float momentum;
    //the starting interaction point
    public Vector2 sPoint;
    //the starting angle
    public Vector3 sAngle;
    //the starting distance
    public float sDistance;
    //record our initial scale;
    public Vector3 sScale;

    public Quaternion sOrientation;
    //the current interaction point
    public Vector2 cPoint;
    //previous interaction point
    public Vector2 pPoint;
    //initial object position
    public Vector3 sPosition;
    //are we manipulating the interaction object
   
    private bool isInteract;
    private Vector3 relToObj;
    private float relativeDistance;
    public Transform dummy;

    // Start is called before the first frame update
    void Start()
    {
        relToObj = transform.position - interactObject.position;
        relativeDistance = Vector3.Distance(transform.position, interactObject.position);
    }

    void Translate()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(cPoint);
        Vector3 lineDir = (transform.position - interactObject.position).normalized; //or just transform.up since the tip always aligns w line

        Vector3 w0 = interactObject.position - mouseRay.origin;
        Vector3 a = lineDir; // Line direction
        Vector3 b = mouseRay.direction; // Ray direction

        float aDotA = Vector3.Dot(a, a);
        float aDotB = Vector3.Dot(a, b);
        float bDotB = Vector3.Dot(b, b);
        float aDotW0 = Vector3.Dot(a, w0);
        float bDotW0 = Vector3.Dot(b, w0);

        float denom = aDotA * bDotB - aDotB * aDotB;

        if (Mathf.Abs(denom) < 1e-6f)
        {
            // The line and ray are nearly parallel
            // Handle this case separately if necessary
        }
        else
        {
            float s = (aDotB * bDotW0 - bDotB * aDotW0) / denom;
            Vector3 projection = interactObject.position + lineDir * s;
            interactObject.position = projection - (lineDir * relativeDistance); //- lineDir * 1.23f;
            //dummy.position = projection - lineDir * 1.23f;
        }
    }

//axis dot view direction >= 0.5 means it's practically flat and we can do vertical
//a * v == means absolutely aligned and we can do either vertical or horizontal


//x ring is aligned on x and z axis
//if either have a value of 1, it can be either horizontal or vertical 
//it is horizontal when the y axis points up/down 
//it is vertical when the y axis points left/right
//if both have a value of 0, it can move horizontal AND vertical 

//for x axis, if a.z * v == 1 AND a.x * v == 0 -> we only manipulate vertically
//for x axis, if a.z * v == 0 AND a.x * v == 0 -> we only manipulate horizontal or vertically based on where we clicked
//for x axis, if a.z * v == 0 AND a.x * v == 1 -> we only manipulate horizontal 

//y ring is aligned on x and z axis
//if either have a value of 1, it can be either horizontal or vertical 
//it is horizontal when the y axis points up/down 
//it is vertical when the y axis points left/right
//if both have a value of 0, it can move horizontal AND vertical 


//for y axis, if a.z * v == 1 AND a.x * v == 0 -> we only manipulate vertically
//for y axis, if a.z * v == 0 AND a.x * v == 0 -> we only manipulate horizontal or vertically based on where we clicked
//for y axis, if a.z * v || a.x * v == 1 AND a.y * v == 0 -> we only manipulate horizontal or 



//a.y * v = 

    void calcDirection(Vector3 a)
    {
        
        //keep in mind that vector dot is not linear and .7 doesn't denote 70% alignment
        //check whehter we're aligned
        if(a.y >= 0.7f)
        {
            Debug.Log("parallel " + Time.realtimeSinceStartup);
            //we're aligned and can use up/down and right/left
        }

        //we're either horizontal or vertical
        if (a.x >= .6f || a.z >= .6f)
        {
            //check the y axis
            var upwards = Mathf.Abs(Vector3.Dot(Camera.main.transform.right, transform.up));
            if(upwards >= .6f)
            {
                Debug.Log("vertical " + Time.realtimeSinceStartup);
                //we're vertical
            }
            else //make bounds maybe <= 0.3 to call that horizontal
            {
                Debug.Log("horizontal " + Time.realtimeSinceStartup);
                //we're close to horizontal
            }
        }
    }

    void Rotate() 
    {
        Vector2 mouseDelta = cPoint - sPoint;

    // This is our constant rotation axis
    Vector3 rotationAxis = transform.up;

    // Project the ring's transform.up onto the camera's view plane
    Vector3 projectedUp = Vector3.ProjectOnPlane(rotationAxis, Camera.main.transform.forward);
    projectedUp.Normalize();

    // Compute the influence of mouse's x and y movements
    float xInfluence = Mathf.Abs(projectedUp.y);
    float yInfluence = Mathf.Abs(projectedUp.x);

    // Calculate rotation amount based on mouse movement and ring's orientation
    float sensitivity = 0.5f;
    float rotationAmount = (mouseDelta.x * xInfluence + mouseDelta.y * yInfluence) * sensitivity;

    // Compute the delta rotation
    Quaternion rotationDelta = Quaternion.AngleAxis(rotationAmount, rotationAxis);

    // Apply the rotation to the initial orientation
    interactObject.rotation = rotationDelta * sOrientation;
    }

    Vector3 StartToWorldPoint(Vector2 point) 
    {
        Ray ray = Camera.main.ScreenPointToRay(point);
        return ray.origin + ray.direction * 10f;  // Adjust distance as needed
    }

    void Rotates()
    {
        //Debug.Log("should be rotation");
        float dVal, dAngle;
        Quaternion delta;

        var v = Camera.main.transform.forward;
        //dx, dy, dz
        Vector3 align = new Vector3(Mathf.Abs(Vector3.Dot(v, transform.right)), Mathf.Abs(Vector3.Dot(v, transform.up)), Mathf.Abs(Vector3.Dot(v, transform.forward)));
        //calcDirection(align);

        switch(_axis)
        {
            case axis.x:
                Debug.Log("x axis rotation: " + align);
                dVal = cPoint.y - sPoint.y;

                //we need to scale -200 -> 200 into 0 -> 400
                dAngle = Mathf.Lerp(-180f, 180f, (dVal + 200f)/ 400f);
                delta = Quaternion.AngleAxis(dVal, interactObject.right);
                //interactObject.eulerAngles = new Vector3(sAngle.x, sAngle.y + dAngle, sAngle.z);
                interactObject.rotation = delta * sOrientation;
                //Debug.Log(dAngle);
            break;

            case axis.y:
                Debug.Log("y axis rotation: " + align);
                dVal = cPoint.x - sPoint.x;
                //calculate the angle delta

                //we need to scale -200 -> 200 into 0 -> 400
                dAngle = Mathf.Lerp(-180f, 180f, (dVal + 200f)/ 400f);
                delta = Quaternion.AngleAxis(dVal, interactObject.up);
                //interactObject.eulerAngles = new Vector3(sAngle.x, sAngle.y + dAngle, sAngle.z);
                interactObject.rotation = delta * sOrientation;
                //interactObject.RotateAround(interactObject.position, Vector3.up, valX);
                //Debug.Log(dAngle);
            break;

            case axis.z:
                Debug.Log("z axis rotaiton");
            break;
        }
        calcDirection(align);

        //make the bounds for rotation relative to the screen size
        //you can only between -180 and 180 from ex. left to right
    }

    void Scale()
    {
        Vector3 ray;
        float alignFactor;
        bool positive;
        //we should store the mouse position at scale start from the center

        // check screen point's alignment with the scaling axis, 
        // if aligned and positive distance, we're scaling up, otherwise we're scaling down to 0

        //if we're disaligned,

        Vector2 onScreen = Camera.main.WorldToScreenPoint(interactObject.position);
        //Debug.Log(cPoint  - onScreen);
        ray = cPoint - onScreen;
        var b = sPoint - onScreen;
        //Are we engaging negative growth?
        alignFactor = Vector3.Dot(b.normalized, ray.normalized);
        float distFactor = Vector2.Distance(cPoint, onScreen) / Vector2.Distance(sPoint, onScreen);
        float dir = Mathf.Abs(alignFactor) * alignFactor;
        //can caculate another dot from the starting point to where the mouse is to see whether we should be shrinking or growing
        //Debug.Log("scaling x: " + alignFactor);
        float factor = distFactor * dir;
        Debug.Log("distance: " + factor);
        
        //multiply the distance by sign of 
        Vector3 _scale = Mathf.Exp(3f*Mathf.Abs(factor) - 3f) * sScale * (factor / Mathf.Abs(factor));  
        switch(_axis)
        {
            case axis.x:

            //Debug.Log(cPoint);
            interactObject.localScale = new Vector3(_scale.x, sScale.y, sScale.z);

            //multiply the distance 
            break;

            case axis.y:

            //Debug.Log(cPoint);
            interactObject.localScale = new Vector3(sScale.x, _scale.y, sScale.z);

            //multiply the distance 
            break;

            case axis.z:

            //Debug.Log(cPoint);
            interactObject.localScale = new Vector3(sScale.x, sScale.y, _scale.z);

            //multiply the distance 
            break;

        }
    }

    //InteractionDriver.onUpdateInteraction.AddListener(Invoke);
    //InteractionDriver.onUpdateInteraction.RemoveListener(Invoke);
    public void Invoke (Vector2 input)
    {
        //Debug.Log("invoking...");
        cPoint = input;
        
        switch(_type){
            case interactType.translate:
                Translate();
            break;
            case interactType.rotate:
                Rotate();
            break;
            case interactType.scale:
                Scale();
            break;

        }
    }

    //tell us when we're entering/exiting direct interaction
    public void stateUpdate(bool state)
    {
            
        if (state)
        {
            isInteract = true;
            cPoint = sPoint;
            //sPoint = cPoint;
            sAngle = interactObject.eulerAngles;
            sOrientation = interactObject.rotation;
            sPosition = interactObject.position;
            pPoint = sPoint;
            sScale = interactObject.localScale;
            sDistance = Vector3.Distance(Camera.main.ScreenToWorldPoint(cPoint), interactObject.position);
            //Debug.Log("stored pose: " + Time.realtimeSinceStartup);
        } 
        else if(!state && momentum < momentumThreshold)
         isInteract = false;
         else
         isInteract = false;
         //call momentum residual function

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_type == interactType.rotate)
        transform.position = interactObject.position + relToObj;
    }
}
