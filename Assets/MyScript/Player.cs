using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using UnityEngine;

public class player : MonoBehaviour {

	public VideoPlayer v;
	// Use this for initialization
	void Start () {
		v = GetComponent<VideoPlayer> ();
	}
	
	// Update is called once per frame
	void OnGUI () {
		if(GUI.Button(new Rect(20,20,50,50),"PLAY")){
			if (!v.isPlaying) {
				v.Play ();
				//GetComponent<AudioSource> ().Play ();
			} else {
				v.Pause ();
				//GetComponent<AudioSource> ().Pause ();
			}
		}
	}
}
