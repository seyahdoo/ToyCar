using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ToyCar : MonoBehaviour
{
    public Transform[] tires;
    public Transform[] tireGraphics;
    
    private RaycastHit[] hits = new RaycastHit[20];

    public float maxRayDistance = 10f;
    public LayerMask layerMask;
    public float suspensionRestDistance;
    public float suspensionForceMultiplier = 100f;
    public float dampingForceMultiplier = 20f;
    public AnimationCurve sideSpeedToTireGripFrontCurve;
    public AnimationCurve sideSpeedToTireGripBackCurve;
    public float tireMass = 1f;
    public float accelerationForceMultiplier = 10f;
    public AnimationCurve accelerationCurve;
    public AnimationCurve brakeCurve;
    public float uprightTorque = 10f;
    public float maxSpeed = 4f;
    public float tireRadius = .1f;
    public AnimationCurve leanFixCurve;
    public AnimationCurve speedToTurnRadiusCurve;
    public float maxTurnRadius = 40;

    public float steeringInput = 0;
    public float accelerationInput = 0;
    public float brakeInput = 0;
    
    private Rigidbody body;

    private Pose startPose;

    private void Awake()
    {
        startPose.position = transform.position;
        startPose.rotation = transform.rotation;
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        steeringInput = Input.GetAxis("Horizontal");
        accelerationInput = Input.GetAxis("Vertical");
        if (Input.GetKeyDown(KeyCode.R))
        {
            body.position = startPose.position;
            body.rotation = startPose.rotation;
            body.angularVelocity = Vector3.zero;
            body.velocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < tires.Length; i++)
        {
            var tire = tires[i];
            var hitCount = Physics.RaycastNonAlloc(tire.position, -tire.up, hits, maxRayDistance, layerMask);
            if (hitCount <= 0) continue;
            
            var hit = hits[0];
            var tirePosition = hit.point;
            var tireVelocity = body.GetPointVelocity(tirePosition);
            var distance = hit.distance;
            var suspensionDirection = tire.up;
            tireGraphics[i].position = hit.point + (Vector3.up * (tireRadius * .5f));
            var offset = suspensionRestDistance - distance;
            if (offset > 0)
            {
                //suspension
                var suspensionForce = offset * suspensionForceMultiplier;
                body.AddForceAtPosition(suspensionForce * suspensionDirection, tirePosition);

                //damping
                var upwardsVelocity = Vector3.Dot(suspensionDirection, tireVelocity);
                var dampingForce = -upwardsVelocity * dampingForceMultiplier;
                body.AddForceAtPosition(dampingForce * suspensionDirection, tirePosition);
                
                //acceleration
                var accelerationDirection = tire.forward;
                var currentForwardSpeed = Vector3.Dot(accelerationDirection, tireVelocity);
                var forceMultiplier = accelerationCurve.Evaluate(currentForwardSpeed / maxSpeed);
                var accelerationForce = accelerationDirection * (accelerationInput * forceMultiplier * accelerationForceMultiplier);
                body.AddForceAtPosition(accelerationForce, tirePosition);
                
                //steering
                var steeringDirection = tire.right;
                var steeringVelocity = Vector3.Dot(steeringDirection, tireVelocity);
                var steeringFactor = steeringVelocity / (currentForwardSpeed + steeringVelocity);
                if (float.IsNaN(steeringFactor))
                {
                    steeringFactor = 0;
                }
                var tireGripFactor = i <= 1
                    ? sideSpeedToTireGripFrontCurve.Evaluate(steeringFactor)
                    : sideSpeedToTireGripBackCurve.Evaluate(steeringFactor);
                var desiredVelocityChange = -steeringVelocity * tireGripFactor;
                var desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
                body.AddForceAtPosition(steeringDirection * (tireMass * desiredAcceleration), tirePosition);
                if (i <= 1)
                {
                    var turnRadius = speedToTurnRadiusCurve.Evaluate(currentForwardSpeed / maxSpeed) * maxTurnRadius;
                    tire.localRotation = Quaternion.Euler(0, steeringInput * turnRadius, 0);
                }
                
            }
        }
        
        //prevent rolling
        var lean = Vector3.Angle(body.transform.up, Vector3.up);
        var leanFixNormalized = leanFixCurve.Evaluate(lean / 90);
        var rotation = Quaternion.FromToRotation(body.transform.up, Vector3.up);
        body.AddTorque(new Vector3(rotation.x, rotation.y, rotation.z) * (leanFixNormalized * uprightTorque));
    }
}
