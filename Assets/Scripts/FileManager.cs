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
    public TMPro.TMP_Text FPS;
    public TMPro.TMP_Text IMUPosAndRot;
    public GameObject indicator;
    public GameObject IMUIndicator;
    private float deltaTime;
    private ESKF eskf = null;

    private float oldTime = -1f;
    private void Awake()
    {
        float sigma_accel = 0.00124f; // [m/s^2]  (value derived from Noise Spectral Density in datasheet)
        float sigma_gyro = 0.276f; // [rad/s] (value derived from Noise Spectral Density in datasheet)
        float sigma_accel_drift = 0.001f * sigma_accel; // [m/s^2 sqrt(s)] (Educated guess, real value to be measured)
        float sigma_gyro_drift = 0.001f * sigma_gyro; // [rad/s sqrt(s)] (Educated guess, real value to be measured)

        float sigma_init_pos = 1.0f; // [m]
        float sigma_init_vel = 0.1f; // [m/s]
        float sigma_init_dtheta = 1.0f; // [rad]
        float sigma_init_accel_bias = 10f * sigma_accel_drift; // [m/s^2]
        float sigma_init_gyro_bias = 10f * sigma_gyro_drift; // [rad/s]

        float sigma_mocap_pos = 0.003f; // [m]
        float sigma_mocap_rot = 0.03f; // [rad]

        eskf = new ESKF(9.812f, new Vector3(0, 0, 1), Vector3.zero, Quaternion.identity, new Vector3(-1.26f, -1.09f, -1.977f), new Vector3(0.114f, -0.01f, 0.0f),
            sigma_init_pos, sigma_init_vel, sigma_init_dtheta, sigma_init_accel_bias, sigma_init_gyro_bias, sigma_accel, sigma_gyro, sigma_accel_drift, sigma_gyro_drift
            );
    }
    IEnumerator Start()
    {

        // 关闭VSync（关键步骤）
        QualitySettings.vSyncCount = 0;

        // 设置目标帧率为屏幕最高刷新率
        Application.targetFrameRate = 60;

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

    float timestamp(int sec, int nsec)
    {
        return (float)(sec) + nsec * 1e-9f;
    }
    // Update is called once per frame
    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f; //平滑实时帧率（FPS）显示器
        float fps = 1.0f / deltaTime;
        FPS.text = "FPS: " + Mathf.Ceil(fps).ToString();

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
                textMeshPro.text = lines[currentLine] + "\n" + "ESKF Readback:" + eskf.GetPos().ToString("0.0000,0.0000,0.0000") + "\n" + ESKF.TestDataInOut(pos).ToString("0.0000,0.0000,0.0000");

                //Debug.Log("TestDllDataInOut:" + ESKF.TestDataInOut(pos).ToString());
                //Debug.Log(lines[currentLine]); // 打印当前行
            }

            if (strFields.Length > 0 && strFields[0] == "IMU")
            {
                float accelX = float.Parse(strFields[1]);
                float accelY = float.Parse(strFields[2]);
                float accelZ = float.Parse(strFields[3]);
                float omegaX = float.Parse(strFields[4]);
                float omegaY = float.Parse(strFields[5]);
                float omegaZ = float.Parse(strFields[6]);
                Vector3 accel = new Vector3(accelX, accelY, accelZ);
                Vector3 omega = new Vector3(omegaX, omegaY, omegaZ);

                int sec = int.Parse(strFields[7]);
                int nsec = int.Parse(strFields[8]);
                float dt;
                
                float currentTime = timestamp(sec, nsec);
                dt = currentTime - oldTime;
                if (dt > 1999f) dt = 0.001f;
                if (eskf is null)
                {
                    Debug.LogError("eskf is null");
                    return;
                }
                eskf.PredictIMU(accel, omega, dt);
                IMUPosAndRot.text = lines[currentLine] + "\n" + "ESKF Readback:" + eskf.GetPos().ToString("0.000000,0.000000,0.000000") + "\nrot:" + eskf.GetQuat().ToString() + "\n@time:" + sec + "," + nsec;
                IMUIndicator.transform.position = eskf.GetPos();
                IMUIndicator.transform.rotation = eskf.GetQuat();

                oldTime = currentTime;
                //Debug.Log("TestDllDataInOut:" + ESKF.TestDataInOut(pos).ToString());
                //Debug.Log(lines[currentLine]); // 打印当前行
            }


            currentLine++;
        }
    }
}
