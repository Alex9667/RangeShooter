using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static ModelsScript;

public class CharControllerScr : MonoBehaviour
{
    private CharacterController characterController;
    private DefaultInput defaultInput;
    private Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;


    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;

    [Header("References")]
    public Transform cameraHolder;
    public Transform FeetTransform;

    [Header("Settings")]
    public PlayerSettingsModel playerSettings;
    public float ViewClampYMin = -70;
    public float ViewClampYMax = 80;
    public LayerMask playerMask;

    [Header("Gravity")]
    public float GravityAmount;
    public float GravityMin;
    private float PlayerGravity;

    public Vector3 jumpingForce;
    private Vector3 jumpingForceVeloity;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;
    private float StanceCheckErrorMargin = 0.05f;

    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    public CharacterStance playerProneStance;

    private float cameraHeight;
    private float cameraHeightVelocity;

    private Vector3 stanceCapsuleCenterVelocity;
    private float stanceCapsuleHeightVelocity;

    private bool isSprinting;

    private Vector3 newMovementSpeed;
    private Vector3 newMovementSpeedVelocity;

    [Header("Weapon")]
    public WeaponControllerScript currentWeapon;


    private void Awake()
    {
        defaultInput = new DefaultInput();

        defaultInput.Character.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => input_View = e.ReadValue<Vector2>();
        defaultInput.Character.Jump.performed += e => jump();
        defaultInput.Character.Crouch.performed += e => crouch();
        defaultInput.Character.Prone.performed += e => prone();
        defaultInput.Character.Sprint.performed += e => toggleSprint();
        defaultInput.Character.SprintReleased.performed += e => stopSprint();


        defaultInput.Enable();


        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localEulerAngles.y;

        if (currentWeapon)
        {
            currentWeapon.Initialise(this);
        }

    }

    private void Update()
    {
        CalculateView();
        CalculateMovement();
        CalculateJump();
        CalculateStance();
    }

    private void CalculateView()
    {
        newCharacterRotation.y += playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -input_View.x : input_View.x) * Time.deltaTime;
        transform.localRotation = Quaternion.Euler(newCharacterRotation);


        newCameraRotation.x += playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime;
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, ViewClampYMin, ViewClampYMax);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateMovement()
    {
        if(input_Movement.y <= 0.2f)
        {
            isSprinting = false;
        }


        var verticalSpeed = playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed;


        if (isSprinting)
        {
            verticalSpeed = playerSettings.RunningForwardSpeed;
            horizontalSpeed = playerSettings.RunningStrafeSpeed;
        }

        if (!characterController.isGrounded)
        {
            playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if(playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchEffector;
        }
        else if(playerStance == PlayerStance.Prone)
        {
            playerSettings.SpeedEffector = playerSettings.ProneEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1;
        }

        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;


        newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed, new Vector3(horizontalSpeed * input_Movement.x * Time.deltaTime, 0, verticalSpeed * input_Movement.y * Time.deltaTime), ref newMovementSpeedVelocity, characterController.isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeed = transform.TransformDirection(newMovementSpeed);

        if(PlayerGravity > GravityMin)
        {
            PlayerGravity -= GravityAmount * Time.deltaTime;
        }

        if(PlayerGravity < -0.1f && characterController.isGrounded)
        {
            PlayerGravity = -0.1f;
        }



        movementSpeed.y += PlayerGravity;
        movementSpeed += jumpingForce * Time.deltaTime;

        characterController.Move(movementSpeed);
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVeloity, playerSettings.JumpFalloff);
    }

    private void CalculateStance()
    {
        var currentStance = playerStandStance;

        if(playerStance == PlayerStance.Crouch)
        {
            currentStance = playerCrouchStance;
        }
        else if(playerStance == PlayerStance.Prone)
        {
            currentStance = playerProneStance;
        }



        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);

    }

    private void jump()
    {
        if (!characterController.isGrounded)
        {
            return;
        }
        if(playerStance == PlayerStance.Crouch || playerStance == PlayerStance.Prone)
        {
            playerStance = PlayerStance.Stand;
            return;
        }

        jumpingForce = Vector3.up * playerSettings.JumpHeight;
        PlayerGravity = 0;

    }

    private void crouch()
    {
        if(playerStance == PlayerStance.Crouch)
        {
            playerStance = PlayerStance.Stand;
            return;
        }
        playerStance = PlayerStance.Crouch;
    }

    private void prone()
    {
        if (playerStance == PlayerStance.Prone)
        {
            playerStance = PlayerStance.Stand;
            return;
        }
        playerStance = PlayerStance.Prone;
    }

    private bool stanceCheck(float stanceCheckHeight)
    {
        var start = new Vector3(FeetTransform.position.x, FeetTransform.position.y + characterController.radius + StanceCheckErrorMargin, FeetTransform.position.z);
        var end = new Vector3(FeetTransform.position.x, FeetTransform.position.y - characterController.radius - StanceCheckErrorMargin, FeetTransform.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    private void toggleSprint()
    {
        if (input_Movement.y <= 0.2f)
        {
            isSprinting = false;
            return;
        }
        isSprinting = !isSprinting;
    }
    private void stopSprint()
    {
        if (playerSettings.SprintingHold)
        {
            isSprinting = false;
        }
    }
}
