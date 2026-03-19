using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SoftBones2D : MonoBehaviour
{
    [Header("Wind")]

    [Tooltip(
        "Intensité globale du vent appliqué aux bones.\n" +
        "Contrôle l'amplitude maximale de rotation générée par le bruit.\n" +
        "Plus la valeur est élevée, plus les bones se plient fortement.\n" +
        "Valeurs typiques : 5 (léger) → 25 (vent fort)."
    )]
    public float windStrength = 15f;

    [Tooltip(
        "Vitesse de variation du vent dans le temps.\n" +
        "Contrôle à quelle vitesse le mouvement change.\n" +
        "Valeur basse = vent lent et lourd.\n" +
        "Valeur élevée = vent rapide et nerveux.\n" +
        "Valeurs typiques : 0.5 (lent) → 2 (rapide)."
    )]
    public float windSpeed = 1f;

    [Tooltip(
        "Variation aléatoire entre les bones.\n" +
        "Empêche un mouvement trop uniforme ou mécanique.\n" +
        "0 = tous les bones bougent pareil.\n" +
        "1 = mouvement très organique et désynchronisé.\n" +
        "Valeurs typiques : 0.3 → 0.8."
    )]
    public float randomness = 0.5f;


    [Header("Damping")]

    [Tooltip(
        "Vitesse de lissage vers la rotation cible.\n" +
        "Contrôle la souplesse globale du mouvement.\n" +
        "Valeur basse = mouvement mou, flottant.\n" +
        "Valeur élevée = mouvement rigide et réactif.\n" +
        "Valeurs typiques : 3 (soft) → 10 (snappy)."
    )]
    public float smooth = 5f;


    [Header("Debug Gizmos")]

    [Tooltip(
        "Affiche les gizmos de debug dans la scène.\n" +
        "Utile pour visualiser les forces et la structure des bones.\n" +
        "N'affecte pas le comportement en jeu."
    )]
    public bool showGizmos = true;

    [Tooltip(
        "Masque les liens partant des bones racine (depth 0).\n" +
        "Permet une lecture plus claire des chaînes secondaires\n" +
        "lors du debug visuel."
    )]
    public bool hideRootLinks = true;

    [Tooltip(
        "Gradient utilisé pour visualiser l'intensité de la force.\n" +
        "Faible force = début du gradient.\n" +
        "Forte force = fin du gradient."
    )]
    public Gradient forceGradient;
    
    [Tooltip(
        "Couleur des bones auquels aucune force n'est appliquée."
    )]
    public Color colorDisabledBones = new Color(1f, 0f, 0f, 0.3f);

    [Tooltip(
        "Rayon des sphères dessinées sur chaque bone.\n" +
        "Purement visuel, pour le debug Scene View.\n" +
        "Aucun impact sur le gameplay."
    )]
    public float gizmoBoneRadius = 0.02f;
    
    [Header("Wind Ignore List")]
    [Tooltip("Liste de bones qui ne recevront pas l'effet du vent (ex: le root ou le sprite principal).")]
    public List<Transform> ignoreBones = new List<Transform>();

    [Header("Mode Options")]
    public bool useFlameMode = false;

public class BoneData
    {
        public Transform transform;
        public Quaternion baseRotation;
        public int depth;
        public float noiseOffset;
        public Vector3 velocity;
    }

    public List<BoneData> bones = new();

    void OnEnable()
    {
        RebuildBones();
    }

    void OnValidate()
    {
        RebuildBones();
    }

    void RebuildBones()
    {
        bones.Clear();

        if (transform == null) return;

        foreach (Transform child in transform)
        {
            CollectChain(child, 0);
        }
    }

    void CollectChain(Transform current, int depth)
    {
        bones.Add(new BoneData
        {
            transform = current,
            baseRotation = current.localRotation,
            depth = depth,
            noiseOffset = Random.Range(0f, 1000f)
        });

        foreach (Transform child in current)
        {
            CollectChain(child, depth + 1);
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        ApplyBoneRotations();
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || bones == null) return;

        float time = Application.isPlaying ? Time.time * windSpeed : Time.realtimeSinceStartup * windSpeed;

        foreach (var bone in bones)
        {
            if (bone.transform == null || bone.transform.parent == null) continue;
            if (hideRootLinks && bone.transform.parent == transform)
                continue;

            Vector3 a = bone.transform.parent.position;
            Vector3 b = bone.transform.position;

            bool isIgnored = ignoreBones.Contains(bone.transform);

            Color color;

            if (isIgnored)
            {
                color = colorDisabledBones;
            }
            else
            {
                float depthFactor = Mathf.Clamp01(bone.depth / 5f);
                float noise = Mathf.PerlinNoise(time + bone.noiseOffset, 0f);
                float angle = Mathf.Abs((noise - 0.5f) * 2f);
                float force = angle * windStrength * depthFactor * randomness;
                float normalizedForce = Mathf.Clamp01(force / windStrength);
                color = forceGradient.Evaluate(normalizedForce);
            }

            Gizmos.color = color;
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(b, gizmoBoneRadius);

#if UNITY_EDITOR
            Handles.color = color;
#endif
        }
    }
    
    void ApplyBoneRotations()
    {
        float time = Time.time * windSpeed;

        foreach (var bone in bones)
        {
            if (bone.transform == null) continue;
            if (ignoreBones.Contains(bone.transform)) continue;

            float depthFactor = Mathf.Clamp01(bone.depth / 5f);

            Quaternion targetRot = bone.baseRotation;

            if (useFlameMode)
            {
                // Flame mode : Perlin + Sin pour tremblement organique
                float noise = Mathf.PerlinNoise(time + bone.noiseOffset, 0f);
                float sineOsc = Mathf.Sin(time * (1f + bone.noiseOffset % 1f) * 2f * Mathf.PI);
                float angle = (noise - 0.5f + sineOsc * 0.3f) * 2f;
                angle *= windStrength * depthFactor * randomness;
                targetRot = bone.baseRotation * Quaternion.Euler(0, 0, angle);
            }
            else
            {
                // Mode classique vent/balancier
                float noise = Mathf.PerlinNoise(time + bone.noiseOffset, 0f);
                float angle = (noise - 0.5f) * 2f;
                angle *= windStrength * depthFactor * randomness;
                targetRot = bone.baseRotation * Quaternion.Euler(0, 0, angle);
            }

            bone.transform.localRotation = Quaternion.Slerp(
                bone.transform.localRotation,
                targetRot,
                Time.deltaTime * smooth
            );
        }
    }

}