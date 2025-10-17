using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("Flight Settings")]
    public float startSpeed = 35f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float maxSpeed = 100f;
    public float minSpeed = 20f;

    [Header("Rotation Settings")]
    public float yawSpeed = 20f;      // left/right
    public float pitchSpeed = 5f;     // base pitch rate
    public float rollSpeed = 100f;    // roll rate
    public float autoLevelSpeed = 1.5f;  // how quickly the jet levels itself

    [Header("Mouse Settings")]
    public float mouseSensitivity = 1.5f;

    [Header("Pitch Acceleration Settings")]
    public float maxPitchAcceleration = 5f;
    public float pitchAccelRate = 2f;
    public float pitchDecelRate = 3f;

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float maxCameraRoll = 15f;
    public float cameraRollSmooth = 5f;

    private Rigidbody rb;
    private float currentSpeed;
    private float currentPitchAccel = 0f;
    private float currentMouseRollInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 0.2f;
        rb.angularDrag = 2f;

        currentSpeed = startSpeed;
        rb.velocity = transform.forward * currentSpeed;

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleInput();
        StabilizeCamera();
    }

    void FixedUpdate()
    {
        rb.velocity = transform.forward * currentSpeed;
    }

    void HandleInput()
    {
        // --- Speed Control ---
        if (Input.GetKey(KeyCode.W))
            currentSpeed += acceleration * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            currentSpeed -= deceleration * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // --- Yaw Control ---
        float yaw = 0f;
        if (Input.GetKey(KeyCode.A)) yaw = -1f;
        else if (Input.GetKey(KeyCode.D)) yaw = 1f;

        // --- Pitch Control ---
        float pitch = 0f;
        bool pitchUp = Input.GetKey(KeyCode.Space);
        bool pitchDown = Input.GetKey(KeyCode.LeftShift);

        if (pitchUp) pitch = -1f;
        else if (pitchDown) pitch = 1f;

        if (pitchUp || pitchDown)
            currentPitchAccel += pitchAccelRate * Time.deltaTime;
        else
            currentPitchAccel -= pitchDecelRate * Time.deltaTime;
        currentPitchAccel = Mathf.Clamp(currentPitchAccel, 0f, maxPitchAcceleration);

        // --- Roll Control ---
        float mouseX = Input.GetAxis("Mouse X");
        currentMouseRollInput = mouseX;

        float roll = 0f;
        if (Mathf.Abs(mouseX) > 0.05f)
        {
            roll = -mouseX * rollSpeed * mouseSensitivity * Time.deltaTime;

            // Apply active control rotation
            float yawRot = yaw * yawSpeed * Time.deltaTime;
            float pitchRot = pitch * (pitchSpeed + currentPitchAccel) * Time.deltaTime;

            Quaternion rotationChange = Quaternion.Euler(pitchRot, yawRot, roll);
            rb.MoveRotation(rb.rotation * rotationChange);
        }
        else
        {
            // --- No roll input ---
            if (!pitchUp && !pitchDown)
            {
                AutoLevelJet(yaw);
            }
            else
            {
                // Maintain pitch while leveling yaw
                float yawRot = yaw * yawSpeed * Time.deltaTime;
                float pitchRot = pitch * (pitchSpeed + currentPitchAccel) * Time.deltaTime;
                Quaternion rotationChange = Quaternion.Euler(pitchRot, yawRot, 0f);
                rb.MoveRotation(rb.rotation * rotationChange);
            }
        }
    }

    void AutoLevelJet(float yawInput)
    {
        // Smoothly level jet roll while keeping heading
        Vector3 currentUp = transform.up;
        Vector3 targetUp = Vector3.Lerp(currentUp, Vector3.up, Time.deltaTime * autoLevelSpeed);

        Quaternion levelRotation = Quaternion.FromToRotation(currentUp, targetUp) * transform.rotation;

        // Allow yaw input while auto-leveling
        if (Mathf.Abs(yawInput) > 0.01f)
            levelRotation *= Quaternion.Euler(0f, yawInput * yawSpeed * Time.deltaTime, 0f);

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, levelRotation, Time.deltaTime * autoLevelSpeed));
    }

    void StabilizeCamera()
    {
        if (playerCamera == null) return;

        // Get jet roll in world space
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;
        float jetRoll = Vector3.SignedAngle(Vector3.up, up, forward);

        // Compute target world roll for camera (never more than ±maxCameraRoll)
        float desiredWorldRoll = Mathf.Clamp(jetRoll, -maxCameraRoll, maxCameraRoll);
        float counterRoll = desiredWorldRoll - jetRoll; // counter-roll to keep horizon within limit

        // Smooth camera local rotation
        Quaternion targetLocalRot = Quaternion.Euler(
            playerCamera.transform.localEulerAngles.x,
            playerCamera.transform.localEulerAngles.y,
            counterRoll
        );

        playerCamera.transform.localRotation = Quaternion.Lerp(
            playerCamera.transform.localRotation,
            targetLocalRot,
            Time.deltaTime * cameraRollSmooth
        );
    }
}