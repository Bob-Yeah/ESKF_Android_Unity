using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    private List<string> lines = new List<string>();
    private int currentLine = 0;
    public TMPro.TMP_Text textMeshPro;
    public GameObject indicator;
    public GameObject IMUIndicator;

    IEnumerator Start()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "mixedTimeSeries.txt");

        // Android需用UnityWebRequest异步加载[2,5](@ref)
        UnityWebRequest www = UnityWebRequest.Get(filePath);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("加载失败: " + www.error);
        }
        else
        {
            string text = www.downloadHandler.text;
            lines.AddRange(text.Split('\n')); // 按行分割
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (currentLine < lines.Count)
        {
            string currentStr = lines[currentLine];
            string[] strFields = currentStr.Split(',');
            if (strFields.Length > 0 && strFields[0] == "Mocap") {

                float posX = float.Parse(strFields[1]);
                float posY = float.Parse(strFields[2]);
                float posZ = float.Parse(strFields[3]);
                float qX = float.Parse(strFields[4]);
                float qY = float.Parse(strFields[5]);
                float qZ = float.Parse(strFields[6]);
                float qW = float.Parse(strFields[7]);
                Vector3 pos = new Vector3(posX, posY, posZ);
                Quaternion rot = new Quaternion(qX, qY, qZ, qW);
                indicator.transform.position = pos;
                indicator.transform.rotation = rot;
                textMeshPro.text = lines[currentLine];
                Debug.Log(lines[currentLine]); // 打印当前行
            }
            
            currentLine++;
        }
    }
}
