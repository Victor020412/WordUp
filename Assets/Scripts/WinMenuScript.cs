﻿using UnityEngine;
using System.Collections;

public class WinMenuScript : MonoBehaviour 
{
    private GUISkin skin;

    public static bool WinActive = false;
    public RectTransform finishMenu;

    private Rect button1Rect = new Rect(15, 15, 160, 30);
    private Rect button2Rect = new Rect(15, 15, 160, 30);

    public string levelText;
    public string levelKeuze;

    // Keyboard control
    string[] buttons = new string[2] { "Menu", "Volgende level" };
    private int selected = 0;

	// Use this for initialization
	void Start () 
    {
        // Load a skin for the buttons
        skin = Resources.Load("ButtonSkin") as GUISkin;

        selected = 0;
	}

    int menuSelection(string[] buttonsArray, int selectedItem, string direction)
    {
        if (direction == "up")
        {
            if (selectedItem == 0)
            {
                selectedItem = buttonsArray.Length - 1;
            }
            else
            {
                selectedItem -= 1;
            }
        }

        if (direction == "down")
        {

            if (selectedItem == buttonsArray.Length - 1)
            {
                selectedItem = 0;
            }
            else
            {
                selectedItem += 1;
            }
        }
        return selectedItem;
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            selected = menuSelection(buttons, selected, "up");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            selected = menuSelection(buttons, selected, "down");
        }
	}

    void OnGUI()
    {
        button1Rect.x = (Screen.width / 2) - (button1Rect.width / 2);
        button1Rect.y = (Screen.height / 2) - (button1Rect.height / 2);

        button2Rect.x = (Screen.width / 2) - (button2Rect.width / 2);
        button2Rect.y = (Screen.height / 2) - (button2Rect.height / 2);

        GUI.FocusControl(buttons[selected]);        

        // Gewonnen menu
        if (WinActive == true)
        {
            button1Rect.y = button1Rect.y + 75;
            button2Rect.y = button2Rect.y - 15;

            // Activeer Ingame menu
            finishMenu.gameObject.SetActive(true);

            // Pauzeer spel
            Time.timeScale = 0;
            
            // Set the skin to use
            GUI.skin = skin;

            GUI.SetNextControlName(buttons[0]);
            // Naar volgende level Button
            if (GUI.Button(
                // Center in X, 2/3 of the height in Y
                button2Rect,
                levelText
                ))
            {
                WinActive = false;
                Time.timeScale = 1;
                finishMenu.gameObject.SetActive(false);
                Application.LoadLevel(levelKeuze);// Load next Level
            }

            GUI.SetNextControlName(buttons[1]);
            // Naar main menu Button
            if (GUI.Button(
                // Center in X, 2/3 of the height in Y
                button1Rect,
                "Menu"
                ))
            {
                WinActive = false;
                Time.timeScale = 1;
                finishMenu.gameObject.SetActive(false);
                Application.LoadLevel("MainMenu"); // Load Main Menu
            }            
        }
    }
}
