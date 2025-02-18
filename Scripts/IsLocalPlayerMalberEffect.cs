
using MalbersAnimations.Controller;

namespace YAAS
{

  public class IsLocalPlayerMalberEffect : IsLocalPlayerEffect
  {
    protected override bool IsLocalPlayer(AbilityCaster caster)
    {
      if (caster.TryGetComponent(out MAnimal animal))
      {
        if (MAnimal.MainAnimal == animal)
        {
          return true;
        }

      }

      return false;
    }
  }
}
