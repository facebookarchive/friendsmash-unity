using System.Collections.Generic;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private static GameStateManager instance;

    public static bool ScoringLockout = true;

    public static int StartingLives = 3, StartingScore = 0;
    private int lives, score;
    private int? highScore;

    private string username = null;
    public static Texture UserTexture;
    public static Texture FriendTexture = null;
    private string friendName = null;
    private string friendID = null;

    public static bool IsFullscreen = false;

    public bool Immortal;
    private static bool immortal;

    public static int ToSmash = -1;

    public static string FriendID
    {
        set { Instance.friendID = value; }
        get { return Instance.friendID; }
    }

    public static string FriendName
    {
        set { Instance.friendName = value; }
        get { return Instance.friendName == null ? "Blue Guy" : Instance.friendName; }
    }

    public static Dictionary<string, Player> leaderboard;

    void Start()
    {
        lives = StartingLives;
        score = StartingScore;
        immortal = Instance.Immortal;
        ScoringLockout = false;
        Time.timeScale = 1.0f;
    }

    public void StartGame()
    {
        Start();
    }

    void Awake()
    {
        DontDestroyOnLoad(this);
    }

    public static GameStateManager Instance { get { return current(); } }
    public static int Score { get { return Instance.score; } }
    public static int HighScore { get { return Instance.highScore.HasValue ? Instance.highScore.Value : 0; } set { Instance.highScore = value; }}
    public static int LivesRemaining { get { return Instance.lives; } }
    public static string Username
    {
        get { return Instance.username; }
        set { Instance.username = value; }
    }
    delegate GameStateManager InstanceStep();

    static InstanceStep init = delegate()
    {
        GameObject container = new GameObject("GameStateManagerManager");
        instance = container.AddComponent<GameStateManager>();
        instance.lives = StartingLives;
        instance.score = StartingScore;
        instance.highScore = null;
        current = then;
        return instance;
    };
    static InstanceStep then = delegate() { return instance; };
    static InstanceStep current = init;

    public static void onFriendDie()
    {
        if (--Instance.lives == 0)
        {
            EndGame();
        }
    }

    public static void onFriendSmash()
    {
        if (!ScoringLockout) ++Instance.score;
    }

    public static void onEnemySmash(GameObject enemy)
    {
        // Instance.fatalEnemy = enemy;
        enemy.AddComponent<EnemyExploder>();
    }

    public static void EndGame()
    {
        if (immortal) return;
        GameObject[] friends = GameObject.FindGameObjectsWithTag("Friend");
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject t in friends)
        {
            Destroy(t);
        }
        foreach (GameObject t in enemies)
        {
            Destroy(t);
        }
        Util.Log("EndGame Instance.highScore = " + Instance.highScore + "\nInstance.score = " + Instance.score);


        Instance.highScore = Instance.score;
        Util.Log("Player has new high score :" + Instance.score);
        if (FB.IsLoggedIn)
        {
            var query = new Dictionary<string, string>();
            query["score"] = Instance.score.ToString();
            FB.API("/me/scores", Facebook.HttpMethod.POST, delegate(FBResult r) { Util.Log("Result: " + r.Text); }, query);
        }
        
        if (FB.IsLoggedIn && !string.IsNullOrEmpty(GameStateManager.FriendID))
        {
            var querySmash = new Dictionary<string, string>();
            querySmash["profile"] = GameStateManager.FriendID;
            FB.API ("/me/" + FB.AppId + ":smash", Facebook.HttpMethod.POST, 
            delegate(FBResult r) { Util.Log("Result: " + r.Text); }, querySmash);
        }

        Application.LoadLevel("MainMenu");
        Time.timeScale = 0.0f;
    }
}
