﻿using UnityEngine;
using System.Collections.Generic;

namespace PhysSound
{
    public class PhysSoundMaterial : ScriptableObject
    {
        public int MaterialTypeKey;
        public int FallbackTypeIndex;
        public int FallbackTypeKey;

        public float PitchRandomness = 0.1f;
        public float SlidePitchMod = 0.05f;
        public Range RelativeVelocityThreshold;
        public float ImpactNormalBias = 1;

        public bool UseCollisionVelocity = true;
        public bool ScaleImpactVolume = true;

        public List<PhysSoundAudioSet> AudioSets = new List<PhysSoundAudioSet>();
        private Dictionary<int, PhysSoundAudioSet> audioSetDic;

        void OnEnable()
        {
            if (AudioSets.Count <= 0)
                return;

            audioSetDic = new Dictionary<int, PhysSoundAudioSet>();

            foreach (PhysSoundAudioSet audSet in AudioSets)
            {
                if (audioSetDic.ContainsKey(audSet.Key))
                {
                    Debug.LogError("PhysSound Material " + name + " has duplicate audio set for Material Type \"" + PhysSoundTypeList.GetKey(audSet.Key) + "\". It will not be used during runtime.");
                    continue;
                }

                audioSetDic.Add(audSet.Key, audSet);
            }

            if (FallbackTypeIndex == 0)
                FallbackTypeKey = -1;
            else
                FallbackTypeKey = AudioSets[FallbackTypeIndex - 1].Key;
        }

        /// <summary>
        /// Gets the impact audio clip based on the given object that was hit, the velocity of the collision, the normal, and the contact point.
        /// </summary>
        public AudioClip GetImpactAudio(PhysSoundBase otherObject, Vector3 relativeVel, Vector3 norm, Vector3 contact)
        {
            if (audioSetDic == null)
                return null;

            PhysSoundMaterial m = null;

            if (otherObject)
                m = otherObject.GetPhysSoundMaterial(contact);

            //Get sounds using collision velocity
            if (UseCollisionVelocity)
            {
                float velNorm = GetImpactVolume(relativeVel, norm);

                if (velNorm < 0)
                    return null;

                if (m)
                {
                    PhysSoundAudioSet audSet;

                    if (audioSetDic.TryGetValue(m.MaterialTypeKey, out audSet))
                        return audSet.GetImpact(velNorm, false);
                    else if (FallbackTypeKey != -1)
                        return audioSetDic[FallbackTypeKey].GetImpact(velNorm, false);
                }
                else if (FallbackTypeKey != -1)
                    return audioSetDic[FallbackTypeKey].GetImpact(velNorm, false);
            }
            //Get sound randomly
            else
            {
                if (m)
                {
                    PhysSoundAudioSet audSet;

                    if (audioSetDic.TryGetValue(m.MaterialTypeKey, out audSet))
                        return audSet.GetImpact(0, true);
                    else if (FallbackTypeKey != -1)
                        return audioSetDic[FallbackTypeKey].GetImpact(0, true);
                }
                else if (FallbackTypeKey != -1)
                    return audioSetDic[FallbackTypeKey].GetImpact(0, true);
            }

            return null;
        }

        /// <summary>
        /// Gets the volume of the slide audio based on the velocity and normal of the collision.
        /// </summary>
        public float GetSlideVolume(Vector3 relativeVel, Vector3 norm)
        {
            float slideAmt = 1 - Mathf.Abs(Vector3.Dot(norm, relativeVel));
            float slideVel = (slideAmt) * relativeVel.magnitude;

            return RelativeVelocityThreshold.Normalize(slideVel);
        }

        /// <summary>
        /// Gets the volume of the impact audio based on the velocity and normal of the collision.
        /// </summary>
        /// <param name="relativeVel"></param>
        /// <param name="norm"></param>
        /// <returns></returns>
        public float GetImpactVolume(Vector3 relativeVel, Vector3 norm)
        {
            float impactAmt = Mathf.Abs(Vector3.Dot(norm.normalized, relativeVel.normalized));
            float impactVel = (impactAmt + (1 - impactAmt) * (1 - ImpactNormalBias)) * relativeVel.magnitude;

            if (impactVel < RelativeVelocityThreshold.Min)
                return -1;

            return RelativeVelocityThreshold.Normalize(impactVel);
        }

        /// <summary>
        /// Checks if this material has an audio set corresponding to the given key index.
        /// </summary>
        public bool HasAudioSet(int keyIndex)
        {
            foreach (PhysSoundAudioSet aud in AudioSets)
            {
                if (aud.CompareKeyIndex(keyIndex))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the audio set corresponding to the given key index, if it exists.
        /// </summary>
        /// <param name="keyIndex"></param>
        /// <returns></returns>
        public PhysSoundAudioSet GetAudioSet(int keyIndex)
        {
            foreach (PhysSoundAudioSet aud in AudioSets)
            {
                if (aud.CompareKeyIndex(keyIndex))
                    return aud;
            }

            return null;
        }

        /// <summary>
        /// Gets the list of audio set names. (Used by the editor to display the list of potential fallback audio sets).
        /// </summary>
        /// <returns></returns>
        public string[] GetFallbackAudioSets()
        {
            string[] names = new string[AudioSets.Count + 1];
            names[0] = "None";

            for (int i = 0; i < AudioSets.Count; i++)
            {
                names[i + 1] = PhysSoundTypeList.GetKey(AudioSets[i].Key);
            }

            return names;
        }
    }

    [System.Serializable]
    public class PhysSoundAudioSet
    {
        public int Key;
        public List<AudioClip> Impacts = new List<AudioClip>();
        public AudioClip Slide;

        /// <summary>
        /// Gets the appropriate audio clip. Either based on the given velocity or picked at random.
        /// </summary>
        public AudioClip GetImpact(float vel, bool random)
        {
            if (Impacts.Count == 0)
                return null;

            if (random)
            {
                return Impacts[Random.Range(0, Impacts.Count)];
            }
            else
            {
                int i = (int)(vel * (Impacts.Count - 1));
                return Impacts[i];
            }
        }

        /// <summary>
        /// Returns true if this Audio Set's key index is the same as the given key index.
        /// </summary>
        public bool CompareKeyIndex(int k)
        {
            return Key == k;
        }
    }
}