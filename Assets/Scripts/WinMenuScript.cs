﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Cloud.Analytics;

public class WinMenuScript : MonoBehaviour 
{
    private GUISkin skin;

    public bool WinActive = false;
    public RectTransform finishMenu;

    private Rect button1Rect = new Rect(15, 15, 160, 30);
    private Rect button2Rect = new Rect(15, 15, 160, 30);

    public string levelText;
    public string levelKeuze;    

    public bool laatstelevel;

    public int levelEnBoss;

    // Analytics
    private bool winMenuStarted = false;
    private float winMenuStartTime;
    private bool continueAfterWinMenu;

	// Use this for initialization
	void Start () 
    {
        // Load a skin for the buttons
        skin = Resources.Load("ButtonSkin") as GUISkin;
	}    

    void OnGUI()
    {
        button1Rect.x = (Screen.width / 2) - (button1Rect.width / 2);
        button1Rect.y = (Screen.height / 2) - (button1Rect.height / 2);

        button2Rect.x = (Screen.width / 2) - (button2Rect.width / 2);
        button2Rect.y = (Screen.height / 2) - (button2Rect.height / 2);        

        // Gewonnen menu
        if (WinActive == true)
        {
            if (!winMenuStarted)
            {
                winMenuStartTime = Time.realtimeSinceStartup;
                winMenuStarted = true;
            }

            GameControl.control.highScore += 100;

            // Stuur bericht naar Gamecontrol voor achievements
            GameControl.control.LevelComplete(levelEnBoss);
            GameControl.control.StilteVerslagen(levelEnBoss);

            // Vind player 'health' voor Onaantasbaar achievement
            GameObject stats = GameObject.Find("Stats");
            Player playerStats = stats.GetComponent<Player>();

            if (levelEnBoss == 0)
            {
                if (playerStats.currentHealth == 10)
                {
                    GameControl.control.onaantasbaar[0] = true;
                }
            }
            if (levelEnBoss == 1)
            {
                if (GameControl.control.icarusFall == false)
                {
                    // Achievement gehaald
                    GameControl.control.IcarusFall();                    
                }
                if (playerStats.currentHealth == 10)
                {
                    GameControl.control.onaantasbaar[1] = true;
                }
            }
            if (levelEnBoss == 2)
            {
                if (GameControl.control.ijsGeraakt == false)
                {
                    // Achievement gehaald
                    GameControl.control.IJsVrij();
                }
                else
                {
                    // zet de waarde weer op false
                    GameControl.control.ijsGeraakt = false;
                }

                if (GameControl.control.droogOverGevallen == false)
                {
                    // Achievement gehaald
                    GameControl.control.DroogOverCheck();
                }
                else
                {
                    // zet de waarde weer op false
                    GameControl.control.droogOverGevallen = false;
                }

                if (playerStats.currentHealth == 10)
                {
                    GameControl.control.onaantasbaar[2] = true;
                }
            }
            if (levelEnBoss == 3)
            {
                if (playerStats.currentHealth == 10)
                {
                    GameControl.control.onaantasbaar[3] = true;
                }
                if (System.Array.TrueForAll(GameControl.control.onaantasbaar, item => item) == true)
                {
                    // Achievement gehaald
                    GameControl.control.OnaantasbaarCheck();
                }                
            }

            button1Rect.y = button1Rect.y + 75;
            button2Rect.y = button2Rect.y - 15;

            // Activeer Ingame menu
            finishMenu.gameObject.SetActive(true);

            // Pauzeer spel
            Time.timeScale = 0;
            
            // Set the skin to use
            GUI.skin = skin;

            if (laatstelevel == false)
            {                
                // Naar volgende level Button
                if (GUI.Button(
                    // Center in X, 2/3 of the height in Y
                    button2Rect,
                    levelText
                    ))
                {
                    continueAfterWinMenu = true; // Analytics
                    StartGameAnalytics();

                    WinActive = false;
                    Time.timeScale = 1;
                    finishMenu.gameObject.SetActive(false);
                    Application.LoadLevel(levelKeuze);// Load next Level
                }
            }            

            if (laatstelevel == true)
            {
                Text score = GameObject.Find("Score").GetComponent<Text>();                
                score.text = GameControl.control.highScore.ToString();
            }

            // Naar main menu Button
            if (GUI.Button(
                // Center in X, 2/3 of the height in Y
                button1Rect,
                "Menu"
                ))
            {
                continueAfterWinMenu = false; // Analytics
                StartGameAnalytics();

                WinActive = false;
                Time.timeScale = 1;
                finishMenu.gameObject.SetActive(false);
                Application.LoadLevel("MainMenu"); // Load Main Menu
            }            
        }
    }

    /**
     * Sends the selected player and level to analytics
     */
    void StartGameAnalytics()
    {
        string customEventName = "BossBattleWon" + Application.loadedLevelName;
        float winMenuDuration = (Time.realtimeSinceStartup - winMenuStartTime);
        float bossBattleDuration = (Time.timeSinceLevelLoad - GameControl.control.bossBattleStartTime);

        AnalyticsResult results = UnityAnalytics.CustomEvent(customEventName, new Dictionary<string, object>
        {
            { "runningTime", Time.timeSinceLevelLoad },
            { "damageTaken", GameControl.control.damageTaken },
            { "kidsFound", GameControl.control.kidsFound },
            { "bossBattleDuration", bossBattleDuration },
            { "winMenuDuration", winMenuDuration },
            { "continueAfterWinMenu", continueAfterWinMenu },
            { "respawns", GameControl.control.respawns },
            { "timesPaused", GameControl.control.timesPaused },
            { "pauseDuration", GameControl.control.pauseDuration },
        });

        if (results != AnalyticsResult.Ok)
            Debug.LogError("Analytics " + customEventName + ": " + results.ToString());
        else
            Debug.Log("Analytics " + customEventName + ": Done");

        // Shots
        customEventName += "Shots";

        AnalyticsResult results2 = UnityAnalytics.CustomEvent(customEventName, new Dictionary<string, object>
        {
            { "projectile1Shot", GameControl.control.projectile1Shot },
            { "projectile2Shot", GameControl.control.projectile2Shot },
            { "projectile3Shot", GameControl.control.projectile3Shot },
        });

        if (results2 != AnalyticsResult.Ok)
            Debug.LogError("Analytics " + customEventName + ": " + results.ToString());
        else
            Debug.Log("Analytics " + customEventName + ": Done");
    }
}
