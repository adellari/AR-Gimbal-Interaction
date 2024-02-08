using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Events; 
using UnityEngine.Scripting;

using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.CoordinateSystem;

using Stopwatch = System.Diagnostics.Stopwatch;



public class TrackMeshAR : MonoBehaviour
{
  public TextAsset configAsset;
  public ARCameraManager arCameraManager;
  
  [SerializeField] private int _width;
  [SerializeField] private int _height;
  [SerializeField] private int _fps;
  

  public AxisInteractionEvent onUpdateInteraction = new AxisInteractionEvent();

  bool printed = false;
  private WebCamTexture _webCamTexture;
  private Texture2D _inputTexture;
  private NativeArray<byte> _inputPixelsBuffer;
  //private NativeArrayUnsafe
  private Texture2D _outputTexture;
  private Color32[] _outputPixelData;

  public Slider scaleSliderXP;
  private float sliderValX = 1.623047f;

  public Slider scaleSliderYP;
  private float sliderValY = 0.9408965f;

  public Slider scaleSliderX;
  private float sliderValXP = -0.3276665f;

  public Slider scaleSliderY;
  private float sliderValYP = 0.02027702f;

  public bool click = false;

  private static UnityEngine.Rect _screenRect;
  [SerializeField] private RawImage _screen;
  private GameObject[] hand;
  [SerializeField] private GameObject _handPoint;

  private Stopwatch _stopwatch;
  private CalculatorGraph _graph;
  private ResourceManager _resourceManager;
  private GpuResources _gpuResources;

  private OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>> _handLandmarkStream;

  public void changeSliderValXP(float value)
  {
    sliderValXP = scaleSliderXP.value;
    Debug.Log("slider Xp changed: " + sliderValXP);
  }

  public void changeSliderValYP(float value)
  {
    sliderValYP = scaleSliderYP.value;
    Debug.Log("slider Yp changed: " + sliderValYP);
  }

  public void changeSliderValX(float value)
  {
    sliderValX = 1 + scaleSliderX.value;
    Debug.Log("slider X changed: " + sliderValX);
  }

  public void changeSliderValY(float value)
  {
    sliderValY = 1 + scaleSliderY.value;
    Debug.Log("slider Y changed: " + sliderValY);
  }

  private void alocBuffer(XRCpuImage image)
  {
    var length = image.width * image.height * 4;
    if (_inputPixelsBuffer == null || _inputPixelsBuffer.Length != length)
    {
      _inputPixelsBuffer = new NativeArray<byte>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
    }
  }

  private unsafe void onFrameReceived(ARCameraFrameEventArgs args)
  {
    if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
    {
      alocBuffer(image);
      if (!printed)
      {
        Debug.Log("screen canvas size: " + _screen.transform.parent.GetComponent<RectTransform>().sizeDelta.x + "x" + _screen.transform.parent.GetComponent<RectTransform>().sizeDelta.y);
        Debug.Log("dimensions of ar camera: " + image.width + "x" + image.height);
        printed = true;
      }
      var conversionParams = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32);
      var ptr = (IntPtr)NativeArrayUnsafeUtility.GetUnsafePtr(_inputPixelsBuffer);
      image.Convert(conversionParams, ptr, _inputPixelsBuffer.Length);
      image.Dispose();

      var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, image.width, image.height, 4 * image.width, _inputPixelsBuffer);
      var currentTimestamp = _stopwatch.ElapsedTicks / (TimeSpan.TicksPerMillisecond / 1000);
      var imageFramePacket = new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp));

      _graph.AddPacketToInputStream("input_video", imageFramePacket).AssertOk();
      StartCoroutine(WaitForEndOfFrameCoroutine());

      if (_handLandmarkStream.TryGetNext(out var handLandmarks))
      {
        foreach (var landmarks in handLandmarks)
        {

          for (var i = 0; i < landmarks.Landmark.Count; i++)
          {
            var landPos = landmarks.Landmark[i];
            landPos.X *= sliderValX;
            landPos.Y *= sliderValY;
            landPos.X += 1 * sliderValXP;
            landPos.Y += 1 * sliderValYP;
            var worldLandmarkPos = _screenRect.GetPoint(landPos);
            
            hand[i].transform.localPosition = worldLandmarkPos;
            //hand[i].transform.position *= 5f;
            //arlistX.Add(worldLandmarkPos.x);
            //arlistY.Add(worldLandmarkPos.y);
          }
          //generateObject(arlistX, arlistY);
        }
      
      float dist = Vector3.Distance(hand[4].transform.position, hand[8].transform.position);
      
      if (dist < 0.05f){

        Vector2 screenPoint = Camera.main.WorldToScreenPoint(hand[4].transform.position);
        if (!click){
          Debug.Log("entering click");
          Ray ray = Camera.main.ScreenPointToRay(screenPoint);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                var interaction = hit.transform.GetComponent<InteractionAxis>();
                if (interaction)
                {
                    onUpdateInteraction.AddListener(interaction.Invoke);
                    interaction.sPoint = screenPoint;
                    interaction.stateUpdate(true); 
                    //onUpdateInteraction.Invoke(Input.mousePosition); 
                }
                //Debug.Log(hit.transform.name);
                Debug.Log("hit");
            }

        }
        else{
          Debug.Log("in click");
          onUpdateInteraction.Invoke(screenPoint);
          //holding click
        }
      click = true;
      }

      else {
        if(click){
          //Debug.Log("click release");
          onUpdateInteraction.RemoveAllListeners();
          Debug.Log("removed all listeners");
        }
        click = false;
      }

      //Debug.Log("click distance: "  + dist);
      }
    }
  }

  private IEnumerator WaitForEndOfFrameCoroutine()
  {
    yield return new WaitForEndOfFrame();
  }

  private IEnumerator Start()
  {
    Debug.Log("Called start function");
    arCameraManager.frameReceived += onFrameReceived;
    
    _stopwatch = new Stopwatch();

    _screenRect = _screen.GetComponent<RectTransform>().rect;
    hand = new GameObject[21];
    for (var i = 0; i < 21; i++)
    {
      
      hand[i] = Instantiate(_handPoint, _screen.transform);
      hand[i].transform.localScale = new Vector3(40f, 40f, 40f);
      hand[i].active = true;
    }
    _gpuResources = GpuResources.Create().Value();
    _resourceManager = new StreamingAssetsResourceManager();
    Debug.Log("finished setting gpu resources");
    yield return _resourceManager.PrepareAssetAsync("hand_landmark_full.bytes");
    yield return _resourceManager.PrepareAssetAsync("hand_recrop.bytes");
    yield return _resourceManager.PrepareAssetAsync("handedness.txt");
    yield return _resourceManager.PrepareAssetAsync("palm_detection_full.bytes");

    _graph = new CalculatorGraph(configAsset.text);

    _graph.SetGpuResources(_gpuResources).AssertOk();
    _handLandmarkStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(_graph, "hand_landmarks");
    _handLandmarkStream.StartPolling().AssertOk();
    Debug.Log("set GPU resources");

    var sidePacket = new PacketMap();
    sidePacket.Emplace("model_complexity", new IntPacket(1));
    sidePacket.Emplace("num_hands", new IntPacket(1));
    sidePacket.Emplace("input_rotation", new IntPacket(270));
    sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(false));
    sidePacket.Emplace("input_vertically_flipped", new BoolPacket(false));

    _graph.StartRun(sidePacket).AssertOk();
    _stopwatch.Start();
    Debug.Log("finished start initialization");
  }

  private void OnDestroy()
  {

    if (_graph != null)
    {
      try
      {
        _graph.CloseAllPacketSources().AssertOk();
        _graph.WaitUntilDone().AssertOk();
      }
      finally
      {
        _graph.Dispose();
      }
    }

    if (_gpuResources != null)
    {
      _gpuResources.Dispose();
    }

    _inputPixelsBuffer.Dispose();

  }

}

