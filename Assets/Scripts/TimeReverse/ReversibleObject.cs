using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public struct ObjectMovementFrameState
{
    public int Time;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public Vector3 AngularVelocity;

    public ObjectMovementFrameState(
        int time,
        Vector3 position,
        Quaternion rotation,
        Vector3 velocity,
        Vector3 angularVelocity
    )
    {
        this.Time = time;
        this.Position = position;
        this.Rotation = rotation;
        this.Velocity = velocity;
        this.AngularVelocity = angularVelocity;
    }

}

[RequireComponent(typeof(Rigidbody))]
public class ReversibleObject : MonoBehaviour, IReversible
{
    #region RelatedComponents
    Rigidbody _rig;
    TimeManager _manager;
    #endregion RelatedComponents

    #region PrivateVar
    int _lastTime;
    int _historyIdx;
    LESortedList<ObjectMovementFrameState, int> _history;
    int _karmaHistoryIdx;
    LESortedList<Tuple<int, int>, int> _karmaHistory;
    #endregion PrivateVar

    #region PublicAccess
    public int NearHistorySearchRange = 10;
    public int ReversibleUID { get; set; }
    #endregion PublicAccess

    private void Awake()
    {
        _rig = GetComponent<Rigidbody>();
        
        _lastTime = TimeManager.MINIMAL_TIME - 1;
        _history = new LESortedList<ObjectMovementFrameState, int>((val) => val.Time);
        _karmaHistory = new LESortedList<Tuple<int, int>, int>((val) => val.Item1);
        _karmaHistoryIdx = 0;
    }

    private void Start()
    {
        _manager = TimeManager.Instance;
        _manager.RegisterReversibleObject(this);

        ObjectMovementFrameState initState = new ObjectMovementFrameState(
            _lastTime,
            _rig.position,
            _rig.rotation,
            _rig.velocity,
            _rig.angularVelocity
        );
        _history.Add(initState);
        _historyIdx = _history.Count;
    }

    private void OnCollisionEnter(Collision other)
    {
        IReversible reversible;
        if(_manager.CurrentTime >= _history[_history.Count - 1].Time && other.gameObject.TryGetComponent<IReversible>(out reversible))
        {
            // Debug.LogFormat("ReversibleObject hit time {0}, {1}",_manager.CurrentTime, _history[_history.Count - 1].Time);
            AddKarmaAsCause(TimeManager.Instance.CurrentTime - 1, reversible.GetReversibleUID());
            Debug.LogFormat("ReversibleObject hit {0} - {1}", ReversibleUID, reversible.GetReversibleUID());
        }
    }

    #region IReversible
    public void SetReversibleUID(int UID)
    {
        ReversibleUID = UID;
        Debug.LogFormat("ReversibleObject with UID {0}", ReversibleUID);
    }
    public int GetReversibleUID()
    {
        return ReversibleUID;
    }
    public void AddKarmaAsEffect(int time, int causeIdx)
    {

    }
    public void AddKarmaAsCause(int time, int effectIdx)
    {
        _karmaHistory.Add(new Tuple<int, int>(time, effectIdx));
    }
    public void OnKarmaDestroyed(int time)
    {
        Debug.Assert(_lastTime <= time);
        Debug.LogFormat("ReversibleObject {0}: delete to {1}", ReversibleUID, time);
        if(_history[_history.Count - 1].Time > time)
        {
            Debug.LogFormat("ReversibleObject {0}: delete current history {1}", ReversibleUID, _history[_history.Count - 1].Time);
            int shrinkIdx = _history.GetLEIndexOfKey(time, _historyIdx - 1, _history.Count) + 1;
            // Debug.LogFormat(
            //     "TimeReverse: from {0}, {1} shrink to {2}, {3}",
            //     _history.Count,
            //     _history[_history.Count - 1].Time,
            //     shrinkIdx,
            //     _history[shrinkIdx - 1].Time
            // );
            _history.FastShrinkTo(shrinkIdx);
            // Debug.LogFormat("TimeReverse: shrinked count {0}", _history.Count);
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
        // time in history, replay, find next
        // else, record
        // Debug.LogFormat("TimeReverse: cur count {0}", _history.Count);
        if(_manager.CurrentTime <= _history[_history.Count - 1].Time)
        {
            // use backward to replay
            OnTimeMoveBackward();
        }
        else
        {
            // not replay, enable physic(gravity)
            _rig.useGravity = true;
            _lastTime = _manager.CurrentTime;
            ObjectMovementFrameState state = new ObjectMovementFrameState(
                _lastTime,
                _rig.position,
                _rig.rotation,
                _rig.velocity,
                _rig.angularVelocity
            );
            int insertResult = _history.Add(state, _historyIdx);
            Debug.Assert(insertResult == _historyIdx);
            _historyIdx++;
        }

    }

    public void OnTimeMoveBackward()
    {
        // in reverse / replay, disable physic
        _rig.useGravity = false;
        // reverse to past
        if(_lastTime >= _manager.CurrentTime)
        {
            // Debug.LogFormat("TimeReverse: reverse back, last time {0}, target time {1}", _lastTime, _manager.CurrentTime);
            _lastTime = _manager.CurrentTime;
            // move historyIdx
            int lastIdx = -1;
            // search near
            for(int nearIdx = _historyIdx - 1; nearIdx >= 0 && nearIdx >= _historyIdx - NearHistorySearchRange; nearIdx--)
            {
                if(_history[nearIdx].Time <= _lastTime)
                {
                    lastIdx = nearIdx;
                    break;
                }
            }
            if(lastIdx >= 0)
            {
                _historyIdx = lastIdx + 1;
            }
            else
            {
                _historyIdx = _history.GetLEIndexOfKey(_lastTime, 0, _historyIdx) + 1;
                // TODO: Current only support object created in the begining
                Debug.Assert(_historyIdx > 0);
            }
            // Debug.LogFormat("TimeReverse: reverse back, last time {0}, target time {1}", _lastTime, _manager.CurrentTime);
        }
        // move to future
        else
        {
            _lastTime = _manager.CurrentTime;
            // move historyIdx
            int lastIdx = -1;
            // search near
            for(int nearIdx = _historyIdx; nearIdx < _history.Count && nearIdx < _historyIdx + NearHistorySearchRange; nearIdx++)
            {
                if(_history[nearIdx].Time > _lastTime)
                {
                    lastIdx = nearIdx;
                    break;
                }
            }
            if(lastIdx >= 0) { _historyIdx = lastIdx; }
            else
            {
                _historyIdx = _history.GetLEIndexOfKey(_lastTime, _historyIdx - 1, _history.Count) + 1;
                Debug.Assert(_historyIdx <= _history.Count);
            }

        }
        // Debug.LogFormat("TimeReverse: reverse back, pos {0} to {1} with {2}", _rig.position, _history[_historyIdx - 1].Position, _rig.velocity);
        // load transform, clean velocity
        _rig.position = _history[_historyIdx - 1].Position;
        _rig.rotation = _history[_historyIdx - 1].Rotation;
        _rig.velocity = _history[_historyIdx - 1].Velocity;
    }

    public void OnTimeMoveResume()
    {
        // load movement state
        _rig.position = _history[_historyIdx - 1].Position;
        _rig.rotation = _history[_historyIdx - 1].Rotation;
        _rig.velocity = _history[_historyIdx - 1].Velocity;
        _rig.angularVelocity = _history[_historyIdx - 1].AngularVelocity;
    }
    #endregion IReversible
}
