using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class Backflip : MonoBehaviour
{
    int _queued = 0;
    public GameObject[] Characters;
    bool _currentlyAnimating = false;

    void Update()
    {
        if (_queued == 0 || _currentlyAnimating)
            return;
        var candidate = Characters.First(ch => ch.activeInHierarchy);
        if (candidate == null)
            return;
        Plugin.BepInLogger.LogInfo($"backflipping {candidate}");
        StartCoroutine(BackflipRoutine(candidate.transform));
    }

    IEnumerator BackflipRoutine(Transform target)
    {
        _currentlyAnimating = true;
        var baseAngle = target.localEulerAngles;
        float timer = 1f;
        while (timer > 0f)
        {
            float offset_angle = (float)Easing.EaseInOut(Mathf.Repeat(timer, 1f), EasingType.Quadratic) * 360f;
            target.localEulerAngles = new Vector3(baseAngle.x + offset_angle, baseAngle.y, baseAngle.z);
            yield return null;
            timer -= Time.deltaTime;
        }
        target.localEulerAngles = baseAngle;
        _currentlyAnimating = false;
    }

    public void DoBackflip()
    {
        ++_queued;
    }
}

