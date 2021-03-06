﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Advertisements.MiniJSON;

public class FacebookLoginChecker : MonoBehaviour {

    private bool loadingFriendsStatus = false;
    private bool LoadingFriendsStatus
    {
        get { return loadingFriendsStatus; }
        set
        {
            loadingFriendsStatus = value;
            AfterRequest();
        }
    }
    private bool readScoreStatus = false;
    private bool ReadScoreStatus
    {
        get { return readScoreStatus; }
        set
        {
            readScoreStatus = value;
            AfterRequest();
        }
    }

    private bool submitScoreStatus = false;
    private bool SubmitScoreStatus
    {
        get { return submitScoreStatus; }
        set
        {
            submitScoreStatus = value;
            AfterRequest();
        }
    }

    private bool connectFacebookStatus = false;
    private bool ConnectFacebookStatus
    {
        get { return connectFacebookStatus; }
        set
        {
            connectFacebookStatus = value;
            AfterRequest();
        }
    }

    int CountRequestConnect = 1;
    int CountRequestReadAllScore = 1;
    int CountSubmitAllScores = 1;
    int CountRequestFriends = 1;
    int CountRequestName = 1;

	// Use this for initialization
	void Start () {
        FB.Init(OnInitComplete, OnHideUnity);
        DontDestroyOnLoad(gameObject);
	}

    private void OnInitComplete()
    {
        if (FB.IsLoggedIn)
        {
            FB.API("/me?fields=id,first_name", Facebook.HttpMethod.GET, RequestNameCallback);
            LoadUserFriends();
        }
        else {
            Destroy(gameObject);
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public void RequestNameCallback(FBResult result)
    {
        if (result.Error == null)
        {
            var dict = Json.Deserialize(result.Text) as Dictionary<string, object>;
            Debug.Log(result.Text);
            StartCoroutine(ConnectFacebook((string)dict["first_name"]));
        }
        else
        {
            Debug.Log(result.Error);
            if (CountRequestName < 3)
            {
                LoadUserFriends();
                CountRequestName++;
            }
        }
    }

    public IEnumerator ConnectFacebook(string first_name)
    {
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("facebook_id", FB.UserId);
        wwwForm.AddField("facebook_name", first_name);
        wwwForm.AddField("app_token", UserDataManager.APP_TOKEN);

        WWW www = new WWW(UserDataManager.URL_CONNECT_FACEBOOK, wwwForm);
        yield return www;

        if (www.error == null)
        {
            Debug.Log("ConnectFacebook : " + www.text);
            StartCoroutine(ReadAllScores());
            StartCoroutine(SubmitAllScores());
            ConnectFacebookStatus = true;
        }
        else
        {
            Debug.Log("ConnectFacebook : " + www.error);
            if (CountRequestConnect < 3)
            {
                StartCoroutine(ConnectFacebook(first_name));
                CountRequestConnect++;
            }
            else
            {
                ConnectFacebookStatus = true;
                ReadScoreStatus = true;
                SubmitScoreStatus = true;
                LoadingFriendsStatus = true;
            }
        }
    }

    public IEnumerator ReadAllScores()
    {
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("facebook_id", FB.UserId);
        wwwForm.AddField("app_token", UserDataManager.APP_TOKEN);

        WWW www = new WWW(UserDataManager.URL_READ_ALL_SCORE, wwwForm);
        yield return www;

        if (www.error == null)
        {
            Debug.Log("ReadAllScores : " + www.text);
            UserScoreData userScore = UserScoreData.Load();
            var dict = Json.Deserialize(www.text) as Dictionary<string, object>;
            List<object> data = dict["data"] as List<object>;
            if (data != null)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    Dictionary<string, object> scoreData = (Dictionary<string, object>)data[i];
                    userScore.RenewScore(int.Parse((string)scoreData["level_system_id"]), int.Parse((string)scoreData["score"]));
                }
            }
            ReadScoreStatus = true;
        }
        else
        {
            Debug.Log("ReadAllScores : " + www.error);
            if (CountRequestReadAllScore < 3)
            {
                StartCoroutine(SubmitAllScores());
                CountRequestReadAllScore++;
            }
            else
            {
                ReadScoreStatus = true;
            }
        }
    }

    public IEnumerator SubmitAllScores()
    {
        UserScoreData userScores = UserScoreData.Load();

        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("facebook_id", FB.UserId);
        wwwForm.AddField("app_token", UserDataManager.APP_TOKEN);
        wwwForm.AddField("scores", userScores.SerializeToJsonUsingGlobalId());
        Debug.Log("facebook_id : " + FB.UserId);
        Debug.Log("app_token : " + UserDataManager.APP_TOKEN);
        Debug.Log("scores : " + userScores.SerializeToJsonUsingGlobalId());

        WWW www = new WWW(UserDataManager.URL_SUBMIT_ALL_SCORE, wwwForm);
        yield return www;

        if (www.error == null)
        {
            Debug.Log("SubmitAllScores : " + www.text);
            SubmitScoreStatus = true;
        }
        else
        {
            if (CountSubmitAllScores < 3)
            {
                StartCoroutine(SubmitAllScores());
                CountSubmitAllScores++;
            }
            else
            {
                SubmitScoreStatus = true;
            }
        }
    }

    public void LoadUserFriends()
    {
        FB.API("/me/friends?fields=id,first_name", Facebook.HttpMethod.GET, CallbackLoadFriends);
    }

    public void CallbackLoadFriends(FBResult result)
    {
        if (result.Error == null)
        {
            var dict = Json.Deserialize(result.Text) as Dictionary<string, object>;
            var tempFriends = (List<object>)(((Dictionary<string, object>)dict)["data"]);

            UserDataManager.Friends.Clear();
            for (int i = 0; i < tempFriends.Count; i++)
            {
                var friendDict = ((Dictionary<string, object>)(tempFriends[i]));
                UserDataManager.Friends.Add(((string)friendDict["id"]), (string)friendDict["first_name"]);
            }

            if (dict.ContainsKey("next"))
            {
                var query = (string)dict["next"];
                FB.API(query, Facebook.HttpMethod.GET, CallbackLoadFriends);
            }
            else
            {
                UserDataManager.IsSuccessLoadFriend = true;
            }
            LoadingFriendsStatus = true;
        }
        else
        {
            Debug.Log("CallbackLoadFriends : " + result.Error);
            if (CountRequestFriends < 3)
            {
                LoadUserFriends();
                CountRequestFriends++;
            }
            else
            {
                LoadingFriendsStatus = true;
            }
        }
    }

    private void AfterRequest()
    {
        if (LoadingFriendsStatus && ReadScoreStatus && SubmitScoreStatus && ConnectFacebookStatus)
        {
            Destroy(gameObject);
        }
    }
}
