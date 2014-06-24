

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Facebook.MiniJSON;
using System;





public class MainMenu : MonoBehaviour
{
    //   Inspector tunable members   //

    public Texture ButtonTexture;
    public Texture PlayTexture;                 //  Texture for main menu button icons
    public Texture BragTexture;
    public Texture ChallengeTexture;
    public Texture StoreTexture;
    public Texture FullScreenTexture;
    public Texture FullScreenActiveTexture;

    public Texture ResourcesTexture;
    
    public Vector2 CanvasSize;                  // size of window on canvas

    public Rect LoginButtonRect;                // Position of login button
    
    public Vector2 ResourcePos;                 // position of resource indicators (not used yet)

    public Vector2 ButtonStartPos;              // position of first button in main menu
    public float ButtonScale;                   // size of main menu buttons
    public float ButtonYGap;                    // gap between buttons in main menu
    public float ChallengeDisplayTime;          // Number of seconds the request sent message is displayed for
    public Vector2 ButtonLogoOffset;            // Offset determining positioning of logo on buttons
    public float TournamentStep;                // Spacing between tournament entries
    public float MouseScrollStep = 40;          // Amount score table moves with each step of the mouse wheel

    public PaymentDialog paymentDialog;

    public GUISkin MenuSkin;           

    public int CoinBalance;
    public int NumLives;
    public int NumBombs;

    public Texture[] CelebTextures;
    public string [] CelebNames;



    //   Private members   //


    private static MainMenu instance;

    private static List<object>                 friends         = null;
    private static Dictionary<string, string>   profile         = null;
    private static List<object>                 scores          = null;
    private static Dictionary<string, Texture>  friendImages    = new Dictionary<string, Texture>();
    
    
    
    private Vector2 scrollPosition = Vector2.zero;

    
    
    private bool    haveUserPicture       = false;
    private float   tournamentLength      = 0;
    private int     tournamentWidth       = 512;

    private int     mainMenuLevel         = 0; // Level index of main menu

    private string popupMessage;
    private float popupTime;
    private float popupDuration;

    enum LoadingState 
    {
        WAITING_FOR_INIT,
        WAITING_FOR_INITIAL_PLAYER_DATA,
        DONE
    };
    
    private LoadingState loadingState = LoadingState.WAITING_FOR_INIT;
    
    void Awake()
    {
        Util.Log("Awake");
   
        paymentDialog = ((PaymentDialog)(GetComponent("PaymentDialog")));

        // allow only one instance of the Main Menu
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        #if UNITY_WEBPLAYER
        // Execute javascript in iframe to keep the player centred
        string javaScript = @"
            window.onresize = function() {
              var unity = UnityObject2.instances[0].getUnity();
              var unityDiv = document.getElementById(""unityPlayerEmbed"");

              var width =  window.innerWidth;
              var height = window.innerHeight;

              var appWidth = " + CanvasSize.x + @";
              var appHeight = " + CanvasSize.y + @";

              unity.style.width = appWidth + ""px"";
              unity.style.height = appHeight + ""px"";

              unityDiv.style.marginLeft = (width - appWidth)/2 + ""px"";
              unityDiv.style.marginTop = (height - appHeight)/2 + ""px"";
              unityDiv.style.marginRight = (width - appWidth)/2 + ""px"";
              unityDiv.style.marginBottom = (height - appHeight)/2 + ""px"";
            }

            window.onresize(); // force it to resize now";
        Application.ExternalCall(javaScript);
        #endif
        DontDestroyOnLoad(gameObject);
        instance = this;
        

        // Initialize FB SDK
        enabled = false;
        FB.Init(SetInit, OnHideUnity);
    }

    private void SetInit()
    {
        Util.Log("SetInit");
        enabled = true; // "enabled" is a property inherited from MonoBehaviour
        if (FB.IsLoggedIn) 
        {
            Util.Log("Already logged in");
            OnLoggedIn();
            loadingState = LoadingState.WAITING_FOR_INITIAL_PLAYER_DATA;
        }
        else
        {
            loadingState = LoadingState.DONE;
        }
    }

    private void OnHideUnity(bool isGameShown)
    {
        Util.Log("OnHideUnity");
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
    
    void LoginCallback(FBResult result)
    {
        Util.Log("LoginCallback");
        
        if (FB.IsLoggedIn)
        {
            OnLoggedIn();
        }
    }

    void OnLoggedIn()
    {
        Util.Log("Logged in. ID: " + FB.UserId);

        // Reqest player info and profile picture
        FB.API("/me?fields=id,first_name,friends.limit(100).fields(first_name,id)", Facebook.HttpMethod.GET, APICallback);
        LoadPicture(Util.GetPictureURL("me", 128, 128),MyPictureCallback);

        // Load high scores
        QueryScores();
        paymentDialog.OnLoggedIn();
    }
    
    private void QueryScores()
    {
        FB.API("/app/scores?fields=score,user.limit(20)", Facebook.HttpMethod.GET, ScoresCallback);
    }
    
    void APICallback(FBResult result)
    {
        Util.Log("APICallback");
        if (result.Error != null)
        {
            Util.LogError(result.Error);
            // Let's just try again
            FB.API("/me?fields=id,first_name,friends.limit(100).fields(first_name,id)", Facebook.HttpMethod.GET, APICallback);
            return;
        }
        
        profile = Util.DeserializeJSONProfile(result.Text);
        GameStateManager.Username = profile["first_name"];
        friends = Util.DeserializeJSONFriends(result.Text);
        checkIfUserDataReady();
    }

    void MyPictureCallback(Texture texture)
    {
        Util.Log("MyPictureCallback");
        
        if (texture ==  null)
        {
            // Let's just try again
            LoadPicture(Util.GetPictureURL("me", 128, 128),MyPictureCallback);

            return;
        }
        
        GameStateManager.UserTexture = texture;
        haveUserPicture = true;
        checkIfUserDataReady();
    }

    private int getScoreFromEntry(object obj)
    {
        Dictionary<string,object> entry = (Dictionary<string,object>) obj;
        return Convert.ToInt32(entry["score"]);
    }

    void ScoresCallback(FBResult result) 
    {
        Util.Log("ScoresCallback");
        if (result.Error != null)
        {
            Util.LogError(result.Error);
            return;
        }

        scores = new List<object>();
        List<object> scoresList = Util.DeserializeScores(result.Text);

        foreach(object score in scoresList) 
        {
            var entry = (Dictionary<string,object>) score;
            var user = (Dictionary<string,object>) entry["user"];

            string userId = (string)user["id"];

            if (string.Equals(userId,FB.UserId))
            {
                // This entry is the current player
                int playerHighScore = getScoreFromEntry(entry);
                Util.Log("Local players score on server is " + playerHighScore);
                if (playerHighScore < GameStateManager.Score)
                {
                    Util.Log("Locally overriding with just acquired score: " + GameStateManager.Score);
                    playerHighScore = GameStateManager.Score;
                }

                entry["score"] = playerHighScore.ToString();
                GameStateManager.HighScore = playerHighScore;
            }

            scores.Add(entry);
            if (!friendImages.ContainsKey(userId))
            {
                // We don't have this players image yet, request it now
                LoadPicture(Util.GetPictureURL(userId, 128, 128),pictureTexture =>
                {
                    if (pictureTexture != null)
                    {
                        friendImages.Add(userId, pictureTexture);
                    }
                });
            }
        }

        // Now sort the entries based on score
        scores.Sort(delegate(object firstObj,
                             object secondObj)
                {
                    return -getScoreFromEntry(firstObj).CompareTo(getScoreFromEntry(secondObj));
                }
            );
    }

    
    void checkIfUserDataReady()
    {
        Util.Log("checkIfUserDataReady");
        if (loadingState == LoadingState.WAITING_FOR_INITIAL_PLAYER_DATA && haveUserPicture && !string.IsNullOrEmpty(GameStateManager.Username))
        {
          Util.Log("user data ready");
          loadingState = LoadingState.DONE;
        }
    }



    void OnLevelWasLoaded(int level)
    {
        Util.Log("OnLevelWasLoaded");
        if (level == mainMenuLevel && loadingState == LoadingState.DONE)
        {
            Util.Log("Returned to main menu");
            // We've returned to the main menu so let's query the scores again
            if (FB.IsLoggedIn)
                QueryScores();
        }
    }

    void OnApplicationFocus( bool hasFocus ) 
    {
      Util.Log ("hasFocus " + (hasFocus ? "Y" : "N"));
    }

    // Convenience function to check if mouse/touch is the tournament area
    private bool IsInTournamentArea (Vector2 p)
    {
        return p.x > Screen.width-tournamentWidth;
    }


    // Scroll the tournament view by some delta
    private void ScrollTournament(float delta)
    {
        scrollPosition.y += delta;
        if (scrollPosition.y > tournamentLength - Screen.height)
            scrollPosition.y = tournamentLength - Screen.height;
        if (scrollPosition.y < 0)
            scrollPosition.y = 0;
    }


    // variables for keeping track of scrolling
    private Vector2 mouseLastPos;
    private bool mouseDragging = false;


    void Update()
    {
        if(Input.touches.Length > 0) 
        {
            Touch touch = Input.touches[0];
            if (IsInTournamentArea (touch.position) && touch.phase == TouchPhase.Moved)
            {
                // dragging
                ScrollTournament (touch.deltaPosition.y*3);
            }
        }

        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            ScrollTournament (MouseScrollStep);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            ScrollTournament (-MouseScrollStep);
        }
        
        if (Input.GetMouseButton(0) && IsInTournamentArea(Input.mousePosition))
        {
            if (mouseDragging)
            {
                ScrollTournament (Input.mousePosition.y - mouseLastPos.y);
            }
            mouseLastPos = Input.mousePosition;
            mouseDragging = true;
        }
        else
            mouseDragging = false;
    }

    //  Button drawing logic //
    
    private Vector2 buttonPos;  // Keeps track of where we've got to on the screen as we draw buttons

    private void BeginButtons()
    {
        // start drawing buttons at the chosen start position
        buttonPos = ButtonStartPos;
    }

    private bool DrawButton(string text, Texture texture)
    {
        // draw a single button and update our position
        bool result = GUI.Button(new Rect (buttonPos.x,buttonPos.y, ButtonTexture.width * ButtonScale, ButtonTexture.height * ButtonScale),text,MenuSkin.GetStyle("menu_button"));
        Util.DrawActualSizeTexture(ButtonLogoOffset*ButtonScale+buttonPos,texture,ButtonScale);
        buttonPos.y += ButtonTexture.height*ButtonScale + ButtonYGap;
        
        if (paymentDialog.DialogEnabled)
            result = false;

        return result;
    }



    void OnGUI()
    {
        GUI.skin = MenuSkin;
        if (Application.loadedLevel != mainMenuLevel) return;  // don't display anything except when in main menu

        if (loadingState == LoadingState.WAITING_FOR_INIT || loadingState == LoadingState.WAITING_FOR_INITIAL_PLAYER_DATA)
        {
            GUI.Label(new Rect(0,0,Screen.width,Screen.height), "Loading...", MenuSkin.GetStyle("centred_text"));
            return;
        }
        
        GUILayout.Box("", MenuSkin.GetStyle("panel_welcome"));
        
        if (!FB.IsLoggedIn)
        {
            GUI.Label( (new Rect(179 , 11, 287, 160)), "Login to Facebook", MenuSkin.GetStyle("text_only"));
            if (GUI.Button(LoginButtonRect, "", MenuSkin.GetStyle("button_login")))
            {
                FB.Login("email,publish_actions", LoginCallback);
            }
        }
        
        if (FB.IsLoggedIn)
        {
            string panelText = "Welcome ";
            
            
            panelText += (!string.IsNullOrEmpty(GameStateManager.Username)) ? string.Format("{0}!", GameStateManager.Username) : "Smasher!";
            
            if (GameStateManager.UserTexture != null) 
                GUI.DrawTexture( (new Rect(8,10, 150, 150)), GameStateManager.UserTexture);


            GUI.Label( (new Rect(179 , 11, 287, 160)), panelText, MenuSkin.GetStyle("text_only"));
        }



        string subTitle = "Let's smash some friends!";
        if (GameStateManager.Score > 0) 
        {
            subTitle = "Score: " + GameStateManager.Score.ToString();
        }
        if (!string.IsNullOrEmpty(subTitle))
        {
            GUI.Label( (new Rect(132, 28, 400, 160)), subTitle, MenuSkin.GetStyle("sub_title"));
        }


        
        BeginButtons();
        
        if (DrawButton("Play",PlayTexture))
        {
            onPlayClicked();
        }

        if (FB.IsLoggedIn)
        {
            if (DrawButton ("Challenge",ChallengeTexture))
            {
                onChallengeClicked();
            }
            if (GameStateManager.Score > 0)
            {
                if (DrawButton ("Brag",BragTexture)) 
                {
                    onBragClicked();
                }
            }

            if (DrawButton ("Store",StoreTexture))
            {
                //Store
                paymentDialog.DialogEnabled = true;
            }
        }

        

        if (FB.IsLoggedIn)
        {
            // Draw resources bar
            Util.DrawActualSizeTexture(ResourcePos,ResourcesTexture);
             
            Util.DrawSimpleText(ResourcePos + new Vector2(47,5)  ,MenuSkin.GetStyle("resources_text"),string.Format("{0}",CoinBalance));
            Util.DrawSimpleText(ResourcePos + new Vector2(137,5) ,MenuSkin.GetStyle("resources_text"),string.Format("{0}",NumBombs));
            Util.DrawSimpleText(ResourcePos + new Vector2(227,5) ,MenuSkin.GetStyle("resources_text"),string.Format("{0}",NumLives));
        }
        
     

        #if UNITY_WEBPLAYER
        if (Screen.fullScreen)
        {
            if (DrawButton("Full Screen",FullScreenActiveTexture))
                SetFullscreenMode(false);
        }
        else 
        {
            if (DrawButton("Full Screen",FullScreenTexture))
                SetFullscreenMode(true);
        }
        #endif
        
        if (FB.IsLoggedIn)
        {
            // Draw the tournament view
            TournamentGui();
        }
        

        DrawPopupMessage();
        
            
    }


    public void AddPopupMessage(string message, float duration)
    {
        popupMessage = message;
        popupTime = Time.realtimeSinceStartup;
        popupDuration = duration;
    }
    public void DrawPopupMessage()
    {
        if (popupTime != 0 && popupTime + popupDuration > Time.realtimeSinceStartup)
        {
            // Show message that we sent a request
            Rect PopupRect = new Rect();
            PopupRect.width = 800;
            PopupRect.height = 100;
            PopupRect.x = Screen.width / 2 - PopupRect.width / 2;
            PopupRect.y = Screen.height / 2 - PopupRect.height / 2;
            GUI.Box(PopupRect,"",MenuSkin.GetStyle("box"));
            GUI.Label(PopupRect, popupMessage, MenuSkin.GetStyle("centred_text"));        
        }

    }

    void TournamentGui() 
    {
        GUILayout.BeginArea(new Rect((Screen.width - 450),0,450,Screen.height));
        
        // Title box
        GUI.Box   (new Rect(0,    - scrollPosition.y, 100,200), "",           MenuSkin.GetStyle("tournament_bar"));
        GUI.Label (new Rect(121 , - scrollPosition.y, 100,200), "Tournament", MenuSkin.GetStyle("heading"));
        
        Rect boxRect = new Rect();

        if(scores != null)
        {
            var x = 0;
            foreach(object scoreEntry in scores) 
            {
                Dictionary<string,object> entry = (Dictionary<string,object>) scoreEntry;
                Dictionary<string,object> user = (Dictionary<string,object>) entry["user"];

                string name     = ((string) user["name"]).Split(new char[]{' '})[0] + "\n";
                string score     = "Smashed: " + entry["score"];

                boxRect = new Rect(0, 121+(TournamentStep*x)-scrollPosition.y , 100,128);
                // Background box
                GUI.Box(boxRect,"",MenuSkin.GetStyle("tournament_entry"));
                
                // Text
                GUI.Label (new Rect(24, 136 + (TournamentStep * x) - scrollPosition.y, 100,128), (x+1)+".", MenuSkin.GetStyle("tournament_position"));      // Rank e.g. "1.""
                GUI.Label (new Rect(250,145 + (TournamentStep * x) - scrollPosition.y, 300,100), name, MenuSkin.GetStyle("tournament_name"));               // name   
                GUI.Label (new Rect(250,193 + (TournamentStep * x) - scrollPosition.y, 300,50), score, MenuSkin.GetStyle("tournament_score"));              // score
                Texture picture;
                if (friendImages.TryGetValue((string) user["id"], out picture)) 
                {
                    GUI.DrawTexture(new Rect(118,128+(TournamentStep*x)-scrollPosition.y,115,115), picture);  // Profile picture
                }
                x++;
            }

        }
        else GUI.Label (new Rect(180,270,512,200), "Loading...", MenuSkin.GetStyle("text_only"));
        
        // Record length so we know how far we can scroll to
        tournamentLength = boxRect.y + boxRect.height + scrollPosition.y;
        
        GUILayout.EndArea();
    }


    //  React to menu buttons  //


    private void onPlayClicked()
    {
        Util.Log("onPlayClicked");
        if (friends != null && friends.Count > 0)
        {
            // Select a random friend and get their picture
            Dictionary<string, string> friend = Util.RandomFriend(friends);
            GameStateManager.FriendName = friend["first_name"];
            GameStateManager.FriendID = friend["id"];
            GameStateManager.CelebFriend = -1;
            LoadPicture(Util.GetPictureURL((string)friend["id"], 128, 128),FriendPictureCallback);
        }
        else
        {
            //We can't access friends
            GameStateManager.CelebFriend = UnityEngine.Random.Range(0,CelebTextures.Length - 1);
            GameStateManager.FriendName = CelebNames[GameStateManager.CelebFriend];
        }
        
        // Start the main game
        Application.LoadLevel("GameStage");
        GameStateManager.Instance.StartGame();
    }
    private void onBragClicked()
    {
        Util.Log("onBragClicked");
        FB.Feed(
                linkCaption: "I just smashed " + GameStateManager.Score.ToString() + " friends! Can you beat it?",
                picture: "http://www.friendsmash.com/images/logo_large.jpg",
                linkName: "Checkout my Friend Smash greatness!",
                link: "http://apps.facebook.com/" + FB.AppId + "/?challenge_brag=" + (FB.IsLoggedIn ? FB.UserId : "guest")
                );
    }
    private void onChallengeClicked()
    {
        Util.Log("onChallengeClicked");
        if (GameStateManager.Score != 0 && GameStateManager.FriendID != null)
        {
            string[] recipient = { GameStateManager.FriendID };
            FB.AppRequest(
                message: "I just smashed you " + GameStateManager.Score.ToString() + " times! Can you beat it?",
                to: recipient,
                filters : "",
                excludeIds : null,
                maxRecipients : null,
                data: "{\"challenge_score\":" + GameStateManager.Score.ToString() + "}",
                title: "Friend Smash Challenge!",
                callback:appRequestCallback
                );
        }
        else
        {
            FB.AppRequest(
                to: null,
                filters : "",
                excludeIds : null,
                message: "Friend Smash is smashing! Check it out.",
                title: "Play Friend Smash with me!",
                callback:appRequestCallback
                );
        }
    }
    private void appRequestCallback (FBResult result)
    {
        Util.Log("appRequestCallback");
        if (result != null)
        {
            var responseObject = Json.Deserialize(result.Text) as Dictionary<string, object>;
            object obj = 0;
            if (responseObject.TryGetValue ("cancelled", out obj))
            {
                Util.Log("Request cancelled");
            }
            else if (responseObject.TryGetValue ("request", out obj))
            {
                AddPopupMessage("Request Sent", ChallengeDisplayTime);
                
                Util.Log("Request sent");
            }
        }
    }

    public void SetFullscreenMode (bool on)
    {
        if (on)
        {
            Screen.SetResolution (Screen.currentResolution.width, Screen.currentResolution.height, true);
        }
        else
        {
            Screen.SetResolution ((int)CanvasSize.x, (int)CanvasSize.y, false);
        }
    }

    public static void FriendPictureCallback(Texture texture)
    {
        GameStateManager.FriendTexture = texture;
    }

    delegate void LoadPictureCallback (Texture texture);


    IEnumerator LoadPictureEnumerator(string url, LoadPictureCallback callback)    
    {
        WWW www = new WWW(url);
        yield return www;
        callback(www.texture);
    }
    void LoadPicture (string url, LoadPictureCallback callback)
    {
        FB.API(url,Facebook.HttpMethod.GET,result =>
        {
            if (result.Error != null)
            {
                Util.LogError(result.Error);
                return;
            }

            var imageUrl = Util.DeserializePictureURLString(result.Text);

            StartCoroutine(LoadPictureEnumerator(imageUrl,callback));
        });
    }
}
