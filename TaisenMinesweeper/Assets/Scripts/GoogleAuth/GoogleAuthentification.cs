using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class GoogleAuthentification 
{
    static bool _isAuthFinisehd = false;
    static Action<string> _onAuthFinished;

    public GoogleAuthentification(Action<string> onAuthFinished)
    {
        if (!_isAuthFinisehd)
        {
            Initialize_DLL(SetCredentialToText);
            _onAuthFinished = onAuthFinished;
        }
    }

    [DllImport("__Internal")]
    private static extern void Initialize_DLL(Action<string> action);


    [DllImport("__Internal")]
    private static extern void Authorize_DLL();

    public void TryAuthorize()
    {
        Authorize_DLL();
    }

    [MonoPInvokeCallback(typeof(Action<string>))]
    static void SetCredentialToText(string credential)
    {
        _isAuthFinisehd = true;
        _onAuthFinished(credential);
    }

    
}
