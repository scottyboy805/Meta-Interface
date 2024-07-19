using UnityEngine;

namespace UltimateReplay.Demo
{
    /// <summary>
    /// A simple FPS camera used in the demo scenes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class SimpleFPSCamera : MonoBehaviour
    {
        // Private
        private Vector2 smooth = Vector2.zero;
        private Vector2 absolute = Vector2.zero;
        private bool lockToggle = true;

        // Public
        /// <summary>
        /// The root object transform.
        /// </summary>
        public Transform root = null;
        /// <summary>
        /// The mouse sensitivity for the camera.
        /// </summary>
        public float sensitivity = 2;
        /// <summary>
        /// The amount of smoothing to apply to the camera movements.
        /// </summary>
        public float smoothing = 3;
        /// <summary>
        /// The angle range for up and down camera movements.
        /// </summary>
        public float lookAngle = 120;

        // Methods
        /// <summary>
        /// Make the camera active.
        /// </summary>
        public void ActivateCamera()
        {
            // Enable the local camera
            GetComponent<Camera>().enabled = true;
        }

        /// <summary>
        /// Make the camera inactive.
        /// </summary>
        public void DeactivateCamera()
        {
            // Disable the local camera
            GetComponent<Camera>().enabled = false;
        }

        private void Start()
        {
            // Update the initial rotation
            if (root != null)
            {
                // Store the initial rotation to prevent snap to 0
                absolute.x = root.rotation.eulerAngles.y;
            }
            else
            {
                // Store the initial rotation to prevent snap to 0
                absolute.x = transform.root.eulerAngles.y;
            }

            // Set lock toggle
            lockToggle = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) == true)
                lockToggle = !lockToggle;

            // Lock the sursor to the screen
            Cursor.lockState = (lockToggle == true) ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = (lockToggle == false);

            // Get mouse delta
            Vector2 delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

            // Get the modified input
            float modified = (sensitivity * smoothing);

            // Scale input base on sensitivity
            delta.Scale(new Vector2(modified, modified));

            // Interpolat movement
            smooth.x = Mathf.Lerp(smooth.x, delta.x, 1 / smoothing);
            smooth.y = Mathf.Lerp(smooth.y, delta.y, 1 / smoothing);

            // Find the absolute position
            absolute += smooth;

            // Clamp the angle
            absolute.y = Mathf.Clamp(absolute.y, -(lookAngle / 2), (lookAngle / 2));

            // Find the rotation around x
            Quaternion lookRotation = Quaternion.AngleAxis(-absolute.y, Vector3.right);
            transform.localRotation = lookRotation;

            // Check for root
            if (root != null)
            {
                // FInd the rotation around y
                Quaternion rotation = Quaternion.AngleAxis(absolute.x, root.up);
                root.localRotation = rotation;
            }
            else
            {
                Quaternion rotation = Quaternion.AngleAxis(absolute.x, transform.up);
                transform.localRotation = rotation;
            }
        }
    }
}
