using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 1.5f;
    public float runSpeed = 5.5f;
    public float rotationSpeed = 12f;
    public float speedSmoothTime = 0.1f;

    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float groundCheckDistance = 0.2f;
    public LayerMask groundMask = ~0;

    public Transform cameraTransform;
    public float mouseSensitivity = 0.1f;
    // public float pitchMin = -30f;
    // public float pitchMax = 70f;

    private CharacterController controller;
    [SerializeField] private Animator animator;
    private PlayerInputActions input;

    private Vector3 velocity;
    private float currentSpeed;
    private float speedVelocity;
    private bool isGrounded;
    private bool isJumping;
    private float cameraPitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new PlayerInputActions();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable() => input.Player.Enable();
    void OnDisable() => input.Player.Disable();

    void Update()
    {
        HandleGroundCheck();
        HandleLook();
        HandleMovement();
        HandleJump();
        HandleGravity();
        UpdateAnimator();
    }

    void HandleGroundCheck()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.Raycast(origin, Vector3.down,
            groundCheckDistance + 0.1f, groundMask);

        Debug.DrawRay(origin, Vector3.down * (groundCheckDistance + 0.1f),
            isGrounded ? Color.green : Color.red);
    }

    void HandleLook()
    {
        if (cameraTransform == null) return;

        Vector2 lookInput = input.Player.Look.ReadValue<Vector2>();

        float yaw = lookInput.x * mouseSensitivity;
        // float pitch = lookInput.y * mouseSensitivity;

        cameraTransform.Rotate(Vector3.up, yaw, Space.World);

        //cameraPitch -= pitch;
        //cameraPitch = Mathf.Clamp(cameraPitch, pitchMin, pitchMax);
        //cameraTransform.localEulerAngles = new Vector3(cameraPitch,
        //    cameraTransform.localEulerAngles.y, 0);
    }

    void HandleMovement()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        Vector3 inputDir = new Vector3(moveInput.x, 0, moveInput.y);

        bool isRunning = input.Player.Sprint.IsPressed();
        bool hasInput = inputDir.magnitude > 0.1f;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0; camRight.y = 0;
        camForward.Normalize(); camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;

        float targetSpeed = 0f;
        if (hasInput)
            targetSpeed = isRunning ? runSpeed : walkSpeed;

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed,
            ref speedVelocity, speedSmoothTime);

        Vector3 localMove = transform.InverseTransformDirection(moveDir);
        float animX = localMove.x;
        float animY = localMove.z;

        float speedScale = isRunning ? 2f : 1f;
        float normalized = currentSpeed / (targetSpeed > 0f ? targetSpeed : walkSpeed);

        animator.SetFloat("MoveX", animX * speedScale * normalized, 0.1f, Time.deltaTime);
        animator.SetFloat("MoveY", animY * speedScale * normalized, 0.1f, Time.deltaTime);

        Vector3 lookDir = camForward;
        if (lookDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                targetRot, rotationSpeed * Time.deltaTime);
        }

        controller.Move(moveDir * currentSpeed * Time.deltaTime);
    }

    void HandleJump()
    {
        if (input.Player.Jump.WasPressedThisFrame() && isGrounded && !isJumping)
        {
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            isJumping = true;
            animator.SetBool("Jump", true);
        }
    }

    void HandleGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (isJumping)
            {
                isJumping = false;
                animator.SetBool("Jump", false);
            }
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        float normalized = currentSpeed / runSpeed;
        animator.SetFloat("Speed", normalized, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", isGrounded);
    }
}