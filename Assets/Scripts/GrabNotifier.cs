using System.Collections;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

public class GrabNotifier : MonoBehaviour
{
    [Header("Interactables")]
    [SerializeField] private HandGrabInteractable _handGrabInteractable;
    [SerializeField] private GrabInteractable _grabInteractable;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Rigidbody _rb;
    private Coroutine _returnCoroutine;
    private bool _isGrabbed;

    private void Awake()
    {
        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _rb = GetComponent<Rigidbody>();

        if (_handGrabInteractable == null)
            _handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        if (_grabInteractable == null)
            _grabInteractable = GetComponentInChildren<GrabInteractable>();
    }

    private void Start()
    {
        if (_handGrabInteractable != null)
        {
            _handGrabInteractable.WhenSelectingInteractorViewAdded += OnGrabbed;
            _handGrabInteractable.WhenSelectingInteractorViewRemoved += OnReleased;
        }
        if (_grabInteractable != null)
        {
            _grabInteractable.WhenSelectingInteractorViewAdded += OnGrabbed;
            _grabInteractable.WhenSelectingInteractorViewRemoved += OnReleased;
        }

        Debug.Log($"GrabNotifier {gameObject.name} - HandGrab: {_handGrabInteractable != null}, Grab: {_grabInteractable != null}");
    }

    private void OnDestroy()
    {
        if (_handGrabInteractable != null)
        {
            _handGrabInteractable.WhenSelectingInteractorViewAdded -= OnGrabbed;
            _handGrabInteractable.WhenSelectingInteractorViewRemoved -= OnReleased;
        }
        if (_grabInteractable != null)
        {
            _grabInteractable.WhenSelectingInteractorViewAdded -= OnGrabbed;
            _grabInteractable.WhenSelectingInteractorViewRemoved -= OnReleased;
        }
    }

    private void OnGrabbed(IInteractorView interactor)
    {
        Debug.Log($"GRABBED: {gameObject.name}");
        _isGrabbed = true;

        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }

        GameManager.Instance.OnObjectGrabbed(this);
    }

    private void OnReleased(IInteractorView interactor)
    {
        Debug.Log($"RELEASED: {gameObject.name}");
        _isGrabbed = false;

        GameManager.Instance.OnObjectReleased(this);

        _returnCoroutine = StartCoroutine(DelayedReturn(2.5f));
    }

    private IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!_isGrabbed)
        {
            if (_rb != null)
            {
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            transform.position = _startPosition;
            transform.rotation = _startRotation;
        }

        _returnCoroutine = null;
    }
}