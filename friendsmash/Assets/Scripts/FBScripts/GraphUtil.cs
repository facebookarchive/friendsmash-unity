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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Utility class for useful operations when working with the Graph API
public class GraphUtil : ScriptableObject
{
    // Generate Graph API query for a user/friend's profile picture
    public static string GetPictureQuery(string facebookID, int? width = null, int? height = null, string type = null, bool onlyURL = false)
    {
        string query = string.Format("/{0}/picture", facebookID);
        string param = width != null ? "&width=" + width.ToString() : "";
        param += height != null ? "&height=" + height.ToString() : "";
        param += type != null ? "&type=" + type : "";
        if (onlyURL) param += "&redirect=false";
        if (param != "") query += ("?g" + param);
        return query;
    }

    // Download an image using WWW from a given URL
    public static void LoadImgFromURL (string imgURL, Action<Texture> callback)
    {
        // Need to use a Coroutine for the WWW call, using Coroutiner convenience class
        Coroutiner.StartCoroutine(
            LoadImgEnumerator(imgURL, callback)
        );
    }
    
    public static IEnumerator LoadImgEnumerator (string imgURL, Action<Texture> callback)
    {
        WWW www = new WWW(imgURL);
        yield return www;
        
        if (www.error != null)
        {
            Debug.LogError(www.error);
            yield break;
        }
        callback(www.texture);
    }

	// Pull out the picture image URL from a JSON user object constructed in FBGraph.GetPlayerInfo() or FBGraph.GetFriends()
    public static string DeserializePictureURL(object userObject)
    {
        // friendObject JSON format in this situation
        // {
        //   "first_name": "Chris",
        //   "id": "10152646005463795",
        //   "picture": {
        //      "data": {
        //          "url": "https..."
        //      }
        //   }
        // }
        var user = userObject as Dictionary<string, object>;

        object pictureObj;
        if (user.TryGetValue("picture", out pictureObj))
        {
            var pictureData = (Dictionary<string, object>)(((Dictionary<string, object>)pictureObj)["data"]);
            return (string)pictureData["url"];
        }
        return null;
    }

    // Pull out score from a JSON user entry object constructed in FBGraph.GetScores()
    public static int GetScoreFromEntry(object obj)
    {
        Dictionary<string,object> entry = (Dictionary<string,object>) obj;
        return Convert.ToInt32(entry["score"]);
    }
}
