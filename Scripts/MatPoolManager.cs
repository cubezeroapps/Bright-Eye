using UnityEngine;
using System.Collections.Generic;
using OpenCvSharp;

public class MatPoolManager : MonoBehaviour
{
    public static MatPoolManager Instance;

    private Queue<Mat> matPool = new Queue<Mat>();

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