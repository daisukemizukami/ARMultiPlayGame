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
    [SerializeField] GameObject OtherCube;   //Share transform
    [SerializeField] private SyncPhase _nowPhase;

    private WebSocket ws;
    public InputField IP_inputField;
    public InputField interval_inputField;

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

    public void OnChangedTargetTransformValue(Vector3 pos)
    {
        if (_nowPhase == SyncPhase.Syncing)
        {
            Debug.Log(pos);

            ws.Send(pos.ToString());

        }

    }

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
    

private void Update()
    {
        tmpTime += Time.deltaTime;
       if( tmpTime > _interval)
        {
            OnChangedTargetTransformValue(_syncObjTransform.position);


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
		ws = new WebSocket(ca);

       

        var context = System.Threading.SynchronizationContext.Current;


        //Add Events
        //On catch message event
        ws.OnMessage += (object sender, MessageEventArgs e) => {
			Debug.Log(e.Data);

            Vector3 pos = StringToVector3(e.Data);
            Debug.Log(e.Data);
     

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
