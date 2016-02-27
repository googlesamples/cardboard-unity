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

public class StarRendering : MonoBehaviour {

	static StarRendering instance;
	public static StarRendering Instance{
		get{
			return instance;
		}
	}

	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;



	public Color Color{
		set{
			if(value.a == 0){
				meshRenderer.enabled = false;
			}
			else{
				meshRenderer.enabled = true;
			}
			meshRenderer.material.color = value;
		}
	}


	[SerializeField] public Constellation[] constellations;
	[SerializeField] int randomStarCount;
	Star[] randomStars;
 	static float starDistance = 40;
	Mesh mesh;
	Vector3[] vertices;
	Vector2[] uv;
	int[] triangles;

	//[SerializeField] bool 

	//Vector4[] tangents;

	public static readonly Vector3[] quadVertices = new Vector3[4]{new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0)};
	public static readonly int[] quadTriangles = new int[6]{0,2,3,3,1,0};
	public static readonly Vector2[] quadUV = new Vector2[4]{new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1,1)};
	
	public bool rebuildStarsInEditor = false;


	void Awake(){
		instance = this;
	}

	void Update(){
		if(VRInput.Instance != null){
			transform.position = VRInput.Instance.Position;
		}
		#if UNITY_EDITOR
		if(rebuildStarsInEditor){
			UpdateStars();
			meshFilter.mesh = mesh;
		}
		#endif
	}

	static Vector3 GetPosition(float distance, float yaw, float pitch){
		return distance*new Vector3(Mathf.Cos(Mathf.Deg2Rad * pitch) * Mathf.Sin(Mathf.Deg2Rad * yaw), Mathf.Sin(Mathf.Deg2Rad * pitch), Mathf.Cos(Mathf.Deg2Rad * pitch) * Mathf.Cos(Mathf.Deg2Rad * yaw));
	}

	void UpdateStars(){
		int starCount = 0;
		//tangents = new Vector4[starCount * 4];

		for(int c=0; c<constellations.Length; c++){
			Constellation con = constellations[c];
			
			for(int i=0; i<con.stars.Length; i++){

				Star s = con.stars[i];
				int idx = i + starCount;

				float pitch = s.pitch + con.pitch;
				float yaw = s.yaw + con.yaw;
				Vector3 position = GetPosition(starDistance, yaw, pitch);

				vertices[4*idx + 0] = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right) * (s.scale*quadVertices[0]) + position;
				vertices[4*idx + 1] = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right) * (s.scale*quadVertices[1]) + position;
				vertices[4*idx + 2] = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right) * (s.scale*quadVertices[2]) + position;
				vertices[4*idx + 3] = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(-pitch, Vector3.right) * (s.scale*quadVertices[3]) + position;
			}
			starCount += con.stars.Length;
		}
		
		//tangents = new Vector4[starCount * 4];
		for(int i=0; i<randomStars.Length; i++){

			Star s = randomStars[i];

			int idx = i + starCount;
			Vector3 position = GetPosition(starDistance, s.yaw, s.pitch);

			vertices[4*idx + 0] = Quaternion.AngleAxis(s.yaw, Vector3.up) * Quaternion.AngleAxis(-s.pitch, Vector3.right) * (s.scale*quadVertices[0]) + position;
			vertices[4*idx + 1] = Quaternion.AngleAxis(s.yaw, Vector3.up) * Quaternion.AngleAxis(-s.pitch, Vector3.right) * (s.scale*quadVertices[1]) + position;
			vertices[4*idx + 2] = Quaternion.AngleAxis(s.yaw, Vector3.up) * Quaternion.AngleAxis(-s.pitch, Vector3.right) * (s.scale*quadVertices[2]) + position;
			vertices[4*idx + 3] = Quaternion.AngleAxis(s.yaw, Vector3.up) * Quaternion.AngleAxis(-s.pitch, Vector3.right) * (s.scale*quadVertices[3]) + position;
		}

		mesh.vertices = vertices;
	}

	void CreateRandomStars(){
		randomStars = new Star[randomStarCount];
		for(int i=0; i<randomStars.Length; i++){
			randomStars[i] = new Star();

			//radius = Mathf.Cos(Mathf.Deg2Rad * s.pitch)
			float cos = Mathf.Cos(Mathf.PI*(Random.value));
			randomStars[i].pitch =180f- 180f* cos*cos;
			randomStars[i].yaw = Random.value * 360f;
			randomStars[i].scale = 0.3f*Random.value* Random.value + 0.15f;
		}
	}


	void InitializeQuads(){

		int conStars = 0;
		for(int c=0; c<constellations.Length; c++){
			Constellation con = constellations[c];
			conStars += con.stars.Length;
			
		}
		int starCount = randomStars.Length + conStars;
		vertices = new Vector3[starCount * 4];
		triangles = new int[starCount * 6];
		uv = new Vector2[starCount * 4];
		

		for(int i=0; i<starCount; i++){
			vertices[4*i + 0] = quadVertices[0];
			vertices[4*i + 1] = quadVertices[1];
			vertices[4*i + 2] = quadVertices[2];
			vertices[4*i + 3] = quadVertices[3];

			triangles[6*i + 0] = quadTriangles[0] + 4*i;
			triangles[6*i + 1] = quadTriangles[1] + 4*i;
			triangles[6*i + 2] = quadTriangles[2] + 4*i;
			triangles[6*i + 3] = quadTriangles[3] + 4*i;
			triangles[6*i + 4] = quadTriangles[4] + 4*i;
			triangles[6*i + 5] = quadTriangles[5] + 4*i;

			uv[4*i + 0] = quadUV[0];
			uv[4*i + 1] = quadUV[1];
			uv[4*i + 2] = quadUV[2];
			uv[4*i + 3] = quadUV[3];
		}

		mesh = new Mesh();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uv;

		
	}

	void CreateConstellations(){
		for(int i=0; i<constellations.Length; i++){
			constellations[i].CreateConstellations();
		}
	}

	void Start(){
		CreateRandomStars();
		InitializeQuads();
		UpdateStars();
		mesh.RecalculateBounds();
		//mesh.bounds = new Bounds(Vector3.zero, new Vector3 (distance, distance, distance));
		meshFilter.mesh = mesh;

		CreateConstellations();
	}
	[System.Serializable]
	public class Constellation{


		public string name;

		public MeshRenderer meshRenderer;
		public MeshFilter meshFilter;

		Mesh mesh;
		Vector3[] vertices;
		Vector2[] uv;
		int[] triangles;

		public Color Color{
			set{
				if(value.a == 0){
					meshRenderer.enabled = false;
				}
				else{
					meshRenderer.enabled = true;
				}
				meshRenderer.material.color = value;
			}
		}

		public void CreateConstellations(){
			int quadCount = connections.Length/2;

			vertices = new Vector3[quadCount*4];
			triangles = new int[quadCount*6];
			uv = new Vector2[quadCount*4];

			for(int i=0; i<quadCount; i++){
				Star s1 = stars[ connections[2*i +0] ];
				Star s2 = stars[ connections[2*i +1] ];
				Vector3 p1 = GetPosition(starDistance, yaw + s1.yaw, pitch + s1.pitch);
				Vector3 p2 = GetPosition(starDistance, yaw + s2.yaw, pitch + s2.pitch);
				Vector3 line = (p2 - p1).normalized;
				Vector3 dir = (p2 + p1).normalized;
				Vector3 tangent1 = Vector3.Cross(line, dir).normalized;
				Vector3 tangent2 = Vector3.Cross(line, dir).normalized;

				vertices[4*i + 0] = p1 - 0.2f * tangent1;
				vertices[4*i + 1] = p1 + 0.2f * tangent1;
				vertices[4*i + 2] = p2 - 0.2f * tangent2;
				vertices[4*i + 3] = p2 + 0.2f * tangent2;

				triangles[6*i + 0] = quadTriangles[0] + 4*i;
				triangles[6*i + 1] = quadTriangles[1] + 4*i;
				triangles[6*i + 2] = quadTriangles[2] + 4*i;
				triangles[6*i + 3] = quadTriangles[3] + 4*i;
				triangles[6*i + 4] = quadTriangles[4] + 4*i;
				triangles[6*i + 5] = quadTriangles[5] + 4*i;

				uv[4*i + 0] = quadUV[0];
				uv[4*i + 1] = quadUV[1];
				uv[4*i + 2] = quadUV[2];
				uv[4*i + 3] = quadUV[3];
			}

			mesh = new Mesh();
			mesh.vertices = vertices;
			mesh.triangles = triangles;
			mesh.uv = uv;
			mesh.RecalculateBounds();
			meshFilter.mesh = mesh;
		}

		public int[] connections;
		public float yaw;
		public float pitch;
		public Star[] stars;
	}
	[System.Serializable]
	public struct Star{
		public float yaw;
		public float pitch;
		public float scale;
	}

}
