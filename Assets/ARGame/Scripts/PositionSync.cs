using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using WebSocketSharp;
using UniRx;
using UnityEngine.UI;

public class PositionSync : MonoBehaviour
{

    [SerializeField] private int _WSPort;
    [SerializeField] private Transform _syncObjTransform;   //Share transform
    [SerializeField] private SyncPhase _nowPhase;
    [SerializeField] private  float speed = 1f;

    private int _MyId;
    public Dictionary<int, GameObject> OherPlayers = new Dictionary<int, GameObject>();

    public GameObject bulletPrefab;
    public GameObject otherPlayerPrefab;


    private WebSocket ws;
    public InputField IP_inputField;
    public InputField interval_inputField;
    [SerializeField]
    private Text _myIdWindow;

    [SerializeField]
    private float tmpTime = 0f;
    [SerializeField]
    private float _interval = 0.1f;

    public enum SyncPhase
    {
        Idling,
        Syncing
    }

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
    public void OnChangedTargetTransformValue(Vector3 pos,Vector3 eulerAngle)
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
            Item.eulerAngleX = eulerAngle.x;
            Item.eulerAngleY = eulerAngle.y;
            Item.eulerAngleZ = eulerAngle.z;

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
        item.posX = pos.position.x;
        item.posY = pos.position.y;
        item.posZ = pos.position.z;
        item.eulerAngleX = pos.eulerAngles.x;
        item.eulerAngleY = pos.eulerAngles.y;
        item.eulerAngleZ = pos.eulerAngles.z;

        string serialisedItemJson = JsonUtility.ToJson(item);
        ws.Send(serialisedItemJson);
    }
    

private void Update()
    {
        tmpTime += Time.deltaTime;
       if( tmpTime > _interval)
        {
          
            OnChangedTargetTransformValue(_syncObjTransform.position,_syncObjTransform.eulerAngles);
     
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
            Vector3 eulerAngles = new Vector3(item.eulerAngleX, item.eulerAngleY, item.eulerAngleZ);

            //otherPlayerのpositionを更新
            if (item.type == "player")
            {

                //TODO OtherPlayer追加をまとめる
                //OtherPlayersDicに追加されていないplayerがいたら追加
                if (!OherPlayers.ContainsKey(item.id))
                {
                    context.Post(state =>
                    {
                        GameObject otherPlayer = Instantiate(otherPlayerPrefab) as GameObject;
                        OherPlayers.Add(item.id, otherPlayer);

                    }, null);
                }

                if (item.id != _MyId)
                {

                    // Main Threadでposition更新を実行する.
                    context.Post(state =>
                    {
                        Debug.Log("player id : " + item.id + " moving");

                        Debug.LogWarning("warning start0");

                        foreach (var otherplayer in OherPlayers)
                        {
                            Debug.LogWarning("warning start");

                            Debug.LogWarning(otherplayer.Key);
                            Debug.LogWarning(otherplayer.Value);

                        }
                        if (item.id != 0)
                        {
                            OherPlayers[item.id].transform.position = pos;
                            OherPlayers[item.id].transform.eulerAngles = eulerAngles;
                        }
                    }, null);

                }

            }
            else if(item.type == "skill")
            {
                // Main Threadでposition更新を実行する.
                context.Post(state =>
                {
                    GameObject bullets = Instantiate(bulletPrefab) as GameObject;
                    bullets.transform.position = pos;
                    bullets.transform.eulerAngles = eulerAngles;
                    Vector3 force;
                    force = bullets.transform.forward * speed;
                    bullets.GetComponent<Rigidbody>().AddForce(force);

                }, null);

       
            }
            //wsが追加されたときに一度呼ばれる
            else if(item.type == "connection")
            {
                _MyId = item.id;
                context.Post(state =>
                {
                    _myIdWindow.text = _MyId.ToString();
                },null );
                Debug.Log("new connection id : " + _MyId);


            }
            else if (item.type == "anotherconnection")
            {
                Debug.Log("anotherconnection");
                context.Post(state =>
                {
                    GameObject otherPlayer = Instantiate(otherPlayerPrefab) as GameObject;
                    Debug.Log("instantiate otherplayer");
                    OherPlayers.Add(item.id,otherPlayer);
                    Debug.Log("OherPlayers.Count :" + OherPlayers.Count);


                }, null);
                Debug.Log("Another Player comming id : " + item.id);
            }



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
