using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// support LessEqual search 
public class LESortedList<TValue, TKey> : List<TValue> where TKey: IComparable<TKey>
{
    #region PrivateValue
    Func<TValue, TKey> _getKey;
    int _fastCount; // deprecate value from index <_fastCount> but don't delete
    #endregion PrivateValue

    #region PublicAccess
    new public int Count { get { return _fastCount; } }
    #endregion PublicAccess

    public LESortedList(Func<TValue, TKey> getKey)
    {
        _getKey = getKey;
        _fastCount = 0;
    }
    new public int Add(TValue value)
    {
        int idx = GetLEIndexOfKey(_getKey(value)) + 1;
        if(idx == _fastCount && _fastCount < base.Count) { base[_fastCount] = value; }
        else { Insert(idx, value); }
        _fastCount++;
        return idx; 
    }
    // try add value to target index,
    // if value not satisfy Sorted condition, fall to normal add
    public int Add(TValue value, int idx)
    {
        if(idx < 0 || idx > _fastCount) { throw new IndexOutOfRangeException(); }
        if(idx > 0 && _getKey(this[idx - 1]).CompareTo(_getKey(value)) > 0)
        {
            // Debug.LogFormat("LESortedList: Add with idx: idx {0}, compare {1} > {2}", idx, _getKey(this[idx - 1]), _getKey(value));
            idx = GetLEIndexOfKey(_getKey(value), 0, idx) + 1;
            // Debug.LogFormat("LESortedList: Add with idx: result {0}", idx);
        }
        else if(idx < _fastCount && _getKey(this[idx]).CompareTo(_getKey(value)) < 0)
        {
            // Debug.LogFormat("LESortedList: Add with idx: idx {0}, compare {1} < {2}", idx, _getKey(this[idx]), _getKey(value));
            idx = GetLEIndexOfKey(_getKey(value), idx, _fastCount) + 1;
            // Debug.LogFormat("LESortedList: Add with idx: result {0}", idx);
        }
        if(idx == _fastCount && _fastCount < base.Count) { base[_fastCount] = value; }
        else { Insert(idx, value); }        
        _fastCount++;
        return idx;
    }
    public int GetLEIndexOfKey(TKey key)
    {
        return GetLEIndexOfKey(key, 0, _fastCount);
    }
    // return -1 if not found
    public int GetLEIndexOfKey(TKey key, int startIdx, int endIdx)
    {
        if(_fastCount == 0 || (_getKey(this[startIdx]).CompareTo(key)) > 0) { return startIdx - 1; }
        if((_getKey(this[endIdx - 1]).CompareTo(key)) <= 0) { return endIdx - 1; }
        for(; startIdx + 1 < endIdx;)
        {
            int midIdx = (startIdx + endIdx) / 2;
            if((_getKey(this[midIdx]).CompareTo(key)) <= 0) { startIdx = midIdx; }
            else { endIdx = midIdx; }
        }
        return startIdx;
    }
    public void FastShrinkTo(int idx)
    {
        // Debug.LogFormat("LESortedList: shrink from {0} to {1}, condition {2}", _fastCount, idx, idx >= 0 && idx <= _fastCount);
        if(idx >= 0 && idx <= _fastCount) { _fastCount = idx; }
        // Debug.LogFormat("LESortedList: shrink result {0} {1}", _fastCount, Count);
    }

    new public TValue this[int idx]
    {
        get {
            if(idx >= 0 && idx < _fastCount) { return base[idx]; }
            throw new IndexOutOfRangeException();
        }
        set {
            if(idx >= 0 && idx < _fastCount) { base[idx] = value; }
            throw new IndexOutOfRangeException();
        }
    }

    // visit element, dismiss _fastCount
    public TValue ForceIndex(int idx)
    {
        return base[idx];
    }
}
