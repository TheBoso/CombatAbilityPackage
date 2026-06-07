using System.Collections;
using Boso.ResourceCore;
using UnityEngine;
using YAAS;

[System.Serializable]
public class DeathEffect : AbilityEffect
{
    public AnimationCategory DeathCategory;
    //  dying is an ability effect. Much more modular!
    public override IEnumerator PerformEffect(AbilityCaster caller)
    {
      if(caller.TryGetComponent(out CharacterAnimationMap anim))
      {
          var deathClip = anim.GetRandomAnimation(DeathCategory);
          if(deathClip == null)
          {
              yield break;
          }

          bool isAnimDone = false;
          var emote = caller.GetComponent<EmoteController>();
          emote.TryPlay(deathClip, () => isAnimDone = true );
          yield return new WaitUntil(() => isAnimDone);
      }
    }
}
