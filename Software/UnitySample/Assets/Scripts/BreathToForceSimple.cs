using UnityEngine;
using Random = UnityEngine.Random;

public class BreathToForceSimple : MonoBehaviour
{
    public enum Distribution
    {
        Uniform,
        Normal
    }

    public float breathConeAngle = 30.0f;
    public float breathReach = 2.0f;
    public float forceMultiplier = 1.0f;
    public float distanceMultiplier = 1.0f;
    public Distribution distribution = Distribution.Normal;
    public int numberOfRays = 1;

    ESP32ClientBluetooth clientBluetooth;

    Vector3 mouthOffset = new Vector3(0.0f, -0.06f, 0.0f);

    const float airDensityNormal = 1.225f; // Martin Wolf: kg/m³
    float mantleSegmentConstant = 2 * Mathf.PI * (1 - Mathf.Cos(30.0f * Mathf.PI / 180.0f));

    float remainingForce;
    float minRemainingForce = 0.01f;
    int maxBounces = 5;

    void Start()
    {
        clientBluetooth = FindObjectOfType<ESP32ClientBluetooth>();
    }

    // Martin Wolf: 50 calls per second
    void FixedUpdate()
    {
        float slmValue = (clientBluetooth.CurrentValue - clientBluetooth.Offset) / clientBluetooth.ScaleFactor;
        if (slmValue > 0.5f) // Martin Wolf: Breathe out with a certain minimal strength
        {
            for (int i = 0; i < numberOfRays; i++)
            {
                ApplyForce(slmValue);
            }
        }
    }

    float RandomGaussian(float minValue = -1.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * Random.value - 1.0f;
            v = 2.0f * Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f || S == 0);

        // Martin Wolf: Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Martin Wolf: Normal Distribution centered between the min and max value and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    void ApplyForce(float slmValue)
    {
        remainingForce = 1.0f;
        int bounces = 0;

        Vector2 randomRotation;

        switch (distribution)
        {
            case Distribution.Uniform:
                randomRotation = Random.insideUnitCircle;
                break;
            case Distribution.Normal:
                float angle = Random.value * 360.0f;
                float r = RandomGaussian();
                randomRotation = new Vector2(r * Mathf.Cos(angle * Mathf.PI / 180.0f), r * Mathf.Sin(angle * Mathf.PI / 180.0f));
                break;
            default:
                randomRotation = Random.insideUnitCircle;
                break;
        }

        randomRotation *= breathConeAngle;
        Quaternion q = Quaternion.Euler(randomRotation);
        Vector3 rayDirection = q * transform.forward;

        // Martin Wolf: All layers except Player
        int layerMask = 1 << 10;
        layerMask = ~layerMask;
        float distance = 0.0f;
        Vector3 hitPoint = transform.position + transform.TransformVector(mouthOffset);

        RaycastHit hit;
        if (Physics.Raycast(hitPoint, rayDirection, out hit, breathReach, layerMask))
        {
            distance += hit.distance;
            if (hit.rigidbody)
            {
                PickUp pickUp = hit.transform.GetComponent<PickUp>();
                if (pickUp)
                {
                    if (!pickUp.PickedUp)
                    {
                        hit.rigidbody.AddForceAtPosition(CalculateForce(slmValue, distance * distanceMultiplier, rayDirection, hit.normal, hit.rigidbody), hit.point, ForceMode.Force);
                    }
                }
                else
                {
                    hit.rigidbody.AddForceAtPosition(CalculateForce(slmValue, distance * distanceMultiplier, rayDirection, hit.normal, hit.rigidbody), hit.point, ForceMode.Force);
                }
            }
            else
            {
                remainingForce *= (1.0f - Vector3.Dot(rayDirection, -hit.normal));
            }

            bounces++;
            Vector3 newRayDirection = Vector3.ProjectOnPlane(rayDirection, hit.normal).normalized;
            hitPoint = hit.point - (rayDirection + newRayDirection) / 2.0f * 0.001f; // Martin Wolf: Small offset to prevent ray from glitching through sharp edges
            rayDirection = newRayDirection;

            while (distance > 0.0f && remainingForce >= minRemainingForce && bounces <= maxBounces) // Martin Wolf: Bounce
            {
                if (Physics.Raycast(hitPoint, rayDirection, out hit, breathReach - distance, layerMask))
                {
                    distance += hit.distance;
                    if (hit.rigidbody)
                    {
                        PickUp pickUp = hit.transform.GetComponent<PickUp>();
                        if (pickUp)
                        {
                            if (!pickUp.PickedUp)
                            {
                                hit.rigidbody.AddForceAtPosition(CalculateForce(slmValue, distance * distanceMultiplier, rayDirection, hit.normal, hit.rigidbody), hit.point, ForceMode.Force);
                            }
                        }
                        else
                        {
                            hit.rigidbody.AddForceAtPosition(CalculateForce(slmValue, distance * distanceMultiplier, rayDirection, hit.normal, hit.rigidbody), hit.point, ForceMode.Force);
                        }
                    }
                    else
                    {
                        remainingForce *= (1.0f - Vector3.Dot(rayDirection, -hit.normal));
                    }

                    bounces++;
                    newRayDirection = Vector3.ProjectOnPlane(rayDirection, hit.normal).normalized;
                    hitPoint = hit.point - (rayDirection + newRayDirection) / 2.0f * 0.001f; // Martin Wolf: Small offset to prevent ray from glitching through sharp edges
                    rayDirection = newRayDirection;
                }
                else
                {
                    distance = 0.0f;
                }
            }
        }
    }

    Vector3 CalculateForce(float slmValue, float distance, Vector3 direction, Vector3 surfaceNormal, Rigidbody rb)
    {
        // Martin Wolf: Shell surface of a spherical segment with a cone angle of 30° (breath cone angle of a human) = 2πr² (1 - cos(30)) = about 0,841787 * r² m²
        float mantleSegmentArea = mantleSegmentConstant * distance * distance;
        Vector3 rbVelocity = Vector3.zero;

        if (rb)
        {
            rbVelocity = rb.velocity;
        }

        // Martin Wolf: Flow volume stays the same at distance -> V = Q / A = m³/s / m² -> m/s
        Vector3 velocityAtDistance = direction * (slmValue / 60.0f / 1000.0f) / mantleSegmentArea - rbVelocity; // Martin Wolf: / 60.0f -> per minute to per second, / 1000.0f -> l = dm³ to m³
        float velocityValue = velocityAtDistance.magnitude;
        velocityAtDistance = velocityAtDistance.normalized;
        if (Vector3.Dot(velocityAtDistance, direction) <= 0.0f)
        {
            return Vector3.zero;
        }

        float force = remainingForce * forceMultiplier * airDensityNormal / 2.0f * (mantleSegmentArea / numberOfRays) * velocityValue * velocityValue; // Martin Wolf: kg/m³ * m² * m/s * m/s = kgm/s² = N

        float dot = Vector3.Dot(velocityAtDistance, -surfaceNormal);

        if (dot >= 0.0f)
        {
            remainingForce *= (1.0f - dot);
            return velocityAtDistance * force * dot;
        }
        else
        {
            return Vector3.zero;
        }
    }
}
