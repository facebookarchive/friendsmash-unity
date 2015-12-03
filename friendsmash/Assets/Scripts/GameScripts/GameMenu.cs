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
using UnityEngine.UI;
using System.Collections.Generic;
using Facebook.Unity;

public class GameMenu : MonoBehaviour
{
    // UI Element References (Set in Unity Editor)
    //  Header
    public GameObject HeaderNotLoggedIn;
    public GameObject HeaderLoggedIn;
    public GameObject LoadingText;
    public Button LoginButton;
    public RawImage ProfilePic;
    public Text WelcomeText;
    public Text ScoreText;
    public Text CoinText;
    public Text BombText;
    //  Leaderboard
    public GameObject LeaderboardPanel;
    public GameObject LeaderboardItemPrefab;
    public ScrollRect LeaderboardScrollRect;
    //  Payment Dialog
    public GameObject PaymentPanel;
    // Game Resources (Set in Unity Editor)
    public GameResources gResources;

    #region Built-In
    void Awake()
    {
        // Initialize FB SDK
        if (!FB.IsInitialized)
        {
            FB.Init(InitCallback);
        }
    }

    // OnApplicationPause(false) is called when app is resumed from the background
    void OnApplicationPause (bool pauseStatus)
    {
        // Do not do anything in the Unity Editor
        if (Application.isEditor) {
            return;
        }

        // Check the pauseStatus for an app resume
        if (!pauseStatus)
        {
            if (FB.IsInitialized)
            {
                // App Launch events should be logged on app launch & app resume
                // See more: https://developers.facebook.com/docs/app-events/unity#quickstart
                FBAppEvents.LaunchEvent();
            }
            else
            {
                FB.Init(InitCallback);
            }
        }
    }

    // OnLevelWasLoaded is called when we return to the main menu
    void OnLevelWasLoaded(int level)
    {
        Debug.Log("OnLevelWasLoaded");
        if (level == 0 && FB.IsInitialized)
        {
            Debug.Log("Returned to main menu");

            // We've returned to the main menu so let's complete any pending score activity
            if (FB.IsLoggedIn)
            {
                RedrawUI();

                // Post any pending High Score
                if (GameStateManager.highScorePending)
                {
                    GameStateManager.highScorePending = false;
                    FBShare.PostScore(GameStateManager.HighScore);
                }
            }
        }
    }
    #endregion

    #region FB Init
    private void InitCallback()
    {
        Debug.Log("InitCallback");

        // App Launch events should be logged on app launch & app resume
        // See more: https://developers.facebook.com/docs/app-events/unity#quickstart
        FBAppEvents.LaunchEvent();

        if (FB.IsLoggedIn) 
        {
            Debug.Log("Already logged in");
            OnLoginComplete();
        }
    }
    #endregion

    #region Login
    public void OnLoginClick ()
    {
        Debug.Log("OnLoginClick");

        // Disable the Login Button
        LoginButton.interactable = false;

        // Call Facebook Login for Read permissions of 'public_profile', 'user_friends', and 'email'
        FBLogin.PromptForLogin(OnLoginComplete);
    }

    private void OnLoginComplete()
    {
        Debug.Log("OnLoginComplete");

        if (!FB.IsLoggedIn)
        {
            // Reenable the Login Button
            LoginButton.interactable = true;
            return;
        }

        // Show loading animations
        LoadingText.SetActive(true);

        // Begin querying the Graph API for Facebook data
        FBGraph.GetPlayerInfo();
        FBGraph.GetFriends();
        FBGraph.GetInvitableFriends();
        FBGraph.GetScores();
    }
    #endregion

    #region GUI
    // Method to update the Game Menu User Interface
    public void RedrawUI ()
    {
        if (FB.IsLoggedIn)
        {
            // Swap GUI Header for a player after login
            HeaderLoggedIn.SetActive(true);
            HeaderNotLoggedIn.SetActive(false);

            // Show HighScore if we have one
            if (GameStateManager.HighScore > 0)
            {
                ScoreText.text = "Score: " + GameStateManager.HighScore.ToString();
            }

            //Set Coin and Bomb counters
            CoinText.text = GameStateManager.CoinBalance.ToString();
            BombText.text = GameStateManager.NumBombs.ToString();
        }

        if (GameStateManager.UserTexture != null && !string.IsNullOrEmpty(GameStateManager.Username))
        {
            // Update Profile Picture
            ProfilePic.enabled = true;
            ProfilePic.texture = GameStateManager.UserTexture;

            // Update Welcome Text
            WelcomeText.text = "Welcome " + GameStateManager.Username + "!";

            // Disable loading animation
            LoadingText.SetActive(false);
        }

        var scores = GameStateManager.Scores;
        if (GameStateManager.ScoresReady && scores.Count > 0)
        {
            // Clear out previous leaderboard
            Transform[] childLBElements = LeaderboardPanel.GetComponentsInChildren<Transform>();
            foreach(Transform childObject in childLBElements)
            {
                if(!LeaderboardPanel.transform.IsChildOf(childObject.transform))
                {
                    Destroy(childObject.gameObject);
                }
            }

            // Populate leaderboard
            for (int i=0; i<scores.Count; i++)
            {
                GameObject LBgameObject = Instantiate (LeaderboardItemPrefab) as GameObject;
                LeaderBoardElement LBelement = LBgameObject.GetComponent<LeaderBoardElement>();
                LBelement.SetupElement(i+1, scores[i]);
                LBelement.transform.SetParent (LeaderboardPanel.transform, false);
            }

            // Scroll to top
            LeaderboardScrollRect.verticalNormalizedPosition = 1f;
        }

        // Update PaymentPanel UI
        PaymentPanel.GetComponent<PaymentDialog>().UpdateUI();
    }
    #endregion

    #region Menu Buttons
    public void OnPlayClicked()
    {
        Debug.Log("OnPlayClicked");

        if (GameStateManager.Friends != null && GameStateManager.Friends.Count > 0)
        {
            // Select a random friend and setup game state
            int randFriendNum = UnityEngine.Random.Range(0,GameStateManager.Friends.Count);
            var friend = GameStateManager.Friends[randFriendNum] as Dictionary<string, object>;
            GameStateManager.FriendName = friend["first_name"] as string;
            GameStateManager.FriendID = friend["id"] as string;
            GameStateManager.CelebFriend = -1;

            // Set friend image
            if (GameStateManager.FriendImages.ContainsKey(GameStateManager.FriendID))
            {
                GameStateManager.FriendTexture = GameStateManager.FriendImages[GameStateManager.FriendID];
            }
            else
            {
                // We don't have this players image yet, request it now
                string friendImgUrl = GraphUtil.DeserializePictureURL(friend);
                GraphUtil.LoadImgFromURL (friendImgUrl, delegate(Texture pictureTexture)
                {
                    if (pictureTexture != null)
                    {
                        GameStateManager.FriendImages.Add(GameStateManager.FriendID, pictureTexture);
                        GameStateManager.FriendTexture = pictureTexture;
                    }
                });
            }
        }
        else
        {
            //We can't access friends -- Use celebrity
            GameStateManager.CelebFriend = UnityEngine.Random.Range(0,gResources.CelebTextures.Length - 1);
            GameStateManager.FriendName = gResources.CelebNames[GameStateManager.CelebFriend];
            GameStateManager.FriendTexture = gResources.CelebTextures[GameStateManager.CelebFriend];
        }
        
        // Start the main game
        Application.LoadLevel("GameStage");
        GameStateManager.Instance.StartGame();
    }

    public void OnBragClicked()
    {
        Debug.Log("OnBragClicked");
        FBShare.ShareBrag();
    }

    public void OnChallengeClicked()
    {
        Debug.Log("OnChallengeClicked");
        FBRequest.RequestChallenge();
    }

    public void OnStoreClicked()
    {
        Debug.Log("OnStoreClicked");
        PaymentPanel.SetActive(true);
    }
    #endregion
}
