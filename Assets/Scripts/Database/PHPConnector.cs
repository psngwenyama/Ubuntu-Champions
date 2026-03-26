using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PHPConnector : MonoBehaviour
{
    private string baseURL = "http://localhost/ubuntu_api/";
    public static PHPConnector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public IEnumerator Register(string username, string email, string password, string role, System.Action<bool, string> callback)
    {
        var userData = new Dictionary<string, string>
        {
            { "username", username },
            { "email", email },
            { "password", password },
            { "role", role }
        };

        string jsonData = JsonConvert.SerializeObject(userData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + "register.php", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                bool success = (bool)response["success"];
                string message = response["message"].ToString();
                callback(success, message);
            }
            else
            {
                callback(false, "Network error: " + request.error);
            }
        }
    }

    public IEnumerator Login(string username, string password, System.Action<bool, string, Dictionary<string, object>> callback)
    {
        var loginData = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        string jsonData = JsonConvert.SerializeObject(loginData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest request = new UnityWebRequest(baseURL + "login.php", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
                bool success = (bool)response["success"];
                string message = response["message"].ToString();

                Dictionary<string, object> userData = null;
                if (success && response.ContainsKey("user"))
                {
                    userData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response["user"].ToString());
                }

                callback(success, message, userData);
            }
            else
            {
                callback(false, "Network error: " + request.error, null);
            }
        }
    }
}