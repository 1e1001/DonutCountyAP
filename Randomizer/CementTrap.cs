using System.Collections;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class CementTrap : MonoBehaviour
{
    const float RUNTIME = 10f;
    static int _queued = 0;
    public float Timer = 0f;
    public HoleSubstanceManager.Substance UnderlyingSubstance;


    void Update()
    {
        if (_queued == 0 || Timer > 0f)
            return;
        if (RM.holeMovement.GetDisableMovement() || RM.holeScale.GetScale() == 0f)
            return;
        Plugin.BepInLogger.LogDebug("doing cement trap");
        StartCoroutine(TrapRoutine());
    }

    IEnumerator TrapRoutine()
    {
        UnderlyingSubstance = RM.substanceManager.GetSubstance();
        RM.substanceManager.SetSubstance(HoleSubstanceManager.Substance.Cement);
        Timer = RUNTIME;
        while (Timer > 0f)
        {
            yield return null;
            Timer -= Time.deltaTime;
        }
        --_queued;
        RM.substanceManager.SetSubstance(UnderlyingSubstance);
    }

    public void DoCementTrap()
    {
        ++_queued;
    }

    public static bool HasNoCement()
    {
        return (RM.substanceManager.GetComponent<CementTrap>()?.Timer ?? 0f) <= 0f;
    }
}
