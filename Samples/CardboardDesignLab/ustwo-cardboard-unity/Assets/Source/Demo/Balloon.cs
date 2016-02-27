// Copyright 2014 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the balloon material settings, randomized positioning, 'floating' effect, and enabling / disabling of mesh renderers and colliders 
/// </summary>
public class Balloon : MonoBehaviour {

	public AudioSource audioSource;
	public MeshRenderer meshRenderer;
	public new Collider collider;

	bool popping = false;
	float popTime = 0;

	public GameObject stringBalloon;

	Vector3 initialPosition;
	Vector3 originalPosition;


	float startup = 0;

	void Awake(){
		originalPosition = transform.localPosition;
		startup = Random.value;
	}

	/// <summary>
	/// Trigger the balloon pop effect
	/// </summary>
	public void Pop(){
		popping = true;
		popTime = Time.time;
		stringBalloon.SetActive(false);
		audioSource.pitch = 0.9f + 0.2f*Random.value;
		audioSource.Play();
	}

	/// <summary>
	/// Regenerate the balloon
	/// </summary>
	public void Restore(bool restorePosition = true){
		popping = false;
		meshRenderer.material.SetFloat("_Explosion", 0);
		gameObject.SetActive(true);

		if(restorePosition){
			initialPosition = originalPosition;
		}
		else{
			initialPosition = transform.localPosition;
		}

		transform.localPosition = initialPosition + (1+startup)*new Vector3(0.05f*Mathf.Sin(0.837f*Time.time + startup),0.1f*Mathf.Sin(Time.time + startup),0.023f*Mathf.Sin(0.776f*Time.time + startup));

		collider.enabled = true;
		stringBalloon.SetActive(true);
	}

	/// <summary>
	/// Depricated, set a texture to the diffuse channel - materials no longer use this 
	/// </summary>
	public void SetTexture(Texture2D t){
		meshRenderer.material.SetTexture("_Diffuse", t);
	}
	/// <summary>
	/// Set the balloon color
	/// </summary>
	public void SetColor(Color c){
		meshRenderer.material.color = c;
	}
	void Update(){

		transform.localPosition = initialPosition + (1+startup)*new Vector3(0.05f*Mathf.Sin(0.837f*Time.time + startup),0.1f*Mathf.Sin(Time.time + startup),0.023f*Mathf.Sin(0.776f*Time.time + startup));
		if(popping){
			float time = 0.5f*(Time.time - popTime);
			float expTime = Smoothing.ExponentialEaseOut(time);
			meshRenderer.material.SetFloat("_Explosion", expTime);
			collider.enabled = false;
			if(time > 1){
				gameObject.SetActive(false);
			}
		}
	}

}
