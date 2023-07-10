using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IReversible
{
    public void SetReversibleUID(int UID);
    public int GetReversibleUID();
    public void AddKarmaAsEffect(int time, int causeIdx);
    public void AddKarmaAsCause(int time, int EffectIdx);
    public void OnKarmaDestroyed(int time);
    public void OnTimeMoveForward();
    public void OnTimeMoveBackward();
    public void OnTimeMoveResume();
}
