using System;
using UnityEngine;

namespace UltimateReplay.Demo
{

    public sealed class CarController : MonoBehaviour
    {
        // Type
        [Serializable]
        public sealed class CarWheel
        {
            // Internal
            internal Rigidbody rb = null;
            internal float steerAngle = 0f;
            internal Vector3 hitPosition = Vector3.zero;
            internal Vector3 hitNormal = Vector3.zero;
            internal float hitHeight = 0f;
            internal bool grounded = false;
            internal string groundedTag = null;
            internal float wheelAngleVelocity = 0f;
            internal float minLength = 0f;
            internal float maxLength = 0f;
            internal float lastLength = 0f;
            internal float springLength = 0f;
            internal float springVelocity = 0f;
            internal float springForce = 0f;
            internal float damperForce = 0f;

            // Public
            public const float springRestLength = 0.18f;
            public const float springMaxTravel = 0.2f;
            public const float springStiffness = 55000;
            public const float damperStiffness = 2750;

            public Transform wheelPivot;
            public Transform wheelVisual;
            public float wheelRadius = 0.5f;

            // Methods
            public void UpdateWheelPhysics()
            {
                minLength = springRestLength - springMaxTravel;
                maxLength = springRestLength + springMaxTravel;

                float suspensionLength = springMaxTravel;

                Vector3 thisUp = wheelPivot.up;

                // Raycast wheel
                if (Physics.Raycast(wheelPivot.position, -thisUp, out RaycastHit hit, maxLength + suspensionLength) == true)
                {
                    lastLength = springLength;
                    springLength = hit.distance - suspensionLength;
                    springLength = Mathf.Clamp(springLength, minLength, maxLength);
                    springVelocity = (lastLength - springLength) / Time.fixedDeltaTime;
                    springForce = springStiffness * (springRestLength - springLength);
                    damperForce = damperStiffness * springVelocity;
                    Vector3 force = thisUp * (springForce + damperForce);
                    rb.AddForceAtPosition(force, hit.point);
                    hitPosition = hit.point;
                    hitNormal = hit.normal;
                    hitHeight = hit.distance;
                    grounded = true;
                    groundedTag = hit.transform.tag;
                }
                else
                {
                    grounded = false;
                    hitHeight = springMaxTravel + springRestLength;
                    groundedTag = "";
                }
            }

            public void UpdateWheelVisuals(bool steerWheel)
            {
                if (steerWheel == true)
                {
                    wheelAngleVelocity = Mathf.Lerp(wheelAngleVelocity, steerAngle, 15 * Time.deltaTime);
                    wheelPivot.localRotation = Quaternion.Euler(Vector3.up * wheelAngleVelocity);
                }
            }
        }

        // Internal
        internal Transform thisTransform = null;
        internal Rigidbody rb = null;

        // Private
        private Vector3 suspensionForward = Vector3.zero;
        private float dir = 0f;
        private float yawGripThreshold = 0.6f;
        private float yawGripMultiplier = 0.15f;
        private bool grounded = false;
        private float speed = 0f;

        // Public
        public Transform centerOfMass;
        public CarWheel[] wheels;

        public float engineForce = 5000;
        public float drag = 3.5f;
        public float rollingFriction = 105;
        public float brakeForce = 3000;

        [NonSerialized]
        public float throttle = 0f;
        [NonSerialized]
        public float steering = 0f;

        // Properties
        public Rigidbody RB
        {
            get { return rb; }
        }

        // Methods
        private void Awake()
        {
            thisTransform = transform;
            rb = GetComponent<Rigidbody>();

            // Set center of mass
            if (centerOfMass != null)
                rb.centerOfMass = centerOfMass.localPosition;

            // Setup all wheels
            SetupWheels();
        }

        private void Update()
        {
            // Get inputs
            throttle = Input.GetAxis("Vertical");
            steering = Input.GetAxis("Horizontal");

            UpdateWheelVisuals();
            UpdateGrounded();
            UpdateSteering();
        }

        private void FixedUpdate()
        {
            UpdateCarPhysics();
        }

        private void SetupWheels()
        {
            foreach (CarWheel wheel in wheels)
            {
                // Initialize wheel
                wheel.rb = rb;

                // Reset position
                wheel.wheelVisual.transform.localEulerAngles = Vector3.zero;
                wheel.wheelVisual.transform.localRotation = Quaternion.identity;
            }
        }

        private void UpdateWheelVisuals()
        {
            Transform wheelTransform;

            int index = 0;
            foreach (CarWheel wheel in wheels)
            {
                // Get wheel transform
                wheelTransform = wheel.wheelVisual;

                // Calculate wheel target
                float length = wheel.wheelRadius;
                float y = Mathf.Lerp(wheelTransform.localPosition.y, -wheel.hitHeight + length, Time.deltaTime * 20f);

                // Update wheel visuals
                wheelTransform.localPosition = new Vector3(0f, y, 0f);
                wheelTransform.Rotate(Vector3.right, XZVector(rb.velocity).magnitude * 1f * dir * 0.2f);

                // Update wheel visuals
                wheel.UpdateWheelVisuals(index < 2);
                index++;
            }
        }

        private void UpdateGrounded()
        {
            grounded = false;

            // Check all wheels
            foreach (CarWheel suspension in wheels)
            {
                // Check for any grounded wheels
                if (suspension.grounded == true)
                {
                    grounded = true;
                }
            }
        }

        private void UpdateSteering()
        {
            int index = 0;
            foreach (CarWheel wheel in wheels)
            {
                // Only update front wheels
                if (index < 2)
                {
                    // Apply wheel steer angles
                    wheel.steerAngle = steering * (37f - Mathf.Clamp(speed * 0.35f - 2f, 0f, 17f));
                }
            }
        }

        private void UpdateCarPhysics()
        {
            // Get car velocity
            Vector3 velocityXZ = XZVector(rb.velocity);
            Vector3 temp = thisTransform.InverseTransformDirection(XZVector(rb.velocity));

            // Get direction
            dir = Mathf.Sign(thisTransform.InverseTransformDirection(velocityXZ).z);

            // Update car speed
            speed = velocityXZ.magnitude * 3.6f * dir;

            // Calculate angular turn velocity
            float angularVelocity = Mathf.Abs(rb.angularVelocity.y);

            // Update all wheels
            foreach (CarWheel wheel in wheels)
            {
                // Update the suspension physics
                wheel.UpdateWheelPhysics();

                // Only update wheels that touch a surface
                if (wheel.grounded == false)
                    continue;

                // Get forward
                suspensionForward = wheel.wheelPivot.forward;

                // Get wheel hit velocity
                Vector3 hitVelocity = XZVector(rb.GetPointVelocity(wheel.hitPosition));

                // Project velocity for wheel
                Vector3 projectedVelocity = Vector3.Project(hitVelocity, wheel.wheelPivot.right);

                // Influence values for car forces
                float accelInfluence = 1f;
                float yawInfluence = 1f;

                // Get slip angle
                float slipAngle = Mathf.Atan2(temp.x, temp.z);
                float slideTolerance = 0.6f;

                // More liable to drift at speed
                if (angularVelocity > 1f)
                {
                    slideTolerance -= 0.2f;
                }

                bool wheelSlip = false;

                // Check if wheel is sliding
                if (Mathf.Abs(slipAngle) > slideTolerance)
                {
                    // Calculate slide force for wheel
                    float slideForce = Mathf.Clamp(Mathf.Abs(slipAngle) * 2.4f - slideTolerance, 0f, 1f);
                    yawInfluence = Mathf.Clamp(1f - slideForce, 0.05f, 1f);

                    // get car speed
                    float magnitude = rb.velocity.magnitude;
                    wheelSlip = true;

                    if (magnitude < 8f)
                    {
                        yawInfluence += (8f - magnitude) / 8f;
                    }

                    if (angularVelocity < yawGripThreshold)
                    {
                        float yaw = (yawGripThreshold - angularVelocity) / yawGripThreshold;
                        yawInfluence += yaw * yawGripMultiplier;
                    }

                    // Check for low acceleration
                    if (Mathf.Abs(throttle) < 0.3f)
                    {
                        yawInfluence += 0.1f;
                    }

                    // Limit influence
                    yawInfluence = Mathf.Clamp01(yawInfluence);
                }

                float slipFactor = 1f;

                // Apply wheel slip amount
                if (wheelSlip == true)
                {
                    slipFactor = 0.5f;
                }

                // Throttle
                rb.AddForceAtPosition(suspensionForward * (throttle * engineForce * slipFactor * accelInfluence), wheel.hitPosition);

                Vector3 pitch = projectedVelocity * (rb.mass * accelInfluence * yawInfluence);

                rb.AddForceAtPosition(-pitch, wheel.hitPosition);
                rb.AddForceAtPosition(suspensionForward * (pitch.magnitude * 0.25f), wheel.hitPosition);

                // Calculate wheel grip amount
                float wheelGrip = Mathf.Clamp01(1f - yawInfluence);

                // Improve grip when not sliding
                if (Mathf.Sign(dir) != Mathf.Sign(throttle) && speed > 2f)
                {
                    wheelGrip = Mathf.Clamp01(wheelGrip += 0.5f);
                }

                // Apply drag forces
                rb.AddForce(velocityXZ * (0f - drag));
                rb.AddForce(velocityXZ * (0f - rollingFriction));
            }

            Drag();
        }

        private void Drag()
        {
            // Check for auto stop condition
            if (Mathf.Abs(speed) < 1f && grounded == true && throttle == 0f)
            {
                bool applyDrag = true;

                // Check if car should roll on surface
                foreach (CarWheel wheel in wheels)
                {
                    if (Vector3.Angle(wheel.hitNormal, Vector3.up) > 1f)
                    {
                        applyDrag = false;
                        break;
                    }
                }

                // Apply drag so that car comes to a full stop
                if (applyDrag == true)
                    rb.drag = (1f - Mathf.Abs(speed)) * 30f;
                else
                    rb.drag = 0f;
            }
            else
                rb.drag = 0f;
        }

        private static Vector3 XZVector(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}