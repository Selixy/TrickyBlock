using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Buttons))]
[CanEditMultipleObjects]
public class ButtonsEditor : Editor
{
    private SerializedProperty idleAmplitude;
    private SerializedProperty idleDuration;
    private SerializedProperty idleEase;

    private SerializedProperty animationTarget;
    private SerializedProperty autoUseFirstChildInLayout;

    private SerializedProperty normalScale;
    private SerializedProperty hoverScale;
    private SerializedProperty selectedScale;
    private SerializedProperty stateDuration;

    private SerializedProperty clickPunchScale;
    private SerializedProperty clickDuration;
    private SerializedProperty clickRotationPunch;

    private SerializedProperty actionType;
    private SerializedProperty sceneToLoad;

    private SerializedProperty optionsPanel;
    private SerializedProperty optionsSlideLeftAmount;
    private SerializedProperty optionsSlideDuration;
    private SerializedProperty optionsSlideEase;

    private void OnEnable()
    {
        idleAmplitude = serializedObject.FindProperty("idleAmplitude");
        idleDuration = serializedObject.FindProperty("idleDuration");
        idleEase = serializedObject.FindProperty("idleEase");

        animationTarget = serializedObject.FindProperty("animationTarget");
        autoUseFirstChildInLayout = serializedObject.FindProperty("autoUseFirstChildInLayout");

        normalScale = serializedObject.FindProperty("normalScale");
        hoverScale = serializedObject.FindProperty("hoverScale");
        selectedScale = serializedObject.FindProperty("selectedScale");
        stateDuration = serializedObject.FindProperty("stateDuration");

        clickPunchScale = serializedObject.FindProperty("clickPunchScale");
        clickDuration = serializedObject.FindProperty("clickDuration");
        clickRotationPunch = serializedObject.FindProperty("clickRotationPunch");

        actionType = serializedObject.FindProperty("actionType");
        sceneToLoad = serializedObject.FindProperty("sceneToLoad");

        optionsPanel = serializedObject.FindProperty("optionsPanel");
        optionsSlideLeftAmount = serializedObject.FindProperty("optionsSlideLeftAmount");
        optionsSlideDuration = serializedObject.FindProperty("optionsSlideDuration");
        optionsSlideEase = serializedObject.FindProperty("optionsSlideEase");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Idle", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(idleAmplitude);
        EditorGUILayout.PropertyField(idleDuration);
        EditorGUILayout.PropertyField(idleEase);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Target", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(animationTarget);
        EditorGUILayout.PropertyField(autoUseFirstChildInLayout);
        EditorGUILayout.HelpBox("For buttons inside a LayoutGroup, animate a child visual instead of the root to avoid layout conflicts.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State Scale", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(normalScale);
        EditorGUILayout.PropertyField(hoverScale);
        EditorGUILayout.PropertyField(selectedScale);
        EditorGUILayout.PropertyField(stateDuration);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Click", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(clickPunchScale);
        EditorGUILayout.PropertyField(clickDuration);
        EditorGUILayout.PropertyField(clickRotationPunch);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(actionType);

        switch (actionType.enumValueIndex)
        {
            case 0: // None
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No action will be executed on click. Click animation remains active.", MessageType.Info);
                break;

            case 1: // Play
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Play Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(sceneToLoad);
                break;

            case 2: // Options
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Options Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(optionsPanel);
                EditorGUILayout.PropertyField(optionsSlideLeftAmount);
                EditorGUILayout.PropertyField(optionsSlideDuration);
                EditorGUILayout.PropertyField(optionsSlideEase);
                break;

            case 3: // Quit
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("No additional settings are required for Quit.", MessageType.Info);
                break;

            case 4: // LoadGame
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Loads the game scene using SceneLoader.", MessageType.Info);
                break;

            case 5: // LoadLobby
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Loads the lobby scene using SceneLoader.", MessageType.Info);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
