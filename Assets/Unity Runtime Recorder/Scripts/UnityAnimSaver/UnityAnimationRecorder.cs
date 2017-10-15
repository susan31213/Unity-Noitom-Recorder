#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using System.Collections;

public class UnityAnimationRecorder : MonoBehaviour {

	// UserData ref.
	UserData userData;

	// save file path
	public string savePath;
	public string fileName;
	public int takeNum = 0;

	// use it when save multiple files
	int fileIndex = 0;

	public KeyCode startRecordKey = KeyCode.Q;
	public KeyCode stopRecordKey = KeyCode.W;

	// options
	public bool showLogGUI = false;
	string logMessage = "";

	public bool recordLimitedFrames = false;
	public int recordFrames = 1000;
	int frameIndex = 0;

	public bool changeTimeScale = false;
	public float timeScaleOnStart = 0.0f;
	public float timeScaleOnRecord = 1.0f;

	Transform[] recordObjs;
	UnityObjectAnimation[] objRecorders;

	bool isStart = false;
	float nowTime = 0.0f;

	// Use this for initialization
	void Start () {
		SetupRecorders ();
		userData = GameObject.FindGameObjectWithTag ("DataManeger").GetComponent<UserData> ();

	}

	void SetupRecorders () {
		recordObjs = gameObject.GetComponentsInChildren<Transform> ();
		objRecorders = new UnityObjectAnimation[recordObjs.Length];

		frameIndex = 0;
		nowTime = 0.0f;

		for (int i = 0; i < recordObjs.Length; i++) {
			string path = AnimationRecorderHelper.GetTransformPathName (transform, recordObjs [i]);
			objRecorders [i] = new UnityObjectAnimation ( path, recordObjs [i]);
		}

		if (changeTimeScale)
			Time.timeScale = timeScaleOnStart;
	}
	
	// Update is called once per frame
	void Update () {
	
		if (Input.GetKeyDown (startRecordKey)) {
			StartRecording ();
		}

		if (Input.GetKeyDown (stopRecordKey)) {
			StopRecording ();
		}

		if (isStart) {
			nowTime += Time.deltaTime;

			for (int i = 0; i < objRecorders.Length; i++) {
				objRecorders [i].AddFrame (nowTime);
			}
		}

	}

	public void StartRecording () {
		CustomDebug ("Start Recorder");
		isStart = true;
		Time.timeScale = timeScaleOnRecord;
	}


	public void StopRecording () {

		if (isStart) 
		{
			CustomDebug ("End Record, generating .anim file");
			isStart = false;

			ExportAnimationClip ();
			ResetRecorder ();
		}

	}

	void ResetRecorder () {
		SetupRecorders ();
	}


	void FixedUpdate () {

		if (isStart) {

			if (frameIndex < recordFrames) {
				for (int i = 0; i < objRecorders.Length; i++) {
					objRecorders [i].AddFrame (nowTime);
				}

				++frameIndex;
			} else {
				isStart = false;
				ExportAnimationClip ();
				CustomDebug ("Recording Finish, generating .anim file");
			}
		}
	}

	void OnGUI () {
		if (showLogGUI)
			GUILayout.Label (logMessage);
	}

	void ExportAnimationClip () {

		// check if there is old anim in folder, get take number
		string[] folder = new string[1];
		folder [0] = "Assets/Animation/" + savePath.Substring (17, userData.userName.Length) + "/" + userData.motion;

		string[] result = AssetDatabase.FindAssets (fileName, folder);
		if(result.Length != 0)
			takeNum = Convert.ToInt32 (AssetDatabase.GUIDToAssetPath (result [result.Length - 1]).Substring (20 + userData.userName.Length + userData.motion.Length * 2, 3));
		else
			takeNum = 0;
		takeNum++;
		string exportFilePath = savePath + fileName + "_" + takeNum.ToString("D3");



		// if record multiple files when run
//		if (fileIndex != 0)
//			exportFilePath += "-" + fileIndex + ".anim";
//		else
			exportFilePath += ".anim";


		AnimationClip clip = new AnimationClip ();
		clip.name = fileName;
		clip.legacy = true;

		for (int i = 0; i < objRecorders.Length; i++) {
			UnityCurveContainer[] curves = objRecorders [i].curves;

			for (int x = 0; x < curves.Length; x++) {
				clip.SetCurve (objRecorders [i].pathName, typeof(Transform), curves [x].propertyName, curves [x].animCurve);
			}
		}

		clip.EnsureQuaternionContinuity ();
		AssetDatabase.CreateAsset ( clip, exportFilePath );

		CustomDebug (".anim file generated to " + exportFilePath);
//		fileIndex++;
	}

	void CustomDebug ( string message ) {
		if (showLogGUI)
			logMessage = message;
		else
			Debug.Log (message);
	}

	public bool getIsStart()  { return isStart; }
	public string getSavePath()  { return savePath; }
	public string getFileName()  { return fileName; }
	public string getFileNameWithIndex()  { return fileName + "_" + takeNum.ToString ("D3"); }
	public int getTakeNum()  { return takeNum; }
}
#endif