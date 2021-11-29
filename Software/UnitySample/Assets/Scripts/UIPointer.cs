using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPointer : MonoBehaviour
{
    public GameObject controllerObject;
    public OVRInput.Controller controller;
    public Camera raycastCamera; // Martin Wolf: Main camera -> is placed at the position of the pointer to execute a raycast and then reset to the former position before the next rendering
    public LineRenderer lineRenderer;
    public MeshRenderer dot;
    public OvrAvatar avatar;
    public float length = 10.0f;

    float handLength = 0.07f;

    GameObject clickedObject = null;
    PointerEventData pointer;

    List<RaycastResult> results;

    PickUp attachedPickUp = null;

    void Awake()
    {
        results = new List<RaycastResult>();
        pointer = new PointerEventData(EventSystem.current);
    }

    void Update()
    {
        if (attachedPickUp)
        {
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, controller))
            {
                if (controller == OVRInput.Controller.LTouch)
                {
                    avatar.HandLeft.enabled = true;
                }
                else if (controller == OVRInput.Controller.RTouch)
                {
                    avatar.HandRight.enabled = true;
                }

                attachedPickUp.PickedUp = false;
                attachedPickUp.ControllerObject = null;
                attachedPickUp.Controller = OVRInput.Controller.None;
                attachedPickUp = null;
                dot.enabled = true;
                lineRenderer.positionCount = 2;
            }
        }
        else
        {
            RaycastHit hit;

            float currentLength = length;
            GameObject currentObject = null;

            if (Physics.Raycast(transform.position + transform.TransformDirection(Vector3.forward) * handLength, transform.TransformDirection(Vector3.forward), out hit, length))
            {
                currentLength = hit.distance;
                currentObject = hit.transform.gameObject;
            }

            Vector3 raycastCamPos = raycastCamera.transform.position + transform.TransformDirection(Vector3.forward) * handLength;
            Quaternion raycastCamRot = raycastCamera.transform.rotation;

            raycastCamera.transform.position = transform.position;
            raycastCamera.transform.rotation = transform.rotation;

            pointer.Reset();
            pointer.position = new Vector2(raycastCamera.pixelWidth / 2.0f, raycastCamera.pixelHeight / 2.0f);
            pointer.pressPosition = pointer.position;
            pointer.delta = Vector2.zero;
            pointer.scrollDelta = Vector2.zero;

            results.Clear();

            EventSystem.current.RaycastAll(pointer, results);

            bool pointerInvalid = true;
            bool currentObjectIsInteractable = false;

            // Martin Wolf: Get the closest interactable raycast result in front of the pointer
            if (results.Count > 0)
            {
                foreach (RaycastResult result in results)
                {
                    float dist = (transform.position - result.worldPosition).magnitude;
                    if (result.isValid && dist <= currentLength && (result.worldPosition - (transform.position + transform.TransformDirection(Vector3.forward) * dist)).magnitude <= 0.001f)
                    {
                        GameObject usedGameObject = result.gameObject;

                        if (usedGameObject.name == "Checkmark")
                        {
                            usedGameObject = usedGameObject.transform.parent.gameObject;
                        }

                        if (usedGameObject.name == "Background")
                        {
                            usedGameObject = usedGameObject.transform.parent.gameObject;
                        }

                        if (usedGameObject.GetComponent<Selectable>())
                        {
                            currentObjectIsInteractable = true;
                        }
                        else if (currentObjectIsInteractable)
                        {
                            continue;
                        }

                        currentLength = dist;
                        currentObject = usedGameObject;
                        pointer.pointerCurrentRaycast = result;
                        pointerInvalid = false;
                    }
                }
            }

            lineRenderer.SetPosition(0, transform.position + transform.TransformDirection(Vector3.forward) * handLength);

            if (pointerInvalid)
            {
                lineRenderer.SetPosition(1, transform.position + transform.TransformDirection(Vector3.forward) * currentLength);
                dot.transform.position = transform.position + transform.TransformDirection(Vector3.forward) * currentLength;

                if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, controller))
                {
                    if (currentObject)
                    {
                        PickUp pickUp = currentObject.GetComponent<PickUp>();
                        if (pickUp && !pickUp.PickedUp)
                        {
                            if (controller == OVRInput.Controller.LTouch)
                            {
                                avatar.HandLeft.enabled = false;
                                avatar.HandLeft.gameObject.transform.position = new Vector3(-100.0f, -100.0f, -100.0f);
                            }
                            else if (controller == OVRInput.Controller.RTouch)
                            {
                                avatar.HandRight.enabled = false;
                                avatar.HandRight.gameObject.transform.position = new Vector3(-100.0f, -100.0f, -100.0f);
                            }

                            pickUp.PickedUp = true;
                            pickUp.ControllerObject = controllerObject;
                            pickUp.Controller = controller;
                            attachedPickUp = pickUp;
                            dot.enabled = false;
                            lineRenderer.positionCount = 0;
                        }
                    }
                }
            }
            else
            {
                lineRenderer.SetPosition(1, pointer.pointerCurrentRaycast.worldPosition);
                dot.transform.position = pointer.pointerCurrentRaycast.worldPosition;

                // Martin Wolf: Initialize Pointer Click Events according to hit UI object
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
                {
                    if (currentObjectIsInteractable && !clickedObject)
                    {
                        clickedObject = currentObject;

                        currentObject = ExecuteEvents.ExecuteHierarchy(clickedObject, pointer, ExecuteEvents.selectHandler);
                        if (currentObject != null)
                        {
                            EventSystem.current.SetSelectedGameObject(currentObject);
                            clickedObject = currentObject;
                        }

                        pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;

                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.pointerEnterHandler);
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.beginDragHandler);
                    }
                }
                else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller))
                {
                    if (clickedObject)
                    {
                        pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.dragHandler);
                    }
                }
                else
                {
                    if (clickedObject)
                    {
                        pointer.pointerPressRaycast = pointer.pointerCurrentRaycast;

                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.endDragHandler);
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.pointerUpHandler);
                        ExecuteEvents.Execute(clickedObject, pointer, ExecuteEvents.pointerExitHandler);

                        EventSystem.current.SetSelectedGameObject(null);

                        clickedObject = null;
                    }
                }
            }

            raycastCamera.transform.position = raycastCamPos;
            raycastCamera.transform.rotation = raycastCamRot;
        }
    }
}
