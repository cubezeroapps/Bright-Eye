using UnityEngine;
using OpenCvSharp;
using System.Collections.Generic;

public class MatPoolManager : MonoBehaviour
{
    private Queue<Mat> matPool = new Queue<Mat>();
    public static MatPoolManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Mat GetMat()
    {
        if (matPool.Count > 0)
        {
            var mat = matPool.Dequeue();
            mat.SetTo(Scalar.All(0));
            return mat;
        }
        else
        {
            return new Mat();
        }
    }

    public void ReturnMat(Mat mat)
    {
        if (mat != null)
        {
            mat.SetTo(Scalar.All(0));
            matPool.Enqueue(mat);
        }
    }

    void OnDestroy()
    {
        while (matPool.Count > 0)
        {
            matPool.Dequeue().Dispose();
        }
    }
}