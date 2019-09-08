using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Triggers;
using UniRx;
using System;

public class BulletManager : MonoBehaviour
{
    [SerializeField] float destroytime = 5f;
    // Start is called before the first frame update
    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(destroytime)).Subscribe(_ =>
        {
            Destroy(gameObject);
           
        }).AddTo(this);

    }

    
}
