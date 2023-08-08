using System.Collections.Generic;
using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using System;
using captureEvent;

public class MainActive : MonoBehaviour
{
    [SerializeField]
    #region value
    public GameObject Click;

    public int inputResolutionY = 224;
    public int inputResolutionX = 224;
    public TextAsset labelsAsset;
    public Text resultClassText;
    public Material preprocessMaterial;
    public Dropdown backendDropdown;
    public int height, width;
    #endregion

    #region Texture
    private Texture2D texture;
    private string inferenceBackend = "CSharpBurst";
    private Model model;
    private IWorker engine;
    private Dictionary<string, Tensor> inputs = new Dictionary<string, Tensor>();
    private string[] labels;
    private RenderTexture targetRT;
    public RawImage displayImage;
    #endregion

    #region Model
    public NNModel Origami_Model;
    static public Model _runtime_Origami;
    static public IWorker _engine;
    #endregion


    // Start is called before the first frame update
    void Start()
    {
        _runtime_Origami = ModelLoader.Load(Origami_Model);
        width = 416;
        height = 416;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //endline
}
