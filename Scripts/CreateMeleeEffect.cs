using System.Collections;
using System.Linq;
using Boso.CoreHealth;
using Boso.ResourceCore;
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
        [SerializeField] private string _spawnPointName;

        public override IEnumerator PerformEffect(AbilityCaster caller)
        {
       
            //  Something needs this component in the model
            var targetObjects = caller.GetComponentsInChildren<CombatSpawnPoint>(true);
            Transform targetObject = null;
            if (string.IsNullOrEmpty(_spawnPointName) == false)
            {
                targetObject = targetObjects.FirstOrDefault(x => x.name == _spawnPointName).transform;    
            }
            else
            {
                targetObject = targetObjects[0].transform;
            }
            
            Vector3 overlapPos =
                targetObject != null ? targetObject.position  : caller.transform.position;

            Collider[] colliders = Physics.OverlapSphere(overlapPos, _radius, OverlapMask);

            foreach (var hit in colliders)
            {
                if (hit.gameObject == caller.gameObject) continue;

                if (hit.TryGetComponent(out BosoHealth health))
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

                    Vector3 dir = (targetObject.position - hit.transform.position).normalized;

                   

                        health.TakeDamage(BaseDamage,
                            caller.gameObject,
                            Random.value <= 0.25f);
                }
            }

            yield return null;
        }
    }
}
