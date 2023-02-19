using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class HTTP_Manager : MonoBehaviour
{
    private string api_url = "https://sid-restapi.herokuapp.com";
    private string token;
    private string username;
    
    [Header("Texts")]
    [SerializeField] private TMP_Text[] score_places ;
    [SerializeField] private TMP_Text name_text;
    [SerializeField] private TMP_Text error_text;

    [Header("Screens")]
    [SerializeField] private GameObject login_screen;
    [SerializeField] private GameObject main_screen;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField username_input;
    [SerializeField] private TMP_InputField password_input;
    [SerializeField] private TMP_InputField score_input;

    private void Start()
    {
        if (string.IsNullOrEmpty(token))
        {
            login_screen.SetActive(true);
            main_screen.SetActive(false);
        }
        else
        {
            login_screen.SetActive(false);
            main_screen.SetActive(true);
            StartCoroutine(GetUserData());
        }
    }

    public void Register()
    {
        User user = new User { username = username_input.text, password = password_input.text };
        string data = JsonUtility.ToJson(user);
        StartCoroutine(RegisterNewUser(data));
    }
    public void Login()
    {
        User user = new User();
        user.username = username_input.text;
        user.password = password_input.text;
        string data = JsonUtility.ToJson(user);
        StartCoroutine(LoginUser(data));
    }
    public void Logout()
    {
        login_screen.SetActive(true);
        main_screen.SetActive(false);
    }
    public void UpdateScore()
    {
        User user = new User();
        user.username = username_input.text;
        if (int.TryParse(score_input.text, out _)) user.data.score = int.Parse(score_input.text);
        string data = JsonUtility.ToJson(user);
        StartCoroutine(UpdateScoreData(data));
    }
    
    IEnumerator RegisterNewUser(string data)
    {
        UnityWebRequest www = UnityWebRequest.Put(api_url + "/api/usuarios", data);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR :" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                AuthJsonData jsonData = JsonUtility.FromJson<AuthJsonData>(www.downloadHandler.text);
                Debug.Log("Registered user:" + jsonData.usuario.username + " with the id: " + jsonData.usuario._id);
            }
            else
            {
                string error_msg = "Status: " + www.responseCode;
                error_msg += "\ncontent-type: " + www.GetResponseHeader("content-type \nError: " + www.error);
                Debug.Log(error_msg);
                error_text.text = "Error: This user is already registered";
                StartCoroutine(HideMessage());
            }

        }
    }
    IEnumerator LoginUser(string data)
    {

        UnityWebRequest www = UnityWebRequest.Put(api_url + "/api/auth/login", data);
        www.method = "POST";
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR :" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                AuthJsonData jsonData = JsonUtility.FromJson<AuthJsonData>(www.downloadHandler.text);
                Debug.Log(jsonData.usuario.username + " logged");
                
                token = jsonData.token;
                username = jsonData.usuario.username;
                PlayerPrefs.SetString("token", token);
                PlayerPrefs.SetString("username", username);
                
                login_screen.SetActive(false);
                main_screen.SetActive(true);
                
                StartCoroutine(ScoreChart());
                name_text.text = jsonData.usuario.username;
            }
            else
            {
                string error_msg = "Status :" + www.responseCode;
                error_msg += "\ncontent-type:" + www.GetResponseHeader("content-type") + "\nError :" + www.error;
                Debug.Log(error_msg);
                error_text.text = "Error: Wrong username or password. (You might not be registered as well)";
                StartCoroutine(HideMessage());
            }
        }
    }
    IEnumerator GetUserData()
    {
        UnityWebRequest www = UnityWebRequest.Get(api_url + "/api/usuarios/" + username);
        www.SetRequestHeader("x-token", token);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR :" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                AuthJsonData jsonData = JsonUtility.FromJson<AuthJsonData>(www.downloadHandler.text);
                Debug.Log(jsonData.usuario.username + " is still logged");
                name_text.text = jsonData.usuario.username;
                StartCoroutine(ScoreChart());
            }
            else
            {
                main_screen.SetActive(false);
                login_screen.SetActive(true);
                string error_msg = "Status :" + www.responseCode;
                error_msg += "\ncontent-type:" + www.GetResponseHeader("content-type") + "\nError :" + www.error;
                error_text.text = "Error: Previous user logged out";
                StartCoroutine(HideMessage());
                Debug.Log(error_msg);
            }

        }
    }
    IEnumerator ScoreChart()
    {
        UnityWebRequest www = UnityWebRequest.Get(api_url + "/api/usuarios");
        www.SetRequestHeader("x-token", token);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR :" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                UserList jsonList = JsonUtility.FromJson<UserList>(www.downloadHandler.text);
                Debug.Log(jsonList.usuarios.Count);
                
                List<User> userLists = jsonList.usuarios;
                List<User> orderedByScore_users = userLists.OrderByDescending(x => x.data.score).ToList<User>();
                
                int currentPlace = 0;
                foreach (User user in orderedByScore_users)
                {
                    if (currentPlace <= 4)
                    {
                        string user_score = currentPlace + 1 + "." + " User:" + user.username + " | Score: " + user.data.score;
                        score_places[currentPlace].text = user_score; currentPlace ++;
                        Debug.Log(user_score);
                    }
                }
            }
            else
            {
                main_screen.SetActive(false);
                login_screen.SetActive(true);
                string error_msg = "Status :" + www.responseCode;
                error_msg += "\ncontent-type:" + www.GetResponseHeader("content-type") + "\nError :" + www.error;
                Debug.Log(error_msg);
            }

        }
    }
    IEnumerator UpdateScoreData(string data)
    {
        UnityWebRequest www = UnityWebRequest.Put(api_url + "/api/usuarios/", data);
        www.method = "PATCH";
        www.SetRequestHeader("x-token", token);
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.Log("NETWORK ERROR :" + www.error);
            login_screen.SetActive(true);
            main_screen.SetActive(false);
        }
        else
        {
            if (www.responseCode == 200)
            {
                AuthJsonData jsonData = JsonUtility.FromJson<AuthJsonData>(www.downloadHandler.text);
                StartCoroutine(ScoreChart());
                Debug.Log(jsonData.usuario.username + " se actualizo " + jsonData.usuario.data.score);
            }
            else
            {
                string error_msg = "Status :" + www.responseCode;
                error_msg += "\ncontent-type:" + www.GetResponseHeader("content-type") + "\nError :" + www.error;;
                
                Debug.Log(error_msg);
            }
        }
    } 
    IEnumerator HideMessage() { yield return new WaitForSeconds(2f); error_text.text = ""; }
}

[System.Serializable]
public class User
{
    public string _id;
    public string username;
    public string password;
    public UserData data;
    public User() { data = new UserData(); }
    public User(string username, string password)
    {
        this.username = username;
        this.password = password;
        data = new UserData();
    }
}

public class AuthJsonData
{
    public User usuario;
    public UserData data;
    public string token;
}

[System.Serializable]
public class UserList { public List<User> usuarios; }