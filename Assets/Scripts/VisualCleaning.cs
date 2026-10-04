using System.Collections.Generic;
using UnityEngine;
using static CleaningManager;

public class VisualCleaning
{
    private Dictionary<
        CleaningManager.CleaningSupplyType,
        CleaningVisual
    > cleaningVisuals;

    private CleaningVisual dusterVisual;
    private CleaningVisual soapVisual;
    private CleaningVisual brushVisual;
    private CleaningVisual clothVisual;

    public VisualCleaning(
        CleaningVisual duster,
        CleaningVisual soap,
        CleaningVisual brush,
        CleaningVisual cloth)
    {
        cleaningVisuals =
            new Dictionary<
                CleaningManager.CleaningSupplyType,
                CleaningVisual
            >
            {
                {
                    CleaningManager.CleaningSupplyType.Duster,
                    duster
                },
                {
                    CleaningManager.CleaningSupplyType.Soap,
                    soap
                },
                {
                    CleaningManager.CleaningSupplyType.Brush,
                    brush
                },
                {
                    CleaningManager.CleaningSupplyType.Cloth,
                    cloth
                }
            };
    }

    public void StartVisual(CleaningManager.CleaningSupplyType supply)
    {
        CleaningVisual visual = GetVisual(supply);

        if (visual == null)
            return;

        if (visual.ModelInstance == null)
        {
            GameObject prefab = Resources.Load<GameObject>(visual.ModelPath);

            if (prefab != null)
            {
                visual.ModelInstance = Object.Instantiate(prefab);
            }
        }

        if (visual.ParticleInstance == null) 
        {
            GameObject particlePrefab = Resources.Load<GameObject>(visual.ParticlePath);
            if (particlePrefab != null) 
            {
                GameObject particleInstance = Object.Instantiate(particlePrefab);
                visual.ParticleInstance = particleInstance.GetComponent<ParticleSystem>();
            }
        }

        if (visual.ModelInstance != null)
            visual.ModelInstance.SetActive(true);
    }

    public void MoveVisual(CleaningManager.CleaningSupplyType supply, Vector3 worldPosition) 
    {
        CleaningVisual visual = GetVisual(supply);
        if (visual == null || visual.ModelInstance == null)
            return;

        visual.ModelInstance.transform.position = worldPosition;

        if (visual.ParticleInstance != null)
        { 
            visual.ParticleInstance.transform.position = worldPosition;

            if(!visual.ParticleInstance.isPlaying)
                visual.ParticleInstance.Play();
        }        
    }

    public void StopVisual(CleaningSupplyType supply)
    {
        CleaningVisual visual = GetVisual(supply);

        if (visual == null)
            return;

        if (visual.ModelInstance != null)
            visual.ModelInstance.SetActive(false);

        if (visual.ParticleInstance != null)
            visual.ParticleInstance.Stop();
    }

    private CleaningVisual GetVisual(CleaningManager.CleaningSupplyType supply)
    {
        if (cleaningVisuals.TryGetValue(supply, out CleaningVisual visual))
        {
            Debug.Log("this is the visual " + visual);
            return visual;
        }

        return null;
    }
}
