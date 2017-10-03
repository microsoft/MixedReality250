// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.using System.Collections;
using HoloToolkit.Unity;
using System.Collections.Generic;
using UnityEngine;

public class SetTutorialText : SingleInstance<SetTutorialText> {

    static string CommonHeaderText = 
        "Welcome to the Mixed Reality Island!";
    static string CommonFooterText =
        "After tapping next, the following UI will allow you to\n" +
        "start a new session or join an existing session.\n";

    static string HoloLensText1 =
        "You have a HoloLens.\n" +
        "You will be shown a bird's eye view of the island.\n" +
        "You can airtap the island to pick it up\n" +
        "look around, and air tap again to place\n" +
        "the island on a surface.  We recommend\n" +
        "a table.  If you have friends with a\n" +
        "HoloLens device then you will see red\n" +
        "clouds over their head in the real world!\n";

    static string HoloLensText2 =
        "If you are playing alone, there isn't\n" +
        "much to do but enjoy the ambient audio.\n" +
        "If you have friends with a Windows Mixed\n"+
        "Reality headset then you have the job of\n"+
        "guiding them through the island to launch\n"+
        "the rocket in the totally safe volcano.\n"+
        "Clues will appear as players on the island\n" +
        "approach obstructions. Give these clues to\n"+
        "your friends on the island.\n";

    static string HoloLensText3 =
        "When all three paths have been completed,\n" +
        "the rocket will launch!\n";


    static string ImmersiveDeviceText1 =
        "You have an immersive mixed reality device.\n" +
        "You will be immersed on an island. Your job\n" +
        "will be to get into the volcano on the island.\n" +
        "You will encounter obstacles along the way.\n" +
        "If you have a friend with a HoloLens, that friend\n" +
        "will get clues about clearing the obstacles.\n" +
        "When all three paths have been completed, the\n" +
        "rocket in the volcano will launch!\n";

    static string ImmersiveDeviceText2 = 
        "Xbox Controller Movement: \n" +
        "     Left Stick Up - Show teleport marker\n" +
        "       If the arrow on the teleport marker is spinning\n" +
        "       when you release Y, you will teleport there.\n" +
        "     A - Select\n" +
        "     X - Toggle Debug Window\n" +
        "     Right Stick Left/Right - Rotate\n";

    static string ImmersiveDeviceText3 =
        "Motion Controller Movement: \n" +
        "     Stick Up - Show teleport marker\n" +
        "       If the arrow on the teleport marker is spinning\n" +
        "       when you release, you will teleport there.\n" +
        "     Trigger - Select\n" +
        "     Stick Left/Right - Rotate\n";

    string[] ImmersiveTutorialScreens = new string[]
    {
        CommonHeaderText,
        ImmersiveDeviceText1,
        ImmersiveDeviceText2,
        ImmersiveDeviceText3,
        CommonFooterText

    };

    string[] HololensTutorialScreens = new string[]
    {
        CommonHeaderText,
        HoloLensText1,
        HoloLensText2,
        HoloLensText3,
        CommonFooterText
    };

    int TutorialIndex = 0;
    string[] TutorialTextScreens;

    TextMesh textMesh;
    // Use this for initialization
    void Start () {
        textMesh = GetComponent<TextMesh>();
        SetTextScreens();
	}
	
    void SetTextScreens()
    {
        
        
        if (UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque)
        {
            TutorialTextScreens = ImmersiveTutorialScreens;
        }
        else
        {
            TutorialTextScreens = HololensTutorialScreens;
        }

        UpdateText();
       
    }

    private void UpdateText()
    {
        if (TutorialIndex < TutorialTextScreens.Length)
        {
            textMesh.text = TutorialTextScreens[TutorialIndex];
        }
        else
            {
            textMesh.text = "A programmer messed up";
        }
    }

    public bool Next()
    {
        TutorialIndex++;
        if (TutorialIndex < TutorialTextScreens.Length)
        {
            UpdateText();
            return true;
        }

        return false;
    }
}
