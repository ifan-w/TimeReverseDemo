using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    #region PrivateVar
    // move
    private Vector2 _moveDirection;
    private Vector2 _lookDirection;
    private bool _isJumpPressed;
    private bool _isSpring;

    // reverse time
    private bool _isReversePressed;
    private PlayerInput _playerInput;

    // grab object
    private bool _isGrabPressed;
    #endregion PrivateVar

    #region PublicAccess
    public Vector2 MoveDirection { get { return _moveDirection; } }
    public Vector2 LookDirection { get { return _lookDirection; } }
    
    public bool IsJumpPressed {
        get {
            if(_isJumpPressed)
            {
                _isJumpPressed = false;
                return true;
            }
            return false;
        } 
    }
    public bool IsGrabPressed {
        get {
            if(_isGrabPressed)
            {
                _isGrabPressed = false;
                return true;
            }
            return false;
        }
    }
    public bool IsSpring { get { return _isSpring; } }

    public bool IsReversePressed { get { return _isReversePressed; } }

    public event Action<int> NumberEvent;
    #endregion PublicAccess

    // Start is called before the first frame update
    void Start()
    {
        _playerInput = GetComponent<PlayerInput>();
    }

    public void OnMove(InputValue value)
    {
        _moveDirection = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        _isJumpPressed = true;
    }

    public void OnLook(InputValue value)
    {
        _lookDirection = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        _isSpring = value.isPressed;
    }

    public void OnReverseTime(InputValue value)
    {
        _isReversePressed = value.isPressed;
        if(TimeManager.Instance != null)
        {
            TimeManager.Instance.IsReverse = _isReversePressed;
        }
    }

    public void OnGrab(InputValue value)
    {
        _isGrabPressed = true;
    }

    #region Numbers
    // skip num/armature switch when reverse
    public void OnNum1(InputValue value)
    {
        if(_isReversePressed) { return; }
        NumberEvent?.Invoke(1);
    }

    public void OnNum2(InputValue value)
    {
        if(_isReversePressed) { return; }
        NumberEvent?.Invoke(2);
    }

    public void OnNum3(InputValue value)
    {
        if(_isReversePressed) { return; }
        NumberEvent?.Invoke(3);
    }

    public void OnNum4(InputValue value)
    {
        if(_isReversePressed) { return; }
        NumberEvent?.Invoke(4);
    }
    #endregion Numbers

    public void RegisterNumberEvent(Action<int> numberEvent)
    {
        NumberEvent += numberEvent;
    }
    public bool IsKeyboardMouse()
    {
        return _playerInput.currentControlScheme == "KeyboardMouse";
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        Cursor.lockState = hasFocus ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
