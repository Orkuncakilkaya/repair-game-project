using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Madafaka : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.applyTransform(this.transform);
    }

    private void applyTransform(Transform transform)
    {
        for (var i = 0; i < transform.childCount; i++)
        {
            var childTransform = transform.GetChild(i);
            if (childTransform.childCount > 0)
            {
                this.applyTransform(childTransform);
            }
            else
            {
                childTransform.gameObject.AddComponent<Child>();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
    }
}