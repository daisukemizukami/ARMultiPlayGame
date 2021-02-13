using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx.Triggers;
using UniRx;
using System;
using WebSocketSharp;
using ARGameSettings;
//using PlayerData;


public class BulletManager : MonoBehaviour
{
    [SerializeField] float destroytime = 5f;
    [SerializeField] public int id = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        Observable.Timer(TimeSpan.FromSeconds(destroytime)).Subscribe(_ =>
        {
            Destroy(gameObject);
           
        }).AddTo(this);

    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<OtherPlayerManager>() != null)
        {
            int hitid = other.gameObject.GetComponent<OtherPlayerManager>().id;
            Debug.LogWarning("hitid : " + hitid);

            //TODO tag playerを変数に
            if (hitid != this.id & other.gameObject.tag == "player")
            {
                WebSocket ws = GameObject.Find("GameManager").GetComponent<PositionSync>().ws;

                JsonData Item = new JsonData();
                Item.type = "hit";
                Item.id = other.gameObject.GetComponent<OtherPlayerManager>().id;
                string serialisedItemJson = JsonUtility.ToJson(Item);
                ws.Send(serialisedItemJson);
                Debug.LogWarning("send ");

            }

        } 
        
    }




}
