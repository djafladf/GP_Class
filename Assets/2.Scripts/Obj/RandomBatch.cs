using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomBatch : MonoBehaviour
{
    [SerializeField] List<GameObject> Rocks;
    [SerializeField] float Radius = 5;

    private void Start()
    {
        float angle = 0;
        Vector3 subAngle = Vector3.forward;
        int flag = 0;
        while(angle < 360 && ++flag < 100)
        {
            var rock = Instantiate(Rocks[Random.Range(0, Rocks.Count)],transform).transform;
            rock.position = transform.position + subAngle * Radius;

            float rad = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));
            rock.position = transform.position + dir * Radius;

            // 회전 (돌이 원 중심 바라보게)
            rock.rotation = Quaternion.LookRotation(dir);

            // 다음 각도 계산: 현재 돌의 x 길이만큼 회전 증가
            float width = rock.GetComponent<MeshRenderer>().bounds.size.x * 0.8f;
            float arcLength = width / Radius; 
            float deltaAngle = arcLength * Mathf.Rad2Deg; 

            angle += deltaAngle;
        }
    }
}
