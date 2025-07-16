using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

public class AsyncCoumpteWrapper : MonoBehaviour
{
    bool init = false;

    public class SharedData
    {
        public volatile bool DataUpdated;
    }

    public class ResultData
    {
        public volatile float[] ProcessedValue;
        public volatile bool IsReady;
    }

    private readonly SharedData _inputData = new SharedData();
    private readonly ResultData _outputData = new ResultData();

    private CancellationTokenSource _cts;
    private bool _isRunning;

    public void StartCalculationTask()
    {
        if (_isRunning) return;

        _isRunning = true;

        RunCalculationLoop().Forget();
    }
    public void PauseCalculationTask() => _isRunning = false;

    void Start()
    {
        _outputData.ProcessedValue = new float[1000000];
        _outputData.IsReady = false;
        _inputData.DataUpdated = true;
        lastTime = Time.realtimeSinceStartup;
        StartCalculationTask();
    }


    void HeavyTask()
    {
        for (int i = 0; i < 1000000; i++)
        {
            // 1. 执行计算逻辑（例如：噪声生成、路径规划）
            _outputData.ProcessedValue[i] = Mathf.Sin(i * 0.1f) * Mathf.Cos(i * 0.05f);
        }
        _outputData.IsReady = true;
    }

    private async UniTaskVoid RunCalculationLoop()
    {
        var token = this.GetCancellationTokenOnDestroy();

        //_cts = new CancellationTokenSource();

        while (!token.IsCancellationRequested)
        {
            if (!_isRunning)
            {
                await UniTask.Yield(PlayerLoopTiming.Update,token); 
                continue;
            }

            await UniTask.Delay(100, DelayType.Realtime, PlayerLoopTiming.Update, token);

            var snapshot = new SharedData
            {
                DataUpdated = _inputData.DataUpdated
            };

            _outputData.IsReady = false;

            await UniTask.RunOnThreadPool(() =>
            {
                HeavyTask();
            });
        }
    }

    // Update is called once per frame
    void Update()
    {

        //HeavyTask();
        if (_outputData.IsReady == true)
        {
            OnResultReady();
            Debug.Log("True update:" + Time.frameCount);
            _outputData.IsReady = false;
        }
        else
        {
            //Debug.Log("Normal update:" + Time.frameCount);
        }
    }

    float lastTime;

    private void OnResultReady()
    {

        Debug.Log($"计算结果就绪 delta time: {Time.realtimeSinceStartup - lastTime}");
        lastTime = Time.realtimeSinceStartup;
        // 此处可触发游戏逻辑（如更新UI、生成特效等）
    }

    void OnDestroy()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
