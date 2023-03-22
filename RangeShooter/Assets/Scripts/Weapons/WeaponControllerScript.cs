using UnityEditor;
using UnityEngine;
using static ModelsScript;
public class WeaponControllerScript : MonoBehaviour
{
    private CharControllerScr characterController;

    [Header("Settings")]
    public WeaponSettingsModel settings;

    bool isInitialised;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;

    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    public ParticleSystem MussleFlash;
    public GameObject ImpaceEffect;

    //  [HideInInspector]
    // public bool IsAimingIn;

    /* [Header("Sights")]
     public Transform SightTarget;
     public float SightOffset;
     public float AimingInTime;
     private Vector3 weaponSwayPosition;
     private Vector3 weaponSwayPositionVelocity;*/

    public Camera Camera;

    private void Start()
    {
        newWeaponRotation = transform.localRotation.eulerAngles;
    }

    public void Initialise(CharControllerScr CharacterController)
    {
        characterController = CharacterController;
        isInitialised= true;
    } 

    private void Update()
    {
        if(!isInitialised)
        {
            return;
        }
        targetWeaponRotation.y += settings.SwayAmount * (settings.SwayXInverted ? -characterController.input_View.x : characterController.input_View.x) * Time.deltaTime;
        targetWeaponRotation.x += settings.SwayAmount * (settings.SwayYInverted ? characterController.input_View.y : -characterController.input_View.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);


        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation);

        if (Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }

    } 

    void Shoot()
    {
        MussleFlash.Play();
        RaycastHit hit;
        if(Physics.Raycast(Camera.transform.position, Camera.transform.forward, out hit))
        {
            Debug.Log(hit.transform.name);

            GameObject impactGO = Instantiate(ImpaceEffect, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(impactGO, 2f);
        }
    }

  /*  private void calculateAimingIn()
    {
        var targetPosition = transform.position;
        if (IsAimingIn)
        {
            targetPosition = characterController.cameraHolder
        }
    }*/

}
