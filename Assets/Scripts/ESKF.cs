using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct NativeVector3
{
    public float x, y, z;
    public Vector3 ToUnityVector() => new Vector3(x, y, z);
    public NativeVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct NativeQuaternion
{
    public float qx, qy, qz, qw;
    public Quaternion ToUnityQuaternion() => new Quaternion(qx, qy, qz, qw);
    public NativeQuaternion(float qx, float qy, float qz, float qw)
    {
        this.qx = qx;
        this.qy = qy;
        this.qz = qz;
        this.qw = qw;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct NativeInitData
{
    public float gravity;
    public float initPosX, initPosY, initPosZ;
    public float initVelX, initVelY, initVelZ;
    public float initQuatX, initQuatY, initQuatZ, initQuatW;
    public float initAccBiasX, initAccBiasY, initAccBiasZ;
    public float initGyroBiasX, initGyroBiasY, initGyroBiasZ;
    public float sigma_init_pos;
    public float sigma_init_vel;
    public float sigma_init_dtheta;
    public float sigma_init_accel_bias;
    public float sigma_init_gyro_bias;
    public float sigma_accel;
    public float sigma_gyro;
    public float sigma_accel_drift;
    public float sigma_gyro_drift;

    public NativeInitData(float _gravity, Vector3 initPos, Vector3 initVel, Quaternion initQuat, Vector3 accelBias, Vector3 gyroBias,
        float _sigma_init_pos, float _sigma_init_vel, float _sigma_init_dtheta, float _sigma_init_accel_bias, float _sigma_init_gyro_bias,
        float _sigma_accel, float _sigma_gyro, float _sigma_accel_drift, float _sigma_gyro_drift)
    {
        gravity = _gravity;
        initPosX = initPos.x;
        initPosY = initPos.y;
        initPosZ = initPos.z;
        initVelX = initVel.x;
        initVelY = initVel.y;
        initVelZ = initVel.z;
        initQuatX = initQuat.x;
        initQuatY = initQuat.y;
        initQuatZ = initQuat.z;
        initQuatW = initQuat.w;
        initAccBiasX = accelBias.x;
        initAccBiasY = accelBias.y;
        initAccBiasZ = accelBias.z;
        initGyroBiasX = gyroBias.x;
        initGyroBiasY = gyroBias.y;
        initGyroBiasZ = gyroBias.z;
        sigma_init_pos = _sigma_init_pos;
        sigma_init_vel = _sigma_init_vel;
        sigma_init_dtheta = _sigma_init_dtheta;
        sigma_init_accel_bias = _sigma_init_accel_bias;
        sigma_init_gyro_bias = _sigma_init_gyro_bias;
        sigma_accel = _sigma_accel;
        sigma_gyro = _sigma_gyro;
        sigma_accel_drift = _sigma_accel_drift;
        sigma_gyro_drift = _sigma_gyro_drift;
    }
}
public static class ESKFNativeMethods
{
    const string DLL_NAME = "eskf_android_library";

    [DllImport(DLL_NAME)] public static extern float getTrace();
    [DllImport(DLL_NAME)] public static extern IntPtr CreateESKFClass(ref NativeInitData initData);
    [DllImport(DLL_NAME)] public static extern IntPtr GetPos(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern IntPtr GetQuat(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern IntPtr GetAccelBias(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern IntPtr GetGyroBias(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern void FreeVector(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern void FreeQuaternion(IntPtr obj);
    [DllImport(DLL_NAME)] public static extern NativeVector3 TestDataInOut(NativeVector3 vec);
    [DllImport(DLL_NAME)] public static extern void PredictIMU(IntPtr obj, NativeVector3 a_m, NativeVector3 omega_m, double dt);
    [DllImport(DLL_NAME)] public static extern void MeasurePos(IntPtr obj, NativeVector3 pos_m, float sigma_meas_pos);
    [DllImport(DLL_NAME)] public static extern void MeasureQuat(IntPtr obj, NativeQuaternion quat_m, float sigma_meas_quat);
    
}
public class ESKF : IDisposable
{

    private IntPtr _nativePtr;

    private float sigma_meas_pos = 0.003f;
    private float sigma_meas_quat = 0.03f;


    public ESKF(float gravity, Vector3 initPos, Vector3 initVel, Quaternion initQuat, Vector3 accelBias, Vector3 gyroBias, 
        float sigma_init_pos, float sigma_init_vel, float sigma_init_dtheta, float sigma_init_accel_bias, float sigma_init_gyro_bias,
        float sigma_accel, float sigma_gyro, float sigma_accel_drift, float sigma_gyro_drift)
    {
        NativeInitData initData = new NativeInitData (gravity, initPos, initVel, initQuat,
            accelBias, gyroBias, sigma_init_pos, sigma_init_vel, sigma_init_dtheta,
            sigma_init_accel_bias, sigma_init_gyro_bias, sigma_accel, sigma_gyro, sigma_accel_drift, sigma_gyro_drift);


        _nativePtr = ESKFNativeMethods.CreateESKFClass(ref initData);
    }

    public Vector3 GetPos()
    {
        IntPtr vecPtr = ESKFNativeMethods.GetPos(_nativePtr);
        NativeVector3 nativeVec = Marshal.PtrToStructure<NativeVector3>(vecPtr);
        Vector3 res = nativeVec.ToUnityVector();
        ESKFNativeMethods.FreeVector(vecPtr);
        return res;
    }

    public Quaternion GetQuat()
    {
        IntPtr quatPtr = ESKFNativeMethods.GetQuat(_nativePtr);
        NativeQuaternion nativeQuat = Marshal.PtrToStructure<NativeQuaternion>(quatPtr);
        Quaternion res = nativeQuat.ToUnityQuaternion();
        ESKFNativeMethods.FreeQuaternion(quatPtr);
        return res;
    }

    public Vector3 GetAccelBias()
    {
        IntPtr vecPtr = ESKFNativeMethods.GetAccelBias(_nativePtr);
        NativeVector3 nativeVec = Marshal.PtrToStructure<NativeVector3>(vecPtr);
        Vector3 res = nativeVec.ToUnityVector();
        ESKFNativeMethods.FreeVector(vecPtr);
        return res;
    }

    public Vector3 GetGyroBias()
    {
        IntPtr vecPtr = ESKFNativeMethods.GetGyroBias(_nativePtr);
        NativeVector3 nativeVec = Marshal.PtrToStructure<NativeVector3>(vecPtr);
        Vector3 res = nativeVec.ToUnityVector();
        ESKFNativeMethods.FreeQuaternion(vecPtr);
        return res;
    }

    public void PredictIMU(Vector3 a_m, Vector3 omega_m, double dt)
    {
        NativeVector3 n_a_m = new NativeVector3(a_m.x, a_m.y, a_m.z);
        NativeVector3 n_omega_m = new NativeVector3(omega_m.x, omega_m.y, omega_m.z);
        ESKFNativeMethods.PredictIMU(_nativePtr, n_a_m, n_omega_m, dt);
    }

    public void MeasurePos(Vector3 pos_m)
    {
        NativeVector3 n_pos_m = new NativeVector3(pos_m.x, pos_m.y, pos_m.z);
        ESKFNativeMethods.MeasurePos(_nativePtr, n_pos_m, sigma_meas_pos);
    }

    public void MeasureQuat(Quaternion quat_m)
    {
        NativeQuaternion n_quat_m = new NativeQuaternion(quat_m.x, quat_m.y, quat_m.z, quat_m.w);
        ESKFNativeMethods.MeasureQuat(_nativePtr, n_quat_m, sigma_meas_quat);
    }

    static public Vector3 TestDataInOut(Vector3 inVec)
    {
        NativeVector3 back = ESKFNativeMethods.TestDataInOut(new NativeVector3(inVec.x, inVec.y, inVec.z));
        return back.ToUnityVector();
    }

    public void Dispose()
    {
        if (_nativePtr != IntPtr.Zero)
        {
            _nativePtr = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    ~ESKF() { Dispose(); }
}
