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
        //Plane RB -- uses physics, no gravity
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

    void FixedUpdate()
    {
        HandleInput();
        rb.velocity = transform.forward * currentSpeed;

        Vector3 targetEuler = rb.rotation.eulerAngles;
        targetEuler.z = 0f;

        Quaternion targetRot = Quaternion.Euler(targetEuler);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRot, Time.deltaTime * cameraRollSmooth);
 
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

        float mouseX = Input.GetAxis("Mouse X");    //rotate input -- mouse left and right

        
            // --- Apply player input rotation ---
        float yawRot = yawInput * yawSpeed * Time.deltaTime;
        float pitchRot = pitchInput * pitchSpeed * Time.deltaTime;
        float rollRot = -mouseX * rollSpeed * mouseSensitivity * Time.deltaTime;

        Quaternion rotationChange = Quaternion.Euler(pitchRot, yawRot, rollRot);
        rb.MoveRotation(rb.rotation * rotationChange);

        // --- Store the last stable heading ---
        lastStableHeading = rb.rotation;
    }

    // Store last heading rotation
    private Quaternion lastStableHeading;

}