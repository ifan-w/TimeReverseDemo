using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabHand : MonoBehaviour
{
    #region PrivateVar
    private GameObject _currentGrabTarget;
    private Grabable _currentGrabable;
    private PlayerInputHandler _input;
    private PlayerController _playerController;
    private ReversiblePlayer _reversiblePlayer;
    #endregion PrivateVar

    #region PublicAccess
    // main camera
    public Camera PlayerCamera;
    // distance and height
    public float DetectDistance;
    public float GrabHoldDistance;
    public float GrabTerminateDistance;
    public float GrabHoldHeightOffset;
    // detect layer
    public LayerMask DetectLayerMask;
    #endregion PublicAccess

    private void Awake()
    {
        _currentGrabTarget = null;
        _input = GetComponent<PlayerInputHandler>();
        _playerController = GetComponent<PlayerController>();
        _reversiblePlayer = GetComponent<ReversiblePlayer>();

        Debug.Assert(GrabHoldDistance + 0.3 < GrabTerminateDistance);
    }

    void Update()
    {
        Ray ray = PlayerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        Grabable grabableObject;

        bool grabKeyDown = _input.IsGrabPressed;

        // input check, update grab state
        if(_currentGrabTarget == null)
        {
            if(Physics.Raycast(ray, out hit, DetectDistance, DetectLayerMask) && hit.collider.TryGetComponent<Grabable>(out grabableObject))
            {
                Debug.DrawLine(ray.origin, ray.direction, Color.red, 0.1f);
                // TODO: UI change
                // input check & grab
                if(grabKeyDown)
                {
                    _currentGrabTarget = hit.collider.gameObject;
                    _currentGrabable = grabableObject;
                    _currentGrabable.OnHold();
                }
            }
            else
            {
                Debug.DrawLine(ray.origin, ray.direction, Color.green, 0.1f);
            }
        }
        else
        {
            // press grab when grab, loose hand
            if(grabKeyDown)
            {
                _currentGrabable.OnLoosed();
                _currentGrabTarget = null;
            }

        }
    }

    public void ResetHand()
    {
        _currentGrabable.OnLoosed();
        _currentGrabTarget = null;
    }

    private void FixedUpdate()
    {
        if(_currentGrabTarget != null)
        {
            int effectUID = _currentGrabable.GetReversibleUID();
            if(effectUID >= 0)
            {
                _playerController.AddKarmaAsCauseToCurrentArmature(TimeManager.Instance.CurrentTime - 1, effectUID);
                Debug.LogFormat("GrabHand: Add Karma with {0} as effect", effectUID);
            }
        }
    }

    public void HoldTargetUpdate()
    {
        if(_currentGrabTarget != null)
        {
            Vector3 holdCenter = transform.position + transform.up * GrabHoldHeightOffset;
            Vector3 vec2Target = _currentGrabable.transform.position - holdCenter;
            if(vec2Target.sqrMagnitude > GrabTerminateDistance * GrabTerminateDistance)
            {
                Debug.LogFormat("GrabHand: Distance {0} Too Far", (_currentGrabable.transform.position - holdCenter).sqrMagnitude);
                _currentGrabable.OnLoosed();
                _currentGrabTarget = null;
                return;
            }
            Vector3 targetPosition = holdCenter
                + Quaternion.AngleAxis(_playerController.CinemachineCameraPitch, transform.right) * transform.forward * GrabHoldDistance;
            Vector3 targetRotation = (_currentGrabTarget.transform.position - transform.position);
            targetRotation.y = 0;
            _currentGrabable.transform.rotation = Quaternion.FromToRotation(Vector3.forward, targetRotation);
            _currentGrabable.MoveTo(targetPosition);
        }
    }
}
