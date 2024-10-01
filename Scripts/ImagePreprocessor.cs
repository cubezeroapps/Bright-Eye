using UnityEngine;
using UnityEngine.UI;
using OpenCvSharp;

public class ImagePreprocessor : MonoBehaviour
{
    public Texture2D ProcessImage(Texture2D inputTexture)
    {
        Mat imgMat = OpenCvSharp.Unity.TextureToMat(inputTexture);

        Mat grayMat = MatPoolManager.Instance.GetMat();
        Mat blurredMat = MatPoolManager.Instance.GetMat();
        Mat morphMat = MatPoolManager.Instance.GetMat();
        Mat binaryMat = MatPoolManager.Instance.GetMat();

        Cv2.CvtColor(imgMat, grayMat, ColorConversionCodes.BGR2GRAY);
        Cv2.FastNlMeansDenoising(grayMat, grayMat, 30);
        Cv2.MedianBlur(grayMat, blurredMat, 5);
        Cv2.EqualizeHist(blurredMat, blurredMat);
        Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
        Cv2.MorphologyEx(blurredMat, morphMat, MorphTypes.Close, kernel);
        Cv2.AdaptiveThreshold(morphMat, binaryMat, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.BinaryInv, 11, 2);

        Texture2D resultTexture = OpenCvSharp.Unity.MatToTexture(binaryMat);

        MatPoolManager.Instance.ReturnMat(imgMat);
        MatPoolManager.Instance.ReturnMat(grayMat);
        MatPoolManager.Instance.ReturnMat(blurredMat);
        MatPoolManager.Instance.ReturnMat(morphMat);
        MatPoolManager.Instance.ReturnMat(binaryMat);

        return resultTexture;
    }
}