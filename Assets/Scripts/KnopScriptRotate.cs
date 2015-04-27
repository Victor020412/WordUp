﻿using UnityEngine;
using System.Collections;

public class KnopScriptRotate : MonoBehaviour {

    public GameObject knop;
    public GameObject[] lichten;

	private int teller = 0;
    private bool ingedrukt;

	// Use this for initialization
	void Start () 
    {
        ingedrukt = false;
	}
	
	// Update is called once per frame
	void Update () 
    {
		if (ingedrukt == true && teller == 0) 
		{
			knop.transform.Translate (0, -Time.deltaTime * 3, 0);
			teller++;
		}
	}

    void OnTriggerEnter2D(Collider2D collision)
    {        
        if (collision.gameObject.tag == "Player")
        {
            if (ingedrukt == false)
            { 
                // Zet licht aan
                foreach (GameObject o in lichten)
                {
                    o.GetComponent<Light>().enabled = true;
                }             

                ingedrukt = true;
            }            
        }
    }    
}
