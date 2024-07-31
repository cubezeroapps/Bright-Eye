using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;

public class ImagePreprocessor : MonoBehaviour
{
    public Texture2D ProcessImage(Texture2D inputTexture)
    {
        Mat imgMat = OpenCvSharp.Unity.TextureToMat(inputTexture);

        Mat grayMat = new Mat();
        Cv2.CvtColor(imgMat, grayMat, ColorConversionCodes.BGR2GRAY);

        Mat denoiseMat = new Mat();
        Cv2.GaussianBlur(grayMat, denoiseMat, new Size(5, 5), 0);

        Mat binaryMat = new Mat();
        Cv2.Threshold(denoiseMat, binaryMat, 0, 255, ThresholdTypes.Otsu);

        Texture2D processedTexture = OpenCvSharp.Unity.MatToTexture(binaryMat);
        return processedTexture;
    }
}
