using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixParticleScaleToOne : MonoBehaviour {

    ParticleSystem particles;
    ParticleSystem.MainModule mainparts;
    ParticleSystem.ShapeModule shapeParts;
    float shapeRadiusStart;
    // Use this for initialization
    void Awake () {
        particles = GetComponent<ParticleSystem>();
        mainparts = particles.main;
        mainparts.startSizeMultiplier = 0.5f / transform.root.localScale.sqrMagnitude;
        shapeParts = particles.shape;
        shapeRadiusStart = shapeParts.radius;
        shapeParts.radius = shapeRadiusStart / transform.root.localScale.sqrMagnitude;
    }

    private void OnEnable()
    {
        shapeParts.radius = shapeRadiusStart / transform.root.localScale.sqrMagnitude;
        mainparts.startSizeMultiplier = 0.25f / transform.root.localScale.sqrMagnitude;
    }
}
