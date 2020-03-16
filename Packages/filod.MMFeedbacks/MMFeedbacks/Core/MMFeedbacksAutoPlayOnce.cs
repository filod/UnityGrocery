using UnityEngine;
using System.Collections;
using MoreMountains.Feedbacks;

[RequireComponent(typeof(MMFeedbacks))]
public class MMFeedbacksAutoPlayOnce : MonoBehaviour
{
    // Use this for initialization
    void Start()
    {
        GetComponent<MMFeedbacks>().Initialization();
        GetComponent<MMFeedbacks>().PlayFeedbacks();
    }
}
