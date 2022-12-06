using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clock : MonoBehaviour
{
    [SerializeField]
    private Transform hourHand;

    [SerializeField]
    private Transform minuteHand;

    [SerializeField]
    private Transform secondHand;

    [Header("Options")]
    [SerializeField]
    private bool smoothing;

    [SerializeField]
    private bool hours24;

    private void OnDrawGizmos()
    {
        DateTime time = DateTime.Now;

        float secondPeriocity = 60.0f;
        float minutePeriocity = 60.0f;
        float hourPeriocity = hours24 ? 24.0f : 12.0f;

        float seconds = (float)time.Second / secondPeriocity;
        float minutes = (float)time.Minute / minutePeriocity;
        float hours = (float)time.Hour / hourPeriocity;

        if (smoothing)
        {
            seconds += ((float)time.Millisecond / 1000.0f) / secondPeriocity;
            minutes += ((float)time.Second / secondPeriocity) / minutePeriocity;
            hours += ((float)time.Minute / minutePeriocity) / hourPeriocity;
        }

        HandleHand(seconds, secondHand);
        HandleHand(minutes, minuteHand);
        HandleHand(hours, hourHand);
    }

    private void HandleHand(float seconds, Transform hand)
    {
        float offset = Mathf.PI / 2.0f;
        Vector3 pos = new Vector3(Mathf.Cos(offset + seconds * Mathf.PI * 2), Mathf.Sin(offset + seconds * Mathf.PI * 2), 0).normalized * 0.5f;

        Vector3 dir = (transform.TransformPoint(pos) - transform.position).normalized;

        Vector3 position = pos * hand.localScale.y;
        position.z = 0.75f;

        hand.localPosition = position;
        hand.rotation = Quaternion.LookRotation(transform.forward, dir);
    }
}
