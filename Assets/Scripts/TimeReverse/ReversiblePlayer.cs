using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct PlayerMovementFrameState
{
    public int Time;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public int AnimatorState;
    public float AnimatorCurrentDuration;
    public float AnimatorParamSpeed;
    public int AnimatorParamBools;

    public PlayerMovementFrameState(
        int time,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity,
        int animatorState,
        float animatorCurrentDuration,
        float animatorParamSpeed,
        bool animatorParamGrounded,
        bool animatorParamJump,
        bool animatorParamFreefall
    )
    {
        this.Time = time;
        this.Position = position;
        this.Rotation = rotation;
        this.Velocity = velocity;
        this.AnimatorState = animatorState;
        this.AnimatorCurrentDuration = animatorCurrentDuration;
        this.AnimatorParamSpeed = animatorParamSpeed;
        this.AnimatorParamBools = (animatorParamGrounded ? 1 : 0) + (animatorParamJump ? 1 << 1 : 0) + (animatorParamFreefall ? 1 << 2 : 0);
    }
}

public class ReversiblePlayer : MonoBehaviour, IReversible
{
    #region PrivateVar
    private int _lastTime;
    // movement history
    private LESortedList<PlayerMovementFrameState, int> _history;
    private int _historyIdx;
    // karma history
    private LESortedList<Tuple<int, int>, int> _karmaHistory; // time, effect idx
    private int _karmaHistoryIdx;

    private TimeManager _manager;

    // animator
    private Animator _animator;

    // armature controller
    private PlayerArmatureController _armature;
    #endregion PrivateVar

    #region PublicAccess
    public event Action UpdatePhysic;
    public bool IsReplay;

    public int NearHistorySearchRange;
    public int ReversibleUID { get; set; }
    #endregion PublicAccess
    
    void Awake()
    {
        _armature = GetComponent<PlayerArmatureController>();
        _animator = GetComponent<Animator>();

        _lastTime = TimeManager.MINIMAL_TIME - 1;
        _history = new LESortedList<PlayerMovementFrameState, int>((val) => val.Time);
        _karmaHistory = new LESortedList<Tuple<int, int>, int>((val) => val.Item1);
        _karmaHistoryIdx = 0;
    }
    void Start()
    {
        AnimatorStateInfo animatorState = _animator.GetCurrentAnimatorStateInfo(0);
        PlayerMovementFrameState initState = new PlayerMovementFrameState(
            _lastTime,
            transform.position,
            transform.rotation,
            Vector3.zero,
            animatorState.shortNameHash,
            animatorState.normalizedTime,
            0.0f, // init speed 0
            false,
            false,
            false
        );
        _history.Add(initState);
        _historyIdx = _history.Count;

        _manager = TimeManager.Instance;
        _manager.RegisterReversibleObject(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetInitTransform(Vector3 position, Quaternion rotation)
    {
        Debug.Assert(_history.Count >= 1);
        PlayerMovementFrameState oldInitState = _history[0];
        PlayerMovementFrameState initState = new PlayerMovementFrameState(
            oldInitState.Time,
            position,
            rotation,
            Vector3.zero,
            oldInitState.AnimatorState,
            oldInitState.AnimatorCurrentDuration,
            0.0f,
            false,
            false,
            false
        );
        _history[0] = initState;
    }

    public void RegisterPhysicUpdate(Action physicUpdate)
    {
        UpdatePhysic += physicUpdate;
    }

    public void CancelAllPhysicUpdate()
    {
        if(UpdatePhysic == null) { return; }
        foreach(var update in UpdatePhysic.GetInvocationList())
        {
            UpdatePhysic -= update as Action;
        }
    }

    #region IReversible
    public void SetReversibleUID(int UID)
    {
        ReversibleUID = UID;
        Debug.LogFormat("ReversiblePlayer with UID {0}", ReversibleUID);    
    }
    public int GetReversibleUID()
    {
        return ReversibleUID;
    }
    public void AddKarmaAsEffect(int time, int causeIdx)
    {
    }
    // record the idx of effect
    public void AddKarmaAsCause(int time, int effectIdx)
    {
        _karmaHistory.Add(new Tuple<int, int>(time, effectIdx));
        Debug.LogFormat("ReversiblePlayer add {0}: {1}", time, effectIdx);
    }
    // when related karma destroyed
    public void OnKarmaDestroyed(int time)
    {
        Debug.Assert(_lastTime <= time);
        Debug.LogFormat("ReversiblePlayer {0}: delete to {1}", ReversibleUID, time);
        if(_history[_history.Count - 1].Time > time)
        {
            _history.FastShrinkTo(_history.GetLEIndexOfKey(time, _historyIdx - 1, _history.Count) + 1);
        }
        int karmaIdx = _karmaHistory.GetLEIndexOfKey(time) + 1;
        int karmaOldCount = _karmaHistory.Count;
        if(karmaIdx >= karmaOldCount) { return; }
        _karmaHistory.FastShrinkTo(karmaIdx);
        for(int idx = karmaIdx; idx < karmaOldCount; idx++)
        {
            var item = _karmaHistory.ForceIndex(idx);
            _manager.DestroyKarmaEffect(item.Item1, item.Item2);
        }
    }
    public void OnTimeMoveForward()
    {
        Debug.Assert(_lastTime < _manager.CurrentTime);
        // not replay, record next movement
        if(!IsReplay)
        {
            // not replay, update armature material and collider
            _armature.SetPlayerIn();
            // not replay, add append/overwrite new state into history
            // shrink _history to current idx point to
            _history.FastShrinkTo(_historyIdx);
            _lastTime = _manager.CurrentTime;
            OnKarmaDestroyed(_lastTime);
            UpdatePhysic?.Invoke();
            AnimatorStateInfo animatorState = _animator.GetCurrentAnimatorStateInfo(0);
            PlayerMovementFrameState state = new PlayerMovementFrameState(
                _lastTime,
                transform.position,
                transform.rotation,
                Vector3.zero,
                animatorState.shortNameHash,
                animatorState.normalizedTime,
                _armature.GetPlayerSpeed(),
                _armature.GetGrounded(),
                _armature.GetJump(),
                _armature.GetFreeFall()
            );
            // Debug.LogFormat("TimeReverse: Time Forward, last point {0}, this point {1}", _history[_historyIdx - 1].Time, _history[_historyIdx].Time);
            int insertResult = _history.Add(state, _historyIdx);
            // Debug.LogFormat("TimeReverse: Time Forward, write to {0}, result {1}", _historyIdx, insertResult);
            Debug.Assert(insertResult == _historyIdx);
            _historyIdx++;

            // Debug.LogFormat("TimeReverse: Armature {0}, ", _armature.armatureIdx);
        }
        else
        {
            // replay, directly use backward
            // Debug.LogFormat("TimeReverse: Armature {0} replay to {1}, {2}", _armature.armatureIdx, _manager.CurrentTime, _lastTime);
            OnTimeMoveBackward();
        }
    }

    public void OnTimeMoveBackward()
    {
        int lastIdx = -1;
        if(_lastTime >= _manager.CurrentTime)
        {
            _lastTime = _manager.CurrentTime;
            for(int nearIdx = _historyIdx - 1; nearIdx >= 0 && nearIdx >= _historyIdx - NearHistorySearchRange; nearIdx--)
            {
                if(_history[nearIdx].Time <= _lastTime)
                {
                    lastIdx = nearIdx;
                    break;
                }
            }
            if(lastIdx >= 0) { _historyIdx = lastIdx + 1;}
            else
            {
                _historyIdx = _history.GetLEIndexOfKey(_lastTime, 0, _historyIdx) + 1;
            }
        }
        else
        {
            _lastTime = _manager.CurrentTime;
            for(int nearIdx = _historyIdx; nearIdx < _history.Count && nearIdx < _historyIdx + NearHistorySearchRange; nearIdx++)
            {
                if(_history[nearIdx].Time > _lastTime)
                {
                    lastIdx = nearIdx;
                    break;
                }
            }
            if(lastIdx >= 0) { _historyIdx = lastIdx;}
            else
            {
                _historyIdx = _history.GetLEIndexOfKey(_lastTime, _historyIdx - 1, _history.Count) + 1;
            }
        }
        var state = _history[_historyIdx - 1];
        transform.position = state.Position;
        transform.rotation = state.Rotation;
        _armature.UpdatePlayerPos(state.Position, state.Rotation);
        if(_historyIdx == _history.Count && _lastTime > state.Time)
        {
            if(IsReplay) { SetReplayTerminate(true); }            
        }
        else
        {
            SetReplayTerminate(false);
            _armature.UpdateSpeed(state.AnimatorParamSpeed);
            _armature.UpdateGround((state.AnimatorParamBools & 1 << 0) != 0);
            _armature.UpdateJump((state.AnimatorParamBools & 1 << 1) != 0);
            _armature.UpdateFreeFall((state.AnimatorParamBools & 1 << 2) != 0);
            AnimatorStateInfo curState = _animator.GetCurrentAnimatorStateInfo(0);
            if(curState.shortNameHash != state.AnimatorState || Mathf.Abs(curState.normalizedTime - state.AnimatorCurrentDuration) > 0.05f)
            {
                _animator.Play(state.AnimatorState, 0,state.AnimatorCurrentDuration);
            }
        }
        // Debug.LogFormat("TimeReverse: Armature {0} replay to {1}, history {2}/{3}", _armature.armatureIdx, _lastTime, state.Time, _history.Count);
    }
    public void OnTimeMoveResume()
    {
        var state = _history[_historyIdx - 1];
        transform.position = state.Position;
        transform.rotation = state.Rotation;
        _armature.UpdateSpeed(state.AnimatorParamSpeed);
        _animator.Play(state.AnimatorState, 0, state.AnimatorCurrentDuration);
    }
    #endregion IReversible

    public int GetLastAvailableTime()
    {
        if(_history.Count < 1) { return _lastTime; }
        // if <_lastTime> out of history, limit to the last record in history
        return Mathf.Min(_lastTime, _history[_history.Count - 1].Time);
    }

    public void SetReplayTerminate(bool isTerminate)
    {
        if(isTerminate)
        {
            _animator.enabled = false;
            _armature.SetPhantom(true);
        }
        else
        {
            _animator.enabled = true;
            _armature.SetPhantom(false);
        }
    }

    public void SetReplayState(bool replay)
    {
        IsReplay = replay;
    }
}
