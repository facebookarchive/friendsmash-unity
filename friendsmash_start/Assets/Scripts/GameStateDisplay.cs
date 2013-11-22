using UnityEngine;
using System.Collections;

public class GameStateDisplay : MonoBehaviour
{
    public Texture HeartTexture;

    public GUIText ScoreDisplay, SmashDisplay;

    private int score
    {
        get
        {
            return GameStateManager.Score;
        }
    }

    private int livesRemaining { get { return GameStateManager.LivesRemaining; } }

    void Start()
    {
        ScoreDisplay.text = "Score: " + score;
        SmashDisplay.text = "Smash " + GameStateManager.FriendName;
    }

    void Update()
    {
        if (ScoreDisplay) ScoreDisplay.text = "Score: " + score;
    }

    void OnGUI()
    {
        int heartX = 10;
        for (int i = 0; i < livesRemaining; ++i)
        {
            GUI.Label(new Rect(heartX + i * (1 + HeartTexture.width), 30, HeartTexture.height, HeartTexture.width), HeartTexture);
        }
		
		GUI.DrawTexture(new Rect((Screen.width-50)/2,80,50,50), GameStateManager.FriendTexture);
    }

}
