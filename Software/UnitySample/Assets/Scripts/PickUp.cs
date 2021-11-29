using UnityEngine;

public class PickUp : MonoBehaviour
{
    public AudioSource impactAudioSource;
    bool pickedUp = false;
    public bool PickedUp { get { return pickedUp; } set { pickedUp = value; } }

    GameObject controllerObject;
    public GameObject ControllerObject { get { return controllerObject; } set { controllerObject = value; } }

    protected OVRInput.Controller controller;
    public OVRInput.Controller Controller { get { return controller; } set { controller = value; } }

    Rigidbody rb;

    public Rigidbody RB { get { return rb; } }

    bool lastPickedUp = false;

    float minSoundVelocity = 0.2f;
    float maxSoundVelocity = 0.6f;

    Vector3 lastPos;
    Quaternion lastRot;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastPos = transform.position;
        lastRot = transform.rotation;
    }

    // Martin Wolf: 50 calls per second
    private void FixedUpdate()
    {
        if (lastPickedUp != pickedUp)
        {
            if (rb)
            {
                if (pickedUp)
                {
                    rb.useGravity = false;
                    rb.isKinematic = true;
                }
                else
                {
                    rb.useGravity = true;
                    rb.isKinematic = false;
                }
            }

            lastPickedUp = pickedUp;
        }

        if (pickedUp)
        {
            transform.position = ControllerObject.transform.position;
            transform.rotation = ControllerObject.transform.rotation;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (impactAudioSource && !impactAudioSource.isPlaying && !pickedUp && collision.relativeVelocity.magnitude > minSoundVelocity)
        {
            impactAudioSource.volume = Mathf.InverseLerp(minSoundVelocity, maxSoundVelocity, collision.relativeVelocity.magnitude);

            impactAudioSource.Play();
        }
    }
}
