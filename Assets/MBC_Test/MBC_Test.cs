using UnityEngine;
using UnityEngine.InputSystem;

public class MBC_Test : MonoBehaviour
{
    float g = 9.8f;
    float r = 25;

    Collider col;
    Rigidbody rig;
    Animator anim;

    private void Awake()
    {
        col = GetComponent<Collider>(); rig  = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
        if (Dir == Vector2.zero) return;

        var cam = Camera.main.transform;

        Vector3 P = rig.position;
        Vector3 N = P.normalized;

        Vector3 R = Vector3.ProjectOnPlane(cam.right, N).normalized;
        Vector3 F = Vector3.Cross(R, N).normalized;

        Vector3 T = F * Dir.y + R * Dir.x * 0.05f;

        if (T.sqrMagnitude < 0.000001f) return;

        float theta = T.magnitude * 5f * Time.fixedDeltaTime / r;
        T.Normalize();

        float c = Mathf.Cos(theta);
        float s = Mathf.Sin(theta);

        Vector3 nextPos = P * c + T * r * s;
        Vector3 nextN = nextPos.normalized;
        Vector3 nextT = T * c - N * s;

        rig.MovePosition(nextPos);
        rig.MoveRotation(Quaternion.LookRotation(nextT, nextN));
        if (Dir.y < 0) Dir.y = 1;
    }

    float Lasty = 1f;
    Vector2 Dir = default;
    void OnMove(InputValue var)
    {
        Dir = var.Get<Vector2>().normalized;
        anim.SetBool("Walk",Dir != Vector2.zero);
    }
    void OnAttack(InputValue var)
    {
        anim.SetTrigger("Attack");
    }
}
