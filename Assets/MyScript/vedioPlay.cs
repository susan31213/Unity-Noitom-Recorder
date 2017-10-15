using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vedioPlay : MonoBehaviour {

	public MovieTexture mv;
	// Use this for initialization
	void Start () {
		GetComponent<Renderer> ().material.mainTexture = mv;
		mv.loop = false;
	}

	void OnGUI () {
		if(GUI.Button(new Rect(20,30,50,50),"PLAY")){
			if (!mv.isPlaying) {
				mv.Play ();
				GetComponent<AudioSource> ().Play ();
			} else {
				mv.Pause ();
				GetComponent<AudioSource> ().Pause ();
			}
		}
	}
}
