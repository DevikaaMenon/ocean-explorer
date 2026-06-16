using UnityEngine;
using System.Collections.Generic;
public class IgnoreCollisionWithTag : MonoBehaviour
{
    [SerializeField]
    public List<string> IgnoredTags = new List<string>();

    private void Start()
    {
        Collider collider = GetComponent<Collider>();
        if (collider is null)
        {
            Debug.LogError("No collider provided!");
            return;
        }

        foreach(var tag in IgnoredTags) {
            GameObject[] objectsToIgnore = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objectsToIgnore)
            {
                Collider otherCollider = obj.GetComponent<Collider>();
                if (otherCollider is not null)
                {
                    Physics.IgnoreCollision(collider, otherCollider);
                }
            }
        }
    }
}