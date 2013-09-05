using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FBUtil : ScriptableObject
{

    public static string GetPictureURL(string facebookID, int? width = null, int? height = null, string type = null, bool secure = true)
    {
        string url = "https://graph.facebook.com/" + facebookID + "/picture";
        string query = width != null ? "&width=" + width.ToString() : "";
        query += height != null ? "&height=" + height.ToString() : "";
        query += type != null ? "&type=" + type : "";
        query += "&access_token=" + FB.AccessToken;
        if (query != "") url += ("?g" + query);
        return url;
    }

    public static IEnumerator GetFriendPictureTexture(string facebookID, int width = 128, int height = 128, string type = null, bool secure = true)
    {
        string url = GetPictureURL(facebookID, width, height, type, secure);
        WWW www = new WWW(url);
        yield return www;
        GameStateManager.FriendTexture = www.texture;
    }

    public delegate void TextureCallback(Texture tex);

    public static IEnumerator GetPictureTexture(string facebookID, int width = 128, int height = 128, string type = null, bool secure = true, TextureCallback callback = null)
    {
        string url = GetPictureURL(facebookID, width, height, type, secure);
        WWW www = new WWW(url);
        yield return www;
        callback(www.texture);
    }

    public static Dictionary<string, string> RandomFriend(List<object> friends)
    {
        var fd = ((Dictionary<string, object>)(friends[Random.Range(0, friends.Count - 1)]));
        var friend = new Dictionary<string, string>();
        friend["id"] = (string)fd["id"];
        friend["first_name"] = (string)fd["first_name"];
        return friend;
    }

    public static Dictionary<string, string> DeserializeJSONProfile(string response)
    {
        var responseObject = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
        object nameH;
        var profile = new Dictionary<string, string>();
        if (responseObject.TryGetValue("first_name", out nameH))
        {
            profile["first_name"] = (string)nameH;
        }
        return profile;
    }
	
	public static List<object> DeserializeScores(string response) 
	{

		var responseObject = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
		object scoresh;
		var scores = new List<object>();
		if (responseObject.TryGetValue ("data", out scoresh)) 
		{
			scores = (List<object>) scoresh;
		}

		return scores;
	}

    public static List<object> DeserializeJSONFriends(string response)
    {
        var responseObject = MiniJSON.Json.Deserialize(response) as Dictionary<string, object>;
        object friendsH;
        var friends = new List<object>();
        if (responseObject.TryGetValue("friends", out friendsH))
        {
            friends = (List<object>)(((Dictionary<string, object>)friendsH)["data"]);
        }
        return friends;
    }
}