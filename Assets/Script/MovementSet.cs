using System;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.player
{
    [RequireComponent(typeof(CharacterController))]
    public class MovementSet : MonoBehaviour
    {
        [System.Serializable]
        public class MovementStats
        {
            [Header("Movement Settings")]
            public float movementSpeed = 5f;
            public float sprintSpeed = 8f;
            public float slideSpeed = 12f;
            public float acceleration = 2f;
            public float frictionBoost = 5f;
            public float airSlideMove = 14f;
            public float rotateBoost = 20f;

            [Header("Rotation settings")]
            public float rotateSpeed = 5f;

            [Header("Stamina Settings")]
            public float maxStamina = 20f;
            public float staminaRegenDelay = 2f;
            public float staminaRegenRate = 40;
            public float staminaDepletRate = 50;

            [Header("Jump & gravity")]
            public float jumpForce = 10f;
            public float gravity = -13f;
            public float slamVel = -80;
        }

        [Header("Current Stats")]
        [SerializeField] private MovementStats currentStats;

        [System.Serializable]
        public class Debug
        {
            public bool isGrounded;
            public bool isSliding;
            public bool isSprinting;
            public bool isJumpOnSprint;
            public bool isJumpOnSlide;

            public float velocity;
            public float currentSpeed;

            public Vector3 currentMoveDirection;
            public Vector3 targetMoveDirection;
            public Vector3 airMoveDirection;
            public Vector3 boostDirection;
            public Vector3 slideMoveDirection;
            public Vector3 verticalVelocity;
            public Vector3 horizontalVelocity;
        }

        [Header("Debugger")]
        [SerializeField] private Debug status;

        private Vector3 previousPosition;
        private Vector3 previousHorizontalPosition;
        protected CharacterController characterController;
        private PlayerInput playerInput;
        private float moveSpeed;
        private float timeSinceLastSlide;
        private float currentStamina;

        [HideInInspector] public InputAction moveControl;
        [HideInInspector] public InputAction jumpControl;
        [HideInInspector] public InputAction sprintControl;
        [HideInInspector] public InputAction slideControl;

        public void Start()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            moveControl = playerInput.actions["Move"];
            jumpControl = playerInput.actions["Jump"];
            sprintControl = playerInput.actions["Sprint"];
            slideControl = playerInput.actions["Slide"];

            if (currentStats == null)
            {
                currentStats = new MovementStats();
            }
            currentStamina = currentStats.maxStamina;
        }

        public void SetMovementStats(MovementStats newStats)
        {
            currentStats = newStats;
            currentStamina = currentStats.maxStamina;
        }

        public void Update()
        {
            status.isGrounded = characterController.isGrounded;

            Movement();
            Rotation();
            Jumpment();
            Sprinting();
            Sliding();
            SlideStamina();
            Slam();
            CheckCeiling();
            CheckVelocity();
        }

        public void Movement()
        {
            status.isGrounded = characterController.isGrounded;

            if (status.isGrounded && status.verticalVelocity.y < 0)
            {
                status.verticalVelocity.y = -2f;
            }

            Vector2 moveinput = moveControl.ReadValue<Vector2>();

            if (status.isSliding)
            {
                Vector2 lastMoveinput = moveinput.normalized;
                status.targetMoveDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
                status.currentMoveDirection = status.targetMoveDirection;
            }
            else
            {
                status.targetMoveDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
            }

            if (status.isGrounded)
            {
                status.currentMoveDirection = Vector3.Lerp(
                    status.currentMoveDirection,
                    status.targetMoveDirection,
                    currentStats.acceleration * Time.deltaTime
                );
            }
            else
            {
                Vector3 airTargetDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
                status.airMoveDirection = Vector3.Lerp(
                    status.airMoveDirection,
                    airTargetDirection,
                    currentStats.acceleration * Time.deltaTime
                );
                status.currentMoveDirection = Vector3.Lerp(
                    status.currentMoveDirection,
                    status.airMoveDirection,
                    currentStats.acceleration * Time.deltaTime
                );
            }

            if (status.isSprinting)
            {
                status.currentMoveDirection = Vector3.Lerp(
                    status.currentMoveDirection,
                    status.targetMoveDirection,
                    currentStats.acceleration * Time.deltaTime
                );
            }

            if (status.isSliding && status.isGrounded)
            {
                Vector3 slideTargetDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
                status.slideMoveDirection = Vector3.Lerp(
                    status.slideMoveDirection,
                    slideTargetDirection,
                    currentStats.frictionBoost * Time.deltaTime
                );
                status.currentMoveDirection = Vector3.Lerp(
                    status.currentMoveDirection,
                    slideTargetDirection,
                    currentStats.frictionBoost * Time.deltaTime
                );
            }

            if (status.isGrounded == false && status.isJumpOnSlide)
            {
                moveSpeed = currentStats.airSlideMove;
                currentStats.rotateSpeed = currentStats.rotateBoost;
            }
            else
            {
                currentStats.rotateSpeed = 5f;
                moveSpeed = status.isSprinting ? currentStats.sprintSpeed :
                          (status.isSliding ? currentStats.slideSpeed : currentStats.movementSpeed);
            }

            characterController.Move(status.currentMoveDirection * moveSpeed * Time.deltaTime);

            status.verticalVelocity.y += currentStats.gravity * Time.deltaTime;
            characterController.Move(status.verticalVelocity * Time.deltaTime);
        }

        void Rotation()
        {
            if (status.currentMoveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(status.currentMoveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    currentStats.rotateSpeed * Time.deltaTime
                );
            }
        }

        void Jumpment()
        {
            if (jumpControl.triggered && status.isGrounded)
            {
                status.verticalVelocity.y = Mathf.Sqrt(currentStats.jumpForce * -2f * currentStats.gravity);
                if (status.currentMoveDirection.magnitude > 0.1f)
                {
                    status.airMoveDirection = status.currentMoveDirection.normalized;
                    if (status.isSprinting)
                    {
                        status.airMoveDirection += transform.forward * currentStats.frictionBoost;
                        status.isJumpOnSprint = true;
                    }
                    else
                    {
                        status.isJumpOnSprint = false;
                    }
                    if (status.isSliding)
                    {
                        status.airMoveDirection += transform.forward * currentStats.frictionBoost;
                        status.isJumpOnSlide = true;
                    }
                    else
                    {
                        status.isJumpOnSlide = false;
                    }
                }
            }
        }

        void Sprinting()
        {
            if (status.isGrounded)
            {
                status.isSprinting = sprintControl.ReadValue<float>() > 0.5f && !status.isSliding;
            }
            else
            {
                status.isSprinting = false;
            }
        }

        void Sliding()
        {
            if (status.isGrounded && status.currentSpeed >= 6)
            {
                status.isSliding = slideControl.ReadValue<float>() > 0.5f && currentStamina > 0;
            }
            else
            {
                status.isSliding = false;
            }
        }

        void Slam()
        {
            if (!status.isGrounded && slideControl.triggered)
            {
                status.verticalVelocity.y += currentStats.slamVel;
            }
        }

        void SlideStamina()
        {
            Vector2 moveInput = moveControl.ReadValue<Vector2>();
            bool isMoving = moveInput.magnitude > 0.1f;

            if (status.isSliding && isMoving)
            {
                currentStamina -= currentStats.staminaDepletRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, currentStats.maxStamina);
                timeSinceLastSlide = 0;
            }
            else
            {
                timeSinceLastSlide += Time.deltaTime;
                if (timeSinceLastSlide >= currentStats.staminaRegenDelay)
                {
                    currentStamina += currentStats.staminaRegenRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0, currentStats.maxStamina);
                }
            }

            if (currentStamina <= 0)
            {
                status.isSliding = false;
            }
        }

        void CheckCeiling()
        {
            if ((characterController.collisionFlags & CollisionFlags.Above) != 0)
            {
                status.verticalVelocity.y = -2f;
            }
        }

        void CheckVelocity()
        {
            Vector3 currentPosition = transform.position;
            Vector3 currentHorizontalPosition = new Vector3(currentPosition.x, 0f, currentPosition.z);
            status.velocity = Vector3.Distance(currentPosition, previousPosition) / Time.deltaTime;
            status.currentSpeed = Vector3.Distance(currentHorizontalPosition, previousHorizontalPosition) / Time.deltaTime;
            previousPosition = currentPosition;
            previousHorizontalPosition = currentHorizontalPosition;
        }
    }
}

