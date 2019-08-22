using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Text;
using System.Security.Cryptography;


using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif


public class ARGame : MonoBehaviour
{
    [Tooltip("The ARSession component controlling the session from which to generate ARWorldMaps.")]
    [SerializeField]
    ARSession m_ARSession;

    [SerializeField]
    Button m_SaveButton;

    public InputField IP_InputField;


    /// <summary>
    /// A UI button component which will generate an ARWorldMap and save it to disk.
    /// </summary>
    public Button saveButton
    {
        get { return m_SaveButton; }
        set { m_SaveButton = value; }
    }


    [Tooltip("A UI button component which will load a previously saved ARWorldMap from disk and apply it to the current session.")]
    [SerializeField]
    Button m_LoadButton;

    /// <summary>
    /// A UI button component which will load a previously saved ARWorldMap from disk and apply it to the current session.
    /// </summary>
    public Button loadButton
    {
        get { return m_LoadButton; }
        set { m_LoadButton = value; }
    }


    public void OnSaveButtonToServer()
    {

        StartCoroutine(OnSend());

    }

    public void OnLoadButton()
    {
        StartCoroutine(Load());
    }

        IEnumerator OnSend()
    {

        var UPurl = "http://localhost:3000/jsonUpload";
        //var UPurl = IP_InputField.text;
        Debug.Log(UPurl);

        ////POSTする情報
        //WWWForm form = new WWWForm();
        //form.AddField("test", "mydata");

        ////URLをPOSTで用意
        //UnityWebRequest webRequest = UnityWebRequest.Post(UPurl, form);
        ////UnityWebRequestにバッファをセット
        //webRequest.downloadHandler = new DownloadHandlerBuffer();
        ////webRequest.uploadHandler = new UploadHandlerBuffer();


        //Json送信テスト
        Myname myobject = new Myname();
        myobject.name = "daisuke";
        string myjson = JsonUtility.ToJson(myobject);

        byte[] postData = System.Text.Encoding.UTF8.GetBytes(myjson);
        Debug.Log("hash :" + _GetHashedTextString(postData));
        Debug.Log("バイナリ :" + postData);

        var webRequest = new UnityWebRequest(UPurl, "POST");
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(postData);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
        //webRequest.responseCode = "arraybuffer";
        yield return webRequest.SendWebRequest();


        ////URLに接続して結果が戻ってくるまで待機
        //yield return webRequest.SendWebRequest();

        //エラーが出ていないかチェック
        if (webRequest.isNetworkError)
        {
            //通信失敗
            Debug.Log(webRequest.error);
        }
        else
        {
            //通信成功
            Debug.Log(webRequest.downloadHandler.text);
        }

    }
    // バイナリデータのハッシュ値（SHA256）を計算・取得する
    protected string _GetHashedTextString(byte[] data)
    {
        // パスワードをUTF-8エンコードでバイト配列として取り出す
        //byte[] byteValues = Encoding.UTF8.GetBytes(passwd);

        // SHA256のハッシュ値を計算する
        SHA256 crypto256 = new SHA256CryptoServiceProvider();
        byte[] hash256Value = crypto256.ComputeHash(data);

        // SHA256の計算結果をUTF8で文字列として取り出す
        StringBuilder hashedText = new StringBuilder();
        for (int i = 0; i < hash256Value.Length; i++)
        {
            // 16進の数値を文字列として取り出す
            hashedText.AppendFormat("{0:X2}", hash256Value[i]);
        }
        return hashedText.ToString();
    }

        IEnumerator Load()
    {
//#if UNITY_IOS

//        var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
//        if (sessionSubsystem == null)
//        {
//            //Log("No session subsystem available. Could not load.");
//            yield break;
//        }
//#endif

        UnityWebRequest www = new UnityWebRequest("http://192.168.1.2:3000/download");
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.SendWebRequest();

        byte[] worldMap_byte;
        var allBytes = new List<byte>();


        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // テキストで結果を表示
            //Debug.Log(www.downloadHandler.text);

            // または、バイナリデータで結果を表示
            worldMap_byte = www.downloadHandler.data;
              Debug.Log("byte.length : " + worldMap_byte.Length);
              Debug.Log("hash" + _GetHashedTextString(worldMap_byte));

            allBytes.AddRange(worldMap_byte);
             
               //Debug.Log(allBytes.Count);


            var worldMap_nativearray = new NativeArray<byte>(allBytes.Count, Allocator.Temp);
            worldMap_nativearray.CopyFrom(allBytes.ToArray());
            ARWorldMap worldMap;
            Debug.Log("worldmap");
            if (ARWorldMap.TryDeserialize(worldMap_nativearray,out worldMap))
                worldMap_nativearray.Dispose();
            Debug.Log("worldmap1");
            Debug.Log(worldMap_nativearray.Length);

            //if (worldMap = null)
            //{
            //    Debug.Log("worldmap is null");
            //}

            if (worldMap.valid)
            {
                Debug.Log("Deserialized successfully.");
            }
            else
            {
                Debug.LogError("Data is not a valid ARWorldMap.");
                yield break;
            }
            Debug.Log("worldmap2");

            Debug.Log("Apply ARWorldMap to current session.");


            //sessionSubsystem.ApplyWorldMap(worldMap);


        }

    }

  







}

[System.Serializable]
public class Myname
{
    public string name;

}


