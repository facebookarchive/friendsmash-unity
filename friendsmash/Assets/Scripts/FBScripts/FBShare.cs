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
using System;
using System.Collections.Generic;
using Facebook.Unity;

// Class responsible for Facebook Sharing in Friend Smash!
//
// In the Unity version of Friend Smash sharing is used for two purposes:
// Publishing scores using Scores API and sharing a completed game using the Share Dialog.
// For more details on sharing games content to Facebook see: https://developers.facebook.com/docs/games/sharing
// For more details on the Scores API see: https://developers.facebook.com/docs/games/scores
//
public static class FBShare
{
    // Prompt the player to share using the Share Dialog
    //
    // This is a sharing flow that allows players to brag about their success in the game.
    // By letting players share content, your game becomes visible to their friends in their newsfeeds,
    // which is an important source of new traffic to your game.
    //
    // Unlike the API-based sharing using FB.ShareLink or FB.FeedShare to call the Share Dialog does NOT require
    // extra publish_actions permissions.
    //
    public static void ShareBrag ()
    {
        // For this share we are using a page hosted on the game server with relevant Open Graph and App Links tags
        // See Open Graph tags: https://developers.facebook.com/docs/sharing/opengraph/object-properties
        // See App Links tags: https://developers.facebook.com/docs/applinks/add-to-content
        //
        // Note: In this git repo, the page is located at X
        string contentURL = GameStateManager.ServerURL + "sharing/share.php";

        // https://developers.facebook.com/docs/unity/reference/current/FB.ShareLink
        FB.ShareLink(
            new Uri(contentURL),
            "Checkout my Friend Smash greatness!",
            "I just smashed " + GameStateManager.Score.ToString() + " friends! Can you beat it?",
            null,
            ShareCallback);
    }

    private static void ShareCallback (IShareResult result)
    {
        Debug.Log("ShareCallback");
        if (result.Error != null)
        {
            Debug.LogError(result.Error);
            return;
        }
        Debug.Log(result.RawResult);
    }

    // The Graph API for Scores allows you to publish scores from your game to Facebook
    // This allows Friend Smash! to create a friends leaderboard keeping track of the top scores achieved by the player and their friends.
    // For more information on the Scores API see: https://developers.facebook.com/docs/games/scores
    //
    // When publishing a player's scores, these scores will be visible to their friends who also play your game.
    // As a result, the player needs to grant the app an extra permission, publish_actions, in order to publish scores.
    // This means we need to ask for the extra permission, as well as handling the
    // scenario where that permission wasn't previously granted.
    //
    public static void PostScore (int score, Action callback = null)
    {
        // Check for 'publish_actions' as the Scores API requires it for submitting scores
        if (FBLogin.HavePublishActions)
        {
            var query = new Dictionary<string, string>();
            query["score"] = score.ToString();
            FB.API(
                "/me/scores",
                HttpMethod.POST,
                delegate(IGraphResult result)
            {
                Debug.Log("PostScore Result: " + result.RawResult);
                // Fetch fresh scores to update UI
                FBGraph.GetScores();
            },
            query
            );
        }
        else
        {
            // Showing context before prompting for publish actions
            // See Facebook Login Best Practices: https://developers.facebook.com/docs/facebook-login/best-practices
            PopupScript.SetPopup("Prompting for Publish Permissions for Scores API", 4f, delegate
            {
                // Prompt for `publish actions` and if granted, post score
                FBLogin.PromptForPublish(delegate
                                         {
                    if (FBLogin.HavePublishActions)
                    {
                        PostScore(score);
                    }
                });
            });
        }
    }
}
