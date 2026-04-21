using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class GrabNotifier : MonoBehaviour
{
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

        if (_grabInteractable == null)
            _grabInteractable = GetComponentInChildren<GrabInteractable>();
    }

    private void Start()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.WhenSelectingInteractorViewAdded += OnGrabbed;
            _grabInteractable.WhenSelectingInteractorViewRemoved += OnReleased;
        }
        else
        {
            Debug.LogError($"No GrabInteractable found on {gameObject.name}!");
        }
    }

    private void OnDestroy()
    {
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