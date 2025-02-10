using System.Collections;
using Boso.CoreHealth;
using MalbersAnimations;
using UnityEngine;

namespace YAAS
{

    [CreateAssetMenu(fileName = "MeleeEffect", menuName = "YAAS/Combat/Melee Effect", order = 1)]
    public class CreateMeleeEffect : AbilityEffect
    {
        //  Where to create the overlap sphere
        [SerializeField] private LayerMask OverlapMask;
        [SerializeField] private float BaseDamage = 1;
        [SerializeField] private ParticleSystem[] OnHitParticle;
        [SerializeField] private AudioClip OnHitSound;
        [SerializeField] private float _radius = 1.0f;
        [SerializeField] private StatID _targetStatID;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            //  Something needs this component in the model
            Transform targetObject = caller.GetComponentInChildren<CombatSpawnPoint>().transform;
            Vector3 overlapPos =
                targetObject != null ? targetObject.position  : caller.transform.position;

            #if UNITY_EDITOR
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * _radius;
            sphere.transform.position = overlapPos;
            Collider[] sphereCol = sphere.GetComponentsInChildren<Collider>();
            foreach (var c in sphereCol)
            {
                Destroy(c);
            }
            Destroy(sphere, 3.0f);
            #endif
            Collider[] colliders = Physics.OverlapSphere(overlapPos, _radius, OverlapMask);

            foreach (var hit in colliders)
            {
                if (hit.gameObject == caller.gameObject) continue;

                if (hit.TryGetComponent(out MDamageable health))
                {
                    if (OnHitParticle != null && OnHitParticle.Length > 0)
                    {
                        Vector3 collisionPoint = hit.ClosestPoint(overlapPos);
                        var particle =
                            Instantiate(
                                OnHitParticle[
                                    UnityEngine.Random.Range(0, OnHitParticle.Length)],
                                hit.transform.position, Quaternion.identity);

                        particle.transform.position = collisionPoint;

                        Destroy(particle, 3.0f);

                    }
                    
                    health.ReceiveDamage(_targetStatID, BaseDamage, StatOption.SubstractValue);

                }
            }

            yield return null;
        }
    }
}
