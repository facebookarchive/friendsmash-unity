using UnityEngine;
using System.Collections;

public class FriendMarker : MonoBehaviour
{

    public Texture FriendTexture, EnemyTexture;
    public Texture[] CelebTextures;

    public float FriendThreshold = 0.5f;

    // Use this for initialization
    void Start()
    {
        if (GameStateManager.FriendTexture != null) FriendTexture = GameStateManager.FriendTexture;
        float diceRoll = Random.value;
        if (diceRoll <= FriendThreshold)
        {
            gameObject.tag = "Friend";
            renderer.material.mainTexture = FriendTexture;
        }
        else
        {
            gameObject.tag = "Enemy";
            int which = Random.Range(0, CelebTextures.Length - 1);
            EnemyTexture = CelebTextures[which];
            renderer.material.mainTexture = EnemyTexture;
        }
    }
}
