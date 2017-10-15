using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class UserData : MonoBehaviour {

	public string userName;
	public string motion;

	public string savePath;
	public string fileName;		// 0: bvh, 1: calc, 2: tag

	// Name Panel var
	public GameObject namePanel;
	public InputField input;

	// Rec Panel var
	public InputField motionName;
	public GameObject recPanel;
	public Text modeText;

	// Use this for initialization
	void Start () {

		namePanel.SetActive (true);
		recPanel.SetActive (false);

	}
	
	// Update is called once per frame
	void Update () {}

	public void setName()
	{
		if (input.text != "") 
		{
			userName = input.text;
			namePanel.SetActive (false);
			recPanel.SetActive (true);

			// If folder is not exist, create a new folder
			if(!AssetDatabase.IsValidFolder("Assets/Animation/" + userName)) {
				AssetDatabase.CreateFolder("Assets/Animation", userName);
			}
		}
		else
			Debug.Log ("Name can't be null!");

	}

	public void setMotionName()
	{
		if (motionName.text != "") {
			motion = motionName.text;
			if (!AssetDatabase.IsValidFolder ("Assets/Animation/" + userName + "/" + motionName.text)) {
				AssetDatabase.CreateFolder ("Assets/Animation/" + userName, motion);
			}
			savePath = "Assets/Animation/" + userName + "/" + motion + "/";
			fileName = userName + "_" + motion;
			modeText.text = motion;
		}
	}
}
