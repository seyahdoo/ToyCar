using UnityEngine;

public class ToyCar : MonoBehaviour
{
    public Transform[] tires;
    public Transform[] tireGraphics;
    public Rigidbody body;
    
    private RaycastHit[] hits = new RaycastHit[20];

    public float maxRayDistance = 10f;
    public LayerMask layerMask;
    public float suspensionRestDistance;
    public float suspensionForceMultiplier = 100f;
    public float dampingForceMultiplier = 20f;
    public float tireGripFactor = 1;
    public float tireMass = 1f;
    public float accelerationForceMultiplier = 10f;
    public AnimationCurve accelerationCurve;
    public AnimationCurve brakeCurve;
    public float uprightTorque = 10f;
    public float maxSpeed = 4f;

    public float steeringInput = 0;
    public float accelerationInput = 0;
    public float brakeInput = 0;
    
    private void Update()
    {
        steeringInput = Input.GetAxis("Horizontal");
        accelerationInput = Input.GetAxis("Vertical");
        accelerationInput = Mathf.Clamp01(accelerationInput);
        brakeInput = Mathf.Clamp(Input.GetAxis("Vertical"), -1, 0);
        brakeInput = -brakeInput;
    }

    private void FixedUpdate()
    {
        for (var i = 0; i < tires.Length; i++)
        {
            var tire = tires[i];
            var hitCount = Physics.RaycastNonAlloc(tire.position, Vector3.down, hits, maxRayDistance, layerMask);
            if (hitCount <= 0) continue;
            
            var tirePosition = tire.position;
            var tireVelocity = body.GetPointVelocity(tirePosition);
            var hit = hits[0];
            var distance = hit.distance;
            var suspensionDirection = tire.up;
            tireGraphics[i].position = hit.point;
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

                var absoluteForwardSpeed = Mathf.Abs(currentForwardSpeed);
                var brakeForceMultiplier = brakeCurve.Evaluate(absoluteForwardSpeed);
                var brakeForce = Mathf.Sign(currentForwardSpeed) * brakeForceMultiplier * brakeInput * -accelerationDirection;
                body.AddForceAtPosition(brakeForce, tirePosition);
                
                if (i <= 1)
                {
                    tire.localRotation = Quaternion.Euler(0, steeringInput * 40f, 0);
                    tireGraphics[i].localRotation = Quaternion.Euler(0, steeringInput * 40f, 0);
                }
                if (i >= 2)
                {
                    var forceMultiplier = accelerationCurve.Evaluate(currentForwardSpeed / maxSpeed);
                    var accelerationForce = accelerationDirection * (accelerationInput * forceMultiplier * accelerationForceMultiplier);
                    Debug.Log(accelerationInput * forceMultiplier);
                    body.AddForceAtPosition(accelerationForce, tirePosition);
                }
                
            }

            var lean = Vector3.Angle(body.transform.up, Vector3.up);
            if (lean > 20)
            {
                var rot = Quaternion.FromToRotation(body.transform.up, Vector3.up);
                body.AddTorque(new Vector3(rot.x, rot.y, rot.z) * uprightTorque);
            }
            
        }
    }
}
