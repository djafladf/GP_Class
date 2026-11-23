using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Serializable]
public class Event : UnityEvent<BaseEventData> { }

[System.Serializable]
public class AnimCurve : ISerializationCallbackReceiver
{
    public string name;
    public string trigger;
    public float StUsement;
    [System.NonSerialized] public HashSet<CurveState> ComboAble;
    public ForTestFloat ComboTime;
    public List<CurveState> ComboAbleInspec;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        ComboAble = new HashSet<CurveState>(ComboAbleInspec);
    }
}

public enum CurveState
{
    none,
    roll,
    jump,
    rattack,
    lattack,
    teleport
}

public enum DamageType
{
    Slash,
    Strike,
    Pierece
}
public enum Elemental 
{ 
    None,
    Fire,
    Originum
}


[System.Serializable]
public class AttackClip
{
    public float StUsement;
    public DamageType DamageType;
    public Elemental Elemental;
    public float DamageRatio;
    public AnimationClip Clip;
}

[System.Serializable]
public class WeaponInfo
{
    public string name;
    public int Rarity;
    public List<AttackClip> One;
    public List<AttackClip> Two;
    public AttackClip OneDash;
    public AttackClip TwoDash;

    public int returnCount(bool IsTwo){ return IsTwo ? Two.Count: One.Count; } 

    public int LV;
    public GameObject Obj;
    public Sprite Im;
}

public class Surface
{
    public Renderer renderer;
    public Mesh mesh;
    public readonly List<Vector3> vertices = new();
    public int[] triangles;

    Vector3[] bindVertices;
    Vector3[] cachedVertices;
    int[] cachedFrames;
    int[] weightStarts;

    BoneWeight1[] weights;
    Transform[] bones;
    Matrix4x4[] bindposes;
    Matrix4x4[] boneMatrices;

    int frame;
    bool manualSkinning;

    public void Bake()
    {
        if (renderer is SkinnedMeshRenderer sk)
            sk.BakeMesh(mesh, true);

        mesh.GetVertices(vertices);
    }

    public void SetupSkinning()
    {
        if (renderer is not SkinnedMeshRenderer sk)
            return;

        Mesh source = sk.sharedMesh;

        if (!source || source.blendShapeCount > 0 ||
            sk.GetComponent<Cloth>() != null)
            return;

        Transform[] skBones = sk.bones;
        Matrix4x4[] poses = source.bindposes;

        if (skBones.Length == 0 || skBones.Length != poses.Length)
            return;

        foreach (var bone in skBones)
            if (!bone) return;

        var perVertex = source.GetBonesPerVertex();
        var allWeights = source.GetAllBoneWeights();

        if (perVertex.Length != source.vertexCount ||
            allWeights.Length == 0)
            return;

        bindVertices = source.vertices;
        bones = skBones;
        bindposes = poses;
        weights = allWeights.ToArray();

        weightStarts = new int[source.vertexCount + 1];

        for (int i = 0; i < source.vertexCount; i++)
            weightStarts[i + 1] = weightStarts[i] + perVertex[i];

        boneMatrices = new Matrix4x4[bones.Length];
        cachedVertices = new Vector3[source.vertexCount];
        cachedFrames = new int[source.vertexCount];

        manualSkinning = true;
    }

    public void UpdatePose()
    {
        if (!manualSkinning)
        {
            if (renderer is SkinnedMeshRenderer)
                Bake();

            return;
        }

        for (int i = 0; i < bones.Length; i++)
            boneMatrices[i] =
                bones[i].localToWorldMatrix * bindposes[i];

        if (frame == int.MaxValue)
        {
            Array.Clear(cachedFrames, 0, cachedFrames.Length);
            frame = 0;
        }

        frame++;
    }

    public Vector3 GetWorldVertex(int index)
    {
        if (!manualSkinning)
            return renderer.transform.TransformPoint(vertices[index]);

        if (cachedFrames[index] == frame)
            return cachedVertices[index];

        Vector3 p = bindVertices[index];
        Vector3 result = Vector3.zero;

        for (int i = weightStarts[index];
             i < weightStarts[index + 1]; i++)
        {
            BoneWeight1 w = weights[i];

            result += boneMatrices[w.boneIndex]
                .MultiplyPoint3x4(p) * w.weight;
        }

        cachedFrames[index] = frame;
        cachedVertices[index] = result;

        return result;
    }
}


[System.Serializable]
public struct SpawnType
{
    public string name;
    public float SpawnGap;
    public int EnemyID;
    public int MinLevel;
    public int MaxLevel;
}

[System.Serializable]
public class ItemType
{

}
[System.Serializable]
public class ForTestV4
{
    public List<Vector4> List;
}

[System.Serializable]
public class ForTestInt
{
    public List<int> List; 
}
[System.Serializable]
public class ForTestFloat
{
    public List<float> List;
}


[System.Serializable]
public class ForTestImage
{
    public List<Image> List;
}

// Map
[System.Serializable]
public class Cell
{
    public int col, row;
    public int Group = 0;
    public bool Right;
    public bool Left;
    public bool Up;
    public bool Down;
};