using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class ImagePredictionAPI:MonoBehaviour {
    private bool debug = true;
    [SerializeField] private UIManager _uIManager;

    public void StartUpload(string imagePath) {
        StartCoroutine(RequestCoroutinePostMultipart<ImageRecognitionResponse>("file",imagePath,API.Predict,(a) => {
            if (a.detections.Count > 0) {
                var response = a.detections[0];
                Debug.Log($"label: {response.label}");

                _uIManager.imageLoader.LoadAndSetImage(imagePath);
                _uIManager.ShowResult(response);
            } else { 
                Notification.Instance.Show(false, "Unable to Identify Object, Please Try Again!!!");            
            }
        },(a) => {
            Debug.LogError($"Error: {a}");
            Notification.Instance.Show(false);
        }));
    }

    private IEnumerator RequestCoroutinePostMultipart<T>(string fieldName,string filePath,string url,UnityAction<T> callbackOnSuccess,
         UnityAction<string> callbackOnFail) {
        if (!File.Exists(filePath)) {
            Debug.LogError("File not exist: " + filePath);
            callbackOnFail.Invoke("File not exist: " + filePath);
            Notification.Instance.Show(false,"File not exist: " + filePath);
            yield break;
        }

        Debug.Log("url: " + url + " filePath: " + filePath);    

        // Wait until the file is unlocked
        while (IsFileLocked(filePath)) {
            Notification.Instance.Show(false,$"File {filePath} is locked, waiting...");
            Debug.Log($"File {filePath} is locked, waiting...");
            yield return new WaitForSeconds(0.5f); // Wait before retrying
        }

        // Read file data after ensuring it's not locked
        byte[] fileData = File.ReadAllBytes(filePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData(fieldName,fileData,Path.GetFileName(filePath),"application/json");

        using (UnityWebRequest request = UnityWebRequest.Post(url,form)) {
            request.SetRequestHeader("accept","application/json");

            yield return request.SendWebRequest();

            SendResponseToAPIMethod(request,url,callbackOnSuccess,callbackOnFail);
        }
    }

    public void SendResponseToAPIMethod<T>(UnityWebRequest request,string url,UnityAction<T> callbackOnSuccess,
            //For responseCode
            //UnityAction<UnityWebRequest> callbackOnFail) {
            UnityAction<string> callbackOnFail) {
        if (request.result == UnityWebRequest.Result.DataProcessingError ||
            request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError) {

            if (debug) {
                Debug.LogError("url " + url + " error " + request.error + " error code " + request.responseCode + " Data " + request.downloadHandler.text);
            }

            T apiResponse = JsonUtility.FromJson<T>(request.downloadHandler.text);
            callbackOnFail?.Invoke(request.error);

            //For responseCode
            //callbackOnFail?.Invoke(request);
        } else {
            if (string.IsNullOrEmpty(request.downloadHandler.text)) {
                Debug.LogError("DownloadHandler text is null");
            } else {
                if (debug) {
                    Debug.Log("url " + url + " Data " + request.downloadHandler.text);
                }
                ParseResponse(request.downloadHandler.text,callbackOnSuccess);
            }
        }
    }
    /// <summary>
    /// This method finishes request process and remove $ sign.
    /// </summary>
    /// <param name="data">Data received from server in JSON format.</param>
    /// <param name="callbackOnSuccess">Callback on success.</param>
    /// <typeparam name="T">Data Model Type.</typeparam>
    private void ParseResponse<T>(string data,UnityAction<T> callbackOnSuccess) {
        data = data.Replace("$oid","oid");
        data = data.Replace("$date","date");
        var parsedData = JsonUtility.FromJson<T>(data);
        callbackOnSuccess?.Invoke(parsedData);
    }
    /// <summary>
    /// Checks if a file is locked by another process.
    /// </summary>
    /// <param name="filePath">The path of the file to check.</param>
    /// <returns>True if the file is locked, otherwise false.</returns>
    public static bool IsFileLocked(string filePath) {
        try {
            using (FileStream stream = File.Open(filePath,FileMode.Open,FileAccess.Read,FileShare.None)) {
                return false; // File is not locked
            }
        } catch (IOException) {
            return true; // File is locked
        }
    }
}
