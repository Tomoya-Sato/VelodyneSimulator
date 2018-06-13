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

	public int horizontalSplit = 1;
	public string LiDAR = "VLP-16.yaml";
	public int frequency = 3;
	
	// input from file
	private float range;
	private float maxVerticalAngle;
	private float minVerticalAngle;
	private float verticalResolution;
	private float horizontalResolution;
	private float accuracy;
	// private int frequency;

	private int rayChannels;
	private float verticalFov;
	private float timeSpan;
	private Vector3 initialVector;
	private StreamWriter sw;
	private float rotCount;
	private float chunkAngle;

	private int writeFlag;

	// Use this for initialization
	void Start () {
		// input parameter from yaml file
		range = float.Parse(YamlUtility.getMessageValue("range", "Assets/Param/" + LiDAR));
		maxVerticalAngle = float.Parse(YamlUtility.getMessageValue("maxVerticalAngle", "Assets/Param/" + LiDAR));
		minVerticalAngle = float.Parse(YamlUtility.getMessageValue("minVerticalAngle", "Assets/Param/" + LiDAR));
		verticalResolution = float.Parse(YamlUtility.getMessageValue("verticalResolution", "Assets/Param/" + LiDAR));
		horizontalResolution = float.Parse(YamlUtility.getMessageValue("horizontalResolution", "Assets/Param/" + LiDAR));
		accuracy = float.Parse(YamlUtility.getMessageValue("accuracy", "Assets/Param/" + LiDAR));
		// frequency = float.Parse(YamlUtility.getMessageValue("frequency", "Assets/Param/" + LiDAR));
		
		verticalFov = maxVerticalAngle - minVerticalAngle;
		rayChannels = (int)(verticalFov / verticalResolution);
		timeSpan = (1f / (float)frequency) / horizontalSplit;
		Time.fixedDeltaTime = timeSpan;
		initialVector = new Vector3(0f, 0f, 1f);
		sw = new StreamWriter(@"saveData.csv",false, Encoding.GetEncoding("UTF-8"));
		rotCount = 0f;
		chunkAngle = 360f / (float)horizontalSplit;
		writeFlag = (int)(360f / chunkAngle);
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
		rot = Quaternion.Euler(0f, rotCount * chunkAngle, 0f);
		rotNegater.SetTRS(new Vector3(0f, 0f, 0f), rot, Vector3.one);
		Vector3 tmpRay, ray, vec;
		float tmpCos, tmpSin, tmpRnd;
		Vector3 error;

		for (int j = 0; j < chunkAngle / horizontalResolution; j++) {
			tmpCos = Mathf.Cos((horizontalResolution * j) * Mathf.Deg2Rad);
			tmpSin = Mathf.Sin((horizontalResolution * j) * Mathf.Deg2Rad);
			tmpRay.x = initialVector.x * tmpCos + initialVector.z * tmpSin;
			tmpRay.z = - initialVector.x * tmpSin + initialVector.z * tmpCos;
			tmpRay.y = 0f;
			
			for (int i = 0; i <= rayChannels; i++) {
				tmpRay.y = Mathf.Tan((maxVerticalAngle - verticalResolution * i) * Mathf.Deg2Rad);
				ray = m.MultiplyPoint3x4(tmpRay);				
				
				Debug.DrawRay(transform.position, ray.normalized * range, Color.red, timeSpan);
				if (Physics.Raycast(transform.position, ray, out Hit, range)) {
					tmpRnd = UnityEngine.Random.Range(-3f, 3f);
					vec = Hit.point - transform.position;
					error = (3f * Mathf.Exp(- tmpRnd * tmpRnd / 2)) * vec.normalized;
					vec = n.MultiplyPoint3x4(Hit.point + error);
					vec = rotNegater.MultiplyPoint3x4(vec);

					if (writeFlag > 0) {
						string text1 = Convert.ToString(vec.x) + "," + Convert.ToString(vec.y) + "," + Convert.ToString(vec.z);
						sw.WriteLine(text1);
					}
				}
			}
		}
		writeFlag = writeFlag - 1;
	}

	void FixedUpdate () {
		Shoot();
		transform.Rotate(new Vector3(0, chunkAngle, 0));
		rotCount += chunkAngle;
		if (rotCount >= 360f) rotCount -= 360f;
	}

}

