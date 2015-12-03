/**
 * Copyright (c) 2014-present, Facebook, Inc. All rights reserved.
 *
 * You are hereby granted a non-exclusive, worldwide, royalty-free license to use,
 * copy, modify, and distribute this software in source code or binary form for use
 * in connection with the web services and APIs provided by Facebook.
 *
 * As with any software that integrates with the Facebook platform, your use of
 * this software is subject to the Facebook Developer Principles and Policies
 * [http://developers.facebook.com/policy/]. This copyright notice shall be
 * included in all copies or substantial portions of the software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using UnityEngine;
using System.Collections.Generic;
using Facebook.Unity;

public class GameStateManager : MonoBehaviour
{
    //   Singleton   //
    private static GameStateManager instance;
    public static GameStateManager Instance { get { return current(); } }
    delegate GameStateManager InstanceStep();
    static InstanceStep init = delegate()
    {
        GameObject container = new GameObject("GameStateManagerManager");
        instance = container.AddComponent<GameStateManager>();
        instance.lives = StartingLives;
        instance.score = StartingScore;
        instance.highScore = null;
        current = final;
        return instance;
    };
    static InstanceStep final = delegate() { return instance; };
    static InstanceStep current = init;

    //   Game Config   //
    // Set the ServerURL to a location you are hosting your game assets
    public static readonly string ServerURL = "https://friendsmash-unity.herokuapp.com/";
    public static readonly int StartingLives = 3, StartingScore = 0;
    
    //   Game State   //
    private int score;
    private int? highScore;
    public static bool ScoringLockout, highScorePending;
    public static int Score { get { return Instance.score; } }
    public static int HighScore {
        get { return Instance.highScore.HasValue ? Instance.highScore.Value : 0; }
        set { Instance.highScore = value; }
    }
    private int lives;
    public static int LivesRemaining { get { return Instance.lives; } }
    public static int CoinBalance, NumBombs;
    public static string FriendName = "Blue Guy";
    public static string FriendID = null;
    public static Texture FriendTexture = null;
    public static int CelebFriend = -1;
    
    //   Facebook Data   //
    public static string Username;
    public static Texture UserTexture;
    public static List<object> Friends;
    public static Dictionary<string, Texture> FriendImages = new Dictionary<string, Texture>();
    public static List<object> InvitableFriends = new List<object>();
        // Scores
    public static bool ScoresReady;
    private static List<object> scores;
    public static List<object> Scores {
        get { return scores; }
        set { scores = value; ScoresReady = true; }
    }
    
    void Awake()
    {
        // Persist through Scene loading
        DontDestroyOnLoad(this);
    }

    public void StartGame()
    {
        lives = StartingLives;
        score = StartingScore;
        ScoringLockout = false;
        Time.timeScale = 1f;
    }

    public static void onFriendSmash()
    {
        if (!ScoringLockout)
        {
            Instance.score++;
        }
    }

    public static void onFriendDie()
    {
        if (--Instance.lives == 0)
        {
            EndGame();
        }
    }

    public static void EndGame()
    {
        Debug.Log("EndGame Instance.highScore = " + Instance.highScore + "\nInstance.score = " + Instance.score);

        // Log custom App Event for game completion
        FBAppEvents.GameComplete(Instance.score);

        // Ensure we have read score from FB before we allow overriding the High Score
        if (FB.IsLoggedIn &&
            Instance.highScore.HasValue &&
            Instance.highScore < Instance.score)
        {
            Debug.Log("Player has new high score :" + Instance.score);
            Instance.highScore = Instance.score;

            //Set a flag so MainMenu can handle posting the score once its scene has loaded
            highScorePending = true;
        }

        //Return to main menu
        Application.LoadLevel("MainMenu");
    }

    // Convenience callback into GameMenu to redraw UI
    public static void CallUIRedraw()
    {
        GameObject gMenuObj = GameObject.Find("GameMenu");
        if (gMenuObj)
        {
            gMenuObj.GetComponent<GameMenu>().RedrawUI();
        }
    }
}
