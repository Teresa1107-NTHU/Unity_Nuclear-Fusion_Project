using UnityEngine;

public class LabelBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (!Camera.main) return;
        transform.rotation = Quaternion.LookRotation(
            transform.position - Camera.main.transform.position,
            Vector3.up
        );
    }
}
