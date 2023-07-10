using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum PlayerMaterialsState
{
    NORMAL,
    FIRST_PERSON,
    PHANTOM
}

public class PlayerArmatureController : MonoBehaviour
{
    // debug
    public int armatureIdx;

    #region PrivateVar
    // components
    private SkinnedMeshRenderer _meshRender;
    private CapsuleCollider _collider;
    private ReversiblePlayer _reversible;

    // animator IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    // animator
    private Animator _animator;

    // player
    private bool _isCurrentPlayer;
    private PlayerController _playerController;
    #endregion PrivateVar

    #region PublicAccess
    // camera info
    public float LocalCinemachineCameraPitch;

    // material
    public Material[] NormalMaterials;
    public Material[] FirstPersonMaterials;
    public Material[] PhantomMaterials;
    #endregion PublicAccess

    private void Awake()
    {
        _meshRender = GetComponentInChildren<SkinnedMeshRenderer>();
        _collider = GetComponent<CapsuleCollider>();
        _animator = GetComponent<Animator>();
        _reversible = GetComponent<ReversiblePlayer>();
        _isCurrentPlayer = false;
        AssignAnimationIDs();
    }

    void Start()
    {
    }
    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }
    // takeover movement from PlayerController
    // called by OnTimeMoveBack
    // mark PlayerController no physic update
    public void UpdatePlayerPos(Vector3 position, Quaternion rotation)
    {
        if(_isCurrentPlayer)
        {
            _playerController.transform.position = position;
            _playerController.transform.rotation = rotation;
            _playerController.MarkPhysicUpdate(false);
        }
    }

    public void SetPhantom(bool isPhantom)
    {
        if(isPhantom)
        {
            _collider.enabled = false;
            SetMaterial(PlayerMaterialsState.PHANTOM);
        }
        else
        {
            _collider.enabled = true;
            SetMaterial(PlayerMaterialsState.NORMAL);
        }
    }

    public void SetPlayerIn()
    {
        _collider.enabled = false;
        SetMaterial(PlayerMaterialsState.FIRST_PERSON);
    }

    public int GetReversibleUID()
    {
        if(_reversible == null) { return -1; }
        return _reversible.GetReversibleUID();
    }

    public void AddKarmaAsCause(int time, int effect)
    {
        _reversible.AddKarmaAsCause(time, effect);
    }

    public void SetMaterial(PlayerMaterialsState materialState)
    {
        if(materialState == PlayerMaterialsState.NORMAL)
        {
            _meshRender.materials = NormalMaterials;
        }
        else if(materialState == PlayerMaterialsState.FIRST_PERSON)
        {
            _meshRender.materials = FirstPersonMaterials;
        }
        else if(materialState == PlayerMaterialsState.PHANTOM)
        {
            _meshRender.materials = PhantomMaterials;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    #region PlayerSwitchIn_Out
    public void OnPlayerSwitchIn(Action physicUpdate, PlayerController controller)
    {
        _isCurrentPlayer = true;
        // _meshRender.enabled = false;
        _collider.enabled = false;
        _reversible.RegisterPhysicUpdate(physicUpdate);
        _reversible.SetReplayState(false);
        _playerController = controller;
        TimeManager.Instance.ReverseTo(_reversible.GetLastAvailableTime());
        SetMaterial(PlayerMaterialsState.FIRST_PERSON);
    }
    public void OnPlayerSwitchOut()
    {
        _isCurrentPlayer = false;
        _meshRender.enabled = true;
        _collider.enabled = true;
        _reversible.CancelAllPhysicUpdate();
        _reversible.SetReplayState(true);
        _playerController = null;
    }
    #endregion PlayerSwitchIn_Out

    #region AnimatorVisit
    public float GetPlayerSpeed()
    {
        return _animator.GetFloat(_animIDSpeed);
    }
    public bool GetGrounded()
    {
        return _animator.GetBool(_animIDGrounded);
    }
    public bool GetJump()
    {
        return _animator.GetBool(_animIDJump);
    }
    public bool GetFreeFall()
    {
        return _animator.GetBool(_animIDFreeFall);
    }
    #endregion AnimatorVisit

    #region AnimatorUpdate
    public void UpdateSpeed(float speed)
    {
        _animator.SetFloat(_animIDSpeed, speed);
    }
    public void UpdateGround(bool grounded)
    {
        _animator.SetBool(_animIDGrounded, grounded);
    }
    public void UpdateJump(bool jump)
    {
        _animator.SetBool(_animIDJump, jump);
    }
    public void UpdateFreeFall(bool freeFall)
    {
        _animator.SetBool(_animIDFreeFall, freeFall);
    }
    public void UpdateMotionSpeed(float motionSpeed)
    {
        _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
    }
    #endregion AnimatorUpdate

    #region AnimationEvent
    private void OnLand(AnimationEvent animationEvent) {}
    private void OnFootstep(AnimationEvent animationEvent) {}
    #endregion AnimationEvent
}
