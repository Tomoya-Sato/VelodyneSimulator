using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;
using YamlDotNet;
using YamlDotNet.RepresentationModel;

public static class YamlUtility {
 
    public static string getMessageValue(string key, string filePass) {
        StreamReader inputFile = new StreamReader(filePass, System.Text.Encoding.UTF8);
        YamlStream yaml = new YamlStream();
        yaml.Load(inputFile);
 
        string[] keys = key.Split('.');
 
        int keyCount = keys.Length;
 
        YamlMappingNode mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
        YamlScalarNode node = null;
 
        for (int i = 0; i < keyCount; i++) {
            if (i == keyCount - 1) {
                node = (YamlScalarNode)mapping.Children[new YamlScalarNode(keys[i])];
            } else {
                mapping = (YamlMappingNode)mapping.Children[new YamlScalarNode(keys[i])];
            }
        }
 
        return node.ToString();
    }
}

public class VelodyneScript : MonoBehaviour {

	public int horizontal_split = 1;
	public string LiDAR = "VLP-16.yaml";
	public int frequency = 3;
	
	// input from file
	private float range;
	private float max_vertical_angle;
	private float min_vertical_angle;
	private float vertical_resolution;
	private float horizontal_resolution;
	private float accuracy;
	// private int frequency;

	private int ray_channels;
	private float vertical_fov;
	private float time_span;
	private Vector3 initial_vector;
	private StreamWriter sw;
	private float rotCount;
	private float chunk_angle;

	private int write_flag;

	// Use this for initialization
	void Start () {
		// input parameter from yaml file
		range = float.Parse(YamlUtility.getMessageValue("range", "Assets/Param/" + LiDAR));
		max_vertical_angle = float.Parse(YamlUtility.getMessageValue("max_vertical_angle", "Assets/Param/" + LiDAR));
		min_vertical_angle = float.Parse(YamlUtility.getMessageValue("min_vertical_angle", "Assets/Param/" + LiDAR));
		vertical_resolution = float.Parse(YamlUtility.getMessageValue("vertical_resolution", "Assets/Param/" + LiDAR));
		horizontal_resolution = float.Parse(YamlUtility.getMessageValue("horizontal_resolution", "Assets/Param/" + LiDAR));
		accuracy = float.Parse(YamlUtility.getMessageValue("accuracy", "Assets/Param/" + LiDAR));
		// frequency = float.Parse(YamlUtility.getMessageValue("frequency", "Assets/Param/" + LiDAR));
		
		vertical_fov = max_vertical_angle - min_vertical_angle;
		ray_channels = (int)(vertical_fov / vertical_resolution);
		time_span = (1f / (float)frequency) / horizontal_split;
		Time.fixedDeltaTime = time_span;
		initial_vector = new Vector3(0f, 0f, 1f);
		sw = new StreamWriter(@"saveData.csv",false, Encoding.GetEncoding("UTF-8"));
		rotCount = 0f;
		chunk_angle = 360f / (float)horizontal_split;
		write_flag = (int)(360f / chunk_angle);
	}
	
	void Shoot() {
		RaycastHit Hit;

		Quaternion rot = transform.rotation;
		Matrix4x4 m = Matrix4x4.identity;
		Matrix4x4 n = Matrix4x4.identity;
		Matrix4x4 rotNegater = Matrix4x4.identity;
		m.SetTRS(new Vector3(0f, 0f, 0f), rot, Vector3.one);
		rot = Quaternion.Euler(-rot.x, -rot.y, -rot.z);
		n.SetTRS(new Vector3(0f, 0f, 0f), rot, Vector3.one);
		rot = Quaternion.Euler(0f, rotCount * chunk_angle, 0f);
		rotNegater.SetTRS(new Vector3(0f, 0f, 0f), rot, Vector3.one);
		Vector3 tmp_ray, ray, vec;
		float tmp_cos, tmp_sin, tmp_rnd;
		Vector3 error;

		for (int j = 0; j < chunk_angle / horizontal_resolution; j++) {
			tmp_cos = Mathf.Cos((horizontal_resolution * j) * Mathf.Deg2Rad);
			tmp_sin = Mathf.Sin((horizontal_resolution * j) * Mathf.Deg2Rad);
			tmp_ray.x = initial_vector.x * tmp_cos + initial_vector.z * tmp_sin;
			tmp_ray.z = - initial_vector.x * tmp_sin + initial_vector.z * tmp_cos;
			tmp_ray.y = 0f;
			
			for (int i = 0; i <= ray_channels; i++) {
				tmp_ray.y = Mathf.Tan((max_vertical_angle - vertical_resolution * i) * Mathf.Deg2Rad);
				ray = m.MultiplyPoint3x4(tmp_ray);				
				
				Debug.DrawRay(transform.position, ray.normalized * range, Color.red, time_span);
				if (Physics.Raycast(transform.position, ray, out Hit, range)) {
					tmp_rnd = UnityEngine.Random.Range(-3f, 3f);
					vec = Hit.point - transform.position;
					error = (3f * Mathf.Exp(- tmp_rnd * tmp_rnd / 2)) * vec.normalized;
					vec = n.MultiplyPoint3x4(Hit.point + error);
					vec = rotNegater.MultiplyPoint3x4(vec);
					// TODO output coordinates
					if (write_flag > 0) {
						string text1 = Convert.ToString(vec.x) + "," + Convert.ToString(vec.y) + "," + Convert.ToString(vec.z);
						sw.WriteLine(text1);
					}
				}
			}
		}
		write_flag = write_flag - 1;
	}

	void FixedUpdate () {
		Shoot();
		transform.Rotate(new Vector3(0, chunk_angle, 0));
		rotCount += chunk_angle;
		if (rotCount >= 360f) rotCount -= 360f;
	}

	// IEnumerator FuncCoroutine() {
	// 	while (true) {
	// 		Shoot();
	// 		transform.Rotate(new Vector3(0, rotate_angle_y, 0));
	// 		yield return new WaitForSeconds(time);
	// 	}
	// }
}

