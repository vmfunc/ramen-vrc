using UnityEngine;
using UnityEditor;

namespace PhysSound
{
    [CustomEditor(typeof(PhysSoundObject))]
    [CanEditMultipleObjects]
    public class PhysSoundObjectEditor : Editor
    {
        float dividerHeight = 2;

        SerializedProperty mat, impactAudio, autoCreate, playClipPoint;
        PhysSoundObject obj;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            obj = target as PhysSoundObject;

            mat = serializedObject.FindProperty("SoundMaterial");
            impactAudio = serializedObject.FindProperty("ImpactAudio");
            autoCreate = serializedObject.FindProperty("AutoCreateSources");
            playClipPoint = serializedObject.FindProperty("PlayClipAtPoint");

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("PhysSound Material:", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(mat, true);

            if (obj.SoundMaterial == null)
            {
                EditorGUILayout.HelpBox("No PhysSound Material is assigned!", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            //Update the audio container list with new audio sets
            foreach (PhysSoundAudioSet audSet in obj.SoundMaterial.AudioSets)
            {
                if (!obj.HasAudioContainer(audSet.Key) && audSet.Slide != null)
                {
                    obj.AddAudioContainer(audSet.Key);
                }
            }

            //Remove any audio containers that don't match with the material.
            for (int i = 0; i < obj.AudioContainers.Count; i++)
            {
                PhysSoundAudioContainer audCont = obj.AudioContainers[i];
                
                if (!obj.SoundMaterial.HasAudioSet(audCont.KeyIndex) || obj.SoundMaterial.GetAudioSet(audCont.KeyIndex).Slide == null)
                {
                    obj.RemoveAudioContainer(audCont.KeyIndex);
                    i--;
                    continue;
                }
            }

            //EditorGUILayout.Separator();

            if (obj.SoundMaterial.AudioSets.Count > 0)
            {
                GUILayout.Box("", GUILayout.MaxWidth(Screen.width - 25f), GUILayout.Height(dividerHeight));

                EditorGUILayout.LabelField("Audio Sources:", EditorStyles.boldLabel);

                if (obj.PlayClipAtPoint)
                {
                    GUI.enabled = false;
                    obj.AutoCreateSources = false;
                }

                EditorGUILayout.PropertyField(autoCreate);
                GUI.enabled = true;

                if (obj.AutoCreateSources)
                {
                    GUI.enabled = false;
                    obj.PlayClipAtPoint = false;
                }

                EditorGUILayout.PropertyField(playClipPoint, new GUIContent("Use PlayClipAtPoint"));

                GUI.enabled = true;

                EditorGUILayout.Separator();

                if (obj.AutoCreateSources && !obj.PlayClipAtPoint)
                {
                    EditorGUILayout.PropertyField(impactAudio, new GUIContent("Template Audio"), true);
                }
                else
                {
                    if (!obj.PlayClipAtPoint)
                        EditorGUILayout.PropertyField(impactAudio, true);
                    else
                        EditorGUILayout.PropertyField(impactAudio, new GUIContent("Template Audio"), true);

                    EditorGUILayout.Separator();

                    for (int i = 0; i < obj.AudioContainers.Count; i++)
                    {
                        PhysSoundAudioContainer audCont = obj.AudioContainers[i];
                        audCont.SlideAudio = EditorGUILayout.ObjectField(PhysSoundTypeList.GetKey(audCont.KeyIndex) + " Slide Audio", audCont.SlideAudio, typeof(AudioSource), true) as AudioSource;
                    }
                }
            }

            EditorUtility.SetDirty(obj);
            EditorGUILayout.Separator();
            serializedObject.ApplyModifiedProperties();
        }
    }
}