using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public string FirstName, Name, FacebookId;
    public Texture ProfilePic;
    public int Score;
    public Dictionary<string, string> Data;

    public void setProfilePic(Texture pic)
    { // convenient for callbacks
        ProfilePic = pic;
    }
}
