using UnityEngine;

public class HandTrackingPlusControllerHelper : MonoBehaviour
{
    public OVRInput.Controller controller;
    public LineRenderer lineRenderer;
    public GameObject dot;

    void Update()
    {
        if (OVRInput.IsControllerConnected(controller))
        {
            lineRenderer.enabled = true;
            dot.SetActive(true);
        }
        else
        {
            lineRenderer.enabled = false;
            dot.SetActive(false);
        }
    }
}
