// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using UnityEngine;

namespace HoloToolkit.Unity.InputModule
{
    // TODO: robertes: comment for HoloToolkit release.
    public class RegisterPointableCanvas :
        MonoBehaviour
    {
        private void Start()
        {
            Canvas canvas = GetComponent<Canvas>();

            if (canvas == null)
            {
                Debug.LogErrorFormat("Object \"{0}\" is missing its canvas component.", name);
            }
            else if (FocusManager.Instance == null)
            {
                Debug.LogError("FocusManager is required.");
            }
            else
            {
                FocusManager.Instance.RegisterPointableCanvas(canvas);
            }
        }
    }
}
