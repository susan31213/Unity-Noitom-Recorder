using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollRectPos : MonoBehaviour {

	ScrollRect sr;

	// Use this for initialization
	void Start () {
		
		sr = GetComponent<ScrollRect> ();

	}
	
	public void setScrollRectPos0()
	{
		sr.verticalNormalizedPosition = 0;
	}
}
