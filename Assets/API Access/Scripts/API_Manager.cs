using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class API_Manager : MonoBehaviour
{
    private string api_RickAndMorty_path = "https://rickandmortyapi.com/api";
    private string api_path = "https://my-json-server.typicode.com/SimonOsorioUPB/SistemasInteractivosDistribuidos";

    [SerializeField] List<RawImage> deck_images = new List<RawImage>();
    [SerializeField] private List<string> deck_images_url = new List<string>();

    public void ShowUserData(string user_id)
    {
        int id = Int32.Parse(user_id);
        deck_images_url = new List<string>();
        StartCoroutine(GetUserData(id));
    }

    private IEnumerator GetUserData(int user_id)
    {
        string url = api_path + "/users/" + user_id;
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.Send();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR:" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                UserData user = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
                
                for (int i = 0; i < user.deck.Length; i++)
                {
                    StartCoroutine(GetCharacterData(user.deck[i],i));
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                string error_message = "Status: " + www.responseCode;
                error_message += "\nContent-Type: " + www.GetResponseHeader("content-type") + "\nError:" + www.error;
                Debug.Log(error_message);
            }
        }
    }
    private IEnumerator GetCharacterData(int id, int image)
    {
        UnityWebRequest www = UnityWebRequest.Get(api_RickAndMorty_path + "/character/" + id);
        yield return www.Send();

        if (www.result == UnityWebRequest.Result.ConnectionError) Debug.Log("NETWORK ERROR:" + www.error);
        else
        {
            if (www.responseCode == 200)
            {
                Character character = JsonUtility.FromJson<Character>(www.downloadHandler.text);
                deck_images_url.Add(character.image);
                StartCoroutine(DownloadImage(deck_images_url[image], image));
            }
            else
            {
                string error_message = "Status: " + www.responseCode;
                error_message += "\nContent-Type: " + www.GetResponseHeader("content-type") + "\nError:" + www.error;
                Debug.Log(error_message);
            }
        }
    }

    IEnumerator DownloadImage(string url, int image)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        
        if (request.isNetworkError || request.isHttpError) Debug.Log(request.error);
        else deck_images[image].texture = ((DownloadHandlerTexture) request.downloadHandler).texture;              
    }
}

[System.Serializable]
public class Character
{
    public int id;
    public string name;
    public string image;
}
[System.Serializable]
public class UserData
{
    public int id;
    public string name;
    public int[] deck;
    public int score;
}