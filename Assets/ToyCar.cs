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
    [Range(0,1)] public float tireGripFactor = 1;
    public float tireMass = 1f;
    public float accelerationForceMultiplier = 10f;
    public AnimationCurve accelerationCurve;
    public AnimationCurve brakeCurve;
    public float uprightTorque = 10f;
    public float maxSpeed = 4f;
    public float tireRadius = .1f;
    public AnimationCurve leanFixCurve;

    public float steeringInput = 0;
    public float accelerationInput = 0;
    public float brakeInput = 0;
    
    private Rigidbody body;


    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        steeringInput = Input.GetAxis("Horizontal");
        accelerationInput = Input.GetAxis("Vertical");
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < tires.Length; i++)
        {
            var tire = tires[i];
            var hitCount = Physics.RaycastNonAlloc(tire.position, Vector3.down, hits, maxRayDistance, layerMask);
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
                var suspensionForce = offset * suspensionForceMultiplier;
                body.AddForceAtPosition(suspensionForce * suspensionDirection, tirePosition);

                var upwardsVelocity = Vector3.Dot(suspensionDirection, tireVelocity);
                var dampingForce = -upwardsVelocity * dampingForceMultiplier;
                body.AddForceAtPosition(dampingForce * suspensionDirection, tirePosition);
                
                var steeringDirection = tire.right;
                var steeringVelocity = Vector3.Dot(steeringDirection, tireVelocity);
                var desiredVelocityChange = -steeringVelocity * tireGripFactor;
                var desiredAcceleration = desiredVelocityChange / Time.fixedDeltaTime;
                body.AddForceAtPosition(steeringDirection * (tireMass * desiredAcceleration), tirePosition);
                
                var accelerationDirection = tire.forward;
                var currentForwardSpeed = Vector3.Dot(accelerationDirection, tireVelocity);

                var forceMultiplier = accelerationCurve.Evaluate(currentForwardSpeed / maxSpeed);
                var accelerationForce = accelerationDirection * (accelerationInput * forceMultiplier * accelerationForceMultiplier);
                body.AddForceAtPosition(accelerationForce, tirePosition);
                
                if (i <= 1)
                {
                    tire.localRotation = Quaternion.Euler(0, steeringInput * 40f, 0);
                }
                
            }

            var lean = Vector3.Angle(body.transform.up, Vector3.up);
            var leanFixNormalized = leanFixCurve.Evaluate(lean / 90);
            var rotation = Quaternion.FromToRotation(body.transform.up, Vector3.up);
            body.AddTorque(new Vector3(rotation.x, rotation.y, rotation.z) * (leanFixNormalized * uprightTorque));
        }
    }
}
