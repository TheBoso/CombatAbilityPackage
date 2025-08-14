using System.Collections;
using Boso.ResourceCore;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Boso.CoreHealth
{

    public class FlinchController : MonoBehaviour
    {
        public UnityEvent OnFlinch;
        public  UnityEvent OnFlinchComplete;
        private BosoHealth _health; // Reference to the health component
        private Animator _animator;
        private Coroutine _flinchRoutine;
        private ObjectInputController _inputController;
        [SerializeField, Range(0.0f, 1.0f)] 
        private float _damageThreshold = 0.3f;
        [SerializeField] private float _flinchTime = 1.5f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _health = GetComponent<BosoHealth>();
            _inputController = GetComponent<ObjectInputController>();
            // Subscribe to the OnDamage event

            if (_health == null)
            {
                Debug.LogError("FlinchController requires a Health component.");
                enabled = false;
            }
            else
            {
                _health.OnDamaged += HandleDamage;
                _health.A_OnDeath.AddListener( HandleDeath);
            }
        }
        private void OnDestroy()
        {
            // Unsubscribe from the OnDamage event
            _health.OnDamaged -= HandleDamage;
            _health.A_OnDeath.RemoveListener(HandleDeath);

        }

        private void HandleDeath()
        {
            _health.OnDamaged -= HandleDamage;
            _health.A_OnDeath.RemoveListener(HandleDeath);
            if (_flinchRoutine != null)
            {
                StopCoroutine(_flinchRoutine);
            }
        }

        private void HandleDamage(float oldValue, float newValue)
        {

            if (_health != null && _health.IsDead())
            {
                return;
            }
            float damage = oldValue - newValue;
            // Calculate the ratio of damage taken relative to the maximum health
            // Example -  0.8f damageThreshold, indicating that any damage equal to or exceeding 80% of the character's
            // maximum health will be considered a 'huge amount' and trigger the flinch behavior."
            // 
            float damageRatio = damage / _health.MaxHealth;

            // if the damage recieved is a % more than the objects
            // maxHealth % 
            if (damageRatio >= _damageThreshold)
            {
                // Damage is considered a huge amount
                // Handle the flinch case here
                Flinch();
            }

        }

        private void Flinch()
        {
            OnFlinch?.Invoke();
            if (_flinchRoutine != null)
            {
                StopCoroutine(_flinchRoutine);
            }
            
            

            _flinchRoutine = StartCoroutine(FlinchRoutine(_flinchTime, _health.LastDamagedBy));
        }
        private IEnumerator FlinchRoutine(float time, GameObject dmgSource)
        {
            OnFlinch?.Invoke();
            bool oldHuman = _inputController.CurrentHumanState;
            bool oldAI = _inputController.CurrentAIState;
            _inputController.SetAIState(false);
            _inputController.SetHumanState(false);
            float damageDirection = 0.5f;
            if (dmgSource != null)
            {
                Vector3 direction = (dmgSource.transform.position - transform.position).normalized;
                float dotRight = Vector3.Dot(transform.right, direction);  // Right (-1 to 1)
                float dotForward = Vector3.Dot(transform.forward, direction); // Forward (-1 to 1)

                if (Mathf.Abs(dotRight) > Mathf.Abs(dotForward))
                {
                    // Prioritize left/right
                    damageDirection = dotRight > 0 ? 1f : -1f;
                }
                else
                {
                    // Prioritize forward/backward
                    damageDirection = dotForward > 0 ? 0.5f : -0.5f;
                }
            }
            
            _animator.SetFloat("HurtDir", damageDirection);
             _animator.Play("Hurt Blend");
             
            yield return new WaitForSeconds(time);
            _inputController.SetAIState(oldAI);
            _inputController.SetHumanState(oldHuman);
            OnFlinchComplete?.Invoke();
        }

        public void SetDamageThreshold(float damageThreshold)
        {
            this._damageThreshold = damageThreshold;
        }

    }
}
