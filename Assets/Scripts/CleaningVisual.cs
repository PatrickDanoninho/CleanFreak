using UnityEngine;

[System.Serializable]
public class CleaningVisual
{
    public string ModelPath;
    public string ParticlePath;

    [HideInInspector]
    public GameObject ModelInstance;

    [HideInInspector]
    public ParticleSystem ParticleInstance;
}
