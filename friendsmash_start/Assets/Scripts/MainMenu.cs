

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

    public GUISkin MenuSkin;           



    //   Private members   //


    private static MainMenu instance;

    private static List<object>                 friends         = null;
    private static Dictionary<string, string>   profile         = null;
    private static List<object>                 scores          = null;
    private static Dictionary<string, Texture>  friendImages    = new Dictionary<string, Texture>();
    
    
    
    private Vector2 scrollPosition = Vector2.zero;

    
    
    private bool    haveUserPicture       = false;
    private float   lastChallengeSentTime = 0;
    private float   tournamentLength      = 0;
    private int     tournamentWidth       = 512;

    private int     mainMenuLevel         = 0; // Level index of main menu

    
    void Awake()
    {
        FbDebug.Log("Awake");
   
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
        

    }

    
    
    private void QueryScores()
    {
        FB.API("/app/scores?fields=score,user.limit(20)", Facebook.HttpMethod.GET, ScoresCallback);
    }
    

    private int getScoreFromEntry(object obj)
    {
        Dictionary<string,object> entry = (Dictionary<string,object>) obj;
        return Convert.ToInt32(entry["score"]);
    }

    void ScoresCallback(FBResult result) 
    {
        FbDebug.Log("ScoresCallback");
        if (result.Error != null)
        {
            FbDebug.Error(result.Error);
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
                FbDebug.Log("Local players score on server is " + playerHighScore);
                if (playerHighScore < GameStateManager.Score)
                {
                    FbDebug.Log("Locally overriding with just acquired score: " + GameStateManager.Score);
                    playerHighScore = GameStateManager.Score;
                }

                entry["score"] = playerHighScore.ToString();
                GameStateManager.HighScore = playerHighScore;
            }

            scores.Add(entry);
            if (!friendImages.ContainsKey(userId))
            {
                // We don't have this players image yet, request it now
                FB.API(Util.GetPictureURL(userId, 128, 128), Facebook.HttpMethod.GET, pictureResult =>
                {
                    if (pictureResult.Error != null)
                    {
                        FbDebug.Error(pictureResult.Error);
                    }
                    else
                    {
                        friendImages.Add(userId, pictureResult.Texture);
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

    

    void OnApplicationFocus( bool hasFocus ) 
    {
      FbDebug.Log ("hasFocus " + (hasFocus ? "Y" : "N"));
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
        return result;
    }



    void OnGUI()
    {
        GUI.skin = MenuSkin;
        if (Application.loadedLevel != mainMenuLevel) return;  // don't display anything except when in main menu

        
        GUILayout.Box("", MenuSkin.GetStyle("panel_welcome"));
        
        
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
            // Draw resources bar
            Util.DrawActualSizeTexture(ResourcePos,ResourcesTexture);
             
            Util.DrawSimpleText(ResourcePos + new Vector2(47,5)  ,MenuSkin.GetStyle("resources_text"),"0");
            Util.DrawSimpleText(ResourcePos + new Vector2(137,5) ,MenuSkin.GetStyle("resources_text"),"0");
            Util.DrawSimpleText(ResourcePos + new Vector2(227,5) ,MenuSkin.GetStyle("resources_text"),"0");
        }
        
     

        #if UNITY_WEBPLAYER
        if (Screen.fullScreen)
        {
            if (DrawButton("Full Screen",FullScreenActiveTexture))
                Screen.fullScreen = false;
        }
        else 
        {
            if (DrawButton("Full Screen",FullScreenTexture))
                Screen.fullScreen = true;
        }
        #endif
        
        
        if (lastChallengeSentTime != 0 && lastChallengeSentTime + ChallengeDisplayTime > Time.realtimeSinceStartup)
        {
            // Show message that we sent a request
            GUI.Label(new Rect(0,0,Screen.width,Screen.height), "Request Sent", MenuSkin.GetStyle("centred_text"));        
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
        FbDebug.Log("onPlayClicked");
        if (friends != null && friends.Count > 0)
        {
            // Select a random friend and get their picture
            Dictionary<string, string> friend = Util.RandomFriend(friends);
            GameStateManager.FriendName = friend["first_name"];
            GameStateManager.FriendID = friend["id"];
            FB.API(Util.GetPictureURL((string)friend["id"], 128, 128), Facebook.HttpMethod.GET, Util.FriendPictureCallback);
        }
        
        // Start the main game
        Application.LoadLevel("GameStage");
        GameStateManager.Instance.StartGame();
    }
}
