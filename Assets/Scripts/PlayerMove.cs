using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class PlayerMove : MonoBehaviour
{
    [Header("Flight Settings")]
    public float startSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float maxSpeed = 100f;
    public float minSpeed = 0f;

    [Header("Rotation Settings")]
    public float yawSpeed = 50f;    //
    public float pitchSpeed = 80f;  // 
    public float rollSpeed = 100f;  // barrel roll

    [Header("Mouse Settings")]
    public float mouseSensitivity = 1.5f;

    private Rigidbody rb;
    private float currentSpeed;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 0.2f;
        rb.angularDrag = 2f;

        currentSpeed = startSpeed;
        rb.velocity = transform.forward * currentSpeed;

        // Lock cursor for better flight control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        // Constant forward motion
        rb.velocity = transform.forward * currentSpeed;
    }

    void HandleInput()
    {
        // --- Speed Control ---
        if (Input.GetKey(KeyCode.W))
        {
            currentSpeed += acceleration * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.S))
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // --- Rotation Control ---
        float yaw = 0f;
        if (Input.GetKey(KeyCode.A))
            yaw = -1f;
        else if (Input.GetKey(KeyCode.D))
            yaw = 1f;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        float pitch = -mouseY * pitchSpeed * mouseSensitivity * Time.deltaTime;
        float roll = -mouseX * rollSpeed * mouseSensitivity * Time.deltaTime;
        float yawRot = yaw * yawSpeed * Time.deltaTime;

        // Combine rotations
        Quaternion rotationChange = Quaternion.Euler(pitch, yawRot, roll);
        rb.MoveRotation(rb.rotation * rotationChange);
    }
}
