using System.Collections;
using MalbersAnimations;
using MalbersAnimations.Controller;
using UnityEngine;

namespace YAAS
{
    
public class MalbersSetMode : AbilityEffect
{
    [SerializeField] private ModeID _mode;
    [SerializeField] private float _time = 0.0f;
    [SerializeField] private AbilityStatus _status = AbilityStatus.PlayOneTime;
    
    public override IEnumerator PerformEffect(AbilityCaster caller)
    {
        if (caller.TryGetComponent(out MAnimal animal))
        {
            animal.Mode_TryActivate(_mode.ID, -99, _status, time: _time);
        }

        yield return null;
    }
}
}
