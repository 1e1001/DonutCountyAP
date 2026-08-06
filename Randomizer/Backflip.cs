using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class Backflip : MonoBehaviour
{
    // TODO: backflip should be a component on the gameui instead

    [HarmonyPatch(typeof(OS1GameUI), "Start")]
    public static class OS1GameUI_Start
    {
        static void Postfix(OS1GameUI __instance)
        {
            foreach (var character in __instance.characters)
                character._characterHolder.AddComponent<Backflip>();
        }
    }

    Vector3 base_angle;
    float timer = 0f;

    void Start()
    {
        base_angle = transform.localEulerAngles;
    }

    void Update()
    {
        if (timer <= 0f)
            return;
        float offset_angle = (float)Easing.EaseInOut(Mathf.Repeat(timer, 1f), EasingType.Quadratic) * 360f;
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            offset_angle = 0f;
            timer = 0f;
        }
        transform.localEulerAngles = new Vector3(base_angle.x + offset_angle, base_angle.y, base_angle.z);
    }

    public void DoBackflip()
    {
        if (!this.isActiveAndEnabled)
            return;
        Plugin.BepInLogger.LogInfo($"backflipping {this.gameObject}");
        timer += 1f;
    }
}

