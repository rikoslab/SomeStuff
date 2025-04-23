using System;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float movementSpeed = 5f;
        public float sprintSpeed = 8f;
        public float slideSpeed = 20f;
        public float acceleration = 5f;
        public float frictionBoost = 2f;
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
            public bool isSprintOnJump;
            public bool isJumpOnSlide;

            public float velocity;

            public Vector3 currentMoveDirection;
            public Vector3 targetMoveDirection;
            public Vector3 airMoveDirection;
            public Vector3 boostDirection;
            public Vector3 slideMoveDirection;
            public Vector3 verticalVelocity;
        }
        [Header("Debugger")]
        [SerializeField] private Debug status;

        private Vector3 previousPosition;
        private CharacterController characterController;
        private PlayerInput playerInput;

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

            
        }

        public void Update()
        {
            status.isGrounded = characterController.isGrounded;

            Movement();
            Jumpment();
            Sprinting();
            Sliding();
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
            float moveSpeed = status.isSprinting ? sprintSpeed : (status.isSliding ? slideSpeed : movementSpeed);
            characterController.Move(status.currentMoveDirection * moveSpeed * Time.deltaTime);

            status.verticalVelocity.y += gravity * Time.deltaTime;
            characterController.Move(status.verticalVelocity * Time.deltaTime);
            transform.rotation.Equals(status.targetMoveDirection);
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
                        status.isSprintOnJump = true;
                    }
                    else
                    {
                        status.isSprintOnJump = false;
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
                    status.isSprintOnJump = false;
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
            if (status.isGrounded && status.velocity >= 6)
            {
                status.isSliding = slideControl.ReadValue<float>() > 0.5f && !status.isGrounded == false;
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
            status.velocity = Vector3.Distance(currentPosition, previousPosition) / Time.deltaTime;
            previousPosition = currentPosition;
        }
    }
}
