using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public enum DoorPos { Left=0, Middle=1, Right=2 };
    [Header("Object Identifiers/Logic")]
    [Tooltip("Door position must be set as left middle or right from unity inspector")]
    public DoorPos position;
    public bool isLocked = true;

    [Header("Mesh References")]
    public MeshRenderer mainDoorMesh;
    public MeshRenderer[] handleMeshes;

    [Header("Animation Settings")]
    [Tooltip("Duration of animation in seconds")]
    public float animationDuration = 1.5f;
    [Tooltip("Rotation amount")]
    public Vector3 rotationOffset = new Vector3(0, 90, 0);

    [Header("Interactables")]
    [SerializeField] private HandGrabInteractable handGrabInteractable;
    [SerializeField] private GrabInteractable grabInteractable;


    private bool handIsNear = true; //FIXME testing only
    private Quaternion closedRotation;
    private Quaternion openRotation;

    // We store the running coroutine so we can interrupt it if needed
    private Coroutine currentAnimation;
    private void Awake() //run before all objects 'start' calls
    {
        // Save the starting rotation as the "closed" state
        closedRotation = transform.localRotation;

        // Calculate the final "open" state by multiplying the start by your offset
        openRotation = closedRotation * Quaternion.Euler(rotationOffset);
    }

    private void Start()
    {
        if(handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded += OnSelected;
            handGrabInteractable.WhenSelectingInteractorViewRemoved += OnDeselected;
        }
        if (grabInteractable != null)
        {
            grabInteractable.WhenSelectingInteractorViewAdded += OnSelected;
            grabInteractable.WhenSelectingInteractorViewRemoved += OnDeselected;
        }
    }

    private void OnDestroy()
    {
        if (handGrabInteractable != null)
        {
            handGrabInteractable.WhenSelectingInteractorViewAdded -= OnSelected;
            handGrabInteractable.WhenSelectingInteractorViewRemoved -= OnDeselected;
        }
        if (grabInteractable != null)
        {
            grabInteractable.WhenSelectingInteractorViewAdded -= OnSelected;
            grabInteractable.WhenSelectingInteractorViewRemoved -= OnDeselected;
        }
    }

    private void OnSelected(IInteractorView interactor)
    {
        isLocked = true; //lockout to prevent double triggers

        Debug.Log($"DoorController: Door {position} Clicked!");
        GameManager.Instance.OnDoorClicked(position);
    }

    private void OnDeselected(IInteractorView interactor)
    {

    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    //if (other.CompareTag("PlayerHand")) handIsNear = true;
    //    int x = 0;
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    //if (other.CompareTag("PlayerHand")) handIsNear = false;
    //    int x = 0;
    //}

    private void Update()
    {
        //if (isLocked || !handIsNear) return;

        //bool leftClick = OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger);
        //bool rightClick = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
        //OVRHand hand = GetComponent<OVRHand>();
        //bool res = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);

        //if (leftClick || rightClick)
        //{
        //    isLocked = true; //lockout to prevent double triggers

        //    Debug.Log($"DoorController: Door {position} Clicked!");
        //    GameManager.Instance.OnDoorClicked(position);
        //}
    }

    // open/close animation public functions
    public void OpenDoor()
    {
        // Stop any currently running animation
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // Start the local function coroutine
        currentAnimation = StartCoroutine(AnimateRotation(openRotation));

        // LOCAL FUNCTION: Keeps scope completely contained!
        IEnumerator AnimateRotation(Quaternion targetRot)
        {
            Quaternion startRot = transform.localRotation;
            float timeElapsed = 0f;

            while (timeElapsed < animationDuration)
            {
                timeElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timeElapsed / animationDuration);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.localRotation = targetRot;
        }
    }

    public void CloseDoor()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // We reuse the EXACT same local function logic, just passing the closed rotation!
        currentAnimation = StartCoroutine(AnimateRotation(closedRotation));

        IEnumerator AnimateRotation(Quaternion targetRot)
        {
            Quaternion startRot = transform.localRotation;
            float timeElapsed = 0f;

            while (timeElapsed < animationDuration)
            {
                timeElapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, timeElapsed / animationDuration);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            transform.localRotation = targetRot;
        }
    }

    // change color of doors
    public void SetDoorColor(Color newColor)
    {
        // Change the main door body
        if (mainDoorMesh != null)
        {
            mainDoorMesh.material.color = newColor;
        }

        // Change the handles
        foreach (MeshRenderer handle in handleMeshes)
        {
            if (handle != null)
            {
                handle.material.color = newColor;
            }
        }
    }
}
