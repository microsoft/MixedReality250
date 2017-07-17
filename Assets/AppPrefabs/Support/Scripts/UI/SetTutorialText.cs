// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetTutorialText : MonoBehaviour {

    string CommonHeaderText = "Welcome to the Mixed Reality Island!";
    string CommonFooterText =
        "\nTap any of this text to continue\nAfter dismissing this screen, the next UI will allow you to\n" +
        "start a new session or join an existing session\n";

    string HoloLensText =
        "You have a HoloLens.\n" +
        "You will be shown a bird's eye view of the island.\n" +
        "You can airtap the island to pick it up\n" +
        "look around, and air tap again to place\n" +
        "the island on a surface.  We recommend\n" +
        "a table.\n" +
        "If you are playing alone, we're afraid there isn't\n" +
        "much to do but enjoy the ambient audio.\n" +
        "If you have friends with a Windows Mixed Reality headset\n" +
        "then you have the job of guiding them through the island\n" +
        "to launch the rocket in the totally safe volcano on the island.\n" +
        "Clues will appear as players on the island approach obstructions.\n" +
        "Give these clues to your friends on the island.\n" +
        "When all three paths have been completed, the rocket will launch!\n" +
        "If you have friends with a HoloLens device then you will see red\n" +
        "clouds over their head in the real world!\n";


    string ImmersiveDeviceText =
        "You have an immersive mixed reality device\n" +
        "You will be immersed on the island. Your job\n" +
        "will be to get into the volcano on the island\n" +
        "you will encounter obstacles along the way\n" +
        "if you have a friend with a HoloLens, that friend\n" +
        "will get a clue about clearing the obstacle.\n" +
        "When all three paths have been completed, the\n" +
        "rocket in the volcano will launch!\n\n" +
        "Movement: \n" +
        "     Hold Y - Show teleport marker\n" +
        "       If the arrow on the teleport marker is spinning\n" +
        "       when you release Y, you will teleport there.\n" +
        "     A - Select\n" +
        "     X - Toggle Debug Window\n" +
        "     Left/Right bumpers - Rotate\n";




    // Use this for initialization
    void Start () {
        SetText();
	}
	
    void SetText()
    {
        TextMesh textMesh = GetComponent<TextMesh>();
        string subtext = "";

        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
        {
            subtext = ImmersiveDeviceText;
        }
        else
        {
            subtext = HoloLensText;
        }

        textMesh.text = string.Format("{0}\n{1}\n{2}", CommonHeaderText, subtext, CommonFooterText);
        DestroyImmediate(GetComponent<BoxCollider>());
        gameObject.AddComponent<BoxCollider>();
    }
}
