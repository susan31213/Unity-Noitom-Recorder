using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TagButton : MonoBehaviour {

	NeuronAnimatorInstance noitom;
	PlayList playList;
	int index;

	public bool isRecPanel = false;

	void Start () {
		noitom = GameObject.FindGameObjectWithTag ("Noitom").GetComponent<NeuronAnimatorInstance> ();

		if (!isRecPanel) {
			playList = GameObject.FindGameObjectWithTag ("PlayList").GetComponent<PlayList> ();
		}

		index = System.Convert.ToInt32(GetComponentInChildren<Text> ().text.Substring(0,2));
	}
	
	public void SelectThisTag()
	{
		playList.SelectTag (index);
	}

	public void DeleteTag()
	{
		SelectThisTag ();
		playList.DeleteTag ();
	}

	public void DeleteTag_REC()
	{
		noitom.DeleteTag (index);
	}
}
