using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WebSocketSharp;
using UniRx;
using UnityEngine.UI;
using ARGameSettings;

public class PositionSync : MonoBehaviour
{

    [SerializeField] private int _WSPort;
    [SerializeField] private Transform _syncObjTransform;   //Share transform
    [SerializeField] SyncPhase _nowPhase;
    [SerializeField] private  float speed = 1f;

    private int _MyId;
    public Dictionary<int, GameObject> OherPlayers = new Dictionary<int, GameObject>();

    public GameObject bulletPrefab;
    public GameObject otherPlayerPrefab;


    public WebSocket ws;
    public InputField IP_inputField;
    public InputField interval_inputField;
    [SerializeField]
    private Text _myIdWindow;

    [SerializeField]
    private float tmpTime = 0f;
    [SerializeField]
    private float _interval = 0.1f;

    

    private void Awake()
    {
        _nowPhase = SyncPhase.Idling;

        //var cTransformValue = gameObject.ObserveEveryValueChanged(_ => _syncObjTransform.position);
        //cTransformValue.Subscribe(x => OnChangedTargetTransformValue(x));

        //intervalの監視
        var intervalValue = gameObject.ObserveEveryValueChanged(_ => interval_inputField.text);
        intervalValue.Subscribe(x => OnChangedTargetIntervalValue(x));

    }


    //playerのposを送信
    public void OnChangedTargetTransformValue(Vector3 pos,Vector3 rotation)
    {
        if (_nowPhase == SyncPhase.Syncing)
        {
            //Debug.Log("Player Pos : " + pos);

            JsonData Item = new JsonData();
            Item.type = "player";
            Item.id = _MyId;
            Item.posX = pos.x;
            Item.posY = pos.y;
            Item.posZ = pos.z;
            Item.rotationX = rotation.x;
            Item.rotationY = rotation.y;
            Item.rotationZ = rotation.z;

            string serialisedItemJson = JsonUtility.ToJson(Item);

            Debug.Log("serialisedItemJson : " + serialisedItemJson);


            ws.Send(serialisedItemJson);
        }
    }


    //UIInputから変更があればすぐに反映
    public void OnChangedTargetIntervalValue(String interval)
    {
        try
        {
            _interval = Convert.ToInt32(interval);
        }
        catch (FormatException)
        {
            _interval = 0.1f;
            interval_inputField.text =Convert.ToString (0.1);

        }
        catch (OverflowException)
        {
            _interval = 0.1f;
            interval_inputField.text = Convert.ToString(0.1);
        }


    }

    //AttackButton押下でmessage送信
    public void SkillShootPos()
    {
        //playerの位置からskillを出す
        var pos = _syncObjTransform;

        JsonData item = new JsonData();
        item.type = "skill";
        item.id = _MyId;
        item.posX = pos.position.x;
        item.posY = pos.position.y;
        item.posZ = pos.position.z;
        item.rotationX = pos.rotation.x;
        item.rotationY = pos.rotation.y;
        item.rotationZ = pos.rotation.z;

        string serialisedItemJson = JsonUtility.ToJson(item);
        ws.Send(serialisedItemJson);
    }
    

private void Update()
    {
        tmpTime += Time.deltaTime;
       if( tmpTime > _interval)
        {
          
            OnChangedTargetTransformValue(_syncObjTransform.position,_syncObjTransform.rotation);
     
            tmpTime = 0;
        }

    }

    /// <summary>
    /// Get Down Start Sync Button
    /// </summary>
    public void OnSyncStartButtonDown()
	{
		var ca = "ws://" + IP_inputField.text + ":" + _WSPort.ToString();
		Debug.Log("Connect to " + ca);
        ws = null;

        ws = new WebSocket(ca);

        var context = System.Threading.SynchronizationContext.Current;


        //Add Events
        //On catch message event
        ws.OnMessage += (object sender, MessageEventArgs e) => {

            Debug.Log("Recieved :" + e.Data);

            JsonData item = JsonUtility.FromJson<JsonData>(e.Data);


            Vector3 pos = new Vector3(item.posX, item.posY, item.posZ);
            Vector3 rotations = new Vector3(item.rotationX, item.rotationY, item.rotationZ);


            //wsが追加されたときに一度呼ばれる
            if (item.type == "connection")
            {
                _MyId = item.id;
                Debug.LogWarning("MyId is set" + _MyId);

                context.Post(state =>
                {
                    _myIdWindow.text = _MyId.ToString();
                }, null);
                Debug.Log("new connection id : " + _MyId);
             }
            //otherPlayerのpositionを更新
            else if (item.type == "player")
            {
                //OtherPlayersDicにaddされていないplayerがいたら追加
                if (!OherPlayers.ContainsKey(item.id)　& item.id != 0 & item.id != _MyId)
                {
                    context.Post(state =>
                    {
                        GameObject otherPlayer = Instantiate(otherPlayerPrefab) as GameObject;
                        OherPlayers.Add(item.id, otherPlayer);
                        otherPlayer.GetComponent<OtherPlayerManager>().id = item.id;

                        foreach (var otherplayer in OherPlayers)
                        {
                            Debug.LogWarning("new OtherPlayer comming");
                            Debug.LogWarning(otherplayer.Key);
                            Debug.LogWarning(otherplayer.Value);
                        }

                    }, null);
                }

                if (item.id != _MyId)
                {

                    // Main Threadでposition更新を実行する.
                    context.Post(state =>
                    {
                        //Debug.Log("player id : " + item.id + " moving");
                        if (item.id != 0)
                        {
                            OherPlayers[item.id].transform.position = pos;
                            OherPlayers[item.id].transform.rotations = rotations;
                        }
                    }, null);

                }

            }
            else if(item.type == "skill")
            {
                // Main Threadでposition更新を実行する.
                context.Post(state =>
                {
                    GameObject bullet = Instantiate(bulletPrefab) as GameObject;
                    bullet.GetComponent<BulletManager>().id = item.id;
                    Debug.LogWarning("bullet id is " + item.id);
                    bullet.transform.position = pos;
                    bullet.transform.rotations = rotations;
                    Vector3 force;
                    force = bullet.transform.forward * speed;
                    bullet.GetComponent<Rigidbody>().AddForce(force);

                }, null);

       
            }
            
            //else if (item.type == "anotherconnection")
            //{
            //    Debug.Log("anotherconnection");
            //    context.Post(state =>
            //    {
            //        GameObject otherPlayer = Instantiate(otherPlayerPrefab) as GameObject;
            //        Debug.Log("instantiate otherplayer");
            //        OherPlayers.Add(item.id,otherPlayer);
            //        Debug.Log("OherPlayers.Count :" + OherPlayers.Count);


            //    }, null);
            //    Debug.Log("Another Player comming id : " + item.id);
            //}



        };

		//On error event
		ws.OnError += (sender, e) => {
			Debug.Log("WebSocket Error Message: " + e.Message);
			_nowPhase = SyncPhase.Idling;
		};

		//On WebSocket close event
		ws.OnClose += (sender, e) => {
			Debug.Log("Disconnected Server");
            _nowPhase = SyncPhase.Idling;

        };

		ws.Connect();

		_nowPhase = SyncPhase.Syncing;
	}

	/// <summary>
	/// Get Down Stop Sync Button
	/// </summary>
	public void OnSyncStopButtonDown()
	{
		ws.Close(); //Disconnect
        ws = null;
        _nowPhase = SyncPhase.Idling;

    }

 
    //serverから受け取ったJson　Stringをvector3型に変換
    public static Vector3 StringToVector3(string sVector)
    {
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }
}
