using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grabable : MonoBehaviour
{
    #region PrivateVar
    private Rigidbody _rig;
    private ReversibleObject _reverseObj;
    private float _originDrag;
    #endregion PrivateVar

    #region PublicAccess
    public float RigidbodyDragOnHold;
    public int NormalLayer;
    public int GrabLayer;
    #endregion PublicAccess

    void Start()
    {
        _rig = GetComponent<Rigidbody>();
        _reverseObj = GetComponent<ReversibleObject>();
    }

    public int GetReversibleUID()
    {
        if(_reverseObj == null) { return -1; }
        return _reverseObj.GetReversibleUID();
    }

    // when player grab object, stop replay
    public void OnHold()
    {
        _rig.useGravity = false;
        _originDrag = _rig.drag;
        _rig.drag = RigidbodyDragOnHold;
        // when player hold this object, remove all following history
        _reverseObj?.OnKarmaDestroyed(TimeManager.Instance.CurrentTime + 1);
        gameObject.layer = GrabLayer;
    }

    public void OnLoosed()
    {
        _rig.drag = _originDrag;
        _rig.useGravity = true;
        gameObject.layer = NormalLayer;
    }

    // add speed to target, clean angularV
    public void MoveTo(Vector3 position)
    {
        _rig.velocity = (position - transform.position) * 20;
        _rig.angularVelocity = Vector3.zero;
        // _rig.transform.position = position;
    }

    private void OnCollisionEnter(Collision other) {
        PlayerController pc;
        if(other.gameObject.TryGetComponent<PlayerController>(out pc))
        {
            Debug.LogFormat("collision with pc");
        }
    }
}
