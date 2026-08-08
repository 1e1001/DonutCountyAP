using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static HoleSubstanceManager;
using static OS1GameUI;

namespace DonutCountyAP.Randomizer;

public class CementTrap : MonoBehaviour
{
    const float RUNTIME = 10f;
    int _queued = 0;
    public float Timer = 0f;
    public Substance UnderlyingSubstance;


    void Update()
    {
        if (_queued == 0 || Timer > 0f)
            return;
        Plugin.BepInLogger.LogInfo($"doing cement trap");
        StartCoroutine(TrapRoutine());
    }

    IEnumerator TrapRoutine()
    {
        UnderlyingSubstance = RM.substanceManager.GetSubstance();
        RM.substanceManager.SetSubstance(Substance.Cement);
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
}
