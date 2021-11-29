using UnityEngine;

[ExecuteAlways]
public class BlobshadowPositioner : MonoBehaviour
{
    [SerializeField] private Transform followTarget;

    void Start()
    {
    }

    void Update()
    {
        transform.position = followTarget.position;
    }
}