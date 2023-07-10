using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    #region Const
    public const int MINIMAL_TIME = 0;
    #endregion Const
    #region Singleton
    private static TimeManager _instance;
    public static TimeManager Instance { get { return _instance; } }
    #endregion Singleton

    #region PublicAccess
    public event Action OnTimeMoveForward;
    public event Action OnTimeMoveBackward;
    public event Action OnTimeMoveResume; // reverse -> forward
    public int CurrentTime { get { return _currentTime; } }
    public bool IsReverse { get { return _isReverse; } set { _isReverse = value; } }

    public int ReverseSpeed = 5;
    #endregion PublicAccess

    #region PrivateVar
    private List<IReversible> _watchedObjects;
    private int _currentTime;

    // reverse flags
    private bool _isReverse;
    private bool _reverseJumpFlag; // true when ReverseTo called
    private bool _lastReverseState;
    #endregion PrivateVar

    private void Awake()
    {
        if(_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        ResetTimeManager();
    }

    private void Start()
    {
    }

    private void ResetTimeManager()
    {
        // forward by default
        _isReverse = false;
        _lastReverseState = false;
        
        _currentTime = -1;

        _watchedObjects = new List<IReversible>();
    }

    private void Update()
    {
    }

    private void FixedUpdate()
    {
        if(_isReverse)
        {
            _currentTime = Mathf.Max(_currentTime - ReverseSpeed, MINIMAL_TIME);
            OnTimeMoveBackward?.Invoke();
            Debug.LogFormat("TimeReverse: Reverse to {0}", _currentTime);
        }
        else if(_reverseJumpFlag)
        {
            OnTimeMoveBackward?.Invoke();
        }
        else
        {
            if(_lastReverseState)
            {
                // current time stop
                OnTimeMoveResume?.Invoke();
            }
            else
            {
                _currentTime += 1;
                Debug.LogFormat("TimeReverse: Forward to {0}", _currentTime);
                OnTimeMoveForward?.Invoke();
            }
        }
        _lastReverseState = _isReverse || _reverseJumpFlag;
        _reverseJumpFlag = false;
    }

    public void RegisterReversibleObject(IReversible obj)
    {
        // use count as UID, start from 0
        obj.SetReversibleUID(_watchedObjects.Count);
        _watchedObjects.Add(obj);

        OnTimeMoveForward += obj.OnTimeMoveForward;
        OnTimeMoveBackward += obj.OnTimeMoveBackward;
        OnTimeMoveResume += obj.OnTimeMoveResume;
    }

    // directly reverse to target time
    // <time> can be greater than current time
    public void ReverseTo(int time)
    {
        _currentTime = Mathf.Max(time, 0);
        _reverseJumpFlag = true;
        _lastReverseState = true;
        Debug.LogFormat("TimeReverse: Jump Reverse to {0}", _currentTime);
    }

    public void DestoryKarmaCause(int time, int causeIdx)
    {
        _watchedObjects[causeIdx].OnKarmaDestroyed(time);
    }
    public void DestroyKarmaEffect(int time, int effectIdx)
    {
        _watchedObjects[effectIdx].OnKarmaDestroyed(time);
    }

}
