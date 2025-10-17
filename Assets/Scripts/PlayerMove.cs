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

    [Header("Camera Settings")]
    public Camera playerCamera;
    public float maxCameraRoll = 15f;
    public float cameraRollSmooth = 5f;

    private Rigidbody rb;
    private float currentSpeed;
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

        // --- Yaw / Pitch / Roll Inputs ---
        float yawInput = 0f;
        if (Input.GetKey(KeyCode.A)) yawInput = -1f;
        else if (Input.GetKey(KeyCode.D)) yawInput = 1f;

        float pitchInput = 0f;
        if (Input.GetKey(KeyCode.Space)) pitchInput = -1f;
        else if (Input.GetKey(KeyCode.LeftShift)) pitchInput = 1f;

        float mouseX = Input.GetAxis("Mouse X");

        bool hasInput = Mathf.Abs(mouseX) > 0.05f || Mathf.Abs(yawInput) > 0.05f || Mathf.Abs(pitchInput) > 0.05f;

        if (hasInput)
        {
            // --- Apply player input rotation ---
            float yawRot = yawInput * yawSpeed * Time.deltaTime;
            float pitchRot = pitchInput * pitchSpeed * Time.deltaTime;
            float rollRot = -mouseX * rollSpeed * mouseSensitivity * Time.deltaTime;

            Quaternion rotationChange = Quaternion.Euler(pitchRot, yawRot, rollRot);
            rb.MoveRotation(rb.rotation * rotationChange);

            // --- Store the last stable heading ---
            lastStableHeading = rb.rotation;
        }
        else
        {
            // --- No input: keep flying toward last heading and level roll ---
            MaintainHeadingAndLevel();
        }
    }

    // Store last heading rotation
    private Quaternion lastStableHeading;

    void MaintainHeadingAndLevel()
    {
        // Smoothly rotate toward last known heading but level roll
        Vector3 forwardDir = lastStableHeading * Vector3.forward;

        // Build a leveled rotation (same heading but upright)
        Quaternion targetRotation = Quaternion.LookRotation(forwardDir, Vector3.up);

        // Smoothly slerp from current to target
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.deltaTime * autoLevelSpeed));
    }

    void StabilizeCamera()
    {
        if (playerCamera == null) return;

        // --- Detect roll input ---
        float mouseX = Input.GetAxis("Mouse X");
        bool hasRollInput = Mathf.Abs(mouseX) > 0.05f;

        // --- Auto-level rotation ---
        Quaternion targetRotation = Quaternion.LookRotation(transform.forward, transform.up);

        if (!hasRollInput)
        {
            // Smoothly level Z rotation (no horizon tilt)
            Vector3 euler = targetRotation.eulerAngles;
            euler.z = 0f;
            targetRotation = Quaternion.Euler(euler);
        }

        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation,Time.deltaTime * cameraRollSmooth);

        // --- Dynamic FOV and camera distance based on speed ---
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        float targetFOV = Mathf.Lerp(75f, 100f, t); // 75° at min speed, 100° at max speed
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * 2f);

        float minCamDistanceZ = -9f; // local Z offset at low speed
        float maxCamDistanceZ = -12f; // local Z offset at high speed
        float targetZ = Mathf.Lerp(minCamDistanceZ, maxCamDistanceZ, t);

        float minCamDistanceY = 4f; // local Z offset at low speed
        float maxCamDistanceY = 5.5f; // local Z offset at high speed
        float targetY = Mathf.Lerp(minCamDistanceY, maxCamDistanceY, t);

        // --- Smooth local camera position (only Z axis, keep Y fixed) ---
        Vector3 localPos = playerCamera.transform.localPosition;
        float smoothedZ = Mathf.Lerp(localPos.z, targetZ, Time.deltaTime * 2f);
        float smoothedY = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * 2f);
        playerCamera.transform.localPosition = new Vector3(localPos.x, smoothedY, smoothedZ);
    }

}