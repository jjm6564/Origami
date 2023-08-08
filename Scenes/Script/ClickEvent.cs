using Microsoft.MixedReality.Toolkit.Windows.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.WebCam;
using Unity.Barracuda;
using UnityEngine.UI;

//https://docs.unity3d.com/2019.1/Documentation/Manual/windowsholographic-photocapture.html
//https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/locatable-camera-in-unity
namespace captureEvent
{

    public class ClickEvent : MonoBehaviour
    {
        public int inputResolutionY = 416;
        public int inputResolutionX = 416;

        public Camera camera;
        public GameObject testSphere;
        public Material preprocessMaterial;
        public TextAsset labelsAsset;
        //public RawImage displayImage;

        #region GameSceneshot
        private Boolean clickOk = false;
        private Texture2D texture;
        string folderPath = Directory.GetCurrentDirectory() + "/screenshots/butterfly.jpg";
        private int width = Screen.width;
        private int height = Screen.height;
        #endregion
        
        #region MSdocs
        private PhotoCapture photoCaptureObject = null; //MRTK docs
        private Texture2D targetTexture = null;
        #endregion

        [SerializeField]
        #region MLpart
        public NNModel OrigamiModel;
        static public Model _runtime_Origami;
        static public IWorker _engine_Origami;

        RenderTexture renderTexture = null;
        RenderTexture targetRT = null;

        private string[] labels;
        #endregion

        void Start()
        {
            testSphere.SetActive(false);
            Debug.Log("path : " + folderPath);
            Debug.Log("webcam Mode :" + WebCamMode.PhotoMode);
            //renderTexture = RenderTexture.GetTemporary(224, 224, 0, RenderTextureFormat.ARGBHalf);
        }

        // Update is called once per frame
        void Update()
        {

        }
        public void Onclick()
        {
            Debug.Log("click");
            
            //if (clickOk)
            {
                PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
                //StartCoroutine(Recognize());
                //Recognize();
            }
        }

        //MRTK docs Code 
        void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            Debug.Log("Capture");
            photoCaptureObject = captureObject;

            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

            CameraParameters c = new();
            c.hologramOpacity = 0.0f;

            c.cameraResolutionWidth = cameraResolution.width;
            c.cameraResolutionHeight = cameraResolution.height;

            c.pixelFormat = CapturePixelFormat.BGRA32;


            captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
        }
        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
        }

        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            
            Debug.Log("start : "+result.success);
            if (result.success)
            {
                string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
                string filePath = Path.Combine(folderPath + "butterfly.jpg", filename);

                //photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
                photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
            }
            else
            {
                Debug.LogError("Unable to start photo mode!");
            }
        }
        void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                Debug.Log("Saved Photo to disk!");
                photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
            }
            else
            {
                Debug.Log("Failed to save Photo to disk");
            }
        }
        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Debug.Log("memory : "+result.success);
            if (result.success)
            {
                // Create our Texture2D for use and set the correct resolution
                Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                targetTexture = new(cameraResolution.width, cameraResolution.height);
                // Copy the raw image data into our target texture
                photoCaptureFrame.UploadImageDataToTexture(targetTexture);

                targetTexture.Apply();
                Debug.Log(targetTexture);
                /*byte[] bytes = targetTexture.EncodeToJPG();
                File.WriteAllBytes(folderPath, bytes);*/
                Recognize();
                // Do as we wish with the texture such as apply it to a material, etc.
            }
            // Clean up
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
        }


        //Barracuda
        //IEnumerator Recognize()//코루틴으로 변경 예정.
        void Recognize()
        {
            //yield return new WaitForEndOfFrame();
            Debug.Log("start Coroutine");
            
            Application.targetFrameRate = 60;
            labels = labelsAsset.text.Split('\n');
            Debug.Log(labels[0]);
            _runtime_Origami = ModelLoader.Load(OrigamiModel);
            targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionX, 0, RenderTextureFormat.ARGBHalf);
            ExecuteML();

        }
        Texture PrepareTextureForInput(Texture src)
        {
            RenderTexture.active = targetRT;
            //normalization is applied in the NormalizeInput shader
            Graphics.Blit(src, targetRT, preprocessMaterial);



            var result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
            result.ReadPixels(new Rect(0, 0, targetRT.width, targetRT.height), 0, 0);
            Debug.Log(targetRT.width);
            Debug.Log(targetRT.height);
            result.Apply();

            Debug.Log("input complete");
            return result;
        }
        void ExecuteML()
        {
            
            Debug.Log("start ML");

            byte[] bytes = targetTexture.EncodeToJPG();
            File.WriteAllBytes(folderPath, bytes);

            var input = new Tensor(PrepareTextureForInput(targetTexture), 3);

            _engine_Origami = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtime_Origami);

            _engine_Origami.Execute(input);
            var output = _engine_Origami.PeekOutput();

            Debug.Log(output.ArgMax()[0]);
            var res = output.ArgMax()[0];

            var label = labels[res];
            //var label = "butterfly";
            var accuracy = output[res];
            Debug.Log("label : "+label);
            Debug.Log("accuarcy : " + accuracy);
            Debug.Log("percent"+Math.Round(accuracy * 100, 1));
            //clean memory
            if (label.Equals("butterfly"))
            {
                Debug.Log($"{label} {Math.Round(accuracy * 100, 1)}%");
                if (Math.Round(accuracy * 100, 1) >= 85)
                {
                    testSphere.SetActive(true);
                }
                
            }

            Resources.UnloadUnusedAssets();
            
            input.Dispose();
            _engine_Origami.Dispose();
        }

        //endline
    }

}