using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public enum GameMode { Movement, Attack, Skill, None }
    public GameMode gameMode { get; set; }

    private void Start()
    {
        GameMode gameMode = GameMode.None;
    }
}
