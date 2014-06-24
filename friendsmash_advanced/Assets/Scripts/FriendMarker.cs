using UnityEngine;
using System.Collections;

public class FriendMarker : MonoBehaviour
{
    private MainMenu mainMenu;
    public Texture FriendTexture, EnemyTexture;
    Texture[] CelebTextures;

    public float FriendThreshold = 0.5f;

    // Use this for initialization
    void Start()
    {

        mainMenu = (MainMenu)(GameObject.Find("Main Menu").GetComponent("MainMenu"));
        CelebTextures = mainMenu.CelebTextures;
        if (GameStateManager.CelebFriend != -1 ) 
            FriendTexture = CelebTextures[GameStateManager.CelebFriend];
        else if (GameStateManager.FriendTexture != null) FriendTexture = GameStateManager.FriendTexture;

        float diceRoll = Random.value;
        if (diceRoll <= FriendThreshold)
        {
            gameObject.tag = "Friend";
            renderer.material.mainTexture = FriendTexture;
        }
        else
        {
            gameObject.tag = "Enemy";
            int numValidCelebs =  GameStateManager.CelebFriend == -1 ? CelebTextures.Length - 1 : CelebTextures.Length - 2;
            int which = Random.Range(0,numValidCelebs);
            if (GameStateManager.CelebFriend == which)
                which = CelebTextures.Length - 1;
            EnemyTexture = CelebTextures[which];
            renderer.material.mainTexture = EnemyTexture;
        }
    }
}
