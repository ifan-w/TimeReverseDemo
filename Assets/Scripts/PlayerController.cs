using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    #region PrivateVar
    // components
    private PlayerInputHandler _input;
    private CharacterController _cc;
    private GrabHand _hand;
    // cinemachine
    private float _cinemachineCameraPitch;
    private float _cinemachineCameraPitchMin = -75.0f;
    private float _cinemachineCameraPitchMax = 75.0f;
    private float _lookMovementThresh = 0.01f;
    // movement
    private float _coyotaTimeout = -1.0f;
    private float _verticalVelocity = 0.0f;
    private float _fallTimeout;
    // attach to armature
    private GameObject _currentArmature;
    private PlayerArmatureController _currentArmatureController;
    private int _currentArmatureIdx;
    // delegate physic update state
    private bool _physicUpdateDelegated;
    private bool _physicUpdateEnabled;
    #endregion PrivateVar

    #region PublicAccess
    [Header("Camera")]
    public GameObject CinemachineCameraTarget;
    public Vector2 MouseSensitivity;
    public CinemachineVirtualCamera VCamera;
    public Vector3 CameraOffset;
    public LayerMask CameraCollisionDetectLayers;

    [Header("Movement")]
    [Range(0.01f, 100.0f)]
    public float WalkSpeed;
    [Range(0.01f, 100.0f)]
    public float SpringSpeed;
    public float Gravity;
    public float JumpVelocity;
    public float FallTimeOutMax;
    public float CinemachineCameraPitch { get { return _cinemachineCameraPitch; } }

    #endregion PublicAccess

    private void Awake()
    {
        _cinemachineCameraPitch = 0.0f;
        _physicUpdateDelegated = false;
    }

    // Start is called before the first frame update
    private void Start()
    {
        _input = GetComponent<PlayerInputHandler>();
        _input.RegisterNumberEvent(SwitchArmature);
        _cc = GetComponent<CharacterController>();
        _hand = GetComponent<GrabHand>();

        // armature
        _currentArmatureIdx = -1;
        SwitchArmature(1);
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    private void FixedUpdate()
    {
        // if Reversible component attached, delegate physic update to Reversible component
        if(!_physicUpdateDelegated)
        {
            PhysicUpdate();
        }
    }

    public void PhysicUpdate()
    {
        _physicUpdateEnabled = true;
        JumpGravityUpdate();
        MoveUpdate();
    }

    private void JumpGravityUpdate()
    {
        bool isjumpPressed = _input.IsJumpPressed;
        // if(_cc.isGrounded || Time.time < _coyotaTimeout)
        if(_cc.isGrounded)
        {
            _fallTimeout = FallTimeOutMax;
            _currentArmatureController.UpdateFreeFall(false);
            _currentArmatureController.UpdateGround(true);
            // force hit ground
            _verticalVelocity = -2.0f;

            if(isjumpPressed)
            {
                _verticalVelocity = JumpVelocity;
                _currentArmatureController.UpdateJump(true);
            }
            else
            {
                _currentArmatureController.UpdateJump(false);
            }
        }
        else
        {
            if(_fallTimeout < 0)
            {
                _currentArmatureController.UpdateFreeFall(true);
            }
            else
            {
                _fallTimeout -= Time.fixedDeltaTime;
            }
            _currentArmatureController.UpdateGround(false);
            _verticalVelocity += Gravity * Time.fixedDeltaTime;
        }
    }

    private void MoveUpdate()
    {
        // Debug.LogFormat("PlayerController: Current Velocity1 {0}", _cc.velocity);
        float targetSpeed = (_input.MoveDirection.sqrMagnitude < 0.01f) ? 0.0f : ((_input.IsSpring) ? SpringSpeed: WalkSpeed);

        Vector3 moveDirection = transform.right * _input.MoveDirection.x + transform.forward * _input.MoveDirection.y;
        // Debug.Log("PlayerController: Before PC move");
        _cc.Move((moveDirection.normalized * targetSpeed + Vector3.up * _verticalVelocity) * Time.fixedDeltaTime);
        // Debug.Log("PlayerController: After PC move");

        // armature
        _currentArmatureController.UpdateSpeed(targetSpeed);
        _currentArmatureController.UpdateMotionSpeed(1.0f);
        _currentArmature.transform.position = transform.position;
        _currentArmature.transform.rotation = transform.rotation;
        _currentArmature.GetComponent<PlayerArmatureController>().LocalCinemachineCameraPitch = _cinemachineCameraPitch;
        // Debug.LogFormat("PlayerController: Current Velocity2 {0}", _cc.velocity);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        IReversible reversible;
        if(hit.gameObject.TryGetComponent<IReversible>(out reversible))
        {
            // Only hit buttom need record karma
            if(hit.point.y < (_cc.transform.position + _cc.center).y - (_cc.height / 2 - _cc.radius))
            {
                // add Karma
                int uid = _currentArmatureController.GetReversibleUID();
                if(uid >= 0) { reversible.AddKarmaAsCause(TimeManager.Instance.CurrentTime, uid); }
                // Debug.Log("PlayerController: hit bottom");
            }
            else
            {
                // Debug.Log("PlayerController: hit but not bottom");

            }
        }
    }

    public void AddKarmaAsCauseToCurrentArmature(int time, int effect)
    {
        _currentArmatureController.AddKarmaAsCause(time, effect);
    }

    private void LateUpdate()
    {
        if(_physicUpdateEnabled)
        {
            UpdateCameraRotation();
            _hand.HoldTargetUpdate();
        }
    }

    public void MarkPhysicUpdate(bool state)
    {
        _physicUpdateEnabled = state;
    }

    private void UpdateCameraRotation()
    {
        // CinemachineCameraTarget.transform.localPosition = CameraOffset;
        if(_input.LookDirection.sqrMagnitude > _lookMovementThresh || Mathf.Abs(CinemachineCameraTarget.transform.localRotation.x - _cinemachineCameraPitch) > 1.0f)
        {
            float movementMultiplier = _input.IsKeyboardMouse() ? 1.0f : Time.deltaTime;
            _cinemachineCameraPitch += (_input.LookDirection.y * MouseSensitivity.y * movementMultiplier) % 360.0f;
            _cinemachineCameraPitch += (
                (_cinemachineCameraPitch < -360.0f) ? 360.0f:0.0f) - ((_cinemachineCameraPitch > 360.0f) ? 360.0f : 0.0f
            );
            _cinemachineCameraPitch = Mathf.Clamp(_cinemachineCameraPitch, _cinemachineCameraPitchMin, _cinemachineCameraPitchMax);
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineCameraPitch, 0.0f, 0.0f);

            // first person, rotate horizontal, rotate whole player when camera rotate
            transform.Rotate(Vector3.up * _input.LookDirection.x * MouseSensitivity.x * movementMultiplier);
        }
        // RaycastHit hit;
        // Vector3[] dir = {
        //     CinemachineCameraTarget.transform.forward,
        //     CinemachineCameraTarget.transform.forward + CinemachineCameraTarget.transform.up * 0.18f + CinemachineCameraTarget.transform.right * 0.31f,
        //     CinemachineCameraTarget.transform.forward + CinemachineCameraTarget.transform.up * 0.18f - CinemachineCameraTarget.transform.right * 0.31f,
        //     CinemachineCameraTarget.transform.forward - CinemachineCameraTarget.transform.up * 0.18f + CinemachineCameraTarget.transform.right * 0.31f,
        //     CinemachineCameraTarget.transform.forward - CinemachineCameraTarget.transform.up * 0.18f - CinemachineCameraTarget.transform.right * 0.31f,
        // };
        // float distance = 0.0f;
        // for(int i = 0; i < dir.Length; i++)
        // {
        //     Debug.DrawRay(CinemachineCameraTarget.transform.position, dir[i].normalized * 0.6f, Color.green, 0.1f);
        //     if(Physics.Raycast(CinemachineCameraTarget.transform.position, dir[i].normalized, out hit, 0.6f, CameraCollisionDetectLayers))
        //     {
        //         distance = Mathf.Max(distance, (0.5f - hit.distance) * 5);
        //         Debug.DrawRay(CinemachineCameraTarget.transform.position, dir[i].normalized * 0.6f, Color.red, 0.1f);
        //     }
        //     else
        //     {
        //         Debug.DrawRay(CinemachineCameraTarget.transform.position, dir[i].normalized * 0.6f, Color.green, 0.1f);
        //     }
        // }
        // CinemachineCameraTarget.transform.position -= CinemachineCameraTarget.transform.forward * distance;
    }

    public void SwitchArmature(int idx)
    {
        idx--;
        if(idx >= PlayerArmatureManager.Instance.NumArmature || idx < 0 || idx == _currentArmatureIdx) { return; }
        _currentArmatureIdx = idx;
        Debug.LogFormat("PlayController: Switch to Armature {0}", _currentArmatureIdx);
        if(_currentArmatureController != null)
        {
            _currentArmatureController.OnPlayerSwitchOut();
        }
        _currentArmature = PlayerArmatureManager.Instance.GetArmature(idx);
        _currentArmatureController = _currentArmature.GetComponent<PlayerArmatureController>();
        _currentArmatureController.OnPlayerSwitchIn(PhysicUpdate, this);
        _physicUpdateDelegated = true;

        // temporary disable CC to move player
        _cc.enabled = false;
        transform.position = _currentArmature.transform.position;
        transform.rotation = _currentArmature.transform.rotation;
        _cinemachineCameraPitch = _currentArmatureController.LocalCinemachineCameraPitch;
        _cc.enabled = true;
    }
}
