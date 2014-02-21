using UnityEngine;
using System.Collections;

public class GameState : ScriptableObject
{
    public int Score;
    public int Lives;
    public GameObject FatalEnemy;

    void Awake()
    {
        Object.DontDestroyOnLoad(this);
    }
}
