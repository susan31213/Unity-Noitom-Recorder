using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System;

public class Webcam : MonoBehaviour {
	public UserData user;
	public GameObject webcam;
	public GameObject hideB;

	public GameObject invis;
	public Text hideBtn;

	public VideoPlayer player;
	WebCamTexture webcamtexture;
	public RawImage rimg;

	//WEB RECORD PART START

	private Thread thread;
	private string picName = "pic";
	private string filePath = "D:/TempPic";
	private string fileExtension = ".png";
	private string ffmpegPath;
	private string videoOutput;
	private int fileCount = 1;
	private bool isGetPic = false;
	private bool isHide = false;

	private Vector3 originePos; 

	//WEB RECORD PART END

	// Use this for initialization
	void Start () {
		webcam.SetActive (false);
		ffmpegPath = Application.dataPath;
		originePos = webcam.transform.position;
	}

	// Update is called once per frame
	void Update () {
		if (isGetPic == true) {
			isGetPic = false;
			StartCoroutine (GetPic ());
		}
	}

	public void startRecord(){
		if (!Directory.Exists (filePath)) {
			Directory.CreateDirectory (filePath);
		} 

		string[] tempFiles = Directory.GetFiles(filePath);
		if (tempFiles.Length > 0) {
			Directory.Delete (filePath, true);
		}

		fileCount = 1;
		webcam.SetActive (true);
		webcamtexture = new WebCamTexture();
		rimg.texture = webcamtexture;
		rimg.material.mainTexture = webcamtexture;
		webcamtexture.Play();
		isGetPic = true;
	}

	public void hideCam(){
		if (!isHide) {
			isHide = true;
			webcam.transform.position = invis.transform.position;
			hideBtn.text = "Show";
		} else {
			isHide = false;
			webcam.transform.position = originePos;
			hideBtn.text = "Hide";
		}
	}

	public void stopRecord(){
		// check file index
		int takeNum = 0;
		string[] folder = new string[1];
		folder [0] = user.savePath.Substring(0, user.savePath.Length-1);

		string[] result = AssetDatabase.FindAssets (user.fileName, folder);
		if (result.Length != 0) {
			takeNum = Convert.ToInt32 (AssetDatabase.GUIDToAssetPath (result [result.Length - 1]).Substring (1 + user.savePath.Length + user.fileName.Length, 3));
		} else {
			takeNum = 0;
		}

		videoOutput = user.savePath + user.fileName + "_" + takeNum.ToString ("000") + ".mp4";
		webcam.SetActive (false);
		webcamtexture.Stop ();
		isGetPic = false;
		thread = new Thread (Run);
		thread.Start ();
	}


	///*
	private IEnumerator GetPic(){
		//UnityEngine.Debug.Log ("getting picture");
		yield return new WaitForEndOfFrame ();

		int newW = webcamtexture.width % 2 == 0 ? webcamtexture.width : webcamtexture.width - 1;
		int newH = webcamtexture.height % 2 == 0 ? webcamtexture.height : webcamtexture.height - 1;

		Texture2D t2d = new Texture2D(newW, newH);
		t2d.SetPixels (webcamtexture.GetPixels());


		if (!Directory.Exists(filePath))
			Directory.CreateDirectory(filePath);

		using (FileStream fs = new FileStream(filePath + "/" + picName + fileCount + fileExtension, FileMode.Create)){
			BinaryWriter bw = new BinaryWriter(fs);
			bw.Write(t2d.EncodeToPNG());
		}

		fileCount++;
		Destroy(t2d);

		isGetPic = true;
	}

	private void Run(){
		using (Process p = new Process())
		{
			p.StartInfo.FileName = ffmpegPath + "/ffmpeg.exe";
			p.StartInfo.Arguments = "-i " + filePath + "/" + picName + "%d"+ fileExtension + " " +videoOutput + " -vf format=yuv420p";

			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardError = true;
			p.StartInfo.RedirectStandardOutput = true;

			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			p.WaitForExit();
			p.CloseMainWindow();
			p.Close();
		}
	}

	public void changeFileName(string newName){
		videoOutput = newName + ".mp4";
	}	


}
