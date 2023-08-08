using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

public class RunInferenceMobileNet : MonoBehaviour
{
    //Object feature recognition
    public int inputResolutionY = 224;
    public int inputResolutionX = 224;
    public RawImage displayImage;
    public NNModel srcModel;
    public TextAsset labelsAsset;
    public Text resultClassText;
    public Material preprocessMaterial;
    public Dropdown backendDropdown;

    private Texture2D texture;
    private string inferenceBackend = "CSharpBurst";
    private Model model;
    private IWorker engine;
    private Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>();
    private string[] labels;
    private RenderTexture targetRT;


    //Object Weight Estimate
    [SerializeField]
    public NNModel Origami_Model;
    static public Model _runtimeModel;
    static public IWorker _engine;

    public int width, depth, height;


    //ray
    float distance; 
    RaycastHit rayHit; 
    Ray ray;
    public GameObject RayObj;
    public GameObject obj;
    public Text text;

    
 
    void Start()
    {

        _runtimeModel = ModelLoader.Load(Origami_Model);

        //_runtimeModel_Monitor = ModelLoader.Load(modelFile_Monitor);

        /*_runtimeModel_Table = ModelLoader.Load(modelFile_Table);
        _runtimeModel_RoundChair = ModelLoader.Load(modelFile_RoundChair);
        _runtimeModel_OfficeChair = ModelLoader.Load(modelFile_OfficeChair);*/
        width = 416;
        height = 416;
        depth = 425;

        ray = new Ray();
    }

    void Update()
    {
        ray.origin = RayObj.transform.position;
        ray.direction = RayObj.transform.forward;
        if (Input.GetKeyDown(KeyCode.R))
        {

            if (Physics.Raycast(ray.origin, ray.direction, out rayHit, 300))
            {
                GameObject Model = GameObject.Find("Model");
                GameObject Model_cld = GameObject.Find("Model_cld");
                if (Model != null)
                {
                    Model.gameObject.SetActive(false);
                }
                if (Model_cld != null)
                {
                    Model_cld.gameObject.SetActive(false);
                }
            }
        }

            if (Input.GetKeyDown(KeyCode.A))
        {
            
            if (Physics.Raycast(ray.origin, ray.direction, out rayHit, 300))
            {
                GameObject Model = GameObject.Find("Model");
                GameObject Model_cld = GameObject.Find("Model_cld");
                if (Model != null)
                {
                    Model.gameObject.SetActive(false);
                }
                if(Model_cld != null)
                {
                    Model_cld.gameObject.SetActive(false);
                }

                obj = rayHit.collider.gameObject;

                if (obj != null && obj.name != "Floor")
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Debug.Log(i);
                        obj = obj.transform.parent.gameObject;
                    }
                    Debug.Log(obj.name);
                }

            }
            
            StartCoroutine(SaveScreeJpg()); //Screenshot the current scene
            StartCoroutine(Recognize()); 
            Debug.Log(texture);

        }
    }


    IEnumerator Recognize()
    {
        yield return new WaitForEndOfFrame();
        Application.targetFrameRate = 60;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        AddBackendOptions();
        //parse neural net labels
        labels = labelsAsset.text.Split('\n');
        //load model
        model = ModelLoader.Load(srcModel);
        //format input texture variable
        targetRT = RenderTexture.GetTemporary(inputResolutionX, inputResolutionY, 0, RenderTextureFormat.ARGBHalf);
        //execute inference
        SelectBackendAndExecuteML();
    }
    IEnumerator SaveScreeJpg()
    {
        yield return new WaitForEndOfFrame();
        texture = new Texture2D(Screen.width, Screen.height);
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();
    }

    public void ExecuteML()
    {
        //Object feature recognition
        displayImage.texture = texture;

        if (inferenceBackend == "CSharpBurst")
        {
            engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);
        }
        else if (inferenceBackend == "ComputePrecompiled")
        {
            engine = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        } 
        else if (inferenceBackend == "PixelShader")
        {
            engine = WorkerFactory.CreateWorker(WorkerFactory.Type.PixelShader, model);
        }

        byte[] bytes = texture.EncodeToJPG();
        File.WriteAllBytes(@"C:\Users\GVR_HS\Desktop\XRFireDrills_2019\Assetsimage.jpg", bytes);


        var input = new Tensor(PrepareTextureForInput(texture), 3);
        engine.Execute(input);
        var output = engine.PeekOutput("butterfly");
        var res = output.ArgMax()[0];
        var label = labels[res];
        var accuracy = output[res];

        //Object Weight Estimate

    

        Tensor tensor = new(1, 416, 416, 3);

        if (label == "butterfly")
        {
            _engine = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel);
            Tensor output2 = _engine.Execute(tensor).PeekOutput("butterfly");
            _engine.Dispose();
            output2.Dispose();
            if (obj != null) obj.gameObject.tag = label;
        }
        
        /*if (label == "non-backrest chair")
        {
            _engine_RoundChair = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel_RoundChair);
            Tensor output2 = _engine_RoundChair.Execute(tensor).PeekOutput();
            resultClassText.text = $"{label} {Math.Round(output2[0]) / 1000}kg";
            _engine_RoundChair.Dispose();
            output2.Dispose();
            if (obj != null) obj.gameObject.tag = label;
        }
        else if(label == "backrest chair")
        {
            _engine_OfficeChair = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel_OfficeChair);
            Tensor output2 = _engine_OfficeChair.Execute(tensor).PeekOutput();
            resultClassText.text = $"{label} {Math.Round(output2[0]) / 500}kg";
            _engine_OfficeChair.Dispose();
            output2.Dispose();
            if (obj != null) obj.gameObject.tag = label;
        }
        else if(label == "Monitor")
        {
            _engine_Monitor = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel_Monitor);
            Tensor output2 = _engine_Monitor.Execute(tensor).PeekOutput();
            resultClassText.text = $"{label} {Math.Round(output2[0]) / 1000}kg";
            _engine_Monitor.Dispose();
            output2.Dispose();
            if (obj != null) obj.gameObject.tag = label;
        }
        else if(label == "Table")
        {
            _engine_Table = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, _runtimeModel_Table);
            Tensor output2 = _engine_Table.Execute(tensor).PeekOutput();
            resultClassText.text = $"{label} {Math.Round(output2[0]) / 1000}kg";
            _engine_Table.Dispose();
            output2.Dispose();
            if (obj != null) obj.gameObject.tag = label;
        }*/
        

        Debug.Log(resultClassText.text);

        //clean memory
        input.Dispose();
        engine.Dispose();
        Resources.UnloadUnusedAssets();

    }

   


    Texture PrepareTextureForInput(Texture2D src)
    {
        RenderTexture.active = targetRT;
        //normalization is applied in the NormalizeInput shader
        Graphics.Blit(src, targetRT, preprocessMaterial);

        var  result = new Texture2D(targetRT.width, targetRT.height, TextureFormat.RGBAHalf, false);
        result.ReadPixels(new Rect(0,0, targetRT.width, targetRT.height), 0, 0);
        result.Apply();
        return result;
    }

    public void AddBackendOptions()
    {
        List<string> options = new List<string> ();
        options.Add("CSharpBurst");
        #if !UNITY_WEBGL
        options.Add("ComputePrecompiled");
        #endif
        options.Add("PixelShader");
        backendDropdown.ClearOptions ();
        backendDropdown.AddOptions(options);
        
    }

    public void SelectBackendAndExecuteML()
    {
        
        if (backendDropdown.options[backendDropdown.value].text == "CSharpBurst")
        {
            inferenceBackend = "CSharpBurst";
        }
        else if (backendDropdown.options[backendDropdown.value].text == "ComputePrecompiled")
        {
            inferenceBackend = "ComputePrecompiled";
        }
        else if (backendDropdown.options[backendDropdown.value].text == "PixelShader")
        {
            inferenceBackend = "PixelShader";
        }
        //ExecuteML(selectedImage);
        ExecuteML();
    }

    private void OnDestroy()
    {
        engine?.Dispose();

        foreach (var key in inputs.Keys)
        {
            inputs[key].Dispose();
        }
		
        inputs.Clear();
    }
}
