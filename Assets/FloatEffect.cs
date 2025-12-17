using UnityEngine;

public class UpDownAndSpinOneAxis : MonoBehaviour
{
    [Header("Up / Down (Y only)")]
    [SerializeField] float amplitude = 0.5f;   // height of the bob
    [SerializeField] float bobSpeed = 1.0f;    // cycles per second-ish

    [Header("Spin (one axis only)")]
    [SerializeField] Axis spinAxis = Axis.Y;
    [SerializeField] float spinSpeed = 90f;    // degrees per second

    Vector3 startPos;
    float spinAngle;

    enum Axis { X, Y, Z }

    void Awake()
    {
        startPos = transform.position;
        // Start from the current rotation on that axis so it doesn't snap on play
        var e = transform.eulerAngles; // read as euler degrees [web:32]
        spinAngle = spinAxis == Axis.X ? e.x : (spinAxis == Axis.Y ? e.y : e.z);
    }

    void Update()
    {
        // 1) Up/down only: lock X and Z, change only Y
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * amplitude;
        transform.position = new Vector3(startPos.x, startPos.y + yOffset, startPos.z);

        // 2) Spin only on one axis: keep the other two angles fixed to 0
        spinAngle += spinSpeed * Time.deltaTime;

        Vector3 euler =
            spinAxis == Axis.X ? new Vector3(spinAngle, 0f, 0f) :
            spinAxis == Axis.Y ? new Vector3(0f, spinAngle, 0f) :
                                 new Vector3(0f, 0f, spinAngle);

        // Set all euler values at once (Unity recommends setting as a whole vector). [web:32]
        transform.eulerAngles = euler;
    }
}