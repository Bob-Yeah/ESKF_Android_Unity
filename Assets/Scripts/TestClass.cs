using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class TestNativeMethods
{
    const string DLL_NAME = "eskf_android_library";

    [DllImport(DLL_NAME)] public static extern IntPtr CreateTestClass(NativeVector3 initData);
    [DllImport(DLL_NAME)] public static extern NativeVector3 GetTestClassValue(IntPtr obj);
}

public class TestClass : IDisposable
{
    private IntPtr _nativePtr;
    public TestClass(NativeVector3 initData)
    {
        _nativePtr = TestNativeMethods.CreateTestClass(initData);
        if (_nativePtr == IntPtr.Zero)
        {
            throw new Exception("Failed to create TestClass instance.");
        }
    }

    public Vector3 GetValue()
    {
        NativeVector3 vecPtr = TestNativeMethods.GetTestClassValue(_nativePtr);

        return vecPtr.ToUnityVector();
    }

    public void Dispose()
    {
        if (_nativePtr != IntPtr.Zero)
        {
            _nativePtr = IntPtr.Zero;
        }
        GC.SuppressFinalize(this);
    }

    ~TestClass() { Dispose(); }
}
