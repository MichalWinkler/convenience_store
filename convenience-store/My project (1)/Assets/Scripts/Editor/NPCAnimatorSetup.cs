using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class NPCAnimatorSetup : Editor
{
    [MenuItem("Tools/Convenience Store/Create NPC Controller")]
    public static void CreateController()
    {
        string path = "Assets/NPC_Basic_Controller.controller";
        
        // 1. Create Controller
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // 2. Add Parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsStanding", AnimatorControllerParameterType.Bool);

        // 3. Find Animations (Kevin Iglesias paths based on search)
        // Adjust these exact names if the internal clip name differs from file name, 
        // but typically standard FBX import names the clip same as file or "mixamo.com".
        // We will try to load the Asset representation.
        
        AnimationClip idleClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Idles/HumanF@Idle01.fbx");
        AnimationClip walkClip = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx");

        if (idleClip == null || walkClip == null)
        {
            Debug.LogError("Could not find animation clips! Please check paths in NPCAnimatorSetup.cs");
            return;
        }

        // 4. Create States
        var rootStateMachine = controller.layers[0].stateMachine;

        // Idle State
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = idleClip;

        // Walk State
        var walkState = rootStateMachine.AddState("Walk");
        walkState.motion = walkClip;

        // 5. Create Transitions
        // Idle -> Walk (Speed > 0.1)
        var toWalk = idleState.AddTransition(walkState);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        toWalk.duration = 0.2f;

        // Walk -> Idle (Speed < 0.1)
        var toIdle = walkState.AddTransition(idleState);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        toIdle.duration = 0.2f;

        Debug.Log($"Successfully created Animator Controller at {path}");
        
        // Select it for the user
        Selection.activeObject = controller;
    }

    private static AnimationClip LoadClip(string path)
    {
        // Try to load the main asset first
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        
        if (clip == null)
        {
            // If it's an FBX, the clip is a sub-asset. We need to load all and find the clip.
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object obj in allAssets)
            {
                if (obj is AnimationClip)
                {
                    return (AnimationClip)obj;
                }
            }
        }
        return clip;
    }
}
