using System;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace Game.player
{
    public class PlayerMovement : MonoBehaviour
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
        public float timeSinceLastSlide;
        public float currentStamina;

        [Header("Jump & gravity")]
        public float jumpForce = 10f;
        public float gravity = -13f;
        public float slamVel = -80;
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
        private CharacterController characterController;
        private PlayerInput playerInput;
        private float moveSpeed;

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

            currentStamina = maxStamina;
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
        private void Movement()
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
                status.currentMoveDirection = Vector3.Lerp(status.currentMoveDirection, status.targetMoveDirection, acceleration * Time.deltaTime);
            }
            else
            {
                Vector3 airTargetDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
                status.airMoveDirection = Vector3.Lerp(status.airMoveDirection, airTargetDirection, acceleration * Time.deltaTime);
                status.currentMoveDirection = Vector3.Lerp(status.currentMoveDirection, status.airMoveDirection, acceleration * Time.deltaTime);
            }

            if (status.isSprinting)
            {
                status.currentMoveDirection = Vector3.Lerp(status.currentMoveDirection, status.targetMoveDirection, acceleration * Time.deltaTime);
            }

            if (status.isSliding && status.isGrounded)
            {
                Vector3 slideTargetDirection = transform.right * moveinput.x + transform.forward * moveinput.y;
                status.slideMoveDirection = Vector3.Lerp(status.slideMoveDirection, slideTargetDirection, frictionBoost * Time.deltaTime);
                status.currentMoveDirection = Vector3.Lerp(status.currentMoveDirection, slideTargetDirection, frictionBoost * Time.deltaTime);
            }
            if (status.isGrounded == false && status.isJumpOnSlide)
            {
                moveSpeed = airSlideMove;
            }
            else
            {
                moveSpeed = status.isSprinting ? sprintSpeed : (status.isSliding ? slideSpeed : movementSpeed);
            }
            
            characterController.Move(status.currentMoveDirection * moveSpeed * Time.deltaTime);

            status.verticalVelocity.y += gravity * Time.deltaTime;
            characterController.Move(status.verticalVelocity * Time.deltaTime);
            transform.rotation.Equals(status.targetMoveDirection);
        }
        void Rotation()
        {
            if (status.isGrounded == false)
            {
                if (status.isJumpOnSlide || status.isJumpOnSprint) 
                { 
                    rotateSpeed = rotateBoost;
                }
            }
            else
            {
                rotateSpeed = 5;
            }

            if (status.currentMoveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(status.currentMoveDirection);

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
            }
        }
        void Jumpment()
        {
            if (jumpControl.triggered && status.isGrounded)
            {
                status.verticalVelocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
                if (status.currentMoveDirection.magnitude > 0.1f)
                {
                    status.airMoveDirection = status.currentMoveDirection.normalized;
                    if (status.isSprinting)
                    {
                        status.airMoveDirection += transform.forward * frictionBoost;
                        status.isJumpOnSprint = true;
                    }
                    else
                    {
                        status.isJumpOnSprint = false;
                    }
                    if (status.isSliding)
                    {
                        status.airMoveDirection += transform.forward * frictionBoost;
                        status.isJumpOnSlide = true;
                    }
                    else
                    {
                        status.isJumpOnSlide = false;
                    }
                }
                else
                {
                    status.airMoveDirection = Vector3.zero;
                    status.isJumpOnSprint = false;
                }
            }
        }
        void Sprinting()
        {
            Vector2 moveInp = moveControl.ReadValue<Vector2>();
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
            Vector2 moveInp = moveControl.ReadValue<Vector2>();
            if (status.isGrounded && status.currentSpeed >= 6)
            {
                status.isSliding = slideControl.ReadValue<float>() > 0.5f && !status.isGrounded == false && currentStamina > 0;
            }
            else
            {
                status.isSliding = false;
            }
        }
        void Slam()
        {
            if (status.isGrounded == false && slideControl.triggered)
            {
                status.verticalVelocity.y += slamVel;
            }
        }
        void SlideStamina()
        {
            Vector2 moveInput = moveControl.ReadValue<Vector2>();
            bool isMoving = moveInput.magnitude > 0.1f;
            if (status.isSliding && isMoving)
            {
                currentStamina -= staminaDepletRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
                timeSinceLastSlide = 0;
            }
            else
            {
                timeSinceLastSlide += Time.deltaTime;
                if (timeSinceLastSlide >= staminaRegenDelay)
                {
                    currentStamina += staminaRegenRate * Time.deltaTime;
                    currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
                }
            }
            if (currentStamina <= 0)
            {
                status.isSliding = false;
            }
        }
        private void CheckCeiling()
        {
            if ((characterController.collisionFlags & CollisionFlags.Above) != 0)
            {
                status.verticalVelocity.y = -2f;
            }
        }
        private void CheckVelocity()
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

