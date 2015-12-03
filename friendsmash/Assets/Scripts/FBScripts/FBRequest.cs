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

// Class responsible for Facebook Game Requests in Friend Smash!
//
// As Friend Smash is a social game that you play with friends, we need a way for players to invite their friends to play.
// With Game Requests, we can build a gameplay loop that allows players to challenge their friends to beat high scores,
// as well as sending invites to friends who aren't yet playing the game.
//
// For more details on Game Requests see: https://developers.facebook.com/docs/games/requests
//
public static class FBRequest
{
    // Prompt the player to send a Game Request to their friends with Friend Smash!
    public static void RequestChallenge ()
    {
        List<string> recipient = null;
        string title, message, data = string.Empty;

        // Check to see if we have played a game against a friend yet during this session
        if (GameStateManager.Score != 0 && GameStateManager.FriendID != null)
        {
            // We have played a game -- lets send a request to the person we just smashed
            title = "Friend Smash Challenge!";
            message = "I just smashed you " + GameStateManager.Score.ToString() + " times! Can you beat it?";
            recipient = new List<string>(){ GameStateManager.FriendID };
            data = "{\"challenge_score\":" + GameStateManager.Score.ToString() + "}";
        }
        else
        {
            // We have not played a game against a friend -- lets open an invite request
            title = "Play Friend Smash with me!";
            message = "Friend Smash is smashing! Check it out.";
        }

        // Prompt user to send a Game Request using FB.AppRequest
        // https://developers.facebook.com/docs/unity/reference/current/FB.AppRequest
        FB.AppRequest(
            message,
            recipient,
            null,
            null,
            null,
            data,
            title,
            AppRequestCallback
            );
    }

    // Callback for FB.AppRequest
    private static void AppRequestCallback (IAppRequestResult result)
    {
        // Error checking
        Debug.Log("AppRequestCallback");
        if (result.Error != null)
        {
            Debug.LogError(result.Error);
            return;
        }
        Debug.Log(result.RawResult);

        // Check response for success - show user a success popup if so
        object obj;
        if (result.ResultDictionary.TryGetValue ("cancelled", out obj))
        {
            Debug.Log("Request cancelled");
        }
        else if (result.ResultDictionary.TryGetValue ("request", out obj))
        {
            PopupScript.SetPopup("Request Sent", 3f);
            Debug.Log("Request sent");
        }
    }
}
