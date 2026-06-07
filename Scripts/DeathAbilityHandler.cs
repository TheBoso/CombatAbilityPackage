using System;
using Boso.ResourceCore;
using UnityEngine;
using YAAS;

public class DeathAbilityHandler : MonoBehaviour
{
   [SerializeField] private AbilityDef _deathAbility;
   
   private BosoHealth _hp;

   private void Awake()
   {
      _hp = GetComponent<BosoHealth>();
      _hp.A_OnDeath.AddListener(OnDeath);
   }

   private void OnDeath()
   {
      // fuck it direct ref
      if (TryGetComponent(out AbilityCaster caster))
      {
         caster.LearnAbility(_deathAbility);
      }
   }

   private void OnDestroy()
   {
      if (_hp != null)
      {
         _hp.A_OnDeath.RemoveListener(OnDeath);
      }
   }
}
