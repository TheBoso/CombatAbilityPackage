using System.Collections;
using Boso.CoreHealth;
using UnityEngine;

namespace YAAS
{

    [CreateAssetMenu(fileName = "MeleeEffect", menuName = "YAAS/Combat/Melee Effect", order = 1)]
    public class CreateMeleeEffect : AbilityEffect
    {
        //  Where to create the overlap sphere
        [SerializeField] private Vector3 _offset;
        [SerializeField] private string OverlapObjectName;
        [SerializeField] private LayerMask OverlapMask;
        [SerializeField] private int BaseDamage = 1;
        [SerializeField] private ParticleSystem[] OnHitParticle;
        [SerializeField] private AudioClip OnHitSound;
        [SerializeField] private float _radius = 1.0f;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
            Transform targetObject = caller.transform.Find(OverlapObjectName);
            Vector3 overlapPos =
                targetObject != null ? targetObject.position + _offset : caller.transform.position + _offset;

            Collider[] colliders = Physics.OverlapSphere(overlapPos, _radius, OverlapMask);

            foreach (var hit in colliders)
            {
                if (hit.gameObject == caller.gameObject) continue;

                if (hit.TryGetComponent(out IDamageable health))
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

                        health.Damage(BaseDamage, caller.transform, CriticalHit: Random.value <= 0.15f);
                    }
                }
            }

            yield return null;
        }
    }
}
