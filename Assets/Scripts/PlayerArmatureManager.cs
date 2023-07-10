using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArmatureManager : MonoBehaviour
{
    #region Singleton
    private static PlayerArmatureManager _instance;
    public static PlayerArmatureManager Instance { get { return _instance; } }
    #endregion Singleton

    #region PrivateVar
    private GameObject[] _armatureList;
    #endregion PrivateVar

    #region PublicAccess
    public GameObject PlayerArmaturePrefab;
    public Vector3 InitialPos;
    public Quaternion InitRotation;
    public float InitDistance;
    public int NumArmature;
    #endregion PublicAccess
    
    private void Awake()
    {
        // singleton
        if(_instance != null)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        _armatureList = new GameObject[NumArmature];
    }
    
    private GameObject InstantiateArmature(int idx)
    {
        GameObject obj = Instantiate(PlayerArmaturePrefab);
        obj.transform.rotation = InitRotation;
        obj.transform.position = InitialPos - obj.transform.forward * InitDistance * idx;
        // obj.GetComponent<ReversiblePlayer>().ResetInitTransform(InitialPos - obj.transform.forward * InitDistance * idx , InitRotation);
        return obj;
    }

    public GameObject GetArmature(int idx)
    {
        if(_armatureList[idx] == null)
        {
            _armatureList[idx] = InstantiateArmature(idx);
            _armatureList[idx].GetComponent<PlayerArmatureController>().armatureIdx = idx;
        }
        return _armatureList[idx];
    }
}
