using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Neuron;

public class PlayList : MonoBehaviour {

	public UserData userData;
	public NeuronAnimatorInstance noitom;
	string filePath;

	public GameObject playImg;
	public GameObject bar;
	public GameObject copyBtn;

	public Dropdown motionList;
	public Dropdown playList;

	public Text usernameText;

	List<StampTag> tags;
	int	nowTag = -1;
	public Transform tagParent;
	public GameObject tagTemplate;
	public InputField newtagName;
	public Text indexText;

	float keyTimer = 0;

	// Use this for initialization
	void Start () {
		tags = new List<StampTag> ();
	}
	
	// Update is called once per frame
	void Update () {
		
		playImg.SetActive (noitom.GetActor ().IsPlayback ());
		bar.SetActive (noitom.GetActor ().IsPlayback ());
		copyBtn.SetActive (noitom.GetActor ().IsPlayback ());

		if (nowTag != -1)
			indexText.text = "" + nowTag.ToString("00");
		else
			indexText.text = "no tag";

		// fast forward / backward
		if (Input.GetKeyDown (KeyCode.LeftArrow))
			noitom.GetActor ().backward ();
		if (Input.GetKeyDown (KeyCode.RightArrow))
			noitom.GetActor ().forward ();
		
		if (Input.GetKey (KeyCode.LeftArrow)) {
			keyTimer += Time.deltaTime;
			if (keyTimer > 1) {
				noitom.GetActor ().backward ();
				keyTimer = 1.5f;
			}
		}
		if (Input.GetKey (KeyCode.RightArrow)) {
			if (keyTimer > 1) {
				noitom.GetActor ().forward ();
			}
			else
				keyTimer += Time.deltaTime;
		}

		if (Input.GetKeyUp (KeyCode.LeftArrow) || Input.GetKeyUp (KeyCode.RightArrow))
			keyTimer = 0;
	}

	public void UpdateMotionList()
	{
		userData = GameObject.FindGameObjectWithTag ("DataManeger").GetComponent<UserData> ();
		usernameText.text = userData.userName;
		string folder = Application.dataPath + "/Animation/" + userData.userName;
		string[] result = System.IO.Directory.GetDirectories (folder);
		motionList.ClearOptions ();

		foreach (string s in result) 
		{
			motionList.options.Add (new Dropdown.OptionData (s.Substring (1 + folder.Length)));
		}

		motionList.RefreshShownValue ();
		motionList.value = motionList.options.Count - 1;
	}

	public void UpdatePlayList()
	{
		string[] folder = new string[1];
		folder[0] = "Assets/Animation/" + userData.userName + "/" + motionList.captionText.text;
		string[] result = AssetDatabase.FindAssets ("Bvh", folder);
		playList.ClearOptions ();

		foreach (string guid in result) 
		{
			playList.options.Add(new Dropdown.OptionData (AssetDatabase.GUIDToAssetPath (guid).Substring (folder[0].Length + 3 + userData.userName.Length + motionList.captionText.text.Length, 3)));
		}

		playList.RefreshShownValue ();
		playList.value = playList.options.Count - 1;
	}

	public void getTagData()
	{
		tags.Clear ();
		foreach (StampTag tag in noitom.GetActor ().readTagKvp) {
			tags.Add (tag);
		}
	}

	public void UpdateTagList()
	{
		foreach (Transform child in tagParent)
			Destroy (child.gameObject);
		for (int i = 0; i < tags.Count; i++) {
			Text tagText = Instantiate (tagTemplate, tagParent).GetComponentInChildren<Text> ();
			tagText.text = i.ToString("00") + ". " + tags [i].name + "\nStart: " + tags [i].startStamp + "\nEnd: " + tags [i].endStamp;
		}
	}

	public void AddTag()
	{
		if (noitom.GetActor ().IsPlayback ()) {
			Instantiate (tagTemplate, tagParent);
			tags.Add(new StampTag(newtagName.text, noitom.GetActor().playbackTimeStamp));
			UpdateTagList ();
		}
	}

	public void DeleteTag()
	{
		tags.RemoveAt (nowTag);
		UpdateTagList ();
	}

	public void SaveTag()
	{
		// tag
		System.IO.FileStream fs = new System.IO.FileStream (filePath + "Tag.txt", System.IO.FileMode.Create);
		System.IO.StreamWriter sw = new System.IO.StreamWriter (fs);
		for(int i=0 ;i<tags.Count; i++) {
			if (tags[i].endStamp != -1) {
				System.Text.StringBuilder line = new System.Text.StringBuilder();
				line.Append(i + " ");
				line.Append(tags[i].name + " ");
				line.Append(tags[i].startStamp + " ");
				line.Append(tags[i].endStamp);
				line.AppendLine ();
				sw.Write (line.ToString());
			}
		}
		sw.Flush ();
		sw.Close ();
		AssetDatabase.Refresh ();
	}

	public void SelectTag(int index)
	{
		nowTag = index;
	}

	public void setTagStart()
	{
		if (nowTag >= 0 && noitom.GetActor ().IsPlayback ()) {
			StampTag newStamp = new StampTag (tags [nowTag].name, noitom.GetActor().playbackTimeStamp);
			newStamp.endStamp = tags [nowTag].endStamp;
			tags.Insert (nowTag, newStamp);
			tags.RemoveAt (nowTag + 1);
			UpdateTagList ();
		}
	}

	public void setTagEnd()
	{
		if (nowTag >= 0 && noitom.GetActor ().IsPlayback ()) {
			StampTag newStamp = new StampTag (tags [nowTag].name, tags[nowTag].startStamp);
			newStamp.endStamp = noitom.GetActor().playbackTimeStamp;
			tags.Insert (nowTag, newStamp);
			tags.RemoveAt (nowTag + 1);
			UpdateTagList ();
		}
	}

	public void setPlaybackfilePath()
	{
		filePath = "Assets/Animation/" + userData.userName + "/" + motionList.captionText.text + "/" + userData.userName + "_" + motionList.captionText.text + "_" + playList.captionText.text;
		noitom.GetActor ().ReadPlaybackData (filePath + "Bvh.txt");
		noitom.GetActor ().ReadPlaybackTag (filePath + "Tag.txt");
	}

	public void PlayAnim()
	{
		noitom.GetActor ().StartPlayback ();
//		RECModel.SetActive (false);
//		PlayModel.SetActive (true);
//		recordModeButton.SetActive (false);
//		playModeButton.SetActive (true);
//
//		Time.timeScale = 1;
//
//		AnimationClip chooseClip = (AnimationClip)AssetDatabase.LoadAssetAtPath ("Assets/Animation/" + userData.userName + "/" + userData.motion + "/" + playList.captionText.text + ".anim", typeof(AnimationClip));
//
//		if (chooseClip != null)
//			anim.clip = chooseClip;
//		else
//			Debug.Log ("Can't find clip.");
//			
//		anim.AddClip(chooseClip, "clip");
//		anim.Play ("clip");
	}

	public void PauseAnim()
	{
		noitom.GetActor ().StartPause ();
	}

	public void StopAnim()
	{
		noitom.GetActor ().StopPlayback ();
	}
}
