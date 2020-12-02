using UnityEngine;
using System.Collections.Generic;

namespace PhysSound
{
    [AddComponentMenu("PhysSound/PhysSound Object")]
    public class PhysSoundObject : PhysSoundBase
    {
        public PhysSoundMaterial SoundMaterial;

        public AudioSource ImpactAudio;
        private float baseImpactVol, baseImpactPitch;

        public bool AutoCreateSources;
        public bool PlayClipAtPoint;

        public List<PhysSoundAudioContainer> AudioContainers = new List<PhysSoundAudioContainer>();
        private Dictionary<int, PhysSoundAudioContainer> audioContainersDic;

        void Start()
        {
            if (SoundMaterial == null)
                return;

            if (AutoCreateSources)
            {
                if (!ImpactAudio.isActiveAndEnabled)
                {
                    ImpactAudio = getAudioCopy(ImpactAudio, gameObject);
                }

                baseImpactVol = ImpactAudio.volume;
                baseImpactPitch = ImpactAudio.pitch;

                audioContainersDic = new Dictionary<int, PhysSoundAudioContainer>();
                AudioContainers = new List<PhysSoundAudioContainer>();

                foreach (PhysSoundAudioSet audSet in SoundMaterial.AudioSets)
                {
                    if (audSet.Slide == null)
                        continue;

                    PhysSoundAudioContainer audCont = new PhysSoundAudioContainer(audSet.Key);
                    audCont.SlideAudio = getAudioCopy(ImpactAudio, this.gameObject);

                    audCont.Initialize(SoundMaterial.GetAudioSet(audCont.KeyIndex).Slide);
                    audioContainersDic.Add(audCont.KeyIndex, audCont);
                    AudioContainers.Add(audCont);
                }

                ImpactAudio.loop = false;
            }
            else
            {
                if (ImpactAudio)
                {
                    ImpactAudio.loop = false;
                    baseImpactVol = ImpactAudio.volume;
                    baseImpactPitch = ImpactAudio.pitch;
                }

                if (AudioContainers.Count > 0)
                {
                    audioContainersDic = new Dictionary<int, PhysSoundAudioContainer>();

                    foreach (PhysSoundAudioContainer audCont in AudioContainers)
                    {
                        if (!SoundMaterial.HasAudioSet(audCont.KeyIndex))
                        {
                            Debug.LogError("PhysSound Object " + gameObject.name + " has an audio container for an invalid Material Type! Select this object in the hierarchy to update its audio container list.");
                            continue;
                        }

                        audCont.Initialize(SoundMaterial.GetAudioSet(audCont.KeyIndex).Slide);
                        audioContainersDic.Add(audCont.KeyIndex, audCont);
                    }
                }
            }
        }

        void Update()
        {
            if (SoundMaterial == null)
                return;

            for (int i = 0; i < AudioContainers.Count; i++)
                AudioContainers[i].UpdateVolume();

            if (ImpactAudio && !ImpactAudio.isPlaying)
                ImpactAudio.Stop();
        }

        /// <summary>
        /// Enables or Disables this script along with its associated AudioSources.
        /// </summary>
        public void SetEnabled(bool enable)
        {
            if (enable && this.enabled == false)
            {
                for (int i = 0; i < AudioContainers.Count; i++)
                {
                    AudioContainers[i].Enable();
                }

                ImpactAudio.enabled = true;
                this.enabled = true;
            }
            else if(!enable && this.enabled == true)
            {
                if (ImpactAudio)
                {
                    ImpactAudio.Stop();
                    ImpactAudio.enabled = false;
                }

                for (int i = 0; i < AudioContainers.Count; i++)
                {
                    AudioContainers[i].Disable();
                }

                this.enabled = false;
            }
        }

        public override PhysSoundMaterial GetPhysSoundMaterial(Vector3 contactPoint)
        {
            return SoundMaterial;
        }

        #region Main Functions

        private void playImpactSound(PhysSoundBase otherObject, Vector3 relativeVelocity, Vector3 normal, Vector3 contactPoint)
        {
            if (ImpactAudio)
            {
                AudioClip a = SoundMaterial.GetImpactAudio(otherObject, relativeVelocity, normal, contactPoint);

                if (a)
                {
                    float pitch = baseImpactPitch + Random.Range(-SoundMaterial.PitchRandomness, SoundMaterial.PitchRandomness);
                    float vol = baseImpactVol * SoundMaterial.GetImpactVolume(relativeVelocity, normal);

                    if (PlayClipAtPoint)
                    {
                        playClipAtPoint(a, transform.position, ImpactAudio, SoundMaterial.ScaleImpactVolume ? vol : ImpactAudio.volume, pitch);
                    }
                    else
                    {
                        ImpactAudio.pitch = pitch;
                        if (SoundMaterial.ScaleImpactVolume)
                            ImpactAudio.volume = vol;

                        ImpactAudio.clip = a;
                        ImpactAudio.Play();
                    }                 
                }
            }
        }

        private void setSlideTargetVolumes(PhysSoundBase otherObject, Vector3 relativeVelocity, Vector3 normal, Vector3 contactPoint, bool exit)
        {
            if (audioContainersDic == null)
                return;

            PhysSoundMaterial m = null;

            if (otherObject)
            {
                //Special case for sliding against a terrain
                if (otherObject is PhysSoundTerrain)
                {
                    PhysSoundTerrain terr = otherObject as PhysSoundTerrain;
                    Dictionary<int, PhysSoundComposition> compDic = terr.GetComposition(contactPoint);

                    foreach (PhysSoundAudioContainer c in audioContainersDic.Values)
                    {
                        PhysSoundComposition comp;
                        float mod = 0;

                        if (compDic.TryGetValue(c.KeyIndex, out comp))
                            mod = comp.GetAverage();

                        c.SetTargetVolumeAndPitch(SoundMaterial, relativeVelocity, normal, exit, mod);
                    }

                    return;
                }
                else
                    m = otherObject.GetPhysSoundMaterial(contactPoint);
            }

            //General cases
            //If the other object has a PhysSound material
            if (m)
            {
                PhysSoundAudioContainer aud;

                if (audioContainersDic.TryGetValue(m.MaterialTypeKey, out aud))
                    aud.SetTargetVolumeAndPitch(SoundMaterial, relativeVelocity, normal, exit);
                else if (SoundMaterial.FallbackTypeKey != -1 && audioContainersDic.TryGetValue(SoundMaterial.FallbackTypeKey, out aud))
                    aud.SetTargetVolumeAndPitch(SoundMaterial, relativeVelocity, normal, exit);
            }
            //If it doesnt we set vols based on the fallback setting of our material
            else
            {
                PhysSoundAudioContainer aud;

                if (SoundMaterial.FallbackTypeKey != -1 && audioContainersDic.TryGetValue(SoundMaterial.FallbackTypeKey, out aud))
                    aud.SetTargetVolumeAndPitch(SoundMaterial, relativeVelocity, normal, exit);
            }
        }

        #endregion

        #region 3D Collision Messages

        void OnCollisionEnter(Collision c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            playImpactSound(c.collider.GetComponent<PhysSoundBase>(), c.relativeVelocity, c.contacts[0].normal, c.contacts[0].point);
        }

        void OnCollisionStay(Collision c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            setSlideTargetVolumes(c.collider.GetComponent<PhysSoundBase>(), c.relativeVelocity, c.contacts[0].normal, c.contacts[0].point, false);
        }

        void OnCollisionExit(Collision c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            setSlideTargetVolumes(c.collider.GetComponent<PhysSoundBase>(), Vector3.zero, Vector3.zero, Vector3.zero, true);
        }

        #endregion

        #region 2D Collision Messages

        void OnCollisionEnter2D(Collision2D c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            playImpactSound(c.collider.GetComponent<PhysSoundBase>(), c.relativeVelocity, c.contacts[0].normal, c.contacts[0].point);
        }

        void OnCollisionStay2D(Collision2D c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            setSlideTargetVolumes(c.collider.GetComponent<PhysSoundBase>(), c.relativeVelocity, c.contacts[0].normal, c.contacts[0].point, false);
        }

        void OnCollisionExit2D(Collision2D c)
        {
            if (SoundMaterial == null || !this.enabled)
                return;

            setSlideTargetVolumes(c.collider.GetComponent<PhysSoundBase>(), Vector3.zero, Vector3.zero, Vector3.zero, true);
        }

        #endregion

        #region Editor

        public bool HasAudioContainer(int keyIndex)
        {
            foreach (PhysSoundAudioContainer aud in AudioContainers)
            {
                if (aud.CompareKeyIndex(keyIndex))
                    return true;
            }

            return false;
        }

        public void AddAudioContainer(int keyIndex)
        {
            AudioContainers.Add(new PhysSoundAudioContainer(keyIndex));
        }

        public void RemoveAudioContainer(int keyIndex)
        {
            for (int i = 0; i < AudioContainers.Count; i++)
            {
                if (AudioContainers[i].KeyIndex == keyIndex)
                {
                    AudioContainers.RemoveAt(i);
                    return;
                }
            }
        }

        #endregion

        #region Utility Methods

        private AudioSource getAudioCopy(AudioSource t, GameObject g)
        {
            AudioSource a = g.AddComponent<AudioSource>();

            if (!t)
                return a;

            a.bypassEffects = t.bypassEffects;
            a.bypassListenerEffects = t.bypassListenerEffects;
            a.bypassReverbZones = t.bypassReverbZones;
            a.dopplerLevel = t.dopplerLevel;
            a.ignoreListenerPause = t.ignoreListenerPause;
            a.ignoreListenerVolume = t.ignoreListenerVolume;
            a.loop = t.loop;
            a.maxDistance = t.maxDistance;
            a.minDistance = t.minDistance;
            a.mute = t.mute;
            a.outputAudioMixerGroup = t.outputAudioMixerGroup;
            a.panStereo = t.panStereo;
            a.pitch = t.pitch;
            a.playOnAwake = t.playOnAwake;
            a.priority = t.priority;
            a.reverbZoneMix = t.reverbZoneMix;
            a.rolloffMode = t.rolloffMode;
            a.spatialBlend = t.spatialBlend;
            a.spread = t.spread;
            a.time = t.time;
            a.timeSamples = t.timeSamples;
            a.velocityUpdateMode = t.velocityUpdateMode;
            a.volume = t.volume;
            return a;
        }

        private void playClipAtPoint(AudioClip clip, Vector3 point, AudioSource template, float volume, float pitch)
        {
            GameObject obj = new GameObject("PointAudio");
            obj.transform.position = point;
            AudioSource aSource = getAudioCopy(template, obj);
            aSource.clip = clip;
            aSource.pitch = pitch;
            aSource.volume = volume;

            aSource.Play();
            Destroy(obj, clip.length);
        }

        #endregion
    }

    [System.Serializable]
    public class PhysSoundAudioContainer
    {
        public int KeyIndex;
        public AudioSource SlideAudio;

        private float targetVolume;
        private float baseVol, basePitch, basePitchRand;

        public PhysSoundAudioContainer(int k)
        {
            KeyIndex = k;
        }

        /// <summary>
        /// Initializes this Audio Container with the given AudioClip. Will do nothing if SlideAudio is not assigned.
        /// </summary>
        /// <param name="clip"></param>
        public void Initialize(AudioClip clip)
        {
            if (SlideAudio == null)
                return;

            SlideAudio.clip = clip;
            baseVol = SlideAudio.volume;
            basePitch = SlideAudio.pitch;
            basePitchRand = basePitch;
            SlideAudio.loop = true;
            SlideAudio.volume = 0;
        }

        /// <summary>
        /// Sets the target volume and pitch of the sliding sound effect based on the given object that was hit, velocity, and normal.
        /// </summary>
        public void SetTargetVolumeAndPitch(PhysSoundMaterial mat, Vector3 relativeVelocity, Vector3 normal, bool exit, float mod = 1)
        {
            if (SlideAudio == null)
                return;

            if (!SlideAudio.isPlaying)
            {
                basePitchRand = basePitch + Random.Range(-mat.PitchRandomness, mat.PitchRandomness);
                SlideAudio.Play();
            }

            SlideAudio.pitch = basePitchRand + relativeVelocity.magnitude * mat.SlidePitchMod;
            targetVolume = exit ? 0 : mat.GetSlideVolume(relativeVelocity, normal) * baseVol * mod;
        }

        /// <summary>
        /// Updates the associated AudioSource to match the target volume and pitch.
        /// </summary>
        public void UpdateVolume()
        {
            if (SlideAudio == null)
                return;

            SlideAudio.volume = Mathf.MoveTowards(SlideAudio.volume, targetVolume, 0.06f);

            if (SlideAudio.volume < 0.01f)
                SlideAudio.Stop();
        }

        /// <summary>
        /// Returns true if this Audio Container's key index is the same as the given key index.
        /// </summary>
        public bool CompareKeyIndex(int k)
        {
            return k == KeyIndex;
        }

        /// <summary>
        /// Disables the associated AudioSource.
        /// </summary>
        public void Disable()
        {
            if (SlideAudio)
            {
                SlideAudio.Stop();
                SlideAudio.enabled = false;
            }
        }

        /// <summary>
        /// Enables the associated AudioSource.
        /// </summary>
        public void Enable()
        {
            if (SlideAudio)
            {
                SlideAudio.enabled = true;
            }
        }
    }
}