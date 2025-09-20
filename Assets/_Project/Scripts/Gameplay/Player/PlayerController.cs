using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        private const float SprintButtonThreshold = 0.5f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 6f;
        [SerializeField, Min(0f)] private float sprintSpeed = 10f;
        [SerializeField, Min(0f)] private float acceleration = 20f;
        [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.5f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpHeight = 3f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1.5f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Vector3 groundCheckOffset = new Vector3(0f, -0.9f, 0f);
        [SerializeField, Range(0.05f, 1f)] private float groundCheckRadius = 0.25f;
        [SerializeField] private bool drawGroundCheckGizmo = true;

        private Rigidbody _rigidbody;
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private Vector2 _moveInput;
        private bool _isSprinting;
        private bool _isGrounded;
        private float _lastTimeGrounded;
        private float _lastJumpPressedTime = float.NegativeInfinity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            _playerInput = GetComponent<PlayerInput>();
        }

        private void OnEnable()
        {
            CacheActions();
        }

        private void OnDisable()
        {
            ReleaseActions();
        }

        private void Update()
        {
            PollInput();
        }

        private void FixedUpdate()
        {
            UpdateGroundedState();
            HandleMovement();
            HandleJump();
            ApplyCustomGravity();
        }

        private void CacheActions()
        {
            if (_playerInput == null)
            {
                return;
            }

            _moveAction = _playerInput.actions?.FindAction("Move");
            _jumpAction = _playerInput.actions?.FindAction("Jump");
            _sprintAction = _playerInput.actions?.FindAction("Sprint");
        }

        private void ReleaseActions()
        {
            _moveAction = null;
            _jumpAction = null;
            _sprintAction = null;
        }

        private void PollInput()
        {
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }

            if (_sprintAction != null)
            {
                _isSprinting = _sprintAction.ReadValue<float>() > SprintButtonThreshold;
            }

            if (_jumpAction != null && _jumpAction.WasPerformedThisFrame())
            {
                _lastJumpPressedTime = Time.time;
            }

            if (_jumpAction != null && _jumpAction.WasReleasedThisFrame())
            {
                Vector3 velocity = _rigidbody.velocity;
                if (velocity.y > 0f)
                {
                    velocity.y *= 0.5f;
                    _rigidbody.velocity = velocity;
                }
            }
        }

        private void UpdateGroundedState()
        {
            Vector3 checkPosition = transform.TransformPoint(groundCheckOffset);
            _isGrounded = Physics.CheckSphere(checkPosition, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

            if (_isGrounded)
            {
                _lastTimeGrounded = Time.time;
            }
        }

        private void HandleMovement()
        {
            Vector3 input = new Vector3(_moveInput.x, 0f, _moveInput.y);
            input = Vector3.ClampMagnitude(input, 1f);

            Vector3 moveDirection = transform.TransformDirection(input);
            moveDirection.y = 0f;
            moveDirection.Normalize();

            float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;
            Vector3 targetVelocity = moveDirection * targetSpeed;

            Vector3 currentVelocity = _rigidbody.velocity;
            Vector3 planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            float control = _isGrounded ? 1f : airControlMultiplier;
            float maxSpeedChange = acceleration * control * Time.fixedDeltaTime;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, maxSpeedChange);

            _rigidbody.velocity = new Vector3(planarVelocity.x, currentVelocity.y, planarVelocity.z);
        }

        private void HandleJump()
        {
            bool jumpBuffered = Time.time - _lastJumpPressedTime <= jumpBufferTime;
            bool coyoteAllowed = Time.time - _lastTimeGrounded <= coyoteTime;

            if (!jumpBuffered || !coyoteAllowed)
            {
                return;
            }

            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            if (gravity <= 0f)
            {
                gravity = 9.81f * gravityMultiplier;
            }
            gravity = Mathf.Max(gravity, 0.01f);

            float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

            Vector3 velocity = _rigidbody.velocity;
            velocity.y = jumpVelocity;
            _rigidbody.velocity = velocity;

            _lastJumpPressedTime = float.NegativeInfinity;
            _isGrounded = false;
        }

        private void ApplyCustomGravity()
        {
            Vector3 velocity = _rigidbody.velocity;
            float gravity = Mathf.Abs(Physics.gravity.y) * gravityMultiplier;
            gravity = Mathf.Max(gravity, 0.01f);

            if (_isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }
            else
            {
                velocity.y -= gravity * Time.fixedDeltaTime;
            }

            _rigidbody.velocity = velocity;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGroundCheckGizmo)
            {
                return;
            }

            Gizmos.color = Color.green;
            Vector3 checkPosition = transform.TransformPoint(groundCheckOffset);
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }
    }
}
