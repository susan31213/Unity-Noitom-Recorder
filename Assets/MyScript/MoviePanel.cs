using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class MoviePanel : MonoBehaviour {

	public GameObject movieP;
	VideoPlayer player;
	public VideoClip[] clips;

	public Dropdown videoIndex;

	// Use this for initialization
	void Start () {
		player = GetComponent<VideoPlayer> ();
		movieP.SetActive (false);
	}
	
	// Update is called once per frame
	void Update () {}

	public void setVideo() {
		StopMovie ();
		player.clip = clips [videoIndex.value];
	}

	public void PlayMovie() {
		movieP.SetActive (true);
		player.Play ();
	}

	public void StopMovie(){
		movieP.SetActive (false);
		player.Stop ();
	}

	public void PauseMovie()
	{
		player.Pause ();
	}
}

