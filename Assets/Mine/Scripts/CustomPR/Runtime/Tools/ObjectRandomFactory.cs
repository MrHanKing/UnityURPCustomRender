using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectRandomFactory : MonoBehaviour
{
    [Tooltip("随机物体")]
    public GameObject item;
    public Collider area;


    public void CreateOne()
    {
        if (area != null && item != null)
        {
            var randomPoint = Random.insideUnitSphere * this.area.bounds.extents.sqrMagnitude;
            var closedPoint = this.area.bounds.ClosestPoint(randomPoint);

            var dir = closedPoint - this.area.bounds.center;
            var endPos = this.area.bounds.center + Random.Range(0f, 1f) * dir;

            Instantiate(this.item, endPos, Quaternion.identity, this.transform);
        }
    }
}
