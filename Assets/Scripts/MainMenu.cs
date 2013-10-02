using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenu : MonoBehaviour
{
    private static List<object> friends = null;
    private static Dictionary<string, string> profile 	= null;
    private static Dictionary<string, object> scores 	= null;
    private static Dictionary<string, Texture> friendImages = new Dictionary<string, Texture>();

    private Vector2 scrollPosition = Vector2.zero;

    public GUISkin MenuSkin;
    public Texture LogoTexture;

    private Rect buttonSize = new Rect(190, 190, 200, 43);

    private static MainMenu instance;

    private void SetInit()
    {
        enabled = true;	// "enabled" is a magic global
        FB.GetAuthResponse(LoginCallback);
    }

    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            // pause the game - we will need to hide
            Time.timeScale = 0;
        }
        else
        {
            // start the game back up - we're getting focus again
            Time.timeScale = 1;
        }
    }

    void Awake()
    {
        // allow only one instance of the Main Menu
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        instance = this;
        enabled = false;
        FB.Init(SetInit, OnHideUnity);
    }

    void OnGUI()
    {
        GUI.skin = MenuSkin;
        if (Application.loadedLevel != 0) return;

        MenuSkin.GetStyle("topbar").fixedWidth = Screen.width;
        GUILayout.Box("", MenuSkin.GetStyle("topbar"));
        GUI.DrawTexture(new Rect(0,0,256,64), LogoTexture);

        string panelText = "Login to Facebook"; 

        if (FB.IsLoggedIn)
        {
            panelText = "Welcome ";
            if (GameStateManager.Username != null) panelText += GameStateManager.Username + "!";
            else panelText += "Smasher!";
        }

        if (GameStateManager.Score > 0) panelText += "\nScore: " + GameStateManager.Score.ToString();

        GUILayout.Box("", MenuSkin.GetStyle("panel_welcome"));
        if (GameStateManager.UserTexture != null) GUI.DrawTexture(new Rect(8, 74, 150, 150), GameStateManager.UserTexture);
        GUI.Label(new Rect(6 + 160 + 6, 72, 459 - (6 + 160 + 6), 160), panelText, MenuSkin.GetStyle("text_only"));

        if (!FB.IsLoggedIn)
        {
            if (GUI.Button(buttonSize, "", MenuSkin.GetStyle("button_login")))
            {
                FB.Login("email,publish_actions", LoginCallback);
            }
        }

        if (GUILayout.Button("", MenuSkin.GetStyle("button_play")))
        {
            if (friends != null && friends.Count > 0)
            {
                Dictionary<string, string> friend = Util.RandomFriend(friends);
                GameStateManager.FriendName = friend["first_name"];
                GameStateManager.FriendID = friend["id"];
                FB.API(Util.GetPictureURL((string)friend["id"], 128, 128), Facebook.HttpMethod.GET, Util.FriendPictureCallback);
            }
            
            Application.LoadLevel("GameStage");
            GameStateManager.Instance.StartGame();
        }

        if (FB.IsLoggedIn)
        {
            if (GameStateManager.Score > 0) 
            {
                if (GUILayout.Button ("", MenuSkin.GetStyle("button_brag"))) 
                {
                    FB.Feed(
                    linkCaption: "I just smashed " + GameStateManager.Score.ToString() + " friends! Can you beat it?",
                    picture: "http://www.friendsmash.com/images/logo_large.jpg",
                    linkName: "Checkout my Friend Smash greatness!",
                    link: "http://apps.facebook.com/friendsmashunity/?challenge_brag=" + (FB.IsLoggedIn ? FB.UserId : "guest")
                    );
                }
            }


            if (GUILayout.Button("", MenuSkin.GetStyle("button_challenge")))
            {
                if (GameStateManager.Score != 0 && GameStateManager.FriendID != null)
                {
                    string[] recipient = { GameStateManager.FriendID };
                    FB.AppRequest(
                        message: "I just smashed you " + GameStateManager.Score.ToString() + " times! Can you beat it?",
                        to: recipient,
                        data: "{\"challenge_score\":" + GameStateManager.Score.ToString() + "}",
                        title: "Friend Smash Challenge!"
                    );
                }
                else
                {
                    FB.AppRequest(
                        message: "Friend Smash is smashing! Check it out.",
                        title: "Play Friend Smash with me!"
                    );
                }
            }

            TournamentCallback();
        }


        #if UNITY_WEBPLAYER
            Screen.fullScreen = GUILayout.Toggle(Screen.fullScreen, "");
        #endif
    }

    void Update()
    {
        if(Input.touches.Length > 0) 
        {
            Touch touch = Input.touches[0];
            if (touch.position.x > Screen.width-512 && touch.phase == TouchPhase.Moved)
            {
                // dragging
                scrollPosition.y += touch.deltaPosition.y*3;
            }
        }
    }

    void SetUserTexture(Texture t)
    {
        GameStateManager.UserTexture = t;
    }

    void LoginCallback(FBResult result)
    {
        FbDebug.Log("call login: " + FB.UserId);
        FbDebug.Log("login result: " + result.Text);
        FB.API("/me?fields=id,first_name,friends.limit(100).fields(first_name,id)", Facebook.HttpMethod.GET, APICallback);
        FB.API(Util.GetPictureURL("me", 128, 128), Facebook.HttpMethod.GET, MyPictureCallback);
        FB.API("/app/scores?fields=score,user.limit(20)", Facebook.HttpMethod.GET, ScoresCallback);
    }

    void ScoresCallback(FBResult result) 
    {
        if (result.Error != null)
        {
            Debug.LogError(result.Error);
            return;
        }

        scores = new Dictionary<string, object>();
        List<object> scoresList = Util.DeserializeScores(result.Text);

        foreach(object score in scoresList) 
        {
            Dictionary<string,object> entry = (Dictionary<string,object>) score;
            Dictionary<string,object> user = (Dictionary<string,object>) entry["user"];

            string userId = (string)user["id"];

            scores.Add(userId, entry);
            FB.API(Util.GetPictureURL(userId, 128, 128), Facebook.HttpMethod.GET, pictureResult =>
            {
                if (pictureResult.Error != null)
                {
                    Debug.LogError(pictureResult.Error);
                }
                else
                {
                    friendImages.Add(userId, pictureResult.Texture);
                }
            });
        }
    }

    void APICallback(FBResult result)
    {
        if (result.Error != null)
        {
            Debug.LogError(result.Error);
            return;
        }

        profile = Util.DeserializeJSONProfile(result.Text);
        GameStateManager.Username = profile["first_name"];
        friends = Util.DeserializeJSONFriends(result.Text);
    }

    void MyPictureCallback(FBResult result)
    {
        if (result.Error != null)
        {
            Debug.LogError(result.Error);
            return;
        }

        GameStateManager.UserTexture = result.Texture;
    }

    void TournamentCallback() {
        GUILayout.BeginArea(new Rect(Screen.width - 512, 64,512,128));
        GUILayout.Box("", MenuSkin.GetStyle("tournament_bar"));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(Screen.width - 512, 192, Screen.width, Screen.height));

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(530), GUILayout.Height(Screen.height - 192));

        if(scores != null)
        {
            var x = 0;
            foreach(object scoreEntry in scores.Values) 
            {
                Dictionary<string,object> entry = (Dictionary<string,object>) scoreEntry;
                Dictionary<string,object> user = (Dictionary<string,object>) entry["user"];

                string name 	= ((string) user["name"]).Split(new char[]{' '})[0] + "\n";
                string score 	= "Smashed: " + entry["score"];

                GUILayout.Box("", MenuSkin.GetStyle("tournament_entry"));
                GUI.Label (new Rect(20, 20+(128*x), 100,128), (x+1)+".", MenuSkin.GetStyle("tournament_position"));
                GUI.Label (new Rect(250,10+(128*x), 300,70), name, MenuSkin.GetStyle("tournament_name"));
                GUI.Label (new Rect(250,50+(128*x), 300,50), score, MenuSkin.GetStyle("tournament_score"));
                Texture picture;
                if (friendImages.TryGetValue((string) user["id"], out picture)) 
                {
                    GUI.DrawTexture(new Rect(116,8+(128*x),115,115), picture);
                }
                x++;
            }
        }
        else GUI.Label (new Rect(20,20,512,200), "Loading...", MenuSkin.GetStyle("text_only"));

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}