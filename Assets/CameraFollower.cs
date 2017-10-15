using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour {

	public Transform followObj;

	void Update () {
		if (Input.GetKey (KeyCode.LeftAlt)) {

			if (Input.GetKey (KeyCode.LeftArrow))
				transform.Translate (Vector3.left * 0.05f);
			if (Input.GetKey (KeyCode.RightArrow))
				transform.Translate (Vector3.right * 0.05f);
			if (Input.GetKey (KeyCode.UpArrow))
				transform.Translate (Vector3.forward * 0.05f);
			if (Input.GetKey (KeyCode.DownArrow))
				transform.Translate (Vector3.back * 0.05f);
		}

		if (Input.GetKey (KeyCode.LeftShift)) {
			if (Input.GetKeyDown (KeyCode.L))
				transform.position = new Vector3 (followObj.transform.position.x, 0, followObj.transform.position.z);
		}
	}
}
