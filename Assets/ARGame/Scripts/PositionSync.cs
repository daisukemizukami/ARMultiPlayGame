using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using UniRx;
using UnityEngine.UI;

public class PositionSync : MonoBehaviour
{

	[SerializeField] private int _WSPort;
	[SerializeField] private Transform _syncObjTransform;   //Share transform
    [SerializeField] GameObject OtherCube;   //Share transform


    [SerializeField] private SyncPhase _nowPhase;

	private WebSocket ws;
    public InputField IP_inputField;

	public enum SyncPhase
	{
		Idling,
		Syncing
	}

	private void Awake()
	{
		_nowPhase = SyncPhase.Idling;

		var cTransformValue = gameObject.ObserveEveryValueChanged(_ => _syncObjTransform.position);
		cTransformValue.Subscribe(x => OnChangedTargetTransformValue(x));
	}

	/// <summary>
	/// Get Down Start Sync Button
	/// </summary>
	public void OnSyncStartButtonDown()
	{
		var ca = "ws://" + IP_inputField.text + ":" + _WSPort.ToString();
		Debug.Log("Connect to " + ca);
		ws = new WebSocket(ca);

        var context = System.Threading.SynchronizationContext.Current;


        //Add Events
        //On catch message event
        ws.OnMessage += (object sender, MessageEventArgs e) => {
			print(e.Data);

            Vector3 pos = StringToVector3(e.Data);
            print(e.Data);
            print(pos.x);
            print(pos.y);
            print(pos.z);


            // Main Threadで実行する.
            context.Post(state =>
            {
                OtherCube.transform.position = pos;

            }, e.Data);


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
        _nowPhase = SyncPhase.Idling;

    }

    public void OnChangedTargetTransformValue(Vector3 pos)
	{
		if (_nowPhase == SyncPhase.Syncing)
		{
			Debug.Log(pos);

            ws.Send(pos.ToString());
            //ws.Send(pos);

        }
       
	}

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