using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using System.Text;
using System.Security.Cryptography;

#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif



/// <summary>
/// Demonstrates the saving and loading of an
/// <a href="https://developer.apple.com/documentation/arkit/arworldmap">ARWorldMap</a>
/// </summary>
/// <remarks>
/// ARWorldMaps are only supported by ARKit, so this API is in the
/// <c>UntyEngine.XR.ARKit</c> namespace.
/// </remarks>
public class ARWorldMapController1 : MonoBehaviour
{

    public InputField IP_InputField;
    [SerializeField] int _httpPort;
    [SerializeField] string _PostDirectory;
    [SerializeField] string _GetDirectory;


    [Tooltip("The ARSession component controlling the session from which to generate ARWorldMaps.")]
    [SerializeField]
    ARSession m_ARSession;

    /// <summary>
    /// The ARSession component controlling the session from which to generate ARWorldMaps.
    /// </summary>
    public ARSession arSession
    {
        get { return m_ARSession; }
        set { m_ARSession = value; }
    }

    [Tooltip("UI Text component to display error messages")]
    [SerializeField]
    Text m_ErrorText;

    /// <summary>
    /// The UI Text component used to display error messages
    /// </summary>
    public Text errorText
    {
        get { return m_ErrorText; }
        set { m_ErrorText = value; }
    }

    [Tooltip("The UI Text element used to display log messages.")]
    [SerializeField]
    Text m_LogText;

    /// <summary>
    /// The UI Text element used to display log messages.
    /// </summary>
    public Text logText
    {
        get { return m_LogText; }
        set { m_LogText = value; }
    }

    [Tooltip("The UI Text element used to display the current AR world mapping status.")]
    [SerializeField]
    Text m_MappingStatusText;

    /// <summary>
    /// The UI Text element used to display the current AR world mapping status.
    /// </summary>
    public Text mappingStatusText
    {
        get { return m_MappingStatusText; }
        set { m_MappingStatusText = value; }
    }

    [Tooltip("A UI button component which will generate an ARWorldMap and save it to disk.")]
    [SerializeField]
    Button m_SaveButton;

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

    /// <summary>
    /// Create an <c>ARWorldMap</c> and save it to disk.
    /// </summary>
    ///


    public void OnSaveButton()
    {
#if UNITY_IOS
        StartCoroutine(Save());
#endif
    }

    /// <summary>
    /// Load an <c>ARWorldMap</c> from disk and apply it
    /// to the current session.
    /// </summary>
    public void OnLoadButton()
    {
#if UNITY_IOS
        StartCoroutine(Load());
#endif
    }

    /// <summary>
    /// Reset the <c>ARSession</c>, destroying any existing trackables,
    /// such as planes. Upon loading a saved <c>ARWorldMap</c>, saved
    /// trackables will be restored.
    /// </summary>
    public void OnResetButton()
    {
        m_ARSession.Reset();
    }

#if UNITY_IOS
    IEnumerator Save()
    {
        var UpUrl = "http://" + IP_InputField.text + ":" + _httpPort.ToString() + "/" + _PostDirectory;

        var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
        if (sessionSubsystem == null)
        {
            Log("No session subsystem available. Could not save.");
            yield break;
        }

        var request = sessionSubsystem.GetARWorldMapAsync();

        while (!request.status.IsDone())
            yield return null;

        if (request.status.IsError())
        {
            Log(string.Format("Session serialization failed with status {0}", request.status));
            yield break;
        }

        var worldMap = request.GetWorldMap();
        request.Dispose();

        Log("Serializing ARWorldMap to byte array...");
        var data = worldMap.Serialize(Allocator.Temp);
        Log(string.Format("ARWorldMap has {0} bytes.", data.Length));

        var rawdata = data.ToArray();

        var webRequest = new UnityWebRequest(UpUrl, "POST");
        webRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(rawdata);
        webRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        yield return webRequest.SendWebRequest();

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


        data.Dispose();
        worldMap.Dispose();
        Log(string.Format("ARWorldMap written to {0}", path));

    }

    // 文字列のハッシュ値（SHA256）を計算・取得する
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
        var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
        if (sessionSubsystem == null)
        {
            Log("No session subsystem available. Could not load.");
            yield break;
        }

        var DlUrl = "http://" + IP_InputField.text + ":" + _httpPort.ToString() + "/" + _GetDirectory;

        UnityWebRequest www = new UnityWebRequest(DlUrl);
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
          
            // または、バイナリデータで結果を表示
            worldMap_byte = www.downloadHandler.data;


            Debug.Log("hash" + _GetHashedTextString(worldMap_byte));


            allBytes.AddRange(worldMap_byte);
          
            var worldMap_nativearray = new NativeArray<byte>(allBytes.Count, Allocator.Temp);
            worldMap_nativearray.CopyFrom(allBytes.ToArray());
            ARWorldMap worldMap;
            if (ARWorldMap.TryDeserialize(worldMap_nativearray, out worldMap))
                worldMap_nativearray.Dispose();

            if (worldMap.valid)
            {
                Log("Deserialized successfully.");
            }
            else
            {
                Log("Data is not a valid ARWorldMap.");
                yield break;
            }

            Log("Apply ARWorldMap to current session.");


            sessionSubsystem.ApplyWorldMap(worldMap);


        }

        //var file = File.Open(path, FileMode.Open);
        //if (file == null)
        //{
        //    Log(string.Format("File {0} does not exist.", path));
        //    yield break;
        //}

        //Log(string.Format("Reading {0}...", path));

        //int bytesPerFrame = 1024 * 10;
        //var bytesRemaining = file.Length;
        //var binaryReader = new BinaryReader(file);
        //var allBytes = new List<byte>();
        //while (bytesRemaining > 0)
        //{
        //    var bytes = binaryReader.ReadBytes(bytesPerFrame);
        //    allBytes.AddRange(bytes);
        //    bytesRemaining -= bytesPerFrame;
        //    yield return null;
        //}

  
    }

    public void OnAttackButton()
    {
        this.GetComponent<PositionSync>().SkillShootPos();
    }




#endif

    string path
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "my_session.worldmap");
        }
    }

    bool supported
    {
        get
        {
#if UNITY_IOS
            var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
            if (sessionSubsystem != null)
                return sessionSubsystem.worldMapSupported;
#endif
            return false;
        }
    }

    void Awake()
    {
        m_LogMessages = new List<string>();
    }

    void Log(string logMessage)
    {
        m_LogMessages.Add(logMessage);
    }

    static void SetActive(Button button, bool active)
    {
        if (button != null)
            button.gameObject.SetActive(active);
    }

    static void SetActive(Text text, bool active)
    {
        if (text != null)
            text.gameObject.SetActive(active);
    }

    static void SetText(Text text, string value)
    {
        if (text != null)
            text.text = value;
    }

    void Update()
    {
        if (supported)
        {
            SetActive(errorText, false);
            SetActive(saveButton, true);
            SetActive(loadButton, true);
            SetActive(mappingStatusText, true);
        }
        else
        {
            SetActive(errorText, true);
            SetActive(saveButton, false);
            SetActive(loadButton, false);
            SetActive(mappingStatusText, false);
        }

#if UNITY_IOS
        var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
#else
        XRSessionSubsystem sessionSubsystem = null;
#endif
        if (sessionSubsystem == null)
            return;

        var numLogsToShow = 20;
        string msg = "";
        for (int i = Mathf.Max(0, m_LogMessages.Count - numLogsToShow); i < m_LogMessages.Count; ++i)
        {
            msg += m_LogMessages[i];
            msg += "\n";
        }
        SetText(logText, msg);

#if UNITY_IOS
        SetText(mappingStatusText, string.Format("Mapping Status: {0}", sessionSubsystem.worldMappingStatus));
#endif
    }

    List<string> m_LogMessages;
}
